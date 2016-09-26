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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents {
	sealed class AssemblyResolver : IAssemblyResolver {
		readonly DsDocumentService documentService;
		readonly FailedAssemblyResolveCache failedAssemblyResolveCache;

		static readonly Version invalidMscorlibVersion = new Version(255, 255, 255, 255);
		static readonly Version newMscorlibVersion = new Version(4, 0, 0, 0);

		public AssemblyResolver(DsDocumentService documentService) {
			this.documentService = documentService;
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
		string[] asmSearchPathsArray = Array.Empty<string>();

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

		bool IAssemblyResolver.AddToCache(AssemblyDef asm) => false;
		void IAssemblyResolver.Clear() { }
		bool IAssemblyResolver.Remove(AssemblyDef asm) => false;
		AssemblyDef IAssemblyResolver.Resolve(IAssembly assembly, ModuleDef sourceModule) =>
			Resolve(assembly, sourceModule)?.AssemblyDef;

		public IDsDocument Resolve(IAssembly assembly, ModuleDef sourceModule = null) {
			var tempAsm = assembly;
			FrameworkRedirect.ApplyFrameworkRedirect(ref tempAsm, sourceModule);
			// OK : System.Runtime 4.0.20.0 => 4.0.0.0
			// BAD: System 4.0.0.0 => 2.0.0.0
			if (tempAsm.Version.Major >= assembly.Version.Major)
				assembly = tempAsm;

			// Most people don't have the old mscrolib 1.x files, but there could still be references
			// to them, eg. some of the older VS SDK Interop assemblies have refs to them.
			if (assembly.Version.Major == 1 && assembly.Name == mscorlibName && PublicKeyBase.TokenEquals(assembly.PublicKeyOrToken, mscorlibPublicKeyToken))
				assembly = mscorlibRef40;

			if (assembly.IsContentTypeWindowsRuntime) {
				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var document = ResolveWinMD(assembly, sourceModule);
				if (document == null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return document;
			}
			else {
				// WinMD files have a reference to mscorlib but its version is always 255.255.255.255
				// since mscorlib isn't really loaded. The resolver only loads exact versions, so
				// we must change the version or the resolve will fail.
				if (assembly.Name == mscorlibName && assembly.Version == invalidMscorlibVersion)
					assembly = new AssemblyNameInfo(assembly) { Version = newMscorlibVersion };

				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var document = ResolveNormal(assembly, sourceModule);
				if (document == null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return document;
			}
		}
		static readonly UTF8String mscorlibName = new UTF8String("mscorlib");
		static readonly PublicKeyToken mscorlibPublicKeyToken = new PublicKeyToken("b77a5c561934e089");
		static readonly AssemblyRef mscorlibRef40 = new AssemblyRefUser(mscorlibName, new Version(4, 0, 0, 0), mscorlibPublicKeyToken);

		IDsDocument ResolveNormal(IAssembly assembly, ModuleDef sourceModule) {
			var existingDocument = documentService.FindAssembly(assembly);
			if (existingDocument != null)
				return existingDocument;

			var document = LookupFromSearchPaths(assembly, sourceModule, true);
			if (document != null)
				return documentService.GetOrAddCanDispose(document);

			if (documentService.Settings.UseGAC) {
				var gacFile = GacInfo.FindInGac(assembly);
				if (gacFile != null)
					return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateDocument(gacFile), true, true);
				foreach (var path in GacInfo.OtherGacPaths) {
					document = TryLoadFromDir(assembly, true, path);
					if (document != null)
						return documentService.GetOrAddCanDispose(document);
				}
			}

			document = LookupFromSearchPaths(assembly, sourceModule, false);
			if (document != null)
				return documentService.GetOrAddCanDispose(document);

			return null;
		}

		IDsDocument LookupFromSearchPaths(IAssembly asmName, ModuleDef sourceModule, bool exactCheck) {
			IDsDocument document;
			string sourceModuleDir = null;
			if (sourceModule != null && File.Exists(sourceModule.Location)) {
				sourceModuleDir = Path.GetDirectoryName(sourceModule.Location);
				document = TryLoadFromDir(asmName, exactCheck, sourceModuleDir);
				if (document != null)
					return document;
			}
			var ary = asmSearchPathsArray;
			foreach (var path in ary) {
				document = TryLoadFromDir(asmName, exactCheck, path);
				if (document != null)
					return document;
			}

			return null;
		}

		IDsDocument TryLoadFromDir(IAssembly asmName, bool exactCheck, string dirPath) {
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

		IDsDocument TryLoadFromDir2(IAssembly asmName, bool exactCheck, string filename) {
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
				bool b = exactCheck ?
					AssemblyNameComparer.CompareAll.Equals(asmName, asm) :
					AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(asmName, asm);
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
