using ListeCourses.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Tests;

// Comme BaseEnMemoire, pour le contexte des recettes. Les tables sont créées
// par CreerTablesSiAbsentes, exactement comme au démarrage du site.
public sealed class BaseRecettesEnMemoire : IDbContextFactory<RecettesDbContext>, IDisposable
{
    private readonly SqliteConnection connexion = new("Data Source=:memory:");
    private readonly DbContextOptions<RecettesDbContext> options;

    public BaseRecettesEnMemoire()
    {
        connexion.Open();
        options = new DbContextOptionsBuilder<RecettesDbContext>()
            .UseSqlite(connexion)
            .Options;

        using var db = CreateDbContext();
        db.CreerTablesSiAbsentes();
    }

    public CoursesDbContext CreerContexteListes() =>
        new(new DbContextOptionsBuilder<CoursesDbContext>().UseSqlite(connexion).Options);

    public RecettesDbContext CreateDbContext() => new(options);

    public void Dispose() => connexion.Dispose();
}
