using ListeCourses.Models;

namespace ListeCourses.Tests;

public class OutilsAffichageTests
{
    private static readonly Unite Grammes = Unite.ParDefaut.Single(u => u.Symbole == "g");

    [Theory]
    [InlineData(2, "2")]
    [InlineData(0.5, "0,5")]
    [InlineData(1.25, "1,25")]
    [InlineData(250, "250")]
    public void Nombre_AuFormatFrancais(decimal quantite, string attendu)
    {
        Assert.Equal(attendu, Quantites.Nombre(quantite));
    }

    [Fact]
    public void Texte_SelonQuantiteEtUnite()
    {
        Assert.Equal("250 g", Quantites.Texte(250, Grammes));
        Assert.Equal("4", Quantites.Texte(4, null));
        Assert.Equal("g", Quantites.Texte(null, Grammes));
        Assert.Equal("", Quantites.Texte(null, null));
    }

    [Theory]
    [InlineData("Bananes", "Fruits et légumes")]
    [InlineData("  farine ", "Épicerie")]
    [InlineData("Produit inconnu", null)]
    [InlineData(null, null)]
    public void Catalogue_DonneLeRayonDUnProduitConnu(string? nom, string? rayon)
    {
        Assert.Equal(rayon, Catalogue.RayonDe(nom));
    }
}
