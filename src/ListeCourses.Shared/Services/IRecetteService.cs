using ListeCourses.Models;

namespace ListeCourses.Services;

// Le "contrat" des recettes, sur le même principe que IListeService :
//  - RecetteServiceEf  (site web)     : accès direct à la base ;
//  - RecetteServiceApi (appli mobile) : appels HTTP à l'API du serveur.
public interface IRecetteService
{
    Task<RecetteResume> CreerAsync(string nom);

    // null si aucune recette ne porte ce code.
    Task<RecetteDetail?> ObtenirAsync(string code);

    // Résumés des recettes connues ; les codes inconnus sont ignorés.
    Task<List<RecetteResume>> ResumesAsync(IEnumerable<string> codes);

    // false si la recette n'existe pas.
    Task<bool> ModifierAsync(string code, ModificationRecette modification);

    // null si le nom est vide ou si la recette n'existe pas.
    Task<Ingredient?> AjouterIngredientAsync(string code, Ingredient ingredient);

    // false si l'ingrédient n'existe pas dans CETTE recette.
    Task<bool> SupprimerIngredientAsync(string code, int id);
}
