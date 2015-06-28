rmdir /s /q %~dp0..\ICSharpCode.Decompiler\bin
rmdir /s /q %~dp0..\ICSharpCode.Decompiler\obj
nuget restore %~dp0..\ILSpy.sln || exit /b 1
%windir%\microsoft.net\framework\v4.0.30319\msbuild %~dp0..\ICSharpCode.Decompiler\ICSharpCode.Decompiler.csproj /p:Configuration=Release "/p:Platform=Any CPU" /p:BuildNuGetPackage=True || exit /b 1
nuget pack %~dp0ICSharpCode.Decompiler.nuspec /Symbols || exit /b 1
