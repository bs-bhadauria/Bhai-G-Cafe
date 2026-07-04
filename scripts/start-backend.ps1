$ErrorActionPreference = "Stop"

$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
  throw ".NET SDK was not found at $dotnet"
}

$serverDir = Join-Path $PSScriptRoot "..\server"
Push-Location $serverDir
try {
  & $dotnet restore
  & $dotnet run --launch-profile BhaiGCafe.Api
}
finally {
  Pop-Location
}
