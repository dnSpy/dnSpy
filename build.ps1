Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$dotnetsdkZipFile = "dotnetsdk.zip"
Write-Host "Downloading latest .NET Core Preview"
Invoke-WebRequest "https://download.visualstudio.microsoft.com/download/pr/78836c06-166d-4145-ae7b-da5693e36665/431a2fd34af25742527bc5cafe4d8fae/dotnet-sdk-3.0.100-preview5-011568-win-x64.zip" -OutFile "$dotnetsdkZipFile"
$tempPath = [System.IO.Path]::GetTempPath()
$dotnetPath = "$tempPath" + [System.IO.Path]::GetRandomFileName()
Write-Host "Extracting the zip file"
Expand-Archive "$dotnetsdkZipFile" -DestinationPath "$dotnetPath" -Force
del "$dotnetsdkZipFile"
$dotnetExe = "$dotnetPath\dotnet.exe"

#
# Show the version in the log
#
Invoke-Expression '&"$dotnetExe" --version'
if ($LastExitCode -ne 0) { exit 1 }

#
# Build it
#
Write-Host "Building (.NET Framework)"
Invoke-Expression '&"$dotnetExe" build -c Release -f net472'
if ($LastExitCode -ne 0) { exit 1 }

Write-Host "Building (.NET Core x86)"
Invoke-Expression '&"$dotnetExe" publish -c Release -f netcoreapp3.0 -r win-x86 --self-contained'
if ($LastExitCode -ne 0) { exit 1 }

Write-Host "Building (.NET Core x64)"
Invoke-Expression '&"$dotnetExe" publish -c Release -f netcoreapp3.0 -r win-x64 --self-contained'
if ($LastExitCode -ne 0) { exit 1 }
