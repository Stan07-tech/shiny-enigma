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
    [InlineData("/classique.html")]
    public async Task Pages_SAffichent(string url)
    {
        var reponse = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains("<html lang=\"fr\">", await reponse.Content.ReadAsStringAsync());
    }

    private async Task<string> CreerListe() =>
        (await (await client.PostAsJsonAsync("/api/listes", new { nom = "Test" }))
            .Content.ReadFromJsonAsync<ListeResume>())!.Code;
}
