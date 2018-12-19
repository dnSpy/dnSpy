/*
    Copyright (C) 2014-2018 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents {
	sealed class AssemblyResolver : IAssemblyResolver {
		readonly DsDocumentService documentService;
		readonly FailedAssemblyResolveCache failedAssemblyResolveCache;
		readonly DotNetCorePathProvider dotNetCorePathProvider;

		static readonly UTF8String mscorlibName = new UTF8String("mscorlib");
		static readonly UTF8String systemRuntimeName = new UTF8String("System.Runtime");
		static readonly UTF8String netstandardName = new UTF8String("netstandard");
		static readonly UTF8String aspNetCoreName = new UTF8String("Microsoft.AspNetCore");
		// netstandard1.5 also uses this version number, but assume it's .NET Core
		static readonly Version minSystemRuntimeNetCoreVersion = new Version(4, 1, 0, 0);
		static readonly Version invalidMscorlibVersion = new Version(255, 255, 255, 255);

		const string TFM_netframework = ".NETFramework";
		const string TFM_netcoreapp = ".NETCoreApp";
		const string TFM_netstandard = ".NETStandard";
		const string UnityEngineFilename = "UnityEngine.dll";

		public AssemblyResolver(DsDocumentService documentService) {
			this.documentService = documentService;
			failedAssemblyResolveCache = new FailedAssemblyResolveCache();
			dotNetCorePathProvider = new DotNetCorePathProvider();
		}

		// PERF: Sometimes various pieces of code tries to resolve the same assembly and this
		// assembly isn't found. This class caches these failed resolves so null is returned
		// without searching for the assembly. It forgets about it after some number of seconds
		// in case the user adds the assembly to one of the search paths or loads it in dnSpy.
		sealed class FailedAssemblyResolveCache {
			const int MAX_CACHE_TIME_SECONDS = 10;
			readonly HashSet<IAssembly> failedAsms = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			readonly object lockObj = new object();
			volatile bool isEmpty = true;
			DateTime lastTime = DateTime.UtcNow;

			public bool IsFailed(IAssembly asm) {
				if (isEmpty)
					return false;
				lock (lockObj) {
					if (failedAsms.Count == 0)
						return false;
					var now = DateTime.UtcNow;
					bool isOld = (now - lastTime).TotalSeconds > MAX_CACHE_TIME_SECONDS;
					if (isOld) {
						isEmpty = true;
						failedAsms.Clear();
						return false;
					}
					return failedAsms.Contains(asm);
				}
			}

			public void MarkFailed(IAssembly asm) {
				// Use ToAssemblyRef() to prevent storing a reference to an AssemblyDef
				var asmKey = asm.ToAssemblyRef();
				lock (lockObj) {
					if (failedAsms.Count == 0)
						lastTime = DateTime.UtcNow;
					isEmpty = false;
					failedAsms.Add(asmKey);
				}
			}
		}

		AssemblyDef IAssemblyResolver.Resolve(IAssembly assembly, ModuleDef sourceModule) =>
			Resolve(assembly, sourceModule)?.AssemblyDef;

		IDsDocument Resolve(IAssembly assembly, ModuleDef sourceModule) {
			if (assembly.IsContentTypeWindowsRuntime) {
				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var document = ResolveWinMD(assembly, sourceModule);
				if (document == null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return document;
			}
			else {
				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var document = ResolveNormal(assembly, sourceModule);
				if (document == null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return document;
			}
		}

		enum FrameworkKind {
			Unknown,
			// This is .NET Framework 1.0-3.5. Search in V2 GAC, not V4 GAC.
			DotNetFramework2,
			// This is .NET Framework 4.0 and later. Search in V4 GAC, not V2 GAC.
			DotNetFramework4,
			DotNetCore,
			Unity,
		}

		sealed class FrameworkPathInfo {
			public readonly string Directory;
			public volatile FrameworkKind FrameworkKind;
			public volatile Version FrameworkVersion;
			public volatile bool Frozen;
			public FrameworkPathInfo(string directory) {
				Directory = directory ?? throw new ArgumentNullException(nameof(directory));
				FrameworkKind = FrameworkKind.Unknown;
			}
		}

		// An array (instead of a dict) is used because it's expected to be small. We can also
		// iterate over it without a lock. Since we use an array we don't need a lock and just
		// overwrite the field (we risk losing a new element but we'll survive if that happens).
		volatile FrameworkPathInfo[] frameworkInfos = Array.Empty<FrameworkPathInfo>();
		FrameworkPathInfo Add(FrameworkPathInfo info) {
			var current = frameworkInfos;
			var newInfos = new FrameworkPathInfo[current.Length + 1];
			for (int i = 0; i < current.Length; i++) {
				var item = current[i];
				if (item.Directory == info.Directory)
					return item;
				newInfos[i] = item;
			}
			newInfos[newInfos.Length - 1] = info;
			frameworkInfos = newInfos;
			return info;
		}
		internal void OnAssembliesCleared() => frameworkInfos = Array.Empty<FrameworkPathInfo>();

		FrameworkKind GetFrameworkKind(ModuleDef module, out Version netCoreVersion, out string sourceModuleDirectoryHint) {
			if (module == null) {
				netCoreVersion = null;
				sourceModuleDirectoryHint = null;
				return FrameworkKind.Unknown;
			}

			var sourceFilename = module.Location;
			if (!string.IsNullOrEmpty(sourceFilename)) {
				bool isExe = (module.Characteristics & Characteristics.Dll) == 0;
				foreach (var info in frameworkInfos) {
					if (FileUtils.IsFileInDir(info.Directory, sourceFilename)) {
						// The same 'module' could be passed in here multiple times, but we can't save the module instance
						// anywhere so only update info if it's an EXE and then mark it as frozen.
						if (isExe && !info.Frozen) {
							info.Frozen = true;
							var newFwkKind = GetFrameworkKind_TargetFrameworkAttribute(module, out var frameworkName, out var fwkVersion);
							if (newFwkKind == FrameworkKind.Unknown)
								newFwkKind = GetFrameworkKind_AssemblyRefs(module, frameworkName, out fwkVersion);
							if (newFwkKind != FrameworkKind.Unknown) {
								info.FrameworkKind = Best(info.FrameworkKind, newFwkKind);
								if (info.FrameworkKind == FrameworkKind.DotNetCore)
									info.FrameworkVersion = fwkVersion;
							}
						}
						if (info.FrameworkKind == FrameworkKind.DotNetCore)
							netCoreVersion = info.FrameworkVersion;
						else
							netCoreVersion = null;
						sourceModuleDirectoryHint = info.Directory;
						return info.FrameworkKind;
					}
				}

				var fwkKind = GetRuntimeFrameworkKind(sourceFilename, out var frameworkVersion);
				if (fwkKind != FrameworkKind.Unknown) {
					if (fwkKind == FrameworkKind.DotNetCore)
						netCoreVersion = frameworkVersion;
					else
						netCoreVersion = null;
					sourceModuleDirectoryHint = null;
					return fwkKind;
				}

				var fwkInfo = new FrameworkPathInfo(Path.GetDirectoryName(sourceFilename));
				fwkInfo.FrameworkKind = GetFrameworkKind_Directory(fwkInfo.Directory);
				if (fwkInfo.FrameworkKind == FrameworkKind.Unknown) {
					fwkInfo.FrameworkKind = GetFrameworkKind_TargetFrameworkAttribute(module, out var frameworkName, out var fwkVersion);
					fwkInfo.FrameworkVersion = fwkVersion;
					if (fwkInfo.FrameworkKind == FrameworkKind.Unknown) {
						fwkInfo.FrameworkKind = GetFrameworkKind_AssemblyRefs(module, frameworkName, out fwkVersion);
						fwkInfo.FrameworkVersion = fwkVersion;
					}
				}
				fwkInfo.Frozen = isExe;
				fwkInfo = Add(fwkInfo);
				if (fwkInfo.FrameworkKind == FrameworkKind.DotNetCore)
					netCoreVersion = fwkInfo.FrameworkVersion;
				else
					netCoreVersion = null;
				sourceModuleDirectoryHint = fwkInfo.Directory;
				return fwkInfo.FrameworkKind;
			}

			netCoreVersion = null;
			sourceModuleDirectoryHint = null;
			return FrameworkKind.Unknown;
		}

		static FrameworkKind Best(FrameworkKind a, FrameworkKind b) {
			if (a == FrameworkKind.DotNetCore || b == FrameworkKind.DotNetCore)
				return FrameworkKind.DotNetCore;
			if (a == FrameworkKind.Unity || b == FrameworkKind.Unity)
				return FrameworkKind.Unity;
			if (a == FrameworkKind.DotNetFramework4 || b == FrameworkKind.DotNetFramework4)
				return FrameworkKind.DotNetFramework4;
			if (a == FrameworkKind.DotNetFramework2 || b == FrameworkKind.DotNetFramework2)
				return FrameworkKind.DotNetFramework2;
			Debug.Assert(a == FrameworkKind.Unknown && b == FrameworkKind.Unknown);
			return FrameworkKind.Unknown;
		}

		FrameworkKind GetRuntimeFrameworkKind(string filename, out Version netCoreVersion) {
			foreach (var gacPath in GacInfo.GacPaths) {
				if (FileUtils.IsFileInDir(gacPath.Path, filename)) {
					netCoreVersion = null;
					Debug.Assert(gacPath.Version == GacVersion.V2 || gacPath.Version == GacVersion.V4);
					return gacPath.Version == GacVersion.V2 ? FrameworkKind.DotNetFramework2 : FrameworkKind.DotNetFramework4;
				}
			}

			netCoreVersion = dotNetCorePathProvider.TryGetDotNetCoreVersion(filename);
			if (netCoreVersion != null)
				return FrameworkKind.DotNetCore;

			netCoreVersion = null;
			return FrameworkKind.Unknown;
		}

		static FrameworkKind GetFrameworkKind_Directory(string directory) {
			if (File.Exists(Path.Combine(directory, UnityEngineFilename)))
				return FrameworkKind.Unity;
			return FrameworkKind.Unknown;
		}

		FrameworkKind GetFrameworkKind_TargetFrameworkAttribute(ModuleDef module, out string frameworkName, out Version version) {
			var asm = module.Assembly;
			if (asm != null && asm.TryGetOriginalTargetFrameworkAttribute(out frameworkName, out version, out _)) {
				if (frameworkName == TFM_netframework)
					return version.Major < 4 ? FrameworkKind.DotNetFramework2 : FrameworkKind.DotNetFramework4;
				if (frameworkName == TFM_netcoreapp)
					return FrameworkKind.DotNetCore;
				if (!dotNetCorePathProvider.HasDotNetCore && frameworkName == TFM_netstandard)
					return FrameworkKind.DotNetFramework4;
				return FrameworkKind.Unknown;
			}

			frameworkName = null;
			version = null;
			return FrameworkKind.Unknown;
		}

		FrameworkKind GetFrameworkKind_AssemblyRefs(ModuleDef module, string frameworkName, out Version version) {
			AssemblyRef mscorlibRef = null;
			AssemblyRef systemRuntimeRef = null;
			// ASP.NET Core *.Views assemblies don't have a TFM attribute, so grab the .NET Core version from an ASP.NET Core asm ref
			AssemblyRef aspNetCoreRef = null;
			foreach (var asmRef in module.GetAssemblyRefs()) {
				var name = asmRef.Name;
				if (name == mscorlibName) {
					if (asmRef.Version != invalidMscorlibVersion) {
						if (mscorlibRef == null || asmRef.Version > mscorlibRef.Version)
							mscorlibRef = asmRef;
					}
				}
				else if (name == systemRuntimeName) {
					if (systemRuntimeRef == null || asmRef.Version > systemRuntimeRef.Version)
						systemRuntimeRef = asmRef;
				}
				else if (name == netstandardName) {
					if (!dotNetCorePathProvider.HasDotNetCore) {
						version = null;
						return FrameworkKind.DotNetFramework4;
					}
					version = null;
					return FrameworkKind.Unknown;
				}
				else if (StartsWith(name, aspNetCoreName)) {
					if (aspNetCoreRef == null || asmRef.Version > aspNetCoreRef.Version)
						aspNetCoreRef = asmRef;
				}
			}

			if (systemRuntimeRef != null) {
				// - .NET Core:
				//		1.0: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.1: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.0: System.Runtime, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.1: System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.2: System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		3.0: System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				// - .NET Standard:
				//		1.0: System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.1: System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.2: System.Runtime, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.3: System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.4: System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.5: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.0: <it has no System.Runtime ref, just a netstandard.dll ref>
				if (frameworkName != TFM_netstandard) {
					if (module.IsClr40Exactly && systemRuntimeRef.Version >= minSystemRuntimeNetCoreVersion) {
						version = aspNetCoreRef?.Version;
						return FrameworkKind.DotNetCore;
					}
				}
			}

			version = null;
			if (mscorlibRef != null) {
				// It can't be Unity since we checked that before this method was called.
				// It can't be .NET Core since it uses System.Runtime.

				if (mscorlibRef.Version.Major >= 4)
					return FrameworkKind.DotNetFramework4;

				// If it's an exe and it's net20-net35, return that
				if ((module.Characteristics & Characteristics.Dll) == 0)
					return FrameworkKind.DotNetFramework2;

				// It's a net20-net35 dll, but it could be referenced by a net4x asm so we
				// can't return net20-net35.
			}

			return FrameworkKind.Unknown;
		}

		static bool StartsWith(UTF8String s, UTF8String value) {
			var d = s?.Data;
			var vd = value?.Data;
			if (d == null || vd == null)
				return false;
			if (d.Length < vd.Length)
				return false;
			for (int i = 0; i < vd.Length; i++) {
				if (d[i] != vd[i])
					return false;
			}
			return true;
		}

		IDsDocument ResolveNormal(IAssembly assembly, ModuleDef sourceModule) {
			var fwkKind = GetFrameworkKind(sourceModule, out var netCoreVersion, out var sourceModuleDirectoryHint);
			if (fwkKind == FrameworkKind.DotNetCore && !dotNetCorePathProvider.HasDotNetCore)
				fwkKind = FrameworkKind.DotNetFramework4;
			IDsDocument document;
			switch (fwkKind) {
			case FrameworkKind.Unknown:
			case FrameworkKind.DotNetFramework2:
			case FrameworkKind.DotNetFramework4:
				var tempAsm = assembly;
				int gacVersion;
				if (!GacInfo.HasGAC2)
					fwkKind = FrameworkKind.DotNetFramework4;
				if (fwkKind == FrameworkKind.DotNetFramework4) {
					FrameworkRedirect.ApplyFrameworkRedirectV4(ref assembly);
					gacVersion = 4;
				}
				else if (fwkKind == FrameworkKind.DotNetFramework2) {
					FrameworkRedirect.ApplyFrameworkRedirectV2(ref assembly);
					gacVersion = 2;
				}
				else {
					Debug.Assert(fwkKind == FrameworkKind.Unknown);
					FrameworkRedirect.ApplyFrameworkRedirect(ref tempAsm, sourceModule);
					// OK : System.Runtime 4.0.20.0 => 4.0.0.0
					// KO : System 4.0.0.0 => 2.0.0.0
					if (tempAsm.Version.Major >= assembly.Version.Major)
						assembly = tempAsm;
					gacVersion = -1;
				}

				var existingDocument = documentService.FindAssembly(assembly);
				if (existingDocument != null)
					return existingDocument;

				document = LookupFromSearchPaths(assembly, sourceModule, sourceModuleDirectoryHint, netCoreVersion);
				if (document != null)
					return documentService.GetOrAddCanDispose(document, assembly);

				var gacFile = GacInfo.FindInGac(assembly, gacVersion);
				if (gacFile != null)
					return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateDocument(gacFile), true, true);
				foreach (var gacPath in GacInfo.OtherGacPaths) {
					if (gacVersion == 4) {
						if (gacPath.Version != GacVersion.V4)
							continue;
					}
					else if (gacVersion == 2) {
						if (gacPath.Version != GacVersion.V2)
							continue;
					}
					else
						Debug.Assert(gacVersion == -1);
					document = TryLoadFromDir(assembly, checkVersion: true, checkPublicKeyToken: true, gacPath.Path);
					if (document != null)
						return documentService.GetOrAddCanDispose(document, assembly);
				}
				break;

			case FrameworkKind.DotNetCore:
			case FrameworkKind.Unity:
				document = LookupFromSearchPaths(assembly, sourceModule, sourceModuleDirectoryHint, netCoreVersion);
				if (document != null)
					return documentService.GetOrAddCanDispose(document, assembly);
				break;

			default:
				throw new InvalidOperationException();
			}

			return null;
		}

		IDsDocument LookupFromSearchPaths(IAssembly asmName, ModuleDef sourceModule, string sourceModuleDir, Version dotNetCoreAppVersion) {
			IDsDocument document;
			if (sourceModuleDir == null && sourceModule != null && !string.IsNullOrEmpty(sourceModule.Location)) {
				try {
					sourceModuleDir = Path.GetDirectoryName(sourceModule.Location);
				}
				catch (ArgumentException) {
				}
				catch (PathTooLongException) {
				}
			}

			if (sourceModuleDir != null) {
				document = TryFindFromDir(asmName, dirPath: sourceModuleDir);
				if (document != null)
					return document;
			}

			int bitness;
			string[] dotNetCorePaths;
			if (dotNetCoreAppVersion != null) {
				bitness = (sourceModule?.GetPointerSize(IntPtr.Size) ?? IntPtr.Size) * 8;
				dotNetCorePaths = dotNetCorePathProvider.TryGetDotNetCorePaths(dotNetCoreAppVersion, bitness);
			}
			else {
				bitness = -1;
				dotNetCorePaths = null;
			}
			if (dotNetCorePaths != null) {
				foreach (var path in dotNetCorePaths) {
					document = TryFindFromDir(asmName, dirPath: path);
					if (document != null)
						return document;
				}
			}

			if (sourceModuleDir != null) {
				document = TryLoadFromDir(asmName, checkVersion: false, checkPublicKeyToken: false, dirPath: sourceModuleDir);
				if (document != null)
					return document;
			}
			if (dotNetCorePaths != null) {
				foreach (var path in dotNetCorePaths) {
					document = TryLoadFromDir(asmName, checkVersion: false, checkPublicKeyToken: false, dirPath: path);
					if (document != null)
						return document;
				}
			}

			return null;
		}

		IDsDocument TryFindFromDir(IAssembly asmName, string dirPath) {
			string baseName;
			try {
				baseName = Path.Combine(dirPath, asmName.Name);
			}
			catch (ArgumentException) { // eg. invalid chars in asmName.Name
				return null;
			}
			return TryFindFromDir2(baseName + ".dll") ??
				   TryFindFromDir2(baseName + ".exe");
		}

		IDsDocument TryFindFromDir2(string filename) => documentService.Find(FilenameKey.CreateFullPath(filename), checkTempCache: true);

		IDsDocument TryLoadFromDir(IAssembly asmName, bool checkVersion, bool checkPublicKeyToken, string dirPath) {
			string baseName;
			try {
				baseName = Path.Combine(dirPath, asmName.Name);
			}
			catch (ArgumentException) { // eg. invalid chars in asmName.Name
				return null;
			}
			return TryLoadFromDir2(asmName, checkVersion, checkPublicKeyToken, baseName + ".dll") ??
				   TryLoadFromDir2(asmName, checkVersion, checkPublicKeyToken, baseName + ".exe");
		}

		IDsDocument TryLoadFromDir2(IAssembly asmName, bool checkVersion, bool checkPublicKeyToken, string filename) {
			if (!File.Exists(filename))
				return null;

			IDsDocument document = null;
			bool error = true;
			try {
				document = documentService.TryCreateDocument(DsDocumentInfo.CreateDocument(filename));
				if (document == null)
					return null;
				document.IsAutoLoaded = true;
				var asm = document.AssemblyDef;
				if (asm == null)
					return null;
				var flags = AssemblyNameComparerFlags.All & ~(AssemblyNameComparerFlags.Version | AssemblyNameComparerFlags.PublicKeyToken);
				if (checkVersion)
					flags |= AssemblyNameComparerFlags.Version;
				if (checkPublicKeyToken)
					flags |= AssemblyNameComparerFlags.PublicKeyToken;
				bool b = new AssemblyNameComparer(flags).Equals(asmName, asm);
				if (!b)
					return null;

				error = false;
				return document;
			}
			finally {
				if (error) {
					if (document is IDisposable)
						((IDisposable)document).Dispose();
				}
			}
		}

		IDsDocument ResolveWinMD(IAssembly assembly, ModuleDef sourceModule) {
			var existingDocument = documentService.FindAssembly(assembly);
			if (existingDocument != null)
				return existingDocument;

			foreach (var winmdPath in GacInfo.WinmdPaths) {
				string file;
				try {
					file = Path.Combine(winmdPath, assembly.Name + ".winmd");
				}
				catch (ArgumentException) {
					continue;
				}
				if (File.Exists(file))
					return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateDocument(file), true, true);
			}
			return null;
		}
	}
}
