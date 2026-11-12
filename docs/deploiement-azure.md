# Déployer sur Azure

Le site web (qui contient aussi l'API et le hub SignalR) est hébergé sur
**Azure App Service** et les données sont stockées dans **Azure SQL Database**,
c'est-à-dire SQL Server dans le cloud.

```
Navigateurs / téléphones ──HTTPS──► App Service (site .NET 10) ──► Azure SQL Database
```

Les commandes ci-dessous utilisent **Azure CLI** dans **PowerShell** (Windows).

## Prérequis

- Un abonnement Azure (le [compte gratuit](https://azure.microsoft.com/free/) suffit pour commencer).
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli-windows) installé.
- Le SDK .NET 10.

## Coûts

| Ressource | Offre proposée ici | Coût indicatif |
|---|---|---|
| Azure SQL Database | Offre gratuite (serverless, 100 000 secondes vCore et 32 Go par mois) | 0 € ; la base se met en pause quand la limite est atteinte |
| App Service | **F1** (gratuit) | 0 € : 60 minutes de CPU par jour, 5 connexions temps réel simultanées au plus |
| App Service | **B1** (recommandé dès qu'on l'utilise vraiment) | Environ 12 € par mois |

Pour une famille, F1 suffit pour essayer. Si l'appli devient lente ou si les mises à
jour en temps réel décrochent, passez en B1 :
`az appservice plan update -g $rg -n $plan --sku B1`.

## 1. Variables

Choisissez un suffixe unique (vos initiales et un nombre, par exemple), car le nom
du serveur SQL et celui du site doivent être uniques dans tout Azure.

```powershell
$suffixe  = "lb2026"                       # à personnaliser
$rg       = "rg-listecourses"
$region   = "francecentral"
$sql      = "sql-listecourses-$suffixe"
$base     = "ListeCourses"
$plan     = "plan-listecourses"
$app      = "listecourses-$suffixe"
$admin    = "adminlc"
$motDePasse = Read-Host "Mot de passe administrateur SQL (12 caractères minimum, majuscules, chiffres, symboles)"
```

## 2. Connexion et groupe de ressources

Un groupe de ressources est un « dossier » qui regroupe tout le projet. Supprimer
ce groupe supprime l'ensemble des ressources qu'il contient.

```powershell
az login
az group create --name $rg --location $region
```

## 3. Azure SQL Database

```powershell
# Le "serveur" logique (l'équivalent d'une instance SQL Server)
az sql server create --resource-group $rg --name $sql --location $region `
    --admin-user $admin --admin-password $motDePasse

# Autoriser les services Azure (dont notre App Service) à s'y connecter
az sql server firewall-rule create --resource-group $rg --server $sql `
    --name AutoriserServicesAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# La base, avec l'offre gratuite (pause automatique si la limite mensuelle est atteinte)
az sql db create --resource-group $rg --server $sql --name $base `
    --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 2 `
    --use-free-limit --free-limit-exhaustion-behavior AutoPause
```

Pour explorer la base avec **SSMS** depuis votre PC, autorisez votre adresse IP :

```powershell
$monIp = (Invoke-RestMethod https://api.ipify.org)
az sql server firewall-rule create --resource-group $rg --server $sql `
    --name MonPC --start-ip-address $monIp --end-ip-address $monIp
```

Dans SSMS, le nom du serveur est celui de la variable `$sql` suivi de
`.database.windows.net` (ex. `sql-listecourses-lb2026.database.windows.net`).
Pour l'afficher :

```powershell
az sql server show --resource-group $rg --name $sql --query fullyQualifiedDomainName -o tsv
```

Choisissez l'authentification SQL Server, l'utilisateur `$admin` (`adminlc`), votre
mot de passe, et le chiffrement « Obligatoire ». Les tables `Listes` et `Articles`
apparaîtront après le premier démarrage du site.

## 4. App Service (le site web)

```powershell
az appservice plan create --resource-group $rg --name $plan --sku F1 --is-linux
az webapp create --resource-group $rg --plan $plan --name $app --runtime "DOTNETCORE:10.0"

# WebSockets : nécessaires à Blazor Server et aux mises à jour en temps réel
az webapp config set --resource-group $rg --name $app --web-sockets-enabled true `
    --startup-file "dotnet ListeCourses.dll"

# Utiliser SQL Server plutôt que SQLite
az webapp config appsettings set --resource-group $rg --name $app --settings BaseDeDonnees=SqlServer

# La chaîne de connexion. Azure la stocke chiffrée et la fournit au site
# comme ConnectionStrings:Courses.
$chaine = "Server=tcp:$sql.database.windows.net,1433;Database=$base;User ID=$admin;Password=$motDePasse;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
az webapp config connection-string set --resource-group $rg --name $app `
    --connection-string-type SQLAzure --settings Courses=$chaine
```

### Si la création du plan échoue

- `Operation cannot be completed without additional quota` : l'abonnement n'a pas de
  quota pour cette offre dans cette région.
- `The selected region is currently not accepting new customers` : la région est saturée.

Dans les deux cas, essayez d'autres régions. Le site peut être dans une autre région
que la base. Cette boucle les essaie l'une après l'autre et s'arrête à la première
qui accepte :

```powershell
foreach ($r in "northeurope", "swedencentral", "germanywestcentral", "uksouth", "italynorth") {
    Write-Host "Essai : $r" -ForegroundColor Cyan
    az appservice plan create --resource-group $rg --name $plan --sku F1 --is-linux --location $r
    if ($LASTEXITCODE -eq 0) {
        $regionSite = $r
        Write-Host "Plan créé en $r" -ForegroundColor Green
        break
    }
}
```

Si aucune région n'accepte F1, relancez la boucle avec `--sku B1`. En dernier recours,
demandez une augmentation de quota dans le portail Azure (Aide + support,
« Limites de service et d'abonnement (quotas) »).

## 5. Publier le site

Il faut d'abord le code sur votre PC. Avec Git :

```powershell
cd $HOME
git clone -b claude/shopping-list-app-1dlkyt https://github.com/Stan07-tech/shiny-enigma.git
cd shiny-enigma
```

Sans Git, téléchargez le ZIP de la branche sur GitHub (bouton **Code** puis
**Download ZIP**), extrayez-le et placez-vous dans le dossier obtenu avec `cd`.
La commande `ls` doit alors afficher `src`, `docs`, `tests` et `README.md`.

Puis, depuis ce dossier :

```powershell
Remove-Item publie, site.zip -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish src/ListeCourses -c Release -r linux-x64 --self-contained false -o publie
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory("$PWD\publie", "$PWD\site.zip")
az webapp deploy --resource-group $rg --name $app --src-path site.zip --type zip
```

- La première ligne supprime le résultat d'une publication précédente.
- `-r linux-x64` ne garde que ce qui sert sur le serveur Linux d'Azure, ce qui réduit
  le ZIP d'environ 43 Mo à 19 Mo.
- Le ZIP est créé avec la classe .NET `ZipFile` plutôt qu'avec `Compress-Archive`.
  Sous Windows PowerShell 5.1, `Compress-Archive` écrit des chemins avec `\`,
  que le serveur Linux refuse (erreur `Kudu Status : 400`).

Si l'envoi échoue avec `ConnectionResetError` ou `Connection aborted`, la connexion
a été coupée pendant l'envoi. Relancez simplement la dernière commande : le premier
essai réveille souvent le service de déploiement, qui était en veille.

Recommencez ces trois commandes à chaque nouvelle version du site.

## 6. Vérifier

```powershell
$adresse = az webapp show --resource-group $rg --name $app --query defaultHostName -o tsv
"https://$adresse/"
```

Ouvrez cette adresse : la page « Mes listes » doit s'afficher. Créez une liste,
puis ouvrez son lien dans un autre navigateur (ou sur votre téléphone) pour
vérifier le partage et la mise à jour en temps réel.

Le premier chargement peut prendre jusqu'à une minute. L'offre F1 met le site en
veille, et la base serverless se met en pause quand elle n'est pas utilisée.

En cas de problème, affichez les journaux du site :

```powershell
az webapp log config --resource-group $rg --name $app --docker-container-logging filesystem
az webapp log tail --resource-group $rg --name $app
```

## 7. L'appli Android

Mettez l'adresse obtenue à l'étape 6 dans `src/ListeCourses.Mobile/Configuration.cs`
(avec le `/` final), puis régénérez l'APK (voir le README).

## Tout supprimer

```powershell
az group delete --name $rg
```

## Pour aller plus loin

- **Identité managée** : le site peut se connecter à Azure SQL sans mot de passe
  dans la chaîne de connexion. C'est plus sûr, mais un peu plus long à configurer.
- **Déploiement automatique** : une action GitHub peut publier le site à chaque push.
- **Migrations EF Core** : elles permettent de faire évoluer les tables sans perdre
  les données (aujourd'hui, les tables sont créées par `EnsureCreated()`).
