Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

#
# Until .NET Core 3.0 is released, use the latest daily build
#
$dotnetsdkZipFile = "dotnetsdk.zip"
Write-Host "Downloading latest build of .NET Core"
Invoke-WebRequest "https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x86.zip" -OutFile "$dotnetsdkZipFile"
$tempPath = [System.IO.Path]::GetTempPath()
$dotnetPath = "$tempPath" + [System.IO.Path]::GetRandomFileName()
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
