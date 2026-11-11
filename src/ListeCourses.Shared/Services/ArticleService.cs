using ListeCourses.Data;
using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Services;

// Toute la logique métier au même endroit, partagée par l'API REST et
// l'interface Blazor (comme une collection de procédures globales en WinDev).
// On passe par une "fabrique" de DbContext : chaque méthode ouvre un contexte
// court, ce qui est la pratique recommandée avec Blazor Server.
public class ArticleService(IDbContextFactory<CoursesDbContext> dbFactory)
{
    // SELECT * FROM Articles ORDER BY Achete, Rayon, Nom
    public async Task<List<Article>> ListerAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Articles
            .OrderBy(a => a.Achete)
            .ThenBy(a => a.Rayon)
            .ThenBy(a => a.Nom)
            .ToListAsync();
    }

    // INSERT INTO Articles ... (renvoie null si le nom est vide)
    public async Task<Article?> AjouterAsync(Article article)
    {
        if (string.IsNullOrWhiteSpace(article.Nom))
            return null;

        var nouvel = new Article
        {
            Nom = article.Nom.Trim(),
            Quantite = Math.Max(1, article.Quantite),
            Rayon = string.IsNullOrWhiteSpace(article.Rayon) ? null : article.Rayon.Trim(),
        };

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Articles.Add(nouvel);
        await db.SaveChangesAsync();
        return nouvel;
    }

    // UPDATE Articles SET Achete = ~Achete WHERE Id = @id
    public async Task<Article?> BasculerAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var article = await db.Articles.FindAsync(id);
        if (article is null) return null;

        article.Achete = !article.Achete;
        await db.SaveChangesAsync();
        return article;
    }

    // DELETE FROM Articles WHERE Id = @id
    public async Task<bool> SupprimerAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Articles.Where(a => a.Id == id).ExecuteDeleteAsync() > 0;
    }

    // DELETE FROM Articles WHERE Achete = 1
    public async Task SupprimerAchetesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Articles.Where(a => a.Achete).ExecuteDeleteAsync();
    }
}
