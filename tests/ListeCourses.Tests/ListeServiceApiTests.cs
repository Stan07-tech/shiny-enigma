using ListeCourses.Models;
using ListeCourses.Services;
using Microsoft.AspNetCore.Http.Connections;

namespace ListeCourses.Tests;

// Tests du client utilisé par l'appli Android (ListeServiceApi), contre le vrai
// serveur démarré en mémoire : HTTP pour les données, SignalR pour le temps réel.
public sealed class ListeServiceApiTests(SiteDeTest site) : IClassFixture<SiteDeTest>, IAsyncDisposable
{
    private readonly List<ListeServiceApi> clients = [];

    // Un "téléphone" : un client HTTP + une connexion SignalR vers le serveur de test.
    private ListeServiceApi NouveauTelephone()
    {
        var http = site.CreateClient();
        var hub = ListeServiceApi.CreerConnexionHub(http.BaseAddress!, options =>
        {
            options.HttpMessageHandlerFactory = _ => site.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;   // le serveur de test ne gère pas les WebSockets
        });
        var client = new ListeServiceApi(http, hub);
        clients.Add(client);
        return client;
    }

    [Fact]
    public async Task CreerObtenirEtResumes()
    {
        var telephone = NouveauTelephone();

        var liste = await telephone.CreerAsync("Maison");

        Assert.Equal(liste, await telephone.ObtenirAsync(liste.Code));
        Assert.Null(await telephone.ObtenirAsync("AAAAAAAAAA"));
        Assert.Equal([liste], await telephone.ResumesAsync([liste.Code, "AAAAAAAAAA"]));
    }

    [Fact]
    public async Task CycleCompletDesArticles()
    {
        var telephone = NouveauTelephone();
        var code = (await telephone.CreerAsync("Maison")).Code;

        Assert.Null(await telephone.AjouterAsync(code, new Article { Nom = " " }));
        Assert.Null(await telephone.AjouterAsync("AAAAAAAAAA", new Article { Nom = "Pain" }));

        var pain = (await telephone.AjouterAsync(code, new Article { Nom = "Pain", Quantite = 2, Rayon = "Boulangerie" }))!;
        var lait = (await telephone.AjouterAsync(code, new Article { Nom = "Lait" }))!;
        Assert.True((await telephone.BasculerAsync(code, pain.Id))!.Achete);
        Assert.Null(await telephone.BasculerAsync(code, 999_999));

        var articles = (await telephone.ArticlesAsync(code))!;
        Assert.Equal(["Lait", "Pain"], articles.Select(a => a.Nom));   // acheté en dernier
        Assert.Equal(2, articles[1].Quantite);

        await telephone.SupprimerAchetesAsync(code);
        Assert.True(await telephone.SupprimerAsync(code, lait.Id));
        Assert.False(await telephone.SupprimerAsync(code, lait.Id));
        Assert.Empty((await telephone.ArticlesAsync(code))!);
        Assert.Null(await telephone.ArticlesAsync("AAAAAAAAAA"));
    }

    [Fact]
    public async Task TempsReel_UnTelephoneEstPrevenuQuandUnAutreModifieLaListe()
    {
        var moi = NouveauTelephone();
        var conjoint = NouveauTelephone();
        var code = (await moi.CreerAsync("Maison")).Code;

        var prevenu = new TaskCompletionSource();
        await using var suivi = await moi.SuivreAsync(code, () =>
        {
            prevenu.TrySetResult();
            return Task.CompletedTask;
        });

        await conjoint.AjouterAsync(code, new Article { Nom = "Pain" });

        // On attend la notification, 10 secondes au plus.
        await prevenu.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Pain", Assert.Single((await moi.ArticlesAsync(code))!).Nom);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in clients) await client.DisposeAsync();
    }
}
