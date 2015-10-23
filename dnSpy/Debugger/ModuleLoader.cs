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
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Debugger.IMModules;
using dnSpy.Files;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger {
	sealed class ModuleLoader {
		public static readonly ModuleLoader Instance = new ModuleLoader();

		bool UseMemoryModules {
			get { return DebuggerSettings.Instance.UseMemoryModules; }
		}

		static DnModule GetDnModule(CorModule module) {
			var dbg = DebugManager.Instance.Debugger;
			if (dbg == null)
				return null;
			foreach (var m in dbg.Modules) {
				if (m.CorModule == module)
					return m;
			}
			return null;
		}

		static DnModule GetDnModule(SerializedDnSpyModule serModAsm) {
			var dbg = DebugManager.Instance.Debugger;
			if (dbg == null)
				return null;
			//TODO: This method should have an AppDomain parameter.
			foreach (var m in dbg.Modules) {
				if (m.SerializedDnModuleWithAssembly.ToSerializedDnSpyModule().Equals(serModAsm))
					return m;
			}
			return null;
		}

		public DnSpyFile LoadModule(CorModule module, bool canLoadDynFile) {
			if (module == null)
				return null;

			var dnModule = GetDnModule(module);
			Debug.Assert(dnModule != null);
			if (dnModule != null)
				return LoadModule(dnModule, canLoadDynFile);

			return LoadModule(module.SerializedDnModuleWithAssembly.ToSerializedDnSpyModule(), canLoadDynFile);
		}

		public DnSpyFile LoadModule(DnModule module, bool canLoadDynFile) {
			if (module == null)
				return null;
			if (UseMemoryModules || module.IsDynamic || module.IsInMemory)
				return InMemoryModuleManager.Instance.LoadFile(module, canLoadDynFile);
			var serModAsm = module.SerializedDnModuleWithAssembly.ToSerializedDnSpyModule();
			return LoadModule(serModAsm, canLoadDynFile);
		}

		IEnumerable<DnSpyFile> AllDnSpyFiles {
			get { return MainWindow.Instance.DnSpyFileListTreeNode.GetAllModuleNodes().Select(a => a.DnSpyFile); }
		}

		IEnumerable<DnSpyFile> AllActiveDnSpyFiles {
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

		public DnSpyFile LoadModule(SerializedDnSpyModule serModAsm, bool canLoadDynFile) {
			const bool isAutoLoaded = true;

			if (UseMemoryModules || serModAsm.IsDynamic || serModAsm.IsInMemory) {
				var dnModule = GetDnModule(serModAsm);
				if (dnModule != null)
					return InMemoryModuleManager.Instance.LoadFile(dnModule, canLoadDynFile);
			}

			foreach (var file in AllActiveDnSpyFiles) {
				var serModFile = file.SerializedDnSpyModule;
				if (serModFile != null && serModFile.Value.Equals(serModAsm))
					return file;
			}
			foreach (var file in AllDnSpyFiles) {
				var serModFile = file.SerializedDnSpyModule;
				if (serModFile != null && serModFile.Value.Equals(serModAsm))
					return file;
			}

			if (serModAsm.IsDynamic || serModAsm.IsInMemory)
				return null;

			string moduleFilename = serModAsm.ModuleName;
			if (!File.Exists(moduleFilename))
				return null;
			string asmFilename = GetAssemblyFilename(moduleFilename, serModAsm.AssemblyFullName);
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
