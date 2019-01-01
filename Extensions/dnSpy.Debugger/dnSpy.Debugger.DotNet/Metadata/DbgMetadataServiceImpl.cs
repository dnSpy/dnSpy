/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.IO;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	[Export(typeof(DbgMetadataService))]
	sealed class DbgMetadataServiceImpl : DbgMetadataService {
		readonly DbgModuleIdProviderService dbgModuleIdProviderService;
		readonly DbgInMemoryModuleService dbgInMemoryModuleService;
		readonly DsDocumentProvider dsDocumentProvider;
		readonly IDsDocumentService documentService;
		readonly DebuggerSettings debuggerSettings;

		bool UseMemoryModules => debuggerSettings.UseMemoryModules;

		[ImportingConstructor]
		DbgMetadataServiceImpl(DbgModuleIdProviderService dbgModuleIdProviderService, DbgInMemoryModuleService dbgInMemoryModuleService, DsDocumentProvider dsDocumentProvider, IDsDocumentService documentService, DebuggerSettings debuggerSettings) {
			this.dbgModuleIdProviderService = dbgModuleIdProviderService;
			this.dbgInMemoryModuleService = dbgInMemoryModuleService;
			this.dsDocumentProvider = dsDocumentProvider;
			this.documentService = documentService;
			this.debuggerSettings = debuggerSettings;
		}

		public override ModuleDef TryGetMetadata(DbgModule module, DbgLoadModuleOptions options) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));

			if (UseMemoryModules || module.IsDynamic || module.IsInMemory || (options & DbgLoadModuleOptions.ForceMemory) != 0)
				return dbgInMemoryModuleService.LoadModule(module);

			var mod = dbgInMemoryModuleService.FindModule(module);
			if (mod != null)
				return mod;

			var id = dbgModuleIdProviderService.GetModuleId(module);
			if (id != null)
				return TryGetMetadata(id.Value, options);

			return null;
		}

		ModuleDef LoadNonDiskFile(ModuleId moduleId, DbgLoadModuleOptions options) {
			if (UseMemoryModules || moduleId.IsDynamic || moduleId.IsInMemory || (options & DbgLoadModuleOptions.ForceMemory) != 0) {
				var module = dbgModuleIdProviderService.GetModule(moduleId);
				if (module != null)
					return dbgInMemoryModuleService.LoadModule(module);
			}

			return null;
		}

		// Priority order: 1) active in-memory/dynamic module, 2) active module, 3) other module
		ModuleDef LoadExisting(ModuleId moduleId) {
			ModuleDef foundModule = null;
			ModuleDef activeModule = null;
			foreach (var info in dsDocumentProvider.DocumentInfos) {
				if (info.Id != moduleId)
					continue;
				if (info.IsActive) {
					if (info.Document is MemoryModuleDefDocument || info.Document is DynamicModuleDefDocument)
						return info.Document.ModuleDef;
					activeModule = activeModule ?? info.Document.ModuleDef;
				}
				else
					foundModule = foundModule ?? info.Document.ModuleDef;
			}
			return activeModule ?? foundModule;
		}

		public override ModuleDef TryGetMetadata(ModuleId moduleId, DbgLoadModuleOptions options) {
			var mod = LoadNonDiskFile(moduleId, options) ?? LoadExisting(moduleId);
			if (mod != null)
				return mod;

			if (moduleId.IsDynamic || moduleId.IsInMemory)
				return null;

			string moduleFilename = moduleId.ModuleName;
			if (!File.Exists(moduleFilename))
				return null;
			var asmFilename = GetAssemblyFilename(moduleFilename, moduleId.AssemblyFullName, moduleId.ModuleNameOnly);

			bool isAutoLoaded = (options & DbgLoadModuleOptions.AutoLoaded) != 0;
			if (!string.IsNullOrEmpty(asmFilename)) {
				var document = documentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(asmFilename), isAutoLoaded);
				if (document == null)
					document = documentService.Resolve(new AssemblyNameInfo(moduleId.AssemblyFullName), null);
				if (document != null) {
					// Common case is a single-file assembly or first module of a multifile assembly
					if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
						return document.ModuleDef;

					foreach (var child in document.Children) {
						if (child.Filename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
							return child.ModuleDef;
					}
				}
			}

			return documentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(moduleFilename), isAutoLoaded)?.ModuleDef;
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
				var filenameNoExt = Path.Combine(Path.GetDirectoryName(moduleFilename), asm.Name);
				var dllFilename = filenameNoExt + ".dll";
				if (File.Exists(dllFilename))
					return dllFilename;
				var exeFilename = filenameNoExt + ".exe";
				if (File.Exists(exeFilename))
					return exeFilename;
			}
			catch {
			}
			return null;
		}
	}
}
