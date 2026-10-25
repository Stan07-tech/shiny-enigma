using ListeCourses.Data;
using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Injection de dépendances : on déclare le DbContext une fois, ASP.NET Core
// le fournit ensuite automatiquement à chaque endpoint qui le demande.
// Pour passer à SQL Server : package Microsoft.EntityFrameworkCore.SqlServer
// puis options.UseSqlServer(...) avec la chaîne "SqlServer" de appsettings.json.
builder.Services.AddDbContext<CoursesDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Courses")));

var app = builder.Build();

// Crée la base et la table si elles n'existent pas encore.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CoursesDbContext>().Database.EnsureCreated();
}

// Sert wwwroot/index.html (l'interface).
app.UseDefaultFiles();
app.UseStaticFiles();

// ----- API REST -----
var api = app.MapGroup("/api/articles");

// SELECT * FROM Articles ORDER BY Achete, Rayon, Nom
api.MapGet("/", async (CoursesDbContext db) =>
    await db.Articles
        .OrderBy(a => a.Achete)
        .ThenBy(a => a.Rayon)
        .ThenBy(a => a.Nom)
        .ToListAsync());

// INSERT INTO Articles ...
api.MapPost("/", async (Article article, CoursesDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(article.Nom))
        return Results.BadRequest("Le nom est obligatoire.");

    article.Id = 0;
    article.Nom = article.Nom.Trim();
    article.Quantite = Math.Max(1, article.Quantite);
    article.DateAjout = DateTime.UtcNow;
    db.Articles.Add(article);
    await db.SaveChangesAsync();
    return Results.Created($"/api/articles/{article.Id}", article);
});

// UPDATE Articles SET Achete = ~Achete WHERE Id = @id
api.MapPatch("/{id:int}/basculer", async (int id, CoursesDbContext db) =>
{
    var article = await db.Articles.FindAsync(id);
    if (article is null) return Results.NotFound();

    article.Achete = !article.Achete;
    await db.SaveChangesAsync();
    return Results.Ok(article);
});

// DELETE FROM Articles WHERE Id = @id
api.MapDelete("/{id:int}", async (int id, CoursesDbContext db) =>
{
    var nb = await db.Articles.Where(a => a.Id == id).ExecuteDeleteAsync();
    return nb == 0 ? Results.NotFound() : Results.NoContent();
});

// DELETE FROM Articles WHERE Achete = 1
api.MapDelete("/achetes", async (CoursesDbContext db) =>
{
    await db.Articles.Where(a => a.Achete).ExecuteDeleteAsync();
    return Results.NoContent();
});

app.Run();
