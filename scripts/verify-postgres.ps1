param(
  [string]$ApiBaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

$health = (Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/health").Content | ConvertFrom-Json

Write-Host "Storage provider: $($health.storage.provider)"
Write-Host "Configured: $($health.storage.configured)"

if ($health.storage.provider -ne "postgres") {
  throw "Backend is not using PostgreSQL yet. Check ConnectionStrings:Postgres in server/appsettings.Local.json."
}

Write-Host "PostgreSQL storage is active."
