using System.Globalization;
using ListeCourses.Models;
using ListeCourses.Services;

namespace ListeCourses.Tests;

// xUnit crée une nouvelle instance de la classe pour CHAQUE test :
// le constructeur sert de préparation et Dispose de nettoyage.
// Chaque test a donc sa propre base, vide.
public sealed class ArticleServiceTests : IDisposable
{
    private readonly BaseEnMemoire baseDeTest = new();
    private readonly ArticleService service;

    public ArticleServiceTests() => service = new ArticleService(baseDeTest);

    public void Dispose() => baseDeTest.Dispose();

    // ----- AjouterAsync -----

    // [Theory] + [InlineData] : le même test exécuté avec plusieurs valeurs.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AjouterAsync_NomVide_RenvoieNullEtNAjouteRien(string nom)
    {
        var resultat = await service.AjouterAsync(new Article { Nom = nom });

        Assert.Null(resultat);
        Assert.Empty(await service.ListerAsync());
    }

    [Fact]
    public async Task AjouterAsync_NettoieLaSaisie()
    {
        var article = await service.AjouterAsync(new Article { Nom = "  Pain  ", Quantite = 0, Rayon = " Boulangerie " });

        Assert.NotNull(article);
        Assert.Equal("Pain", article.Nom);
        Assert.Equal(1, article.Quantite);          // une quantité < 1 devient 1
        Assert.Equal("Boulangerie", article.Rayon);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AjouterAsync_RayonVide_DevientNull(string? rayon)
    {
        var article = await service.AjouterAsync(new Article { Nom = "Piles", Rayon = rayon });

        Assert.NotNull(article);
        Assert.Null(article.Rayon);
    }

    [Fact]
    public async Task AjouterAsync_IgnoreIdEtAcheteFournisParLAppelant()
    {
        var article = await service.AjouterAsync(new Article { Id = 42, Nom = "Lait", Achete = true });

        Assert.NotNull(article);
        Assert.NotEqual(42, article.Id);
        Assert.False(article.Achete);
    }

    [Fact]
    public async Task AjouterAsync_EnregistreLArticleEnBase()
    {
        var article = await service.AjouterAsync(new Article { Nom = "Lait", Quantite = 2, Rayon = "Frais" });

        var enBase = Assert.Single(await service.ListerAsync());
        Assert.Equal(article!.Id, enBase.Id);
        Assert.Equal("Lait", enBase.Nom);
        Assert.Equal(2, enBase.Quantite);
    }

    // ----- ListerAsync -----

    [Fact]
    public async Task ListerAsync_TrieParAchatPuisRayonPuisNom_SansTenirCompteDesMajusculesNiDesAccents()
    {
        // Le tri dépend de la culture : on fixe le français pour un résultat stable.
        var cultureAvant = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            await Ajouter("Pommes", "Fruits");
            await Ajouter("Yaourts", "frais");
            await Ajouter("Riz", "Épicerie");
            await Ajouter("Lait", "Frais");
            await Ajouter("Pain", "Boulangerie");
            await Ajouter("Piles", null);
            var beurre = await Ajouter("Beurre", "Frais");
            await service.BasculerAsync(beurre.Id);   // acheté : doit passer en fin de liste

            var noms = (await service.ListerAsync()).Select(a => a.Nom);

            // Sans rayon d'abord, puis les rayons dans l'ordre alphabétique
            // ("Épicerie" avec les E, "frais" avec "Frais"), les articles achetés à la fin.
            Assert.Equal(["Piles", "Pain", "Riz", "Lait", "Yaourts", "Pommes", "Beurre"], noms);
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureAvant;
        }
    }

    // ----- BasculerAsync -----

    [Fact]
    public async Task BasculerAsync_InverseLEtatAcheteAChaqueAppel()
    {
        var article = await Ajouter("Lait", "Frais");

        Assert.True((await service.BasculerAsync(article.Id))!.Achete);
        Assert.False((await service.BasculerAsync(article.Id))!.Achete);
    }

    [Fact]
    public async Task BasculerAsync_IdInconnu_RenvoieNull()
    {
        Assert.Null(await service.BasculerAsync(999));
    }

    // ----- SupprimerAsync -----

    [Fact]
    public async Task SupprimerAsync_ArticleExistant_LeSupprimeEtRenvoieVrai()
    {
        var pain = await Ajouter("Pain", "Boulangerie");
        await Ajouter("Lait", "Frais");

        Assert.True(await service.SupprimerAsync(pain.Id));

        var restant = Assert.Single(await service.ListerAsync());
        Assert.Equal("Lait", restant.Nom);
    }

    [Fact]
    public async Task SupprimerAsync_IdInconnu_RenvoieFaux()
    {
        Assert.False(await service.SupprimerAsync(999));
    }

    // ----- SupprimerAchetesAsync -----

    [Fact]
    public async Task SupprimerAchetesAsync_NeSupprimeQueLesArticlesAchetes()
    {
        var pain = await Ajouter("Pain", "Boulangerie");
        var lait = await Ajouter("Lait", "Frais");
        await Ajouter("Riz", "Épicerie");
        await service.BasculerAsync(pain.Id);
        await service.BasculerAsync(lait.Id);

        await service.SupprimerAchetesAsync();

        var restant = Assert.Single(await service.ListerAsync());
        Assert.Equal("Riz", restant.Nom);
    }

    private async Task<Article> Ajouter(string nom, string? rayon) =>
        (await service.AjouterAsync(new Article { Nom = nom, Rayon = rayon }))!;
}
