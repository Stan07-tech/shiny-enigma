using ListeCourses.Models;
using Microsoft.AspNetCore.SignalR;

namespace ListeCourses.Services;

// Hub SignalR : les applis mobiles s'y connectent et "suivent" une ou plusieurs
// listes. Chaque liste correspond à un groupe SignalR.
public sealed class HubListes : Hub
{
    public static string Groupe(string code) => $"liste:{code}";

    public Task Suivre(string code) =>
        CodeListe.EstValide(code)
            ? Groups.AddToGroupAsync(Context.ConnectionId, Groupe(code))
            : Task.CompletedTask;

    public Task NePlusSuivre(string code) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, Groupe(code));
}
