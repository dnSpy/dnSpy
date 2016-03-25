/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.IO;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Shared.Files;

namespace dnSpy.Files {
	sealed class AssemblyResolver : IAssemblyResolver {
		readonly FileManager fileManager;
		readonly FailedAssemblyResolveCache failedAssemblyResolveCache;

		static readonly Version invalidMscorlibVersion = new Version(255, 255, 255, 255);
		static readonly Version newMscorlibVersion = new Version(4, 0, 0, 0);

		public AssemblyResolver(FileManager fileManager) {
			this.fileManager = fileManager;
			this.failedAssemblyResolveCache = new FailedAssemblyResolveCache();
		}

		public void AddSearchPath(string s) {
			lock (asmSearchPathsLockObj) {
				asmSearchPaths.Add(s);
				asmSearchPathsArray = asmSearchPaths.ToArray();
			}
		}
		readonly object asmSearchPathsLockObj = new object();
		readonly List<string> asmSearchPaths = new List<string>();
		string[] asmSearchPathsArray = new string[0];

		// PERF: Sometimes various pieces of code tries to resolve the same assembly and this
		// assembly isn't found. This class caches these failed resolves so null is returned
		// without searching for the assembly. It forgets about it after some number of seconds
		// in case the user adds the assembly to one of the search paths or loads it in dnSpy.
		sealed class FailedAssemblyResolveCache {
			const int MAX_CACHE_TIME_SECONDS = 10;
			readonly HashSet<IAssembly> failedAsms = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			readonly object lockObj = new object();
			DateTime lastTime = DateTime.UtcNow;

			public bool IsFailed(IAssembly asm) {
				lock (lockObj) {
					if (failedAsms.Count == 0)
						return false;
					var now = DateTime.UtcNow;
					bool isOld = (now - lastTime).TotalSeconds > MAX_CACHE_TIME_SECONDS;
					if (isOld) {
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
					failedAsms.Add(asmKey);
				}
			}
		}

		bool IAssemblyResolver.AddToCache(AssemblyDef asm) {
			return false;
		}

		void IAssemblyResolver.Clear() {
		}

		bool IAssemblyResolver.Remove(AssemblyDef asm) {
			return false;
		}

		AssemblyDef IAssemblyResolver.Resolve(IAssembly assembly, ModuleDef sourceModule) {
			var file = Resolve(assembly, sourceModule);
			return file == null ? null : file.AssemblyDef;
		}

		public IDnSpyFile Resolve(IAssembly assembly, ModuleDef sourceModule = null) {
			var tempAsm = assembly;
			FrameworkRedirect.ApplyFrameworkRedirect(ref tempAsm, sourceModule);
			// OK : System.Runtime 4.0.20.0 => 4.0.0.0
			// BAD: System 4.0.0.0 => 2.0.0.0
			if (tempAsm.Version.Major >= assembly.Version.Major)
				assembly = tempAsm;

			if (assembly.IsContentTypeWindowsRuntime) {
				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var file = ResolveWinMD(assembly, sourceModule);
				if (file == null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return file;
			}
			else {
				// WinMD files have a reference to mscorlib but its version is always 255.255.255.255
				// since mscorlib isn't really loaded. The resolver only loads exact versions, so
				// we must change the version or the resolve will fail.
				if (assembly.Name == mscorlibName && assembly.Version == invalidMscorlibVersion)
					assembly = new AssemblyNameInfo(assembly) { Version = newMscorlibVersion };

				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var file = ResolveNormal(assembly, sourceModule);
				if (file == null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return file;
			}
		}
		static readonly UTF8String mscorlibName = new UTF8String("mscorlib");

		IDnSpyFile ResolveNormal(IAssembly assembly, ModuleDef sourceModule) {
			var existingFile = fileManager.FindAssembly(assembly);
			if (existingFile != null)
				return existingFile;

			var file = LookupFromSearchPaths(assembly, sourceModule, true);
			if (file != null)
				return fileManager.GetOrAddCanDispose(file);

			if (fileManager.Settings.UseGAC) {
				var gacFile = GacInfo.FindInGac(assembly);
				if (gacFile != null)
					return fileManager.TryGetOrCreateInternal(DnSpyFileInfo.CreateFile(gacFile), true, true);
				foreach (var path in GacInfo.OtherGacPaths) {
					file = TryLoadFromDir(assembly, true, path);
					if (file != null)
						return fileManager.GetOrAddCanDispose(file);
				}
			}

			file = LookupFromSearchPaths(assembly, sourceModule, false);
			if (file != null)
				return fileManager.GetOrAddCanDispose(file);

			return null;
		}

		IDnSpyFile LookupFromSearchPaths(IAssembly asmName, ModuleDef sourceModule, bool exactCheck) {
			IDnSpyFile file;
			string sourceModuleDir = null;
			if (sourceModule != null && File.Exists(sourceModule.Location)) {
				sourceModuleDir = Path.GetDirectoryName(sourceModule.Location);
				file = TryLoadFromDir(asmName, exactCheck, sourceModuleDir);
				if (file != null)
					return file;
			}
			var ary = asmSearchPathsArray;
			foreach (var path in ary) {
				file = TryLoadFromDir(asmName, exactCheck, path);
				if (file != null)
					return file;
			}

			return null;
		}

		IDnSpyFile TryLoadFromDir(IAssembly asmName, bool exactCheck, string dirPath) {
			string baseName;
			try {
				baseName = Path.Combine(dirPath, asmName.Name);
			}
			catch (ArgumentException) { // eg. invalid chars in asmName.Name
				return null;
			}
			return TryLoadFromDir2(asmName, exactCheck, baseName + ".dll") ??
				   TryLoadFromDir2(asmName, exactCheck, baseName + ".exe");
		}

		IDnSpyFile TryLoadFromDir2(IAssembly asmName, bool exactCheck, string filename) {
			if (!File.Exists(filename))
				return null;

			IDnSpyFile file = null;
			bool error = true;
			try {
				file = fileManager.TryCreateDnSpyFile(DnSpyFileInfo.CreateFile(filename));
				if (file == null)
					return null;
				file.IsAutoLoaded = true;
				var asm = file.AssemblyDef;
				if (asm == null)
					return null;
				bool b = exactCheck ?
					AssemblyNameComparer.CompareAll.Equals(asmName, asm) :
					AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(asmName, asm);
				if (!b)
					return null;

				error = false;
				return file;
			}
			finally {
				if (error) {
					if (file is IDisposable)
						((IDisposable)file).Dispose();
				}
			}
		}

		IDnSpyFile ResolveWinMD(IAssembly assembly, ModuleDef sourceModule) {
			var existingFile = fileManager.FindAssembly(assembly);
			if (existingFile != null)
				return existingFile;

			foreach (var winmdPath in GacInfo.WinmdPaths) {
				string file;
				try {
					file = Path.Combine(winmdPath, assembly.Name + ".winmd");
				}
				catch (ArgumentException) {
					continue;
				}
				if (File.Exists(file))
					return fileManager.TryGetOrCreateInternal(DnSpyFileInfo.CreateFile(file), true, true);
			}
			return null;
		}
	}
}
