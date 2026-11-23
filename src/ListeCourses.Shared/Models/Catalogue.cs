namespace ListeCourses.Models;

// Produits courants et leur rayon, pour la saisie automatique : en tapant "Bana",
// on se voit proposer "Bananes", et le rayon "Fruits et légumes" est rempli tout seul.
// Les noms déjà utilisés dans la liste ou la recette s'ajoutent à ces suggestions.
public static class Catalogue
{
    public static readonly IReadOnlyDictionary<string, string> Produits = Creer(new()
    {
        ["Fruits et légumes"] =
        [
            "Abricots", "Ail", "Avocats", "Bananes", "Carottes", "Céleri", "Cerises", "Champignons",
            "Citrons", "Clémentines", "Concombre", "Courgettes", "Échalotes", "Épinards", "Fraises",
            "Haricots verts", "Kiwis", "Laitue", "Melon", "Oignons", "Oranges", "Pamplemousse",
            "Persil", "Poireaux", "Poires", "Poivrons", "Pommes", "Pommes de terre", "Raisin",
            "Radis", "Salade", "Tomates",
        ],
        ["Boulangerie"] = ["Baguette", "Brioche", "Croissants", "Pain", "Pain de mie", "Pains au chocolat"],
        ["Frais"] =
        [
            "Beurre", "Comté", "Crème fraîche", "Emmental râpé", "Fromage", "Fromage blanc", "Jambon",
            "Lait", "Lardons", "Mozzarella", "Œufs", "Parmesan", "Pâte brisée", "Pâte feuilletée",
            "Yaourts",
        ],
        ["Boucherie"] = ["Bœuf haché", "Côtelettes", "Escalopes", "Poulet", "Rôti de porc", "Saucisses", "Steaks"],
        ["Poissonnerie"] = ["Cabillaud", "Crevettes", "Moules", "Saumon", "Thon"],
        ["Épicerie"] =
        [
            "Biscuits", "Café", "Céréales", "Chocolat", "Confiture", "Farine", "Huile d'olive",
            "Huile de tournesol", "Ketchup", "Levure", "Lentilles", "Mayonnaise", "Miel", "Moutarde",
            "Pâtes", "Poivre", "Riz", "Sauce tomate", "Sel", "Sucre", "Sucre vanillé", "Thé",
            "Thon en boîte", "Vinaigre",
        ],
        ["Surgelés"] = ["Frites surgelées", "Glace", "Légumes surgelés", "Pizza surgelée", "Poisson pané"],
        ["Boissons"] = ["Bière", "Eau", "Eau gazeuse", "Jus d'orange", "Sirop", "Vin"],
        ["Hygiène"] = ["Coton-tiges", "Dentifrice", "Déodorant", "Gel douche", "Mouchoirs", "Papier toilette", "Shampooing"],
        ["Entretien"] = ["Éponges", "Lessive", "Liquide vaisselle", "Pastilles lave-vaisselle", "Sacs poubelle", "Sopalin"],
    });

    public static IEnumerable<string> Rayons => Produits.Values.Distinct();

    // Le rayon d'un produit connu (sans tenir compte des majuscules), ou null.
    public static string? RayonDe(string? nom) =>
        nom is not null && Produits.TryGetValue(nom.Trim(), out var rayon) ? rayon : null;

    private static Dictionary<string, string> Creer(Dictionary<string, string[]> parRayon) =>
        parRayon.SelectMany(r => r.Value.Select(p => (Produit: p, Rayon: r.Key)))
            .ToDictionary(x => x.Produit, x => x.Rayon, StringComparer.CurrentCultureIgnoreCase);
}
