using System.Text.Json.Serialization;

namespace ListeCourses.Models;

// Une recette partageable : comme une liste, elle est identifiée par un code secret.
public class Recette
{
    public int Id { get; set; }
    public string Code { get; set; } = "";           // Index unique
    public string Nom { get; set; } = "";
    public string? Instructions { get; set; }        // NVARCHAR(MAX) NULL
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Relation 1-N : une recette contient plusieurs ingrédients.
    public List<Ingredient> Ingredients { get; set; } = [];
}

public class Ingredient
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public decimal? Quantite { get; set; }           // 250 (avec l'unité g), 4 (sans unité)
    public int? UniteId { get; set; }                // FOREIGN KEY vers Unites
    public string? Rayon { get; set; }

    [JsonIgnore]
    public int RecetteId { get; set; }

    // L'article correspondant dans une liste de courses (même nom, quantité, unité et rayon).
    public Article VersArticle() =>
        new() { Nom = Nom, Quantite = Quantite ?? 1, UniteId = UniteId, Rayon = Rayon };
}

// Ce que les clients voient d'une recette (DTO, comme ListeResume).
public record RecetteResume(string Code, string Nom, int NbIngredients);

public record RecetteDetail(string Code, string Nom, string? Instructions, List<Ingredient> Ingredients);

// Corps de la requête de modification du nom et des instructions.
public record ModificationRecette(string Nom, string? Instructions);

// Résultat de l'ajout de plusieurs articles d'un coup dans une liste.
public record ResultatAjout(int Ajoutes, int DejaPresents);
