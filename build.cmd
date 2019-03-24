@echo off
REM The reason we don't use dotnet build is that dotnet build doesn't support COM references yet https://github.com/0xd4d/dnSpy/issues/1053
REM dotnet build -c Release -f net472
REM dotnet publish -c Release -f netcoreapp3.0 -r win-x86 --self-contained
REM dotnet publish -c Release -f netcoreapp3.0 -r win-x64 --self-contained

REM .NET Framework x86 and x64
msbuild -v:m -t:Restore || goto :error
msbuild -v:m -t:Build -p:Configuration=Release -p:TargetFramework=net472 || goto :error

REM .NET Core x86
msbuild -v:m -t:Restore -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x86 -p:SelfContained=True || goto :error
msbuild -v:m -t:Publish -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x86 -p:SelfContained=True || goto :error

REM .NET Core x64
msbuild -v:m -t:Restore -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x64 -p:SelfContained=True || goto :error
msbuild -v:m -t:Publish -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x64 -p:SelfContained=True || goto :error
goto :EOF

:error
exit /b %errorlevel%
