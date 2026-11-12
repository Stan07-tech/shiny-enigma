# Publie le site sur Azure App Service (Linux) : publication, ZIP, envoi.
#
# Utilisation, depuis le dossier du dépôt :
#   powershell -ExecutionPolicy Bypass -File .\publier.ps1 -GroupeRessources $rg -Application $app
#
# "-ExecutionPolicy Bypass" autorise ce script pour cette exécution seulement
# (par défaut, Windows bloque les scripts .ps1).

param(
    [Parameter(Mandatory)] [string] $GroupeRessources,
    [Parameter(Mandatory)] [string] $Application
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$dossier = Join-Path $PSScriptRoot 'publie'
$zip = Join-Path $PSScriptRoot 'site.zip'
Remove-Item $dossier, $zip -Recurse -Force -ErrorAction SilentlyContinue

Write-Host '1/3 Publication du site...' -ForegroundColor Cyan
dotnet publish src/ListeCourses -c Release -r linux-x64 --self-contained false -o $dossier
if ($LASTEXITCODE -ne 0) { throw 'La publication a échoué.' }

Write-Host '2/3 Création du ZIP...' -ForegroundColor Cyan
# On ajoute les fichiers un par un pour écrire les chemins avec des "/" :
# Compress-Archive et ZipFile.CreateFromDirectory de Windows PowerShell 5.1
# écrivent des "\", que le serveur Linux d'Azure refuse.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $dossier -Recurse -File | ForEach-Object {
        $nom = $_.FullName.Substring($dossier.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $nom) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

Write-Host '3/3 Envoi vers Azure...' -ForegroundColor Cyan
az webapp deploy --resource-group $GroupeRessources --name $Application --src-path $zip --type zip
if ($LASTEXITCODE -ne 0) { throw "L'envoi vers Azure a échoué." }

Write-Host 'Site publié.' -ForegroundColor Green
