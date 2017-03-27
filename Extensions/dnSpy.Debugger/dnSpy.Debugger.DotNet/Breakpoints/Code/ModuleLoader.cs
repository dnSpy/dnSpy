/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	//TODO: Remove it once the real class is available
	[Export(typeof(ModuleLoader))]
	sealed class ModuleLoader {
		readonly IModuleIdProvider moduleIdProvider;
		readonly IDocumentTreeView documentTreeView;
		readonly IDsDocumentService documentService;

		[ImportingConstructor]
		ModuleLoader(IModuleIdProvider moduleIdProvider, IDocumentTreeView documentTreeView, IDsDocumentService documentService) {
			this.moduleIdProvider = moduleIdProvider;
			this.documentTreeView = documentTreeView;
			this.documentService = documentService;
		}

		public IEnumerable<IDsDocument> AllActiveDocuments {
			get {
				yield break;
			}
		}

		public IEnumerable<IDsDocument> AllDocuments {
			get {
				var hash = new HashSet<IDsDocument>(documentTreeView.GetAllModuleNodes().Select(a => a.Document));
				foreach (var c in documentService.GetDocuments())
					hash.Add(c);
				foreach (var f in hash.ToArray()) {
					foreach (var c in f.Children)
						hash.Add(c);
				}

				return hash;
			}
		}

		IDsDocument LoadNonDiskFile(ModuleId moduleId, bool canLoadDynFile) => null;

		IDsDocument LoadExisting(ModuleId moduleId) {
			foreach (var file in AllActiveDocuments) {
				var otherId = moduleIdProvider.Create(file.ModuleDef);
				if (otherId.Equals(moduleId))
					return file;
			}

			foreach (var file in AllDocuments) {
				var moduleIdFile = moduleIdProvider.Create(file.ModuleDef);
				if (moduleIdFile.Equals(moduleId))
					return file;
			}

			return null;
		}

		public IDsDocument LoadModule(ModuleId moduleId, bool canLoadDynFile, bool diskFileOk, bool isAutoLoaded) {
			IDsDocument document;
			if (diskFileOk) {
				document = LoadExisting(moduleId) ?? LoadNonDiskFile(moduleId, canLoadDynFile);
				if (document != null)
					return document;
			}
			else {
				document = LoadNonDiskFile(moduleId, canLoadDynFile) ?? LoadExisting(moduleId);
				if (document != null)
					return document;
			}

			if (moduleId.IsDynamic || moduleId.IsInMemory)
				return null;

			string moduleFilename = moduleId.ModuleName;
			if (!File.Exists(moduleFilename))
				return null;
			string asmFilename = GetAssemblyFilename(moduleFilename, moduleId.AssemblyFullName, moduleId.ModuleNameOnly);

			if (!string.IsNullOrEmpty(asmFilename)) {
				document = documentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(asmFilename), isAutoLoaded);
				if (document == null)
					document = documentService.Resolve(new AssemblyNameInfo(moduleId.AssemblyFullName), null);
				if (document != null) {
					// Common case is a one-file assembly or first module of a multifile assembly
					if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
						return document;

					foreach (var child in document.Children) {
						if (child.Filename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
							return child;
					}
				}
			}

			return documentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(moduleFilename), isAutoLoaded);
		}

		static string GetAssemblyFilename(string moduleFilename, string assemblyFullName, bool moduleNameOnly) {
			if (string.IsNullOrEmpty(moduleFilename) || (string.IsNullOrEmpty(assemblyFullName) && !moduleNameOnly))
				return null;
			if (moduleNameOnly)
				return moduleFilename;
			try {
				var asm = new AssemblyNameInfo(assemblyFullName);
				if (Path.GetFileName(asm.Name) != asm.Name || asm.Name.Length == 0)
					return null;
				var fn = Path.Combine(Path.GetDirectoryName(moduleFilename), asm.Name);
				var fnDll = fn + ".dll";
				if (File.Exists(fnDll))
					return fnDll;
				var fnExe = fn + ".exe";
				if (File.Exists(fnExe))
					return fnExe;
			}
			catch {
			}
			return null;
		}
	}
}
