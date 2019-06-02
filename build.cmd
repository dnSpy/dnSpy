@echo off

REM TODO remove this when appveyor build image has .NET Core 3.0 installed
powershell -NoLogo -NoProfile -File build.ps1 -ExecutionPolicy Bypass


echo dotnet SDK version
dotnet --version

REM The reason we don't use dotnet build is that dotnet build doesn't support COM references yet https://github.com/0xd4d/dnSpy/issues/1053
REM dotnet build -c Release -f net472
REM dotnet publish -c Release -f netcoreapp3.0 -r win-x86 --self-contained
REM dotnet publish -c Release -f netcoreapp3.0 -r win-x64 --self-contained

echo Building .NET Framework x86 and x64 binaries
msbuild -v:m -t:Restore -p:Configuration=Release -p:TargetFramework=net472 || goto :error
msbuild -v:m -t:Build   -p:Configuration=Release -p:TargetFramework=net472 || goto :error

echo Building .NET Core x86 binaries
msbuild -v:m -t:Restore -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x86 -p:SelfContained=True || goto :error
msbuild -v:m -t:Publish -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x86 -p:SelfContained=True || goto :error

echo Building .NET Core x64 binaries
msbuild -v:m -t:Restore -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x64 -p:SelfContained=True || goto :error
msbuild -v:m -t:Publish -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x64 -p:SelfContained=True || goto :error
goto :EOF

:error
exit /b %errorlevel%
