using ListeCourses.Components;
using ListeCourses.Data;
using ListeCourses.Models;
using ListeCourses.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Injection de dépendances : on déclare les services une fois, ASP.NET Core
// les fournit ensuite automatiquement à qui les demande (API ou composants).
// Pour passer à SQL Server : package Microsoft.EntityFrameworkCore.SqlServer
// puis options.UseSqlServer(...) avec la chaîne "SqlServer" de appsettings.json.
builder.Services.AddDbContextFactory<CoursesDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Courses")));
builder.Services.AddSingleton<ArticleService>();

// Blazor en mode "Interactive Server" : le C# tourne sur le serveur et
// l'affichage est mis à jour dans le navigateur via une connexion SignalR.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Crée la base et la table si elles n'existent pas encore.
using (var db = app.Services.GetRequiredService<IDbContextFactory<CoursesDbContext>>().CreateDbContext())
{
    db.Database.EnsureCreated();
}

app.UseAntiforgery();
app.MapStaticAssets();

// ----- API REST -----
var api = app.MapGroup("/api/articles");

api.MapGet("/", (ArticleService service) => service.ListerAsync());

api.MapPost("/", async (Article article, ArticleService service) =>
    await service.AjouterAsync(article) is { } nouvel
        ? Results.Created($"/api/articles/{nouvel.Id}", nouvel)
        : Results.BadRequest("Le nom est obligatoire."));

api.MapPatch("/{id:int}/basculer", async (int id, ArticleService service) =>
    await service.BasculerAsync(id) is { } article
        ? Results.Ok(article)
        : Results.NotFound());

api.MapDelete("/{id:int}", async (int id, ArticleService service) =>
    await service.SupprimerAsync(id) ? Results.NoContent() : Results.NotFound());

api.MapDelete("/achetes", async (ArticleService service) =>
{
    await service.SupprimerAchetesAsync();
    return Results.NoContent();
});

// ----- Interface Blazor -----
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ListeCourses.Pages.Accueil).Assembly);

app.Run();
