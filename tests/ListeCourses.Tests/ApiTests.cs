using System.Net;
using System.Net.Http.Json;
using ListeCourses.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Tests;

// Tests d'intégration : WebApplicationFactory démarre le vrai site web en
// mémoire (sans ouvrir de port) et fournit un HttpClient pour l'appeler.
// La base est redirigée vers un fichier temporaire pour ne pas toucher
// à courses.db.
public sealed class SiteDeTest : WebApplicationFactory<Program>
{
    private readonly string cheminBase = Path.Combine(Path.GetTempPath(), $"courses-tests-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Courses", $"Data Source={cheminBase}");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();   // libère le fichier avant de le supprimer
        File.Delete(cheminBase);
    }
}

// IClassFixture : un seul site démarré pour tous les tests de cette classe.
// Les tests partagent donc la base : chacun crée sa propre liste.
public sealed class ApiTests(SiteDeTest site) : IClassFixture<SiteDeTest>
{
    private readonly HttpClient client = site.CreateClient();

    [Fact]
    public async Task PostListe_Renvoie201AvecUnCode_AccessibleEnMinuscules()
    {
        var reponse = await client.PostAsJsonAsync("/api/listes", new { nom = "Maison" });

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var liste = (await reponse.Content.ReadFromJsonAsync<ListeResume>())!;
        Assert.True(CodeListe.EstValide(liste.Code));

        var lue = await client.GetFromJsonAsync<ListeResume>($"/api/listes/{liste.Code.ToLowerInvariant()}");
        Assert.Equal(liste, lue);
    }

    [Theory]
    [InlineData("/api/listes/AAAAAAAAAA")]
    [InlineData("/api/listes/AAAAAAAAAA/articles")]
    public async Task ListeInconnue_Renvoie404(string url)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task PostArticle_Renvoie201EtLArticleEstListe()
    {
        var code = await CreerListe();

        var reponse = await client.PostAsJsonAsync($"/api/listes/{code}/articles", new { nom = "Café", rayon = "Épicerie" });

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var cree = (await reponse.Content.ReadFromJsonAsync<Article>())!;
        Assert.Equal($"/api/listes/{code}/articles/{cree.Id}", reponse.Headers.Location?.OriginalString);
        var articles = await client.GetFromJsonAsync<List<Article>>($"/api/listes/{code}/articles");
        Assert.Contains(articles!, a => a.Id == cree.Id && a.Nom == "Café");
    }

    [Fact]
    public async Task PostArticle_NomVide_Renvoie400()
    {
        var code = await CreerListe();

        var reponse = await client.PostAsJsonAsync($"/api/listes/{code}/articles", new { nom = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task PostArticle_ListeInconnue_Renvoie404()
    {
        var reponse = await client.PostAsJsonAsync("/api/listes/AAAAAAAAAA/articles", new { nom = "Pain" });

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task ArticleDUneAutreListe_Renvoie404()
    {
        var maison = await CreerListe();
        var bbq = await CreerListe();
        var pain = await (await client.PostAsJsonAsync($"/api/listes/{maison}/articles", new { nom = "Pain" }))
            .Content.ReadFromJsonAsync<Article>();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsync($"/api/listes/{bbq}/articles/{pain!.Id}/basculer", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/listes/{bbq}/articles/{pain.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/l/AAAAAAAAAA")]
    [InlineData("/l/AAAAAAAAAA/rayon/Frais")]
    [InlineData("/l/AAAAAAAAAA/sans-rayon")]
    [InlineData("/recettes")]
    [InlineData("/r/AAAAAAAAAA")]
    [InlineData("/classique.html")]
    public async Task Pages_SAffichent(string url)
    {
        var reponse = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains("<html lang=\"fr\">", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Recettes_CycleCompletParLApi()
    {
        var creation = await client.PostAsJsonAsync("/api/recettes", new { nom = "Crêpes" });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var code = (await creation.Content.ReadFromJsonAsync<RecetteResume>())!.Code;

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync($"/api/recettes/{code}/ingredients", new { nom = " " })).StatusCode);
        var farine = await (await client.PostAsJsonAsync($"/api/recettes/{code}/ingredients",
            new { nom = "Farine", quantite = "250 g", rayon = "Épicerie" })).Content.ReadFromJsonAsync<Ingredient>();
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"/api/recettes/{code.ToLowerInvariant()}", new { nom = "Crêpes", instructions = "Mélanger." })).StatusCode);

        var recette = await client.GetFromJsonAsync<RecetteDetail>($"/api/recettes/{code}");
        Assert.Equal("Mélanger.", recette!.Instructions);
        Assert.Equal("250 g", Assert.Single(recette.Ingredients).Quantite);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/recettes/{code}/ingredients/{farine!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/recettes/{code}/ingredients/{farine.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/recettes/AAAAAAAAAA")).StatusCode);
    }

    [Fact]
    public async Task Lot_AjouteDesArticlesAUneListe()
    {
        var code = await CreerListe();

        var reponse = await client.PostAsJsonAsync($"/api/listes/{code}/articles/lot",
            new[] { new { nom = "Farine (250 g)", rayon = "Épicerie" }, new { nom = "Lait", rayon = "Frais" } });

        Assert.Equal(new ResultatAjout(2, 0), await reponse.Content.ReadFromJsonAsync<ResultatAjout>());
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync("/api/listes/AAAAAAAAAA/articles/lot", new[] { new { nom = "Pain" } })).StatusCode);
    }

    private async Task<string> CreerListe() =>
        (await (await client.PostAsJsonAsync("/api/listes", new { nom = "Test" }))
            .Content.ReadFromJsonAsync<ListeResume>())!.Code;
}

// Reproduit la base Azure actuelle : créée AVANT les recettes, elle ne contient
// que les tables Listes et Articles (avec des données). Au démarrage, le site
// doit y ajouter les tables des recettes sans toucher au reste.
public sealed class SiteSurAncienneBase : WebApplicationFactory<Program>
{
    public readonly string CheminBase = Path.Combine(Path.GetTempPath(), $"courses-ancienne-{Guid.NewGuid()}.db");
    public string CodeListeExistante = "";

    public SiteSurAncienneBase()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ListeCourses.Data.CoursesDbContext>()
            .UseSqlite($"Data Source={CheminBase}").Options;
        using var db = new ListeCourses.Data.CoursesDbContext(options);
        db.Database.EnsureCreated();
        var liste = new Liste { Code = "ANCIENNE22", Nom = "Ancienne", Articles = [new Article { Nom = "Pain" }] };
        db.Listes.Add(liste);
        db.SaveChanges();
        CodeListeExistante = liste.Code;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Courses", $"Data Source={CheminBase}");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(CheminBase);
    }
}

public sealed class MiseANiveauTests(SiteSurAncienneBase site) : IClassFixture<SiteSurAncienneBase>
{
    [Fact]
    public async Task AncienneBase_RecoitLesTablesDesRecettes_SansPerdreLesListes()
    {
        var client = site.CreateClient();

        var creation = await client.PostAsJsonAsync("/api/recettes", new { nom = "Crêpes" });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

        var articles = await client.GetFromJsonAsync<List<Article>>($"/api/listes/{site.CodeListeExistante}/articles");
        Assert.Equal("Pain", Assert.Single(articles!).Nom);
    }
}
