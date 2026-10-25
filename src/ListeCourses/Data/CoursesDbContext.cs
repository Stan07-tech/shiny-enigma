using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Data;

// Le DbContext représente la connexion à la base (comme HOuvreConnexion
// en WinDev). Chaque DbSet<T> correspond à une table.
public class CoursesDbContext(DbContextOptions<CoursesDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(e =>
        {
            e.Property(a => a.Nom).HasMaxLength(100).IsRequired();
            e.Property(a => a.Rayon).HasMaxLength(50);
        });
    }
}
