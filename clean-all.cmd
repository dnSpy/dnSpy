@echo off

pushd Extensions\ILSpy.Decompiler\NRefactory && git clean -xdf && popd || goto :error
pushd Libraries\ICSharpCode.TreeView && git clean -xdf && popd || goto :error
pushd Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler && git clean -xdf && popd || goto :error
pushd dnSpy\dnSpy.Images && git clean -xdf && popd || goto :error
pushd Extensions\dnSpy.Debugger\netcorefiles && git clean -xdf && popd || goto :error
pushd dnSpy\Roslyn\Roslyn.ExpressionCompiler && git clean -xdf && popd || goto :error
pushd Extensions\dnSpy.Debugger\Mono.Debugger.Soft && git clean -xdf && popd || goto :error
git clean -xdf || goto :error

goto :EOF

:error
exit /b %errorlevel%
