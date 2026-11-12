using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ListeCourses.Data;

// Les recettes ont leur propre DbContext, dans la même base que les listes.
// Pourquoi ? EnsureCreated() ne crée les tables que dans une base VIDE : il
// n'ajouterait pas les tables des recettes à une base qui contient déjà les listes.
// Avec un contexte séparé, on peut créer uniquement ses tables (voir CreerTablesSiAbsentes).
public class RecettesDbContext(DbContextOptions<RecettesDbContext> options) : DbContext(options)
{
    public DbSet<Recette> Recettes => Set<Recette>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recette>(e =>
        {
            e.Property(r => r.Code).HasMaxLength(CodeListe.Longueur).IsRequired();
            e.HasIndex(r => r.Code).IsUnique();
            e.Property(r => r.Nom).HasMaxLength(100).IsRequired();

            e.HasMany(r => r.Ingredients)
                .WithOne()
                .HasForeignKey(i => i.RecetteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ingredient>(e =>
        {
            e.Property(i => i.Nom).HasMaxLength(100).IsRequired();
            e.Property(i => i.Quantite).HasMaxLength(30);
            e.Property(i => i.Rayon).HasMaxLength(50);
        });
    }

    // Crée les tables Recettes et Ingredients si elles n'existent pas encore
    // (par exemple dans la base Azure créée avant l'arrivée des recettes).
    public void CreerTablesSiAbsentes()
    {
        try
        {
            // Équivalent de : SELECT TOP 1 1 FROM Recettes
            _ = Recettes.Any();
        }
        catch (Exception e) when (EstTableManquante(e))
        {
            // La table n'existe pas : on crée les tables de CE contexte uniquement.
            this.GetService<IRelationalDatabaseCreator>().CreateTables();
        }
    }

    // Toute autre erreur (base injoignable...) doit remonter, pas déclencher une création.
    private static bool EstTableManquante(Exception e) => e switch
    {
        Microsoft.Data.SqlClient.SqlException sql => sql.Number == 208,       // Invalid object name
        Microsoft.Data.Sqlite.SqliteException sqlite => sqlite.Message.Contains("no such table"),
        _ => e.InnerException is not null && EstTableManquante(e.InnerException),
    };
}
