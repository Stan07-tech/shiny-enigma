using ListeCourses.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ListeCourses.Tests;

// Ces tests ont besoin d'un vrai SQL Server (les migrations sont écrites pour lui).
// Ils ne s'exécutent que si la variable d'environnement LISTECOURSES_SQLSERVER contient
// une chaîne de connexion, par exemple :
//   Server=localhost,1433;User ID=sa;Password=...;TrustServerCertificate=True
// Sinon, ils sont simplement ignorés ("Skipped").
public sealed class SqlServerFactAttribute : FactAttribute
{
    public static string? Connexion => Environment.GetEnvironmentVariable("LISTECOURSES_SQLSERVER");

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrEmpty(Connexion))
            Skip = "Définir LISTECOURSES_SQLSERVER pour tester les migrations sur SQL Server.";
    }
}

public sealed class MigrationsSqlServerTests : IDisposable
{
    private const string MigrationInitiale = "20261123215431_Initiale";

    // Une base neuve (et vide) par test, supprimée à la fin.
    private readonly string nomBase = $"ListeCoursesTest_{Guid.NewGuid():N}";

    private CoursesDbContext Contexte()
    {
        var chaine = new SqlConnectionStringBuilder(SqlServerFactAttribute.Connexion) { InitialCatalog = nomBase }.ConnectionString;
        return new CoursesDbContext(new DbContextOptionsBuilder<CoursesDbContext>().UseSqlServer(chaine).Options);
    }

    // Recrée une base "d'avant les migrations" : la structure de la migration Initiale,
    // mais sans la table __EFMigrationsHistory, comme le faisait EnsureCreated().
    private void CreerAncienneBase(bool avecRecettes)
    {
        using var db = Contexte();
        db.GetService<IMigrator>().Migrate(MigrationInitiale);
        db.Database.ExecuteSqlRaw("DROP TABLE [__EFMigrationsHistory]");
        if (!avecRecettes) db.Database.ExecuteSqlRaw("DROP TABLE [Ingredients]; DROP TABLE [Recettes];");
        db.Database.ExecuteSqlRaw("""
            INSERT INTO Listes (Code, Nom, DateCreation) VALUES ('ANCIENNE22', 'Maison', SYSUTCDATETIME());
            INSERT INTO Articles (Nom, Quantite, Rayon, Achete, DateAjout, ListeId)
            VALUES ('Pain', 2, 'Boulangerie', 0, SYSUTCDATETIME(), SCOPE_IDENTITY());
            """);
    }

    private record LigneIngredient(string Nom, decimal? Quantite, string? Symbole);

    [SqlServerFact]
    public void BaseNeuve_RecoitLesMigrationsEtLesUnites()
    {
        using var db = Contexte();

        MiseANiveauBase.Appliquer(db);

        Assert.Equal(["20261123215431_Initiale", "20261123215547_Unites"], db.Database.GetAppliedMigrations());
        Assert.Equal(["Pce", "g", "Kg", "L", "cL", "CaS", "CaC", "pn"], db.Unites.OrderBy(u => u.Ordre).Select(u => u.Symbole));
    }

    [SqlServerFact]
    public void AncienneBase_EstRattacheeEtSesQuantitesConverties()
    {
        CreerAncienneBase(avecRecettes: true);
        using (var db = Contexte())
        {
            db.Database.ExecuteSqlRaw("""
                INSERT INTO Recettes (Code, Nom, DateCreation) VALUES ('CREPES2222', 'Crêpes', SYSUTCDATETIME());
                DECLARE @r int = SCOPE_IDENTITY();
                INSERT INTO Ingredients (Nom, Quantite, RecetteId) VALUES
                    (N'Farine', N'250 g', @r), (N'Œufs', N'4', @r), (N'Lait', N'1,5 l', @r),
                    (N'Sucre', N'250g', @r), (N'Sel', N'1 pincée', @r), (N'Beurre', NULL, @r),
                    (N'Amandes', N'une poignée', @r), (N'Vanille', N'2 sachets', @r),
                    (N'Crème', N'20 cl', @r), (N'Huile', N'2 CaS', @r), (N'Pâtes', N'1 kilogramme', @r);
                """);
        }

        using var nouvelle = Contexte();
        MiseANiveauBase.Appliquer(nouvelle);

        var lignes = nouvelle.Database.SqlQueryRaw<LigneIngredient>("""
            SELECT i.Nom, i.Quantite, u.Symbole FROM Ingredients i LEFT JOIN Unites u ON u.Id = i.UniteId ORDER BY i.Id
            """).ToList();
        Assert.Equal(
        [
            new("Farine", 250m, "g"), new("Œufs", 4m, null), new("Lait", 1.5m, "L"),
            new("Sucre", 250m, "g"), new("Sel", 1m, "pn"), new("Beurre", null, null),
            new("Amandes (une poignée)", null, null), new("Vanille (2 sachets)", null, null),
            new("Crème", 20m, "cL"), new("Huile", 2m, "CaS"), new("Pâtes", 1m, "Kg"),
        ], lignes);

        // Les listes existantes sont intactes ; la quantité est devenue décimale.
        var pain = nouvelle.Articles.Single();
        Assert.Equal(("Pain", 2m, (int?)null), (pain.Nom, pain.Quantite, pain.UniteId));
    }

    [SqlServerFact]
    public void BaseDAvantLesRecettes_RecoitLesTablesDesRecettes()
    {
        CreerAncienneBase(avecRecettes: false);

        using var db = Contexte();
        MiseANiveauBase.Appliquer(db);

        Assert.Empty(db.Recettes);
        Assert.Equal("Pain", db.Articles.Single().Nom);
        Assert.Equal(2, db.Database.GetAppliedMigrations().Count());
    }

    [SqlServerFact]
    public void Appliquer_UneSecondeFois_NeChangeRien()
    {
        using (var db = Contexte()) MiseANiveauBase.Appliquer(db);

        using var encore = Contexte();
        MiseANiveauBase.Appliquer(encore);

        Assert.Equal(8, encore.Unites.Count());
        Assert.Equal(2, encore.Database.GetAppliedMigrations().Count());
    }

    public void Dispose()
    {
        if (string.IsNullOrEmpty(SqlServerFactAttribute.Connexion)) return;
        using var db = Contexte();
        SqlConnection.ClearAllPools();
        db.Database.EnsureDeleted();
    }
}
