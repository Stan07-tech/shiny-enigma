using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ListeCourses.Data;

// Mise à jour de la structure d'une base SQL Server (Azure SQL) au démarrage du site.
//
// Les changements de structure sont décrits par les MIGRATIONS EF Core (dossier
// Data/Migrations), l'équivalent de scripts de mise à jour numérotés : EF note dans
// la table __EFMigrationsHistory celles qui ont déjà été appliquées.
//
// Cas particulier : les bases créées avant les migrations (avec EnsureCreated) n'ont
// pas cette table. On les "rattache" à la migration Initiale, qui décrit exactement
// leur structure, pour que seules les migrations suivantes leur soient appliquées.
public static class MiseANiveauBase
{
    private const string MigrationInitiale = "20261123215431_Initiale";
    private const string VersionEf = "10.0.15";

    public static void Appliquer(CoursesDbContext db)
    {
        // Base inexistante : Migrate() la crée, inutile de chercher d'anciennes tables.
        var baseExiste = db.GetService<IRelationalDatabaseCreator>().Exists();

        if (baseExiste && TableExiste(db, "Listes") && !TableExiste(db, "__EFMigrationsHistory"))
        {
            // Base d'avant les recettes : on crée leurs tables (structure de la migration Initiale).
            if (!TableExiste(db, "Recettes"))
                db.Database.ExecuteSqlRaw(TablesRecettes);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1});
                """, MigrationInitiale, VersionEf);
        }

        // Applique les migrations manquantes (toutes, sur une base neuve).
        db.Database.Migrate();
    }

    private static bool TableExiste(CoursesDbContext db, string nom) =>
        db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = {nom}").Single() > 0;

    // Copié du script de la migration Initiale (dotnet ef migrations script 0 Initiale).
    private const string TablesRecettes = """
        CREATE TABLE [Recettes] (
            [Id] int NOT NULL IDENTITY,
            [Code] nvarchar(10) NOT NULL,
            [Nom] nvarchar(100) NOT NULL,
            [Instructions] nvarchar(max) NULL,
            [DateCreation] datetime2 NOT NULL,
            CONSTRAINT [PK_Recettes] PRIMARY KEY ([Id])
        );
        CREATE TABLE [Ingredients] (
            [Id] int NOT NULL IDENTITY,
            [Nom] nvarchar(100) NOT NULL,
            [Quantite] nvarchar(30) NULL,
            [Rayon] nvarchar(50) NULL,
            [RecetteId] int NOT NULL,
            CONSTRAINT [PK_Ingredients] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_Ingredients_Recettes_RecetteId] FOREIGN KEY ([RecetteId]) REFERENCES [Recettes] ([Id]) ON DELETE CASCADE
        );
        CREATE INDEX [IX_Ingredients_RecetteId] ON [Ingredients] ([RecetteId]);
        CREATE UNIQUE INDEX [IX_Recettes_Code] ON [Recettes] ([Code]);
        """;
}
