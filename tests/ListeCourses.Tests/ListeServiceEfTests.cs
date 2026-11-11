using System.Globalization;
using ListeCourses.Models;
using ListeCourses.Services;

namespace ListeCourses.Tests;

// xUnit crée une nouvelle instance de la classe pour CHAQUE test :
// le constructeur sert de préparation et Dispose de nettoyage.
// Chaque test a donc sa propre base, vide.
public sealed class ListeServiceEfTests : IDisposable
{
    private readonly BaseEnMemoire baseDeTest = new();
    private readonly NotificateurListes notificateur = new();
    private readonly ListeServiceEf service;

    public ListeServiceEfTests() => service = new ListeServiceEf(baseDeTest, notificateur);

    public void Dispose() => baseDeTest.Dispose();

    // ----- Listes -----

    [Fact]
    public async Task CreerAsync_GenereUnCodeValideEtUnique()
    {
        var a = await service.CreerAsync("Maison");
        var b = await service.CreerAsync("Maison");

        Assert.True(CodeListe.EstValide(a.Code));
        Assert.NotEqual(a.Code, b.Code);
        Assert.Equal(new ListeResume(a.Code, "Maison", 0), a);
    }

    [Theory]
    [InlineData("", "Liste de courses")]
    [InlineData("   ", "Liste de courses")]
    [InlineData("  Barbecue  ", "Barbecue")]
    public async Task CreerAsync_NettoieLeNom(string saisi, string attendu)
    {
        var liste = await service.CreerAsync(saisi);

        Assert.Equal(attendu, liste.Nom);
    }

    [Fact]
    public async Task ObtenirAsync_CodeInconnu_RenvoieNull()
    {
        Assert.Null(await service.ObtenirAsync("AAAAAAAAAA"));
    }

    [Fact]
    public async Task ResumesAsync_CompteLesArticlesRestantsEtIgnoreLesCodesInconnus()
    {
        var maison = await service.CreerAsync("Maison");
        var bbq = await service.CreerAsync("Barbecue");
        await Ajouter(maison.Code, "Pain", null);
        var lait = await Ajouter(maison.Code, "Lait", null);
        await service.BasculerAsync(maison.Code, lait.Id);

        var resumes = await service.ResumesAsync([maison.Code.ToLowerInvariant(), bbq.Code, "AAAAAAAAAA", "n'importe quoi"]);

        Assert.Equal(2, resumes.Count);
        Assert.Contains(new ListeResume(maison.Code, "Maison", 1), resumes);   // Lait acheté : 1 restant
        Assert.Contains(new ListeResume(bbq.Code, "Barbecue", 0), resumes);
    }

    // ----- Articles -----

    [Fact]
    public async Task ArticlesAsync_ListeInconnue_RenvoieNull()
    {
        Assert.Null(await service.ArticlesAsync("AAAAAAAAAA"));
    }

    // [Theory] + [InlineData] : le même test exécuté avec plusieurs valeurs.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AjouterAsync_NomVide_RenvoieNullEtNAjouteRien(string nom)
    {
        var liste = await service.CreerAsync("Maison");

        Assert.Null(await service.AjouterAsync(liste.Code, new Article { Nom = nom }));
        Assert.Empty((await service.ArticlesAsync(liste.Code))!);
    }

    [Fact]
    public async Task AjouterAsync_ListeInconnue_RenvoieNull()
    {
        Assert.Null(await service.AjouterAsync("AAAAAAAAAA", new Article { Nom = "Pain" }));
    }

    [Fact]
    public async Task AjouterAsync_NettoieLaSaisie()
    {
        var liste = await service.CreerAsync("Maison");

        var article = await service.AjouterAsync(liste.Code,
            new Article { Id = 42, Nom = "  Pain  ", Quantite = 0, Rayon = " Boulangerie ", Achete = true });

        Assert.NotNull(article);
        Assert.Equal("Pain", article.Nom);
        Assert.Equal(1, article.Quantite);          // une quantité < 1 devient 1
        Assert.Equal("Boulangerie", article.Rayon);
        Assert.NotEqual(42, article.Id);            // Id et Achete fournis par l'appelant ignorés
        Assert.False(article.Achete);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AjouterAsync_RayonVide_DevientNull(string? rayon)
    {
        var liste = await service.CreerAsync("Maison");

        var article = await service.AjouterAsync(liste.Code, new Article { Nom = "Piles", Rayon = rayon });

        Assert.Null(article!.Rayon);
    }

    [Fact]
    public async Task LesListesSontIsolees()
    {
        var maison = await service.CreerAsync("Maison");
        var bbq = await service.CreerAsync("Barbecue");

        await Ajouter(maison.Code, "Pain", null);
        await Ajouter(bbq.Code, "Merguez", null);

        Assert.Equal("Pain", Assert.Single((await service.ArticlesAsync(maison.Code))!).Nom);
        Assert.Equal("Merguez", Assert.Single((await service.ArticlesAsync(bbq.Code))!).Nom);
    }

    [Fact]
    public async Task ArticlesAsync_TrieParAchatPuisRayonPuisNom_SansTenirCompteDesMajusculesNiDesAccents()
    {
        // Le tri dépend de la culture : on fixe le français pour un résultat stable.
        var cultureAvant = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            var code = (await service.CreerAsync("Maison")).Code;
            await Ajouter(code, "Pommes", "Fruits");
            await Ajouter(code, "Yaourts", "frais");
            await Ajouter(code, "Riz", "Épicerie");
            await Ajouter(code, "Lait", "Frais");
            await Ajouter(code, "Pain", "Boulangerie");
            await Ajouter(code, "Piles", null);
            var beurre = await Ajouter(code, "Beurre", "Frais");
            await service.BasculerAsync(code, beurre.Id);   // acheté : doit passer en fin de liste

            var noms = (await service.ArticlesAsync(code))!.Select(a => a.Nom);

            // Sans rayon d'abord, puis les rayons dans l'ordre alphabétique
            // ("Épicerie" avec les E, "frais" avec "Frais"), les articles achetés à la fin.
            Assert.Equal(["Piles", "Pain", "Riz", "Lait", "Yaourts", "Pommes", "Beurre"], noms);
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureAvant;
        }
    }

