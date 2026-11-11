# Liste de courses (.NET 10 : ASP.NET Core, Blazor, MAUI)

Petite application pour apprendre .NET : ajouter des articles, les cocher
quand ils sont achetés, les supprimer, et parcourir la liste rayon par rayon
(`/rayon/Boulangerie`, `/sans-rayon`).

Trois interfaces partagent le même code métier :
- **Site web Blazor** (page d'accueil `/`) : l'interface est écrite en C# ;
- **Site web JavaScript** (`/classique.html`) : HTML + JavaScript qui appelle l'API REST ;
- **Appli Android** (.NET MAUI Blazor Hybrid) : la même page Blazor, avec une
  base SQLite stockée sur le téléphone (fonctionne sans connexion).

## Structure de la solution

| Projet | Rôle |
|---|---|
| `src/ListeCourses.Shared` | Bibliothèque partagée : modèle, accès aux données, logique métier, page Blazor |
| `src/ListeCourses` | Site web ASP.NET Core (Blazor + API REST) |
| `src/ListeCourses.Mobile` | Appli mobile .NET MAUI (Android, et Windows pour tester sur PC) |

| Fichier | Rôle | Équivalent WinDev / SQL |
|---|---|---|
| `Shared/Models/Article.cs` | L'entité `Article` | Fichier de l'analyse / table |
| `Shared/Data/CoursesDbContext.cs` | Accès à la base (EF Core) | `HOuvreConnexion` + analyse |
| `Shared/Services/ArticleService.cs` | Logique métier partagée | Collection de procédures globales |
| `Shared/Pages/Accueil.razor` | Interface Blazor (HTML + C#), tous les articles ou un rayon | Fenêtre / page WEBDEV |
| `Shared/Pages/SansRayon.razor` | Articles sans rayon (réutilise `Accueil`) | Fenêtre ouverte avec un paramètre |
| `ListeCourses/Program.cs` | Configuration du site + API REST | Code du projet / webservice |
| `ListeCourses/wwwroot/classique.html` | Interface HTML + JavaScript | Page WEBDEV avec code navigateur |
| `ListeCourses.Mobile/MauiProgram.cs` | Configuration de l'appli mobile | Code du projet |

## Lancer le site web

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ListeCourses
```

Puis ouvrir l'URL affichée dans la console (ex. : http://localhost:5xxx).
La base SQLite `courses.db` est créée automatiquement au premier lancement.

## Appli Android

Prérequis : SDK .NET 10 + workload MAUI (`dotnet workload install maui-android`),
ou Visual Studio 2026 avec la charge de travail « Développement .NET Multi-platform App UI ».

Générer l'APK :

```bash
dotnet publish src/ListeCourses.Mobile -f net10.0-android -c Release
```

Le fichier `com.shinyenigma.listecourses-Signed.apk` se trouve ensuite dans
`src/ListeCourses.Mobile/bin/Release/net10.0-android/publish/`. Il est signé avec
une clé de débogage : parfait pour l'installer sur son propre téléphone, mais il
faudra une clé personnelle pour une publication sur le Play Store.

Sous Windows, on peut aussi lancer l'appli directement sur le PC (cible
`net10.0-windows10.0.19041.0` dans Visual Studio) pour tester sans téléphone.

> Le projet mobile nécessite le workload MAUI : sans lui, compiler toute la
> solution échoue. Le site web seul se compile avec `dotnet build src/ListeCourses`.

## API

| Méthode | URL | SQL équivalent |
|---|---|---|
| GET | `/api/articles` | `SELECT … ORDER BY Achete, Rayon, Nom` |
| POST | `/api/articles` | `INSERT` |
| PATCH | `/api/articles/{id}/basculer` | `UPDATE … SET Achete = ~Achete` |
| DELETE | `/api/articles/{id}` | `DELETE … WHERE Id = @id` |
| DELETE | `/api/articles/achetes` | `DELETE … WHERE Achete = 1` |

## Passer à SQL Server (site web)

1. `dotnet add src/ListeCourses package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.15`
2. Dans `Program.cs`, remplacer `UseSqlite(...GetConnectionString("Courses"))`
   par `UseSqlServer(...GetConnectionString("SqlServer"))`.
3. Adapter la chaîne `SqlServer` dans `appsettings.json`.
