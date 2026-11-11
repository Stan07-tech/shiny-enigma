using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Data;

// Le DbContext représente la connexion à la base (comme HOuvreConnexion
// en WinDev). Chaque DbSet<T> correspond à une table.
public class CoursesDbContext(DbContextOptions<CoursesDbContext> options) : DbContext(options)
{
    public DbSet<Liste> Listes => Set<Liste>();
    public DbSet<Article> Articles => Set<Article>();

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
        });
    }
}
