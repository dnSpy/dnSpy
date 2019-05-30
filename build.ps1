Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -UseBasicParsing -OutFile "$env:temp\dotnet-install.ps1"
# Update global.json if this version number gets updated
& $env:temp\dotnet-install.ps1 -Channel 3.0 -Version 3.0.100-preview5-011568 -InstallDir "$env:ProgramFiles\dotnet" -Architecture x64
