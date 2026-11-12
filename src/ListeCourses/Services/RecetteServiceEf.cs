using System.Security.Cryptography;
using ListeCourses.Data;
using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Services;

// Implémentation "serveur" des recettes : accès direct à la base avec EF Core.
public class RecetteServiceEf(IDbContextFactory<RecettesDbContext> dbFactory) : IRecetteService
{
    private const int MaxResumes = 100;

    // INSERT INTO Recettes (Code, Nom) VALUES (...)
    public async Task<RecetteResume> CreerAsync(string nom)
    {
        var recette = new Recette
        {
            Code = RandomNumberGenerator.GetString(CodeListe.Alphabet, CodeListe.Longueur),
            Nom = Nettoyer(nom, 100) ?? "Nouvelle recette",
        };

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Recettes.Add(recette);
        await db.SaveChangesAsync();
        return new RecetteResume(recette.Code, recette.Nom, 0);
    }

    // SELECT ... FROM Recettes r LEFT JOIN Ingredients i ON ... WHERE r.Code = @code
    public async Task<RecetteDetail?> ObtenirAsync(string code)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var recette = await db.Recettes
            .Include(r => r.Ingredients)
            .SingleOrDefaultAsync(r => r.Code == code);
        if (recette is null) return null;

        // Ingrédients dans l'ordre de saisie (Id croissant).
        return new RecetteDetail(recette.Code, recette.Nom, recette.Instructions,
            recette.Ingredients.OrderBy(i => i.Id).ToList());
    }

    public async Task<List<RecetteResume>> ResumesAsync(IEnumerable<string> codes)
    {
        var valides = codes.Select(c => c.ToUpperInvariant()).Where(CodeListe.EstValide)
            .Distinct().Take(MaxResumes).ToList();
        if (valides.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Recettes
            .Where(r => valides.Contains(r.Code))
            .Select(r => new RecetteResume(r.Code, r.Nom, r.Ingredients.Count))
            .ToListAsync();
    }

    // UPDATE Recettes SET Nom = @nom, Instructions = @instructions WHERE Code = @code
    public async Task<bool> ModifierAsync(string code, ModificationRecette modification)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var recette = await db.Recettes.SingleOrDefaultAsync(r => r.Code == code);
        if (recette is null) return false;

        recette.Nom = Nettoyer(modification.Nom, 100) ?? recette.Nom;
        recette.Instructions = string.IsNullOrWhiteSpace(modification.Instructions) ? null : modification.Instructions.Trim();
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Ingredient?> AjouterIngredientAsync(string code, Ingredient ingredient)
    {
        var nom = Nettoyer(ingredient.Nom, 100);
        if (nom is null) return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var recetteId = await db.Recettes.Where(r => r.Code == code).Select(r => (int?)r.Id).SingleOrDefaultAsync();
        if (recetteId is null) return null;

        var nouvel = new Ingredient
        {
            RecetteId = recetteId.Value,
            Nom = nom,
            Quantite = Nettoyer(ingredient.Quantite, 30),
            Rayon = Nettoyer(ingredient.Rayon, 50),
        };
        db.Ingredients.Add(nouvel);
        await db.SaveChangesAsync();
        return nouvel;
    }

    // DELETE FROM Ingredients WHERE Id = @id AND RecetteId = (recette du code)
    public async Task<bool> SupprimerIngredientAsync(string code, int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Ingredients
            .Where(i => i.Id == id && db.Recettes.Any(r => r.Id == i.RecetteId && r.Code == code))
            .ExecuteDeleteAsync() > 0;
    }

    // Texte nettoyé (espaces retirés, longueur limitée), ou null s'il est vide.
    private static string? Nettoyer(string? texte, int longueur)
    {
        if (string.IsNullOrWhiteSpace(texte)) return null;
        var t = texte.Trim();
        return t.Length <= longueur ? t : t[..longueur];
    }
}
