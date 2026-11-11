using System.Collections.Concurrent;

namespace ListeCourses.Services;

// "Boîte aux lettres" interne au serveur : chaque modification d'une liste y est
// signalée, et tous les abonnés sont prévenus :
//  - les pages Blazor ouvertes sur le site (abonnées à un code précis) ;
//  - le hub SignalR (abonné à tout), qui relaie aux applis mobiles.
public sealed class NotificateurListes
{
    private readonly ConcurrentDictionary<object, (string? Code, Func<string, Task> Rappel)> abonnes = new();

    // code null = toutes les listes.
    public IAsyncDisposable Abonner(string? code, Func<string, Task> rappel)
    {
        var cle = new object();
        abonnes[cle] = (code, rappel);
        return new Desabonnement(() =>
        {
            abonnes.TryRemove(cle, out _);
            return Task.CompletedTask;
        });
    }

    public void Signaler(string code)
    {
        foreach (var (c, rappel) in abonnes.Values)
        {
            // On n'attend pas les abonnés : la requête qui a modifié la liste
            // n'a pas à patienter pendant que les autres écrans se rechargent.
            if (c is null || c == code) _ = ExecuterAsync(rappel, code);
        }
    }

    private static async Task ExecuterAsync(Func<string, Task> rappel, string code)
    {
        try
        {
            await rappel(code);
        }
        catch (Exception)
        {
            // Un abonné en erreur (page fermée...) ne doit pas gêner les autres.
        }
    }
}
