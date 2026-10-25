# Liste de courses (ASP.NET Core + EF Core)

Petite application web pour apprendre .NET : ajouter des articles, les cocher
quand ils sont achetés, les supprimer.

## Lancer l'application

Prérequis : [SDK .NET 8](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ListeCourses
```

Puis ouvrir l'URL affichée dans la console (ex. : http://localhost:5xxx).
La base SQLite `courses.db` est créée automatiquement au premier lancement.

## Structure

| Fichier | Rôle | Équivalent WinDev / SQL |
|---|---|---|
| `Models/Article.cs` | L'entité `Article` | Fichier de l'analyse / table |
| `Data/CoursesDbContext.cs` | Accès à la base (EF Core) | `HOuvreConnexion` + analyse |
| `Program.cs` | Configuration + API REST | Procédures / webservice |
| `wwwroot/index.html` | Interface (HTML + JavaScript) | Fenêtre / page WEBDEV |

## API

| Méthode | URL | SQL équivalent |
|---|---|---|
| GET | `/api/articles` | `SELECT … ORDER BY Achete, Rayon, Nom` |
| POST | `/api/articles` | `INSERT` |
| PATCH | `/api/articles/{id}/basculer` | `UPDATE … SET Achete = ~Achete` |
| DELETE | `/api/articles/{id}` | `DELETE … WHERE Id = @id` |
| DELETE | `/api/articles/achetes` | `DELETE … WHERE Achete = 1` |

## Passer à SQL Server

1. `dotnet add src/ListeCourses package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11`
2. Dans `Program.cs`, remplacer `UseSqlite(...GetConnectionString("Courses"))`
   par `UseSqlServer(...GetConnectionString("SqlServer"))`.
3. Adapter la chaîne `SqlServer` dans `appsettings.json`.
