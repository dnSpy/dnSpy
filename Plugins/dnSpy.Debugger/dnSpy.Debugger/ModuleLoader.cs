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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.IMModules;

namespace dnSpy.Debugger {
	interface IModuleLoader {
		IDnSpyFile LoadModule(CorModule module, bool canLoadDynFile, bool isAutoLoaded);
		IDnSpyFile LoadModule(DnModule module, bool canLoadDynFile, bool isAutoLoaded);
		IDnSpyFile LoadModule(ModuleId moduleId, bool canLoadDynFile, bool diskFileOk, bool isAutoLoaded);
		DnModule GetDnModule(CorModule module);
	}

	[Export(typeof(IModuleLoader))]
	sealed class ModuleLoader : IModuleLoader {
		bool UseMemoryModules => debuggerSettings.UseMemoryModules;

		readonly Lazy<ITheDebugger> theDebugger;
		readonly IDebuggerSettings debuggerSettings;
		readonly IFileManager fileManager;
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;
		readonly IModuleIdCreator moduleIdCreator;

		[ImportingConstructor]
		ModuleLoader(Lazy<ITheDebugger> theDebugger, IDebuggerSettings debuggerSettings, IFileManager fileManager, Lazy<IInMemoryModuleManager> inMemoryModuleManager, IModuleIdCreator moduleIdCreator) {
			this.theDebugger = theDebugger;
			this.debuggerSettings = debuggerSettings;
			this.fileManager = fileManager;
			this.inMemoryModuleManager = inMemoryModuleManager;
			this.moduleIdCreator = moduleIdCreator;
		}

		public DnModule GetDnModule(CorModule module) {
			var dbg = theDebugger.Value.Debugger;
			if (dbg == null)
				return null;
			foreach (var m in dbg.Modules) {
				if (m.CorModule == module)
					return m;
			}
			return null;
		}

		DnModule GetDnModule(ModuleId moduleId) {
			var dbg = theDebugger.Value.Debugger;
			if (dbg == null)
				return null;
			//TODO: This method should have an AppDomain parameter.
			foreach (var m in dbg.Modules) {
				if (m.SerializedDnModule.ToModuleId().Equals(moduleId))
					return m;
			}
			return null;
		}

		public IDnSpyFile LoadModule(CorModule module, bool canLoadDynFile, bool isAutoLoaded) {
			if (module == null)
				return null;

			var dnModule = GetDnModule(module);
			Debug.Assert(dnModule != null);
			if (dnModule != null)
				return LoadModule(dnModule, canLoadDynFile, isAutoLoaded);

			return LoadModule(module.SerializedDnModule.ToModuleId(), canLoadDynFile, diskFileOk: false, isAutoLoaded: isAutoLoaded);
		}

		public IDnSpyFile LoadModule(DnModule module, bool canLoadDynFile, bool isAutoLoaded) {
			if (module == null)
				return null;
			if (UseMemoryModules || module.IsDynamic || module.IsInMemory)
				return inMemoryModuleManager.Value.LoadFile(module, canLoadDynFile);
			var file = inMemoryModuleManager.Value.FindFile(module);
			if (file != null)
				return file;
			var serMod = module.SerializedDnModule;
			return LoadModule(serMod.ToModuleId(), canLoadDynFile, diskFileOk: false, isAutoLoaded: isAutoLoaded);
		}

		IEnumerable<IDnSpyFile> AllDnSpyFiles => inMemoryModuleManager.Value.AllDnSpyFiles;

		IEnumerable<IDnSpyFile> AllActiveDnSpyFiles {
			get {
				foreach (var file in AllDnSpyFiles) {
					var cmdf = file as CorModuleDefFile;
					if (cmdf != null) {
						if (cmdf.DnModule.Process.HasExited || cmdf.DnModule.Debugger.ProcessState == DebuggerProcessState.Terminated)
							continue;
						yield return cmdf;
						continue;
					}

					var mmdf = file as MemoryModuleDefFile;
					if (mmdf != null) {
						if (mmdf.Process.HasExited || mmdf.Process.Debugger.ProcessState == DebuggerProcessState.Terminated)
							continue;
						yield return mmdf;
						continue;
					}

					yield return file;
				}
			}
		}

		IDnSpyFile LoadNonDiskFile(ModuleId moduleId, bool canLoadDynFile) {
			if (UseMemoryModules || moduleId.IsDynamic || moduleId.IsInMemory) {
				var dnModule = GetDnModule(moduleId);
				if (dnModule != null)
					return inMemoryModuleManager.Value.LoadFile(dnModule, canLoadDynFile);
			}

			return null;
		}

		IDnSpyFile LoadExisting(ModuleId moduleId) {
			foreach (var file in AllActiveDnSpyFiles) {
				var otherId = moduleIdCreator.Create(file.ModuleDef);
				if (otherId.Equals(moduleId))
					return file;
			}

			foreach (var file in AllDnSpyFiles) {
				var serModFile = moduleIdCreator.Create(file.ModuleDef);
				if (serModFile.Equals(moduleId))
					return file;
			}

			return null;
		}

		public IDnSpyFile LoadModule(ModuleId moduleId, bool canLoadDynFile, bool diskFileOk, bool isAutoLoaded) {
			IDnSpyFile file;
			if (diskFileOk) {
				file = LoadExisting(moduleId) ?? LoadNonDiskFile(moduleId, canLoadDynFile);
				if (file != null)
					return file;
			}
			else {
				file = LoadNonDiskFile(moduleId, canLoadDynFile) ?? LoadExisting(moduleId);
				if (file != null)
					return file;
			}

			if (moduleId.IsDynamic || moduleId.IsInMemory)
				return null;

			string moduleFilename = moduleId.ModuleName;
			if (!File.Exists(moduleFilename))
				return null;
			string asmFilename = GetAssemblyFilename(moduleFilename, moduleId.AssemblyFullName, moduleId.ModuleNameOnly);

			if (!string.IsNullOrEmpty(asmFilename)) {
				file = fileManager.TryGetOrCreate(DnSpyFileInfo.CreateFile(asmFilename), isAutoLoaded);
				if (file == null)
					file = fileManager.Resolve(new AssemblyNameInfo(moduleId.AssemblyFullName), null);
				if (file != null) {
					// Common case is a one-file assembly or first module of a multifile assembly
					if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
						return file;

					foreach (var child in file.Children) {
						if (child.Filename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
							return child;
					}
				}
			}

			return fileManager.TryGetOrCreate(DnSpyFileInfo.CreateFile(moduleFilename), isAutoLoaded);
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
