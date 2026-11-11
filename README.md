# Liste de courses (.NET 10 : ASP.NET Core, Blazor, MAUI, SignalR)

Petite application pour apprendre .NET : des listes de courses **partageables**,
mises à jour **en temps réel** entre plusieurs personnes, consultables rayon par rayon.

- Chaque liste a un **code secret** (ex. `K7MZ2QH9XA`) ; son lien
  `https://…/l/K7MZ2QH9XA` suffit pour y accéder, sans compte.
- Chaque appareil retient les listes qu'il a créées ou ouvertes (« Mes listes »).
- Quand quelqu'un ajoute ou coche un article, les écrans des autres se mettent à jour seuls.

Trois interfaces partagent le même code :
- **Site web Blazor** : l'interface est écrite en C# ;
- **Appli Android** (.NET MAUI Blazor Hybrid) : les mêmes pages Blazor, qui appellent l'API du serveur ;
- **Page JavaScript** (`/classique.html?liste=CODE`) : HTML + JavaScript qui appelle l'API REST.

## Architecture

```
 Appli Android ──HTTP (API REST) + SignalR──┐
                                            ▼
 Navigateur ────Blazor Server (SignalR)──► Site ASP.NET Core ──EF Core──► Azure SQL (prod)
                                           (Azure App Service)            SQLite (local)
```

Les pages ne connaissent que l'interface `IListeService`, réalisée deux fois :
- `ListeServiceEf` (site web) : accès direct à la base avec Entity Framework Core ;
- `ListeServiceApi` (appli mobile) : appels HTTP à l'API + notifications SignalR.

## Structure de la solution

| Projet | Rôle |
|---|---|
| `src/ListeCourses.Shared` | Modèles, contrat `IListeService`, client API, pages Blazor |
| `src/ListeCourses` | Site web : base de données, API REST, hub SignalR |
| `src/ListeCourses.Mobile` | Appli mobile .NET MAUI (Android, et Windows pour tester sur PC) |
| `tests/ListeCourses.Tests` | Tests xUnit : unitaires et d'intégration |

| Fichier | Rôle | Équivalent WinDev / SQL |
|---|---|---|
| `Shared/Models/Liste.cs`, `Article.cs` | Les entités (relation 1-N) | Fichiers de l'analyse / tables |
| `Shared/Services/IListeService.cs` | Contrat de la logique métier | Prototypes des procédures |
| `Shared/Services/ListeServiceApi.cs` | Client HTTP + SignalR (appli mobile) | Appels de webservice |
| `Shared/Pages/MesListes.razor` | Accueil : mes listes, créer, rejoindre | Fenêtre principale |
| `Shared/Pages/PageListe.razor` | Une liste, tous les articles ou un rayon | Fenêtre / page WEBDEV |
| `ListeCourses/Data/CoursesDbContext.cs` | Accès à la base (EF Core) | `HOuvreConnexion` + analyse |
| `ListeCourses/Services/ListeServiceEf.cs` | Logique métier côté serveur | Collection de procédures globales |
| `ListeCourses/Services/HubListes.cs` | Hub SignalR (temps réel) | — |
| `ListeCourses/Program.cs` | Configuration du site + API REST | Code du projet / webservice |
| `ListeCourses.Mobile/Configuration.cs` | Adresse du serveur pour l'appli | Paramètres du projet |

## Lancer le site web en local

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ListeCourses
```

Puis ouvrir l'URL affichée dans la console (ex. : http://localhost:5xxx).
La base SQLite `courses.db` est créée automatiquement au premier lancement.

> Si vous aviez un `courses.db` d'une version précédente (sans les listes),
> supprimez-le : la structure des tables a changé.

## Déployer sur Azure

Voir [docs/deploiement-azure.md](docs/deploiement-azure.md) : App Service pour le
site, Azure SQL Database pour les données.

## Tests (xUnit)

```bash
dotnet test tests/ListeCourses.Tests
```

- `ListeServiceEfTests.cs` : tests unitaires du service serveur. Chaque test
  reçoit une base SQLite **en mémoire** neuve (`BaseEnMemoire.cs`).
- `ApiTests.cs` : tests d'intégration. `WebApplicationFactory` démarre le vrai
  site en mémoire et l'appelle en HTTP (codes 201, 400, 404, pages).
- `ListeServiceApiTests.cs` : le client de l'appli mobile contre le vrai serveur,
  y compris la notification en temps réel par SignalR entre deux « téléphones ».

Vocabulaire : `[Fact]` = un test ; `[Theory]` + `[InlineData]` = le même test
avec plusieurs jeux de valeurs ; `Assert.Equal(attendu, obtenu)` = la vérification.

## Appli Android

Prérequis : SDK .NET 10 + workload MAUI (`dotnet workload install maui-android`),
ou Visual Studio 2026 avec la charge de travail « Développement .NET Multi-platform App UI ».

1. Indiquer l'adresse du site déployé dans `src/ListeCourses.Mobile/Configuration.cs`.
2. Générer l'APK :

```bash
dotnet publish src/ListeCourses.Mobile -f net10.0-android -c Release
```

Le fichier `com.shinyenigma.listecourses-Signed.apk` se trouve ensuite dans
`src/ListeCourses.Mobile/bin/Release/net10.0-android/publish/`. Il est signé avec
une clé de débogage : parfait pour l'installer sur son propre téléphone, mais il
faudra une clé personnelle pour une publication sur le Play Store.

L'appli a besoin d'Internet pour lire et modifier les listes.

> Le projet mobile nécessite le workload MAUI : sans lui, compiler toute la
> solution échoue. Le site web seul se compile avec `dotnet build src/ListeCourses`.

## API

Toutes les adresses d'articles passent par le code de la liste : connaître l'`Id`
d'un article ne suffit pas pour le modifier.

| Méthode | URL | Rôle |
|---|---|---|
| POST | `/api/listes` | Créer une liste (`{ "nom": "Maison" }`) |
| GET | `/api/listes/{code}` | Nom et nombre d'articles restants |
| POST | `/api/listes/resumes` | Résumés de plusieurs listes (`["CODE1", "CODE2"]`) |
| GET | `/api/listes/{code}/articles` | Articles de la liste |
| POST | `/api/listes/{code}/articles` | Ajouter un article |
| PATCH | `/api/listes/{code}/articles/{id}/basculer` | Cocher / décocher |
| DELETE | `/api/listes/{code}/articles/{id}` | Supprimer un article |
| DELETE | `/api/listes/{code}/articles/achetes` | Retirer les articles achetés |

Temps réel : hub SignalR `/hub/listes`, méthodes `Suivre(code)` / `NePlusSuivre(code)`,
message reçu `ListeModifiee(code)`.

## Limites actuelles

- Qui possède le lien d'une liste peut la modifier : ne le partagez qu'avec les bonnes personnes.
- Pas de suppression définitive d'une liste (« ✕ » la retire seulement de « Mes listes »).
- Les tables sont créées par `EnsureCreated()` : pour faire évoluer leur structure
  sans perdre les données, il faudra passer aux **migrations** EF Core.
