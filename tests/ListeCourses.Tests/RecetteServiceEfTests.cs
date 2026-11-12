using ListeCourses.Models;
using ListeCourses.Services;

namespace ListeCourses.Tests;

public sealed class RecetteServiceEfTests : IDisposable
{
    private readonly BaseRecettesEnMemoire baseDeTest = new();
    private readonly RecetteServiceEf service;

    public RecetteServiceEfTests() => service = new RecetteServiceEf(baseDeTest);

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public void CreerTablesSiAbsentes_PeutEtreAppeleePlusieursFois()
    {
        using var db = baseDeTest.CreateDbContext();

        db.CreerTablesSiAbsentes();   // déjà créées par le constructeur : ne doit rien faire

        Assert.Empty(db.Recettes);
    }

    [Theory]
    [InlineData("  Crêpes  ", "Crêpes")]
    [InlineData("", "Nouvelle recette")]
    public async Task CreerAsync_GenereUnCodeEtNettoieLeNom(string saisi, string attendu)
    {
        var recette = await service.CreerAsync(saisi);

        Assert.True(CodeListe.EstValide(recette.Code));
        Assert.Equal(new RecetteResume(recette.Code, attendu, 0), recette);
    }

    [Fact]
    public async Task ObtenirAsync_CodeInconnu_RenvoieNull()
    {
        Assert.Null(await service.ObtenirAsync("AAAAAAAAAA"));
    }

    [Fact]
    public async Task Ingredients_AjoutNettoyageOrdreEtSuppression()
    {
        var code = (await service.CreerAsync("Crêpes")).Code;

        Assert.Null(await service.AjouterIngredientAsync(code, new Ingredient { Nom = "  " }));
        Assert.Null(await service.AjouterIngredientAsync("AAAAAAAAAA", new Ingredient { Nom = "Farine" }));

        var farine = (await service.AjouterIngredientAsync(code, new Ingredient { Nom = " Farine ", Quantite = " 250 g ", Rayon = " Épicerie " }))!;
        await service.AjouterIngredientAsync(code, new Ingredient { Nom = "Œufs", Quantite = "4", Rayon = "Frais" });
        await service.AjouterIngredientAsync(code, new Ingredient { Nom = "Sel", Quantite = "  " });

        var recette = (await service.ObtenirAsync(code))!;
        Assert.Equal(["Farine", "Œufs", "Sel"], recette.Ingredients.Select(i => i.Nom));   // ordre de saisie
        Assert.Equal("250 g", recette.Ingredients[0].Quantite);
        Assert.Equal("Épicerie", recette.Ingredients[0].Rayon);
        Assert.Null(recette.Ingredients[2].Quantite);

        Assert.True(await service.SupprimerIngredientAsync(code, farine.Id));
        Assert.False(await service.SupprimerIngredientAsync(code, farine.Id));
        Assert.Equal(2, (await service.ObtenirAsync(code))!.Ingredients.Count);
    }

    [Fact]
    public async Task SupprimerIngredient_DUneAutreRecette_EstRefuse()
    {
        var crepes = (await service.CreerAsync("Crêpes")).Code;
        var gateau = (await service.CreerAsync("Gâteau")).Code;
        var farine = (await service.AjouterIngredientAsync(crepes, new Ingredient { Nom = "Farine" }))!;

        Assert.False(await service.SupprimerIngredientAsync(gateau, farine.Id));
        Assert.Single((await service.ObtenirAsync(crepes))!.Ingredients);
    }

    [Fact]
    public async Task ModifierAsync_ChangeLeNomEtLesInstructions()
    {
        var code = (await service.CreerAsync("Crepes")).Code;

        Assert.True(await service.ModifierAsync(code, new ModificationRecette(" Crêpes ", "  Mélanger.  ")));
        var recette = (await service.ObtenirAsync(code))!;
        Assert.Equal("Crêpes", recette.Nom);
        Assert.Equal("Mélanger.", recette.Instructions);

        // Nom vide : on garde l'ancien. Instructions vides : effacées.
        Assert.True(await service.ModifierAsync(code, new ModificationRecette("", " ")));
        recette = (await service.ObtenirAsync(code))!;
        Assert.Equal("Crêpes", recette.Nom);
        Assert.Null(recette.Instructions);

        Assert.False(await service.ModifierAsync("AAAAAAAAAA", new ModificationRecette("X", null)));
    }

    [Fact]
    public async Task ResumesAsync_CompteLesIngredientsEtIgnoreLesCodesInconnus()
    {
        var crepes = (await service.CreerAsync("Crêpes")).Code;
        await service.AjouterIngredientAsync(crepes, new Ingredient { Nom = "Farine" });
        await service.AjouterIngredientAsync(crepes, new Ingredient { Nom = "Lait" });

        var resumes = await service.ResumesAsync([crepes.ToLowerInvariant(), "AAAAAAAAAA", "n'importe quoi"]);

        Assert.Equal([new RecetteResume(crepes, "Crêpes", 2)], resumes);
    }

    [Theory]
    [InlineData("Farine", "250 g", "Farine (250 g)")]
    [InlineData("Sel", null, "Sel")]
    [InlineData("Sel", " ", "Sel")]
    public void NomPourListe_AjouteLaQuantiteEntreParentheses(string nom, string? quantite, string attendu)
    {
        Assert.Equal(attendu, new Ingredient { Nom = nom, Quantite = quantite }.NomPourListe());
    }
}
