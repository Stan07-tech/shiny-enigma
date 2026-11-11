using ListeCourses.Models;

namespace ListeCourses.Services;

// Le "contrat" de la logique métier. Les pages Blazor ne connaissent que cette
// interface ; deux classes la réalisent :
//  - ListeServiceEf  (site web)     : accès direct à la base via Entity Framework ;
//  - ListeServiceApi (appli mobile) : appels HTTP à l'API du serveur + SignalR.
// Toutes les opérations passent par le code de la liste : sans le code, pas d'accès.
public interface IListeService
{
    // Crée une liste et renvoie son code de partage.
    Task<ListeResume> CreerAsync(string nom);

    // null si aucune liste ne porte ce code.
    Task<ListeResume?> ObtenirAsync(string code);

    // Résumés des listes connues ; les codes inconnus sont ignorés.
    Task<List<ListeResume>> ResumesAsync(IEnumerable<string> codes);

    // null si la liste n'existe pas.
    Task<List<Article>?> ArticlesAsync(string code);

    // null si le nom est vide ou si la liste n'existe pas.
    Task<Article?> AjouterAsync(string code, Article article);

    // null / false si l'article n'existe pas dans CETTE liste.
    Task<Article?> BasculerAsync(string code, int id);
    Task<bool> SupprimerAsync(string code, int id);

    Task SupprimerAchetesAsync(string code);

    // Appelle "surModification" chaque fois que quelqu'un modifie la liste.
    // L'abonnement s'arrête quand on "dispose" l'objet renvoyé.
    Task<IAsyncDisposable> SuivreAsync(string code, Func<Task> surModification);
}
