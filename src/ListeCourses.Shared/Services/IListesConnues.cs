using System.Text.Json;
using Microsoft.JSInterop;

namespace ListeCourses.Services;

// Les listes "connues" de cet appareil (créées ou ouvertes ici).
// Sans compte utilisateur, c'est l'appareil qui s'en souvient.
public interface IListesConnues
{
    Task<List<string>> CodesAsync();
    Task AjouterAsync(string code);
    Task RetirerAsync(string code);
}

// Même chose pour les recettes.
public interface IRecettesConnues
{
    Task<List<string>> CodesAsync();
    Task AjouterAsync(string code);
    Task RetirerAsync(string code);
}

// Stockage de codes dans le localStorage du navigateur. Fonctionne aussi dans
// l'appli mobile, car la page Blazor y est affichée par un navigateur intégré (WebView).
// Classe "abstract" : elle ne sert que de base aux deux classes ci-dessous,
// qui ne diffèrent que par la clé de stockage.
public abstract class CodesConnusNavigateur(IJSRuntime js, string cle)
{
    public async Task<List<string>> CodesAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", cle);
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize(json, JsonListes.Default.ListString) ?? []; }
        catch (JsonException) { return []; }
    }

    public async Task AjouterAsync(string code)
    {
        var codes = await CodesAsync();
        if (codes.Contains(code)) return;
        codes.Insert(0, code);   // le plus récent en premier
        await EnregistrerAsync(codes);
    }

    public async Task RetirerAsync(string code)
    {
        var codes = await CodesAsync();
        if (codes.Remove(code)) await EnregistrerAsync(codes);
    }

    private Task EnregistrerAsync(List<string> codes) =>
        js.InvokeVoidAsync("localStorage.setItem", cle, JsonSerializer.Serialize(codes, JsonListes.Default.ListString)).AsTask();
}

public sealed class ListesConnuesNavigateur(IJSRuntime js) : CodesConnusNavigateur(js, "listes-connues"), IListesConnues;

public sealed class RecettesConnuesNavigateur(IJSRuntime js) : CodesConnusNavigateur(js, "recettes-connues"), IRecettesConnues;
