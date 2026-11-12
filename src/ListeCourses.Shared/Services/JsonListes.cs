using System.Text.Json;
using System.Text.Json.Serialization;
using ListeCourses.Models;

namespace ListeCourses.Services;

// Sérialisation JSON générée à la compilation (source generator) plutôt que par
// réflexion : indispensable dans l'APK Android, où le code inutilisé est retiré
// et où la réflexion peut donc échouer.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Article))]
[JsonSerializable(typeof(List<Article>))]
[JsonSerializable(typeof(ListeResume))]
[JsonSerializable(typeof(List<ListeResume>))]
[JsonSerializable(typeof(NouvelleListe))]
[JsonSerializable(typeof(ResultatAjout))]
[JsonSerializable(typeof(RecetteResume))]
[JsonSerializable(typeof(List<RecetteResume>))]
[JsonSerializable(typeof(RecetteDetail))]
[JsonSerializable(typeof(Ingredient))]
[JsonSerializable(typeof(ModificationRecette))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string))]
public partial class JsonListes : JsonSerializerContext;

// Corps de la requête de création d'une liste ou d'une recette.
public record NouvelleListe(string Nom);
