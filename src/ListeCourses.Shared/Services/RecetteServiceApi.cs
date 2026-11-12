using System.Net;
using System.Net.Http.Json;
using ListeCourses.Models;
using static ListeCourses.Services.OutilsHttp;

namespace ListeCourses.Services;

// Implémentation "client" des recettes, utilisée par l'appli mobile.
public sealed class RecetteServiceApi(HttpClient http) : IRecetteService
{
    public async Task<RecetteResume> CreerAsync(string nom)
    {
        var reponse = await http.PostAsJsonAsync("api/recettes", new NouvelleListe(nom), JsonListes.Default.NouvelleListe);
        reponse.EnsureSuccessStatusCode();
        return (await reponse.Content.ReadFromJsonAsync(JsonListes.Default.RecetteResume))!;
    }

    public async Task<RecetteDetail?> ObtenirAsync(string code) =>
        await LireOuNullAsync(await AvecReessaisAsync(() => http.GetAsync($"api/recettes/{code}")), JsonListes.Default.RecetteDetail);

    public async Task<List<RecetteResume>> ResumesAsync(IEnumerable<string> codes)
    {
        var liste = codes.ToList();
        var reponse = await AvecReessaisAsync(() => http.PostAsJsonAsync("api/recettes/resumes", liste, JsonListes.Default.ListString));
        reponse.EnsureSuccessStatusCode();
        return (await reponse.Content.ReadFromJsonAsync(JsonListes.Default.ListRecetteResume))!;
    }

    public async Task<bool> ModifierAsync(string code, ModificationRecette modification)
    {
        var reponse = await http.PutAsJsonAsync($"api/recettes/{code}", modification, JsonListes.Default.ModificationRecette);
        if (reponse.StatusCode == HttpStatusCode.NotFound) return false;
        reponse.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<Ingredient?> AjouterIngredientAsync(string code, Ingredient ingredient)
    {
        if (string.IsNullOrWhiteSpace(ingredient.Nom)) return null;
        return await LireOuNullAsync(
            await http.PostAsJsonAsync($"api/recettes/{code}/ingredients", ingredient, JsonListes.Default.Ingredient),
            JsonListes.Default.Ingredient);
    }

    public async Task<bool> SupprimerIngredientAsync(string code, int id)
    {
        var reponse = await http.DeleteAsync($"api/recettes/{code}/ingredients/{id}");
        if (reponse.StatusCode == HttpStatusCode.NotFound) return false;
        reponse.EnsureSuccessStatusCode();
        return true;
    }
}
