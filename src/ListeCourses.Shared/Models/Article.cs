using System.Text.Json.Serialization;

namespace ListeCourses.Models;

// Une "entité" : l'équivalent d'un fichier de données (analyse WinDev)
// ou d'une table SQL Server. Chaque propriété devient une colonne.
public class Article
{
    public int Id { get; set; }                      // Clé primaire (IDENTITY)
    public string Nom { get; set; } = "";            // NVARCHAR NOT NULL
    public decimal Quantite { get; set; } = 1;          // DECIMAL(9,2) : 0,5 Kg, 1,5 L
    public int? UniteId { get; set; }                   // FOREIGN KEY vers Unites (NULL = sans unité)
    public string? Rayon { get; set; }               // Le "?" = colonne NULLable
    public bool Achete { get; set; }                 // BIT
    public DateTime DateAjout { get; set; } = DateTime.UtcNow;

    // Clé étrangère vers la liste (FOREIGN KEY ... REFERENCES Listes(Id)).
    // [JsonIgnore] : inutile de l'envoyer aux clients, ils passent par le code de la liste.
    [JsonIgnore]
    public int ListeId { get; set; }
}
