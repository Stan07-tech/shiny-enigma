# Liste de courses (ASP.NET Core + EF Core)

Petite application web pour apprendre .NET : ajouter des articles, les cocher
quand ils sont achetés, les supprimer.

Deux interfaces utilisent les mêmes données :
- **Blazor** (page d'accueil `/`) : l'interface est écrite en C# ;
- **JavaScript** (`/classique.html`) : HTML + JavaScript qui appelle l'API REST.

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
| `Services/ArticleService.cs` | Logique métier partagée (API + Blazor) | Collection de procédures globales |
| `Program.cs` | Configuration + API REST | Code du projet / webservice |
| `Components/Pages/Accueil.razor` | Interface Blazor (HTML + C#) | Fenêtre / page WEBDEV |
| `Components/App.razor`, `Routes.razor` | Squelette HTML et routage Blazor | Projet / modèle de pages |
| `wwwroot/classique.html` | Interface HTML + JavaScript | Page WEBDEV avec code navigateur |

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
