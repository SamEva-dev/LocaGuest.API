# Script pour vérifier les données dans la DB après test

Write-Host "`n=== VERIFICATION BASE DE DONNEES ===" -ForegroundColor Cyan

$dbPath = "locaguest.db"

if (Test-Path $dbPath) {
    Write-Host "`nBase de données trouvée: $dbPath" -ForegroundColor Green
    
    # Utiliser EF Core pour lire les données
    Write-Host "`nExécution de dotnet ef dbcontext scaffold..." -ForegroundColor Yellow
    
    # Alternative: Utiliser DB Browser for SQLite ou DBeaver
    Write-Host "`nPour voir les données, utilisez:" -ForegroundColor Yellow
    Write-Host "1. DB Browser for SQLite: https://sqlitebrowser.org/"
    Write-Host "2. Visual Studio: Server Explorer → Add Connection → SQLite"
    Write-Host "3. VS Code: Extension SQLite Viewer"
    
    Write-Host "`nOu utilisez les requêtes suivantes dans votre outil:" -ForegroundColor Cyan
    Write-Host @"

-- Vérifier les locataires
SELECT Id, Code, FirstName, LastName, Email, Status 
FROM Tenants 
ORDER BY CreatedAtUtc DESC 
LIMIT 5;

-- Vérifier les biens
SELECT Id, Code, Name, Address, Rent, Status 
FROM Properties 
ORDER BY CreatedAtUtc DESC 
LIMIT 5;

-- Vérifier les séquences
SELECT TenantId, EntityPrefix, LastValue, UpdatedAtUtc
FROM TenantSequences;

"@

} else {
    Write-Host "`nERREUR: Base de données non trouvée!" -ForegroundColor Red
    Write-Host "Chemin attendu: $((Get-Location).Path)\$dbPath"
}

Write-Host "`n=== FIN VERIFICATION ===" -ForegroundColor Cyan
