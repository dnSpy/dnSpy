/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Debugger.IMModules;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger {
	sealed class ModuleLoader {
		public static readonly ModuleLoader Instance = new ModuleLoader();

		bool UseMemoryModules {
			get { return DebuggerSettings.Instance.UseMemoryModules; }
		}

		public static DnModule GetDnModule(CorModule module) {
			var dbg = DebugManager.Instance.Debugger;
			if (dbg == null)
				return null;
			foreach (var m in dbg.Modules) {
				if (m.CorModule == module)
					return m;
			}
			return null;
		}

		static DnModule GetDnModule(SerializedDnSpyModule serMod) {
			var dbg = DebugManager.Instance.Debugger;
			if (dbg == null)
				return null;
			//TODO: This method should have an AppDomain parameter.
			foreach (var m in dbg.Modules) {
				if (m.SerializedDnModule.ToSerializedDnSpyModule().Equals(serMod))
					return m;
			}
			return null;
		}

		public IDnSpyFile LoadModule(CorModule module, bool canLoadDynFile) {
			if (module == null)
				return null;

			var dnModule = GetDnModule(module);
			Debug.Assert(dnModule != null);
			if (dnModule != null)
				return LoadModule(dnModule, canLoadDynFile);

			return LoadModule(module.SerializedDnModule.ToSerializedDnSpyModule(), canLoadDynFile);
		}

		public IDnSpyFile LoadModule(DnModule module, bool canLoadDynFile) {
			if (module == null)
				return null;
			if (UseMemoryModules || module.IsDynamic || module.IsInMemory)
				return InMemoryModuleManager.Instance.LoadFile(module, canLoadDynFile);
			var file = InMemoryModuleManager.Instance.FindFile(module);
			if (file != null)
				return file;
			var serMod = module.SerializedDnModule.ToSerializedDnSpyModule();
			return LoadModule(serMod, canLoadDynFile);
		}

		IEnumerable<IDnSpyFile> AllDnSpyFiles {
			get { return InMemoryModuleManager.AllDnSpyFiles; }
		}

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

		IDnSpyFile LoadNonDiskFile(SerializedDnSpyModule serMod, bool canLoadDynFile) {
			if (UseMemoryModules || serMod.IsDynamic || serMod.IsInMemory) {
				var dnModule = GetDnModule(serMod);
				if (dnModule != null)
					return InMemoryModuleManager.Instance.LoadFile(dnModule, canLoadDynFile);
			}

			return null;
		}

		IDnSpyFile LoadExisting(SerializedDnSpyModule serMod) {
			foreach (var file in AllActiveDnSpyFiles) {
				var serModFile = file.SerializedDnSpyModule;
				if (serModFile != null && serModFile.Value.Equals(serMod))
					return file;
			}

			foreach (var file in AllDnSpyFiles) {
				var serModFile = file.SerializedDnSpyModule;
				if (serModFile != null && serModFile.Value.Equals(serMod))
					return file;
			}

			return null;
		}

		public IDnSpyFile LoadModule(SerializedDnSpyModule serMod, bool canLoadDynFile, bool diskFileOk = false) {
			const bool isAutoLoaded = true;

			if (diskFileOk) {
				var file = LoadExisting(serMod) ?? LoadNonDiskFile(serMod, canLoadDynFile);
				if (file != null)
					return file;
			}
			else {
				var file = LoadNonDiskFile(serMod, canLoadDynFile) ?? LoadExisting(serMod);
				if (file != null)
					return file;
			}

			if (serMod.IsDynamic || serMod.IsInMemory)
				return null;

			string moduleFilename = serMod.ModuleName;
			if (!File.Exists(moduleFilename))
				return null;
			string asmFilename = GetAssemblyFilename(moduleFilename, serMod.AssemblyFullName);
			var dnspyFileList = MainWindow.Instance.DnSpyFileList;
			lock (dnspyFileList.GetLockObj()) {
				if (string.IsNullOrEmpty(asmFilename))
					return dnspyFileList.OpenFileDelay(moduleFilename, isAutoLoaded);

				var file = dnspyFileList.OpenFileDelay(asmFilename, isAutoLoaded);
				if (file == null)
					return null;

				// Common case is a one-file assembly or first module of a multifile assembly
				if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
					return file;

				var loadedMod = MainWindow.Instance.DnSpyFileListTreeNode.FindModule(file, moduleFilename);
				if (loadedMod != null)
					return loadedMod;

				Debug.Fail("Shouldn't be here.");
				return dnspyFileList.OpenFileDelay(moduleFilename, isAutoLoaded);
			}
		}

		static string GetAssemblyFilename(string moduleFilename, string assemblyFullName) {
			if (string.IsNullOrEmpty(moduleFilename) || string.IsNullOrEmpty(assemblyFullName))
				return null;
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
