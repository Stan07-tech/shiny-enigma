using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Data;

// Le DbContext représente la connexion à la base (comme HOuvreConnexion
// en WinDev). Chaque DbSet<T> correspond à une table.
public class CoursesDbContext(DbContextOptions<CoursesDbContext> options) : DbContext(options)
{
    public DbSet<Liste> Listes => Set<Liste>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Recette> Recettes => Set<Recette>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Unite> Unites => Set<Unite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Liste>(e =>
        {
            e.Property(l => l.Code).HasMaxLength(CodeListe.Longueur).IsRequired();
            e.HasIndex(l => l.Code).IsUnique();                 // CREATE UNIQUE INDEX
            e.Property(l => l.Nom).HasMaxLength(100).IsRequired();

            // FOREIGN KEY Articles(ListeId) REFERENCES Listes(Id) ON DELETE CASCADE
            e.HasMany(l => l.Articles)
                .WithOne()
                .HasForeignKey(a => a.ListeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.Property(a => a.Nom).HasMaxLength(100).IsRequired();
            e.Property(a => a.Rayon).HasMaxLength(50);
            e.Property(a => a.Quantite).HasPrecision(9, 2);
            e.HasOne<Unite>().WithMany().HasForeignKey(a => a.UniteId).OnDelete(DeleteBehavior.Restrict);
        });

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
            e.Property(i => i.Quantite).HasPrecision(9, 2);
            e.Property(i => i.Rayon).HasMaxLength(50);
            e.HasOne<Unite>().WithMany().HasForeignKey(i => i.UniteId).OnDelete(DeleteBehavior.Restrict);
        });

        // Table de référence : les unités sont insérées à la création de la base
        // (HasData = INSERT INTO Unites ... dans la migration).
        modelBuilder.Entity<Unite>(e =>
        {
            e.Property(u => u.Symbole).HasMaxLength(10).IsRequired();
            e.Property(u => u.Libelle).HasMaxLength(40).IsRequired();
            e.HasData(Unite.ParDefaut);
        });
    }
}
