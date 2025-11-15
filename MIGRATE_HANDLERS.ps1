# Script PowerShell pour migrer automatiquement tous les handlers vers UnitOfWork
$files = @(
    "src\LocaGuest.Application\Features\Rentability\Commands\SaveScenario\SaveRentabilityScenarioCommandHandler.cs",
    "src\LocaGuest.Application\Features\Rentability\Commands\DeleteScenario\DeleteRentabilityScenarioCommandHandler.cs",
    "src\LocaGuest.Application\Features\Rentability\Commands\CloneScenario\CloneRentabilityScenarioCommandHandler.cs",
    "src\LocaGuest.Application\Features\Rentability\Commands\RestoreVersion\RestoreScenarioVersionCommandHandler.cs",
    "src\LocaGuest.Application\Features\Tenants\Queries\GetTenants\GetTenantsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Tenants\Queries\GetTenant\GetTenantQueryHandler.cs",
    "src\LocaGuest.Application\Features\Tenants\Queries\GetAvailableTenants\GetAvailableTenantsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Properties\Queries\GetProperties\GetPropertiesQueryHandler.cs",
    "src\LocaGuest.Application\Features\Properties\Queries\GetProperty\GetPropertyQueryHandler.cs",
    "src\LocaGuest.Application\Features\Properties\Queries\GetFinancialSummary\GetFinancialSummaryQueryHandler.cs",
    "src\LocaGuest.Application\Features\Properties\Queries\GetPropertyContracts\GetPropertyContractsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Properties\Queries\GetPropertyPayments\GetPropertyPaymentsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Contracts\Queries\GetAllContracts\GetAllContractsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Contracts\Queries\GetContractStats\GetContractStatsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Analytics\Queries\GetRevenueEvolution\GetRevenueEvolutionQueryHandler.cs",
    "src\LocaGuest.Application\Features\Analytics\Queries\GetAvailableYears\GetAvailableYearsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Analytics\Queries\GetProfitabilityStats\GetProfitabilityStatsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Analytics\Queries\GetPropertyPerformance\GetPropertyPerformanceQueryHandler.cs",
    "src\LocaGuest.Application\Features\Rentability\Queries\GetUserScenarios\GetUserScenariosQueryHandler.cs",
    "src\LocaGuest.Application\Features\Rentability\Queries\GetScenarioVersions\GetScenarioVersionsQueryHandler.cs",
    "src\LocaGuest.Application\Features\Settings\Queries\GetUserSettings\GetUserSettingsQueryHandler.cs"
)

foreach ($file in $files) {
    $path = Join-Path $PSScriptRoot $file
    if (Test-Path $path) {
        $content = Get-Content $path -Raw
        
        # Ajouter using Domain.Repositories si pas présent
        if ($content -notmatch "using LocaGuest.Domain.Repositories;") {
            $content = $content -replace "(using LocaGuest.Domain.*;)", "`$1`nusing LocaGuest.Domain.Repositories;"
        }
        
        # Remplacer ILocaGuestDbContext par IUnitOfWork + ILocaGuestDbContext (pour queries qui lisent directement)
        $content = $content -replace "private readonly ILocaGuestDbContext _context;", "private readonly IUnitOfWork _unitOfWork;`n    private readonly ILocaGuestDbContext _context;"
        
        # Mettre à jour le constructeur
        $content = $content -replace "public \w+\(\s+ILocaGuestDbContext context,", "public `$0(`n        IUnitOfWork unitOfWork,`n        ILocaGuestDbContext context,"
        $content = $content -replace "{\s+_context = context;", "{`n        _unitOfWork = unitOfWork;`n        _context = context;"
        
        # Remplacer SaveChangesAsync par CommitAsync
        $content = $content -replace "await _context\.SaveChangesAsync\(([^)]*)\);", "await _unitOfWork.CommitAsync(`$1);"
        
        # Sauvegarder
        Set-Content $path $content -NoNewline
        Write-Host "Migrated: $file" -ForegroundColor Green
    }
}

Write-Host "`nMigration complete!" -ForegroundColor Cyan
