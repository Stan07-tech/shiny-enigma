using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace ListeCourses.Services;

// Outils communs aux clients HTTP de l'appli mobile (listes et recettes).
internal static class OutilsHttp
{
    private const int NombreEssais = 3;

    // Lecture avec nouvelles tentatives. Au réveil du serveur (offre gratuite d'Azure :
    // site en veille, base en pause), les premières requêtes peuvent échouer ou expirer.
    // On ne réessaie que les lectures : réessayer un ajout pourrait le faire en double.
    public static async Task<HttpResponseMessage> AvecReessaisAsync(Func<Task<HttpResponseMessage>> requete)
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
    public static async Task<T?> LireOuNullAsync<T>(HttpResponseMessage reponse, JsonTypeInfo<T> type) where T : class
    {
        if (reponse.StatusCode == HttpStatusCode.NotFound) return null;
        reponse.EnsureSuccessStatusCode();
        return await reponse.Content.ReadFromJsonAsync(type);
    }
}
