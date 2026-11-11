namespace ListeCourses.Models;

// Une liste de courses partageable. Le Code (secret) sert à la fois
// d'identifiant public et de "clé d'accès" : qui connaît le code accède à la liste.
public class Liste
{
    public int Id { get; set; }
    public string Code { get; set; } = "";           // Index unique
    public string Nom { get; set; } = "";
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Relation 1-N : une liste contient plusieurs articles.
    public List<Article> Articles { get; set; } = [];
}
