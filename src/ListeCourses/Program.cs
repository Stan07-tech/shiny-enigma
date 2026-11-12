using ListeCourses.Components;
using ListeCourses.Data;
using ListeCourses.Models;
using ListeCourses.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Injection de dépendances : on déclare les services une fois, ASP.NET Core
// les fournit ensuite automatiquement à qui les demande (API ou composants).
// Base de données : SQLite en local, SQL Server (Azure SQL) en production,
// selon le réglage "BaseDeDonnees" (appsettings.json ou paramètres Azure).
// Les deux DbContext (listes et recettes) utilisent la même base.
void ConfigurerBase(DbContextOptionsBuilder options)
{
    var chaine = builder.Configuration.GetConnectionString("Courses");
    if (builder.Configuration["BaseDeDonnees"] == "SqlServer")
        // Azure SQL "serverless" se met en pause : on réessaie le temps qu'il redémarre.
        options.UseSqlServer(chaine, sql => sql.EnableRetryOnFailure());
    else
        options.UseSqlite(chaine);
}
builder.Services.AddDbContextFactory<CoursesDbContext>(ConfigurerBase);
builder.Services.AddDbContextFactory<RecettesDbContext>(ConfigurerBase);
builder.Services.AddSingleton<NotificateurListes>();
builder.Services.AddSingleton<IListeService, ListeServiceEf>();
builder.Services.AddSingleton<IRecetteService, RecetteServiceEf>();
builder.Services.AddScoped<IListesConnues, ListesConnuesNavigateur>();
builder.Services.AddScoped<IRecettesConnues, RecettesConnuesNavigateur>();
builder.Services.AddScoped<IPartage, PartageNavigateur>();
builder.Services.AddSignalR();

// Blazor en mode "Interactive Server" : le C# tourne sur le serveur et
// l'affichage est mis à jour dans le navigateur via une connexion SignalR.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Crée la base et les tables si elles n'existent pas encore.
using (var db = app.Services.GetRequiredService<IDbContextFactory<CoursesDbContext>>().CreateDbContext())
{
    db.Database.EnsureCreated();
}
using (var db = app.Services.GetRequiredService<IDbContextFactory<RecettesDbContext>>().CreateDbContext())
{
    db.CreerTablesSiAbsentes();
}

// Chaque modification d'une liste est relayée aux applis mobiles qui la suivent.
var hub = app.Services.GetRequiredService<IHubContext<HubListes>>();
app.Services.GetRequiredService<NotificateurListes>().Abonner(null, code =>
    hub.Clients.Group(HubListes.Groupe(code)).SendAsync("ListeModifiee", code));

app.UseAntiforgery();
app.MapStaticAssets();
app.MapHub<HubListes>("/hub/listes");

// ----- API REST (utilisée par l'appli mobile et la page JavaScript) -----
var api = app.MapGroup("/api/listes");

api.MapPost("/", async (NouvelleListe demande, IListeService service) =>
{
    var liste = await service.CreerAsync(demande.Nom);
    return Results.Created($"/api/listes/{liste.Code}", liste);
});

api.MapGet("/{code}", async (string code, IListeService service) =>
    await service.ObtenirAsync(Normaliser(code)) is { } liste ? Results.Ok(liste) : Results.NotFound());

api.MapPost("/resumes", (List<string> codes, IListeService service) => service.ResumesAsync(codes));

var articles = api.MapGroup("/{code}/articles");

articles.MapGet("/", async (string code, IListeService service) =>
    await service.ArticlesAsync(Normaliser(code)) is { } liste ? Results.Ok(liste) : Results.NotFound());

articles.MapPost("/", async (string code, Article article, IListeService service) =>
{
    if (string.IsNullOrWhiteSpace(article.Nom))
        return Results.BadRequest("Le nom est obligatoire.");

    return await service.AjouterAsync(Normaliser(code), article) is { } nouvel
        ? Results.Created($"/api/listes/{Normaliser(code)}/articles/{nouvel.Id}", nouvel)
        : Results.NotFound();
});

articles.MapPatch("/{id:int}/basculer", async (string code, int id, IListeService service) =>
    await service.BasculerAsync(Normaliser(code), id) is { } article ? Results.Ok(article) : Results.NotFound());

articles.MapDelete("/{id:int}", async (string code, int id, IListeService service) =>
    await service.SupprimerAsync(Normaliser(code), id) ? Results.NoContent() : Results.NotFound());

articles.MapPost("/lot", async (string code, List<Article> lot, IListeService service) =>
    await service.AjouterPlusieursAsync(Normaliser(code), lot) is { } resultat ? Results.Ok(resultat) : Results.NotFound());

articles.MapDelete("/achetes", async (string code, IListeService service) =>
{
    if (await service.ObtenirAsync(Normaliser(code)) is null) return Results.NotFound();
    await service.SupprimerAchetesAsync(Normaliser(code));
    return Results.NoContent();
});

// ----- API REST des recettes -----
var recettes = app.MapGroup("/api/recettes");

recettes.MapPost("/", async (NouvelleListe demande, IRecetteService service) =>
{
    var recette = await service.CreerAsync(demande.Nom);
    return Results.Created($"/api/recettes/{recette.Code}", recette);
});

recettes.MapGet("/{code}", async (string code, IRecetteService service) =>
    await service.ObtenirAsync(Normaliser(code)) is { } recette ? Results.Ok(recette) : Results.NotFound());

recettes.MapPost("/resumes", (List<string> codes, IRecetteService service) => service.ResumesAsync(codes));

recettes.MapPut("/{code}", async (string code, ModificationRecette modification, IRecetteService service) =>
    await service.ModifierAsync(Normaliser(code), modification) ? Results.NoContent() : Results.NotFound());

recettes.MapPost("/{code}/ingredients", async (string code, Ingredient ingredient, IRecetteService service) =>
{
    if (string.IsNullOrWhiteSpace(ingredient.Nom))
        return Results.BadRequest("Le nom est obligatoire.");

    return await service.AjouterIngredientAsync(Normaliser(code), ingredient) is { } nouvel
        ? Results.Created($"/api/recettes/{Normaliser(code)}/ingredients/{nouvel.Id}", nouvel)
        : Results.NotFound();
});

recettes.MapDelete("/{code}/ingredients/{id:int}", async (string code, int id, IRecetteService service) =>
    await service.SupprimerIngredientAsync(Normaliser(code), id) ? Results.NoContent() : Results.NotFound());

// ----- Interface Blazor -----
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ListeCourses.Pages.MesListes).Assembly);

app.Run();

// Les codes sont stockés en majuscules ; on accepte "k7mz2qh9xa" dans l'adresse.
static string Normaliser(string code) => code.ToUpperInvariant();