    [Fact]
    public async Task BasculerAsync_InverseLEtatAcheteAChaqueAppel()
    {
        var code = (await service.CreerAsync("Maison")).Code;
        var article = await Ajouter(code, "Lait", "Frais");

        Assert.True((await service.BasculerAsync(code, article.Id))!.Achete);
        Assert.False((await service.BasculerAsync(code, article.Id))!.Achete);
    }

    // Sécurité : connaître l'Id d'un article ne suffit pas, il faut le code de SA liste.
    [Fact]
    public async Task BasculerEtSupprimer_ArticleDUneAutreListe_SontRefuses()
    {
        var maison = await service.CreerAsync("Maison");
        var bbq = await service.CreerAsync("Barbecue");
        var pain = await Ajouter(maison.Code, "Pain", null);

        Assert.Null(await service.BasculerAsync(bbq.Code, pain.Id));
        Assert.False(await service.SupprimerAsync(bbq.Code, pain.Id));

        var intact = Assert.Single((await service.ArticlesAsync(maison.Code))!);
        Assert.False(intact.Achete);
    }

    [Fact]
    public async Task SupprimerAsync_ArticleExistant_LeSupprimeEtRenvoieVrai()
    {
        var code = (await service.CreerAsync("Maison")).Code;
        var pain = await Ajouter(code, "Pain", "Boulangerie");
        await Ajouter(code, "Lait", "Frais");

        Assert.True(await service.SupprimerAsync(code, pain.Id));
        Assert.Equal("Lait", Assert.Single((await service.ArticlesAsync(code))!).Nom);
    }

    [Fact]
    public async Task SupprimerAsync_IdInconnu_RenvoieFaux()
    {
        var code = (await service.CreerAsync("Maison")).Code;

        Assert.False(await service.SupprimerAsync(code, 999));
    }

    [Fact]
    public async Task SupprimerAchetesAsync_NeSupprimeQueLesAchetesDeCetteListe()
    {
        var maison = await service.CreerAsync("Maison");
        var bbq = await service.CreerAsync("Barbecue");
        var pain = await Ajouter(maison.Code, "Pain", null);
        await Ajouter(maison.Code, "Riz", null);
        var merguez = await Ajouter(bbq.Code, "Merguez", null);
        await service.BasculerAsync(maison.Code, pain.Id);
        await service.BasculerAsync(bbq.Code, merguez.Id);

        await service.SupprimerAchetesAsync(maison.Code);

        Assert.Equal("Riz", Assert.Single((await service.ArticlesAsync(maison.Code))!).Nom);
        Assert.Single((await service.ArticlesAsync(bbq.Code))!);   // l'autre liste n'est pas touchée
    }

    // ----- Temps réel -----

    [Fact]
    public async Task SuivreAsync_PrevientSeulementPourLaListeSuivieEtJusquAuDesabonnement()
    {
        var maison = await service.CreerAsync("Maison");
        var bbq = await service.CreerAsync("Barbecue");
        var notifications = 0;

        var suivi = await service.SuivreAsync(maison.Code, () =>
        {
            Interlocked.Increment(ref notifications);
            return Task.CompletedTask;
        });

        await Ajouter(maison.Code, "Pain", null);     // +1
        await Ajouter(bbq.Code, "Merguez", null);     // autre liste : rien
        await suivi.DisposeAsync();
        await Ajouter(maison.Code, "Lait", null);     // désabonné : rien

        Assert.Equal(1, notifications);
    }

    private async Task<Article> Ajouter(string code, string nom, string? rayon) =>
        (await service.AjouterAsync(code, new Article { Nom = nom, Rayon = rayon }))!;
}
