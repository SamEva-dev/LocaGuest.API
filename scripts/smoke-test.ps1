param(
  [string]$BaseUrl = "https://localhost:5001",
  [string]$Token = ""
)

Write-Host "=== LocaGuest API Smoke Test ===" -ForegroundColor Cyan

$headers = @{}
if ($Token -ne "") {
  $headers["Authorization"] = "Bearer $Token"
}

function Invoke-Api([string]$method, [string]$url) {
  try {
    Write-Host "-> $method $url" -ForegroundColor Yellow
    $resp = Invoke-RestMethod -Method $method -Uri $url -Headers $headers -SkipCertificateCheck
    $json = $resp | ConvertTo-Json -Depth 6
    Write-Host $json
  }
  catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
      $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $body = $reader.ReadToEnd()
      Write-Host $body -ForegroundColor DarkRed
    }
  }
}

# Health
Invoke-Api GET "$BaseUrl/health"

# Dashboard
Invoke-Api GET "$BaseUrl/api/dashboard/summary"
Invoke-Api GET "$BaseUrl/api/dashboard/activities?limit=5"
Invoke-Api GET "$BaseUrl/api/dashboard/deadlines"
Invoke-Api GET "$BaseUrl/api/dashboard/charts/occupancy?year=2025"
Invoke-Api GET "$BaseUrl/api/dashboard/charts/revenue?year=2025"

# Lists
Invoke-Api GET "$BaseUrl/api/properties?page=1&pageSize=5"
Invoke-Api GET "$BaseUrl/api/tenants?page=1&pageSize=5"

Write-Host "=== Done ===" -ForegroundColor Cyan
