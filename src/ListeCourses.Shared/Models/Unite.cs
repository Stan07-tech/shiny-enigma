using System.Globalization;

namespace ListeCourses.Models;

// Table de référence des unités de mesure (g, Kg, cL...), partagée par
// les articles des listes et les ingrédients des recettes.
public class Unite
{
    public int Id { get; set; }
    public string Symbole { get; set; } = "";        // "g", "CaS"
    public string Libelle { get; set; } = "";        // "Gramme(s)", "Cuillère à soupe"
    public int Ordre { get; set; }                   // ordre dans le menu déroulant

    // Unités fournies à la création de la base (voir CoursesDbContext, HasData).
    // Pour en ajouter : INSERT INTO Unites (Id, Symbole, Libelle, Ordre) VALUES (...)
    public static readonly Unite[] ParDefaut =
    [
        new() { Id = 1, Symbole = "Pce", Libelle = "pièce(s)", Ordre = 1 },
        new() { Id = 2, Symbole = "g", Libelle = "Gramme(s)", Ordre = 2 },
        new() { Id = 3, Symbole = "Kg", Libelle = "Kilogramme(s)", Ordre = 3 },
        new() { Id = 4, Symbole = "L", Libelle = "Litres", Ordre = 4 },
        new() { Id = 5, Symbole = "cL", Libelle = "Centilitres", Ordre = 5 },
        new() { Id = 6, Symbole = "CaS", Libelle = "Cuillère à soupe", Ordre = 6 },
        new() { Id = 7, Symbole = "CaC", Libelle = "Cuillère à café", Ordre = 7 },
        new() { Id = 8, Symbole = "pn", Libelle = "Pincée", Ordre = 8 },
    ];
}

// Mise en forme des quantités, commune à toutes les pages.
public static class Quantites
{
    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

    // 2 -> "2", 0.5 -> "0,5", 1.25 -> "1,25"
    public static string Nombre(decimal quantite) => quantite.ToString("0.##", Francais);

    // "250 g", "2 Pce", "4" (sans unité), "" (ni quantité ni unité)
    public static string Texte(decimal? quantite, Unite? unite) => (quantite, unite) switch
    {
        (null, null) => "",
        (null, _) => unite.Symbole,
        (_, null) => Nombre(quantite.Value),
        _ => $"{Nombre(quantite.Value)} {unite.Symbole}",
    };
}
