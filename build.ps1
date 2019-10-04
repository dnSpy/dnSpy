param([string]$buildtfm = 'all')
$ErrorActionPreference = 'Stop'

$net_tfm = 'net472'
$netcore_tfm = 'netcoreapp3.0'
$configuration = 'Release'
$net_baseoutput = "dnSpy\dnSpy\bin\$configuration"
$apphostpatcher_dir = "Build\AppHostPatcher"

#
# The reason we don't use dotnet build is that dotnet build doesn't support COM references yet https://github.com/0xd4d/dnSpy/issues/1053
#
# dotnet build -c $configuration -f $net_tfm
# ...
# dotnet publish -c $configuration -f $netcore_tfm -r win-x86 --self-contained
# ...
# dotnet publish -c $configuration -f $netcore_tfm -r win-x64 --self-contained
# ...

function Build-NetFramework {
	Write-Host 'Building .NET Framework x86 and x64 binaries'

	$outdir = "$net_baseoutput\$net_tfm"

	msbuild -v:m -restore -t:Build -p:Configuration=$configuration -p:TargetFramework=$net_tfm
	if ($LASTEXITCODE) { exit $LASTEXITCODE }

	# move all files to a bin sub dir but keep the exe files
	Rename-Item $outdir bin
	New-Item -ItemType Directory $outdir > $null
	Move-Item $net_baseoutput\bin $outdir
	foreach ($filename in 'dnSpy-x86.exe', 'dnSpy-x86.exe.config', 'dnSpy-x86.pdb',
			 'dnSpy.exe', 'dnSpy.exe.config', 'dnSpy.pdb',
			 'dnSpy.Console.exe', 'dnSpy.Console.exe.config', 'dnSpy.Console.pdb') {
		Move-Item $outdir\bin\$filename $outdir
	}
}

function Build-NetCore {
	param([string]$arch)

	Write-Host "Building .NET Core $arch binaries"

	$rid = "win-$arch"
	$outdir = "$net_baseoutput\$netcore_tfm\$rid"
	$publishDir = "$outdir\publish"

	msbuild -v:m -restore -t:Publish -p:Configuration=$configuration -p:TargetFramework=$netcore_tfm -p:RuntimeIdentifier=$rid -p:SelfContained=True
	if ($LASTEXITCODE) { exit $LASTEXITCODE }

	# move all files to a bin sub dir but keep the exe apphosts
	$tmpbin = 'tmpbin'
	Rename-Item $publishDir $tmpbin
	New-Item -ItemType Directory $publishDir > $null
	Move-Item $outdir\$tmpbin $publishDir
	Rename-Item $publishDir\$tmpbin bin
	foreach ($exe in 'dnSpy.exe', 'dnSpy.Console.exe') {
		Move-Item $publishDir\bin\$exe $publishDir
		& $apphostpatcher_dir\bin\$configuration\$net_tfm\AppHostPatcher.exe $publishDir\$exe -d bin
		if ($LASTEXITCODE) { exit $LASTEXITCODE }
	}
}

$buildNet	  = $buildtfm -eq 'all' -or $buildtfm -eq 'net'
$buildCoreX86 = $buildtfm -eq 'all' -or $buildtfm -eq 'core-x86'
$buildCoreX64 = $buildtfm -eq 'all' -or $buildtfm -eq 'core-x64'

if ($buildCoreX86 -or $buildCoreX64) {
	msbuild -v:m -restore -t:Build -p:Configuration=$configuration -p:TargetFramework=$net_tfm $apphostpatcher_dir\AppHostPatcher.csproj
	if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

if ($buildNet) {
	Build-NetFramework
}

if ($buildCoreX86) {
	Build-NetCore x86
}

if ($buildCoreX64) {
	Build-NetCore x64
}
