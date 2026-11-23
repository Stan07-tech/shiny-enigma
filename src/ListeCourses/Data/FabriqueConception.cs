using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ListeCourses.Data;

// Utilisée uniquement par l'outil "dotnet ef" pour générer les migrations.
// Les migrations sont écrites pour SQL Server (Azure SQL) : c'est la base de production.
// En local et dans les tests, SQLite crée les tables directement (EnsureCreated).
public class FabriqueConception : IDesignTimeDbContextFactory<CoursesDbContext>
{
    public CoursesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CoursesDbContext>()
            .UseSqlServer("Server=localhost;Database=ListeCourses;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
}
