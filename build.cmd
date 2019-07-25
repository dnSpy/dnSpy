@echo off
echo dotnet SDK version
dotnet --version
msbuild -version
REM not in the path
REM csc -version
REM vbc -version

REM The reason we don't use dotnet build is that dotnet build doesn't support COM references yet https://github.com/0xd4d/dnSpy/issues/1053
REM dotnet build -c Release -f net472
REM ...add commands to move files to bin sub dir, see below...
REM dotnet publish -c Release -f netcoreapp3.0 -r win-x86 --self-contained
REM ...add commands to patch apphost and move files to bin sub dir, see below...
REM dotnet publish -c Release -f netcoreapp3.0 -r win-x64 --self-contained
REM ...add commands to patch apphost and move files to bin sub dir, see below...

echo Building .NET Framework x86 and x64 binaries
msbuild -v:m -restore -t:Build -p:Configuration=Release -p:TargetFramework=net472 || goto :error
REM move all files to a bin sub dir but keep the exe files
ren dnSpy\dnSpy\bin\Release\net472 bin || goto :error
mkdir dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\bin dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy-x86.exe dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy-x86.exe.config dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy-x86.pdb dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy.exe dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy.exe.config dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy.pdb dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy.Console.exe dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy.Console.exe.config dnSpy\dnSpy\bin\Release\net472 || goto :error
move dnSpy\dnSpy\bin\Release\net472\bin\dnSpy.Console.pdb dnSpy\dnSpy\bin\Release\net472 || goto :error

echo Building .NET Core x86 binaries
msbuild -v:m -restore -t:Publish -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x86 -p:SelfContained=True || goto :error
REM move all files to a bin sub dir but keep the exe apphosts
ren dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish tmpbin || goto :error
mkdir dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish || goto :error
move dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\tmpbin dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish || goto :error
ren dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish\tmpbin bin || goto :error
move dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish\bin\dnSpy.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish || goto :error
move dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish\bin\dnSpy.Console.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish || goto :error
Build\AppHostPatcher\bin\Release\net472\AppHostPatcher.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish\dnSpy.exe -d bin || goto :error
Build\AppHostPatcher\bin\Release\net472\AppHostPatcher.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x86\publish\dnSpy.Console.exe -d bin || goto :error

echo Building .NET Core x64 binaries
msbuild -v:m -restore -t:Publish -p:Configuration=Release -p:TargetFramework=netcoreapp3.0 -p:RuntimeIdentifier=win-x64 -p:SelfContained=True || goto :error
REM move all files to a bin sub dir but keep the exe apphosts
ren dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish tmpbin || goto :error
mkdir dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish || goto :error
move dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\tmpbin dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish || goto :error
ren dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish\tmpbin bin || goto :error
move dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish\bin\dnSpy.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish || goto :error
move dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish\bin\dnSpy.Console.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish || goto :error
Build\AppHostPatcher\bin\Release\net472\AppHostPatcher.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish\dnSpy.exe -d bin || goto :error
Build\AppHostPatcher\bin\Release\net472\AppHostPatcher.exe dnSpy\dnSpy\bin\Release\netcoreapp3.0\win-x64\publish\dnSpy.Console.exe -d bin || goto :error

goto :EOF

:error
exit /b %errorlevel%
