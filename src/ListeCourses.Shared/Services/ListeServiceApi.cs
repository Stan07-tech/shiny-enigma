using System.Net;
using System.Net.Http.Json;
using ListeCourses.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace ListeCourses.Services;

// Implémentation "client" utilisée par l'appli mobile : chaque méthode appelle
// l'API REST du serveur, et les notifications arrivent par SignalR.
public sealed class ListeServiceApi(HttpClient http, HubConnection hub) : IListeService, IAsyncDisposable
{
    private const int NombreEssais = 3;

    private readonly SemaphoreSlim verrouConnexion = new(1, 1);
    private readonly List<(string Code, Func<Task> Rappel)> abonnements = [];
    private bool gestionnairesInstalles;

    // Prépare la connexion SignalR vers le hub du serveur.
    // "configurer" permet aux tests de remplacer le transport réseau.
    public static HubConnection CreerConnexionHub(Uri adresseServeur, Action<HttpConnectionOptions>? configurer = null) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(adresseServeur, "hub/listes"), options => configurer?.Invoke(options))
            .WithAutomaticReconnect()
            .AddJsonProtocol(o => o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, JsonListes.Default))
            .Build();

    public async Task<ListeResume> CreerAsync(string nom)
    {
        var reponse = await http.PostAsJsonAsync("api/listes", new NouvelleListe(nom), JsonListes.Default.NouvelleListe);
        reponse.EnsureSuccessStatusCode();
        return (await reponse.Content.ReadFromJsonAsync(JsonListes.Default.ListeResume))!;
    }

    public async Task<ListeResume?> ObtenirAsync(string code) =>
        await LireOuNullAsync(await AvecReessaisAsync(() => http.GetAsync($"api/listes/{code}")), JsonListes.Default.ListeResume);

    public async Task<List<ListeResume>> ResumesAsync(IEnumerable<string> codes)
    {
        var liste = codes.ToList();
        var reponse = await AvecReessaisAsync(() => http.PostAsJsonAsync("api/listes/resumes", liste, JsonListes.Default.ListString));
        reponse.EnsureSuccessStatusCode();
        return (await reponse.Content.ReadFromJsonAsync(JsonListes.Default.ListListeResume))!;
    }

    public async Task<List<Article>?> ArticlesAsync(string code) =>
        await LireOuNullAsync(await AvecReessaisAsync(() => http.GetAsync($"api/listes/{code}/articles")), JsonListes.Default.ListArticle);

    public async Task<Article?> AjouterAsync(string code, Article article)
    {
        if (string.IsNullOrWhiteSpace(article.Nom)) return null;
        return await LireOuNullAsync(
            await http.PostAsJsonAsync($"api/listes/{code}/articles", article, JsonListes.Default.Article),
            JsonListes.Default.Article);
    }

    public async Task<Article?> BasculerAsync(string code, int id) =>
        await LireOuNullAsync(await http.PatchAsync($"api/listes/{code}/articles/{id}/basculer", null), JsonListes.Default.Article);

    public async Task<bool> SupprimerAsync(string code, int id)
    {
        var reponse = await http.DeleteAsync($"api/listes/{code}/articles/{id}");
        if (reponse.StatusCode == HttpStatusCode.NotFound) return false;
        reponse.EnsureSuccessStatusCode();
        return true;
    }

    public async Task SupprimerAchetesAsync(string code) =>
        (await http.DeleteAsync($"api/listes/{code}/articles/achetes")).EnsureSuccessStatusCode();

    public async Task<IAsyncDisposable> SuivreAsync(string code, Func<Task> surModification)
    {
        var abonnement = (code, surModification);
        lock (abonnements) abonnements.Add(abonnement);

        await verrouConnexion.WaitAsync();
        try
        {
            InstallerGestionnaires();
            if (hub.State == HubConnectionState.Disconnected)
                await hub.StartAsync();
            await hub.InvokeAsync("Suivre", code);
        }
        catch (Exception)
        {
            // Pas de réseau : la liste reste utilisable, seulement sans mise à jour automatique.
        }
        finally
        {
            verrouConnexion.Release();
        }

        return new Desabonnement(async () =>
        {
            bool dernier;
            lock (abonnements)
            {
                abonnements.Remove(abonnement);
                dernier = !abonnements.Any(a => a.Code == code);
            }
            if (dernier && hub.State == HubConnectionState.Connected)
            {
                try { await hub.InvokeAsync("NePlusSuivre", code); } catch (Exception) { }
            }
        });
    }

    private void InstallerGestionnaires()
    {
        if (gestionnairesInstalles) return;
        gestionnairesInstalles = true;

        hub.On<string>("ListeModifiee", code => PrevenirAsync(a => a.Code == code));

        // Après une coupure réseau, le serveur nous a oubliés : on se réabonne,
        // puis on recharge tout, au cas où des modifications auraient été manquées.
        hub.Reconnected += async _ =>
        {
            foreach (var code in Codes()) await hub.InvokeAsync("Suivre", code);
            await PrevenirAsync(_ => true);
        };
    }

    private string[] Codes()
    {
        lock (abonnements) return abonnements.Select(a => a.Code).Distinct().ToArray();
    }

    private async Task PrevenirAsync(Func<(string Code, Func<Task> Rappel), bool> filtre)
    {
        (string Code, Func<Task> Rappel)[] cibles;
        lock (abonnements) cibles = abonnements.Where(filtre).ToArray();
        foreach (var (_, rappel) in cibles)
        {
            try { await rappel(); } catch (Exception) { }
        }
    }

    // Lecture avec nouvelles tentatives. Au réveil du serveur (offre gratuite d'Azure :
    // site en veille, base en pause), les premières requêtes peuvent échouer ou expirer.
    // On ne réessaie que les lectures : réessayer un ajout pourrait le faire en double.
    private static async Task<HttpResponseMessage> AvecReessaisAsync(Func<Task<HttpResponseMessage>> requete)
    {
        for (var essai = 1; ; essai++)
        {
            try
            {
                var reponse = await requete();
                if ((int)reponse.StatusCode < 500 || essai == NombreEssais) return reponse;
            }
            catch (Exception) when (essai < NombreEssais)
            {
                // Erreur réseau ou délai dépassé : on attend un peu, puis on recommence.
            }
            await Task.Delay(TimeSpan.FromSeconds(2 * essai));
        }
    }

    // Renvoie null sur 404, lève une exception sur les autres erreurs.
    private static async Task<T?> LireOuNullAsync<T>(HttpResponseMessage reponse,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type) where T : class
    {
        if (reponse.StatusCode == HttpStatusCode.NotFound) return null;
        reponse.EnsureSuccessStatusCode();
        return await reponse.Content.ReadFromJsonAsync(type);
    }

    public async ValueTask DisposeAsync() => await hub.DisposeAsync();
}

// Petit objet dont le DisposeAsync exécute une action : sert à "se désabonner".
public sealed class Desabonnement(Func<Task> action) : IAsyncDisposable
{
    private Func<Task>? action = action;

    public async ValueTask DisposeAsync()
    {
        var a = Interlocked.Exchange(ref action, null);
        if (a is not null) await a();
    }
}
