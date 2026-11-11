using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ListeCourses.Services;

// Partager le lien d'une liste. Chaque plateforme a sa façon de faire :
// navigateur (menu de partage ou presse-papiers) ou Android (menu natif).
public interface IPartage
{
    // Le lien complet à envoyer, ex. https://monsite/l/K7MZ2QH9XA
    string Lien(string code);

    // Renvoie true si le lien a été copié dans le presse-papiers
    // (pour afficher "Lien copié"), false si un menu de partage s'est ouvert.
    Task<bool> PartagerAsync(string titre, string lien);
}

// Version navigateur (site web).
public sealed class PartageNavigateur(NavigationManager navigation, IJSRuntime js) : IPartage
{
    public string Lien(string code) => navigation.ToAbsoluteUri($"l/{code}").ToString();

    public async Task<bool> PartagerAsync(string titre, string lien)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/ListeCourses.Shared/partage.js");
        return await module.InvokeAsync<bool>("partager", titre, lien);
    }
}
