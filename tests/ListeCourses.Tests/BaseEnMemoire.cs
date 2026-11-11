using ListeCourses.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Tests;

// Une base SQLite "en mémoire" : elle n'existe que tant que la connexion
// reste ouverte. Chaque test part ainsi d'une base vide, sans fichier à nettoyer.
// Elle joue le rôle de la fabrique de DbContext qu'utilise ArticleService.
public sealed class BaseEnMemoire : IDbContextFactory<CoursesDbContext>, IDisposable
{
    private readonly SqliteConnection connexion = new("Data Source=:memory:");
    private readonly DbContextOptions<CoursesDbContext> options;

    public BaseEnMemoire()
    {
        connexion.Open();
        options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseSqlite(connexion)
            .Options;

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public CoursesDbContext CreateDbContext() => new(options);

    public void Dispose() => connexion.Dispose();
}
