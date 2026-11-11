using System.Net;
using System.Net.Http.Json;
using ListeCourses.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

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
// Les tests partagent donc la base : chacun ne vérifie que ce qu'il a créé.
public sealed class ApiTests(SiteDeTest site) : IClassFixture<SiteDeTest>
{
    private readonly HttpClient client = site.CreateClient();

    [Fact]
    public async Task Post_ArticleValide_Renvoie201EtLArticleEstListe()
    {
        var reponse = await client.PostAsJsonAsync("/api/articles", new { nom = "Café", rayon = "Épicerie" });

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var cree = await reponse.Content.ReadFromJsonAsync<Article>();
        Assert.Equal($"/api/articles/{cree!.Id}", reponse.Headers.Location?.OriginalString);

        var articles = await client.GetFromJsonAsync<List<Article>>("/api/articles");
        Assert.Contains(articles!, a => a.Id == cree.Id && a.Nom == "Café");
    }

    [Fact]
    public async Task Post_NomVide_Renvoie400()
    {
        var reponse = await client.PostAsJsonAsync("/api/articles", new { nom = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Patch_Basculer_MarqueLArticleCommeAchete()
    {
        var cree = await (await client.PostAsJsonAsync("/api/articles", new { nom = "Thé" }))
            .Content.ReadFromJsonAsync<Article>();

        var reponse = await client.PatchAsync($"/api/articles/{cree!.Id}/basculer", null);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.True((await reponse.Content.ReadFromJsonAsync<Article>())!.Achete);
    }

    [Theory]
    [InlineData("PATCH", "/api/articles/999999/basculer")]
    [InlineData("DELETE", "/api/articles/999999")]
    public async Task IdInconnu_Renvoie404(string methode, string url)
    {
        var reponse = await client.SendAsync(new HttpRequestMessage(new HttpMethod(methode), url));

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/rayon/Frais")]
    [InlineData("/sans-rayon")]
    public async Task PagesBlazor_SAffichent(string url)
    {
        var reponse = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains("🛒", await reponse.Content.ReadAsStringAsync());
    }
}
