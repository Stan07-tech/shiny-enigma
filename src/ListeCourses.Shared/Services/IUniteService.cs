using System.Net.Http.Json;
using ListeCourses.Models;
using static ListeCourses.Services.OutilsHttp;

namespace ListeCourses.Services;

// Les unités de mesure (table de référence), pour les menus déroulants.
public interface IUniteService
{
    Task<List<Unite>> ListerAsync();
}

// Version appli mobile : lue une fois sur le serveur, puis gardée en mémoire
// (la table change rarement).
public sealed class UniteServiceApi(HttpClient http) : IUniteService
{
    private List<Unite>? cache;

    public async Task<List<Unite>> ListerAsync()
    {
        if (cache is not null) return cache;
        var reponse = await AvecReessaisAsync(() => http.GetAsync("api/unites"));
        reponse.EnsureSuccessStatusCode();
        return cache = (await reponse.Content.ReadFromJsonAsync(JsonListes.Default.ListUnite))!;
    }
}
