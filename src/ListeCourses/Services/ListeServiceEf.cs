using System.Security.Cryptography;
using ListeCourses.Data;
using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Services;

// Implémentation "serveur" de IListeService : accès direct à la base avec
// Entity Framework Core (comme une collection de procédures globales en WinDev).
// On passe par une "fabrique" de DbContext : chaque méthode ouvre un contexte
// court, ce qui est la pratique recommandée avec Blazor Server.
public class ListeServiceEf(IDbContextFactory<CoursesDbContext> dbFactory, NotificateurListes notificateur) : IListeService
{
    private const int MaxResumes = 100;

    // INSERT INTO Listes (Code, Nom) VALUES (...)
    public async Task<ListeResume> CreerAsync(string nom)
    {
        var liste = new Liste
        {
            Code = RandomNumberGenerator.GetString(CodeListe.Alphabet, CodeListe.Longueur),
            Nom = string.IsNullOrWhiteSpace(nom) ? "Liste de courses" : Tronquer(nom.Trim(), 100),
        };

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Listes.Add(liste);
        await db.SaveChangesAsync();
        return new ListeResume(liste.Code, liste.Nom, 0);
    }

    public async Task<ListeResume?> ObtenirAsync(string code) =>
        (await ResumesAsync([code])).SingleOrDefault();

    // SELECT Code, Nom, (SELECT COUNT(*) FROM Articles WHERE ... AND Achete = 0)
    // FROM Listes WHERE Code IN (...)
    public async Task<List<ListeResume>> ResumesAsync(IEnumerable<string> codes)
    {
        var valides = codes.Select(c => c.ToUpperInvariant()).Where(CodeListe.EstValide)
            .Distinct().Take(MaxResumes).ToList();
        if (valides.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Listes
            .Where(l => valides.Contains(l.Code))
            .Select(l => new ListeResume(l.Code, l.Nom, l.Articles.Count(a => !a.Achete)))
            .ToListAsync();
    }

    // SELECT a.* FROM Articles a JOIN Listes l ON ... WHERE l.Code = @code
    public async Task<List<Article>?> ArticlesAsync(string code)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var liste = await db.Listes
            .Include(l => l.Articles)
            .SingleOrDefaultAsync(l => l.Code == code);
        if (liste is null) return null;

        // Le tri se fait en C# et non en SQL : SQLite compare les textes octet par
        // octet ("frais" après "Fruits", "Épicerie" en dernier). En SQL Server, une
        // collation French_CI_AS donnerait directement le bon ordre.
        var comparaison = StringComparer.CurrentCultureIgnoreCase;
        return liste.Articles
            .OrderBy(a => a.Achete)
            .ThenBy(a => a.Rayon, comparaison)
            .ThenBy(a => a.Nom, comparaison)
            .ToList();
    }

    // INSERT INTO Articles ... (renvoie null si le nom est vide ou la liste inconnue)
    public async Task<Article?> AjouterAsync(string code, Article article)
    {
        if (string.IsNullOrWhiteSpace(article.Nom))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var listeId = await IdListeAsync(db, code);
        if (listeId is null) return null;

        var nouvel = new Article
        {
            ListeId = listeId.Value,
            Nom = Tronquer(article.Nom.Trim(), 100),
            Quantite = Math.Max(1, article.Quantite),
            Rayon = string.IsNullOrWhiteSpace(article.Rayon) ? null : Tronquer(article.Rayon.Trim(), 50),
        };
        db.Articles.Add(nouvel);
        await db.SaveChangesAsync();
        notificateur.Signaler(code);
        return nouvel;
    }

    // UPDATE Articles SET Achete = ~Achete WHERE Id = @id AND ListeId = (liste du code)
    public async Task<Article?> BasculerAsync(string code, int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var article = await DansLaListe(db, code).SingleOrDefaultAsync(a => a.Id == id);
        if (article is null) return null;

        article.Achete = !article.Achete;
        await db.SaveChangesAsync();
        notificateur.Signaler(code);
        return article;
    }

    // DELETE FROM Articles WHERE Id = @id AND ListeId = (liste du code)
    public async Task<bool> SupprimerAsync(string code, int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var supprime = await DansLaListe(db, code).Where(a => a.Id == id).ExecuteDeleteAsync() > 0;
        if (supprime) notificateur.Signaler(code);
        return supprime;
    }

    // DELETE FROM Articles WHERE Achete = 1 AND ListeId = (liste du code)
    public async Task SupprimerAchetesAsync(string code)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await DansLaListe(db, code).Where(a => a.Achete).ExecuteDeleteAsync() > 0)
            notificateur.Signaler(code);
    }

    public Task<IAsyncDisposable> SuivreAsync(string code, Func<Task> surModification) =>
        Task.FromResult(notificateur.Abonner(code, _ => surModification()));

    // Les articles d'une liste : TOUTES les opérations passent par là, ce qui
    // empêche de modifier l'article d'une autre liste en devinant son Id.
    private static IQueryable<Article> DansLaListe(CoursesDbContext db, string code) =>
        db.Articles.Where(a => db.Listes.Any(l => l.Id == a.ListeId && l.Code == code));

    private static Task<int?> IdListeAsync(CoursesDbContext db, string code) =>
        db.Listes.Where(l => l.Code == code).Select(l => (int?)l.Id).SingleOrDefaultAsync();

    private static string Tronquer(string texte, int longueur) =>
        texte.Length <= longueur ? texte : texte[..longueur];
}
