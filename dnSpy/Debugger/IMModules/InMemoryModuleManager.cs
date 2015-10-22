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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dndbg.DotNet;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Files;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Debugger.IMModules {
	sealed class InMemoryModuleManager {
		public static readonly InMemoryModuleManager Instance = new InMemoryModuleManager();
		readonly MyAssemblyResolver myAssemblyResolver;
		ClassLoader classLoader;

		bool UseDebugSymbols {
			get { return DecompilerSettingsPanel.CurrentDecompilerSettings.UseDebugSymbols; }
		}

		IEnumerable<DnSpyFile> AllDnSpyFiles {
			get { return DnSpyFileListTreeNode.GetAllModuleNodes().Select(a => a.DnSpyFile); }
		}

		IEnumerable<MemoryModuleDefFile> AllMemoryModuleDefFiles {
			get { return AllDnSpyFiles.OfType<MemoryModuleDefFile>(); }
		}

		DnSpyFileList DnSpyFileList {
			get { return MainWindow.Instance.DnSpyFileList; }
		}

		DnSpyFileListTreeNode DnSpyFileListTreeNode {
			get { return MainWindow.Instance.DnSpyFileListTreeNode; }
		}

		InMemoryModuleManager() {
			this.myAssemblyResolver = new MyAssemblyResolver(this);
		}

		internal void OnLoaded() {
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				classLoader = new ClassLoader();
				dbg.OnCorModuleDefCreated += DnDebugger_OnCorModuleDefCreated;
				dbg.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				dbg.OnModuleAdded += DnDebugger_OnModuleAdded;
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
				break;

			case DebuggerProcessState.Stopped:
				if (dbg.IsEvaluating)
					break;

				LoadNewClasses();
				ReloadInMemoryModulesFromMemory();
				break;

			case DebuggerProcessState.Terminated:
				classLoader = null;
				break;
			}
		}

		void DnDebugger_OnModuleAdded(object sender, ModuleDebuggerEventArgs e) {
			if (e.Added)
				OnModuleAdded(e.Module);
		}

		void OnModuleAdded(DnModule module) {
			// If an assembly is visible in the treeview, and a new netmodule gets added, add a
			// new netmodule node to the assembly in the treeview.

			var manifestModule = module.Assembly.Modules[0];

			// If it's the manifest module, it can't possibly have been inserted in the treeview already
			if (manifestModule == module)
				return;

			// Update a dynamic assembly, if one exists
			{
				var manifestKey = CorModuleDefFile.CreateKey(manifestModule);
				var manifestFile = FindAssemblyByKey(manifestKey);
				var manifestNode = DnSpyFileListTreeNode.FindAssemblyNode(manifestFile);
				if (manifestNode != null) {
					var moduleKey = CorModuleDefFile.CreateKey(module);
					manifestNode.EnsureChildrenFiltered();
					Debug.Assert(manifestNode.Children.Count >= 1);
					var moduleNode = manifestNode.Children.OfType<AssemblyTreeNode>().FirstOrDefault(a => moduleKey.Equals(a.DnSpyFile.Key));
					Debug.Assert(moduleNode == null);
					if (moduleNode == null) {
						var dict = new Dictionary<ModuleDef, CorModuleDefFile>();
						var newFile = new CorModuleDefFile(dict, module, UseDebugSymbols);
						UpdateResolver(module.GetOrCreateCorModuleDef());
						Initialize(new[] { newFile.DnModule.CorModuleDef });
						manifestNode.Children.Add(new AssemblyTreeNode(newFile));
					}
				}
			}

			// Update an in-memory assembly, if one exists
			if (manifestModule.Address != 0 && module.Address != 0) {
				var manifestKey = MemoryModuleDefFile.CreateKey(manifestModule.Process, manifestModule.Address);
				var manifestFile = FindAssemblyByKey(manifestKey);
				var manifestNode = DnSpyFileListTreeNode.FindAssemblyNode(manifestFile);
				if (manifestNode != null) {
					var moduleKey = MemoryModuleDefFile.CreateKey(module.Process, module.Address);
					manifestNode.EnsureChildrenFiltered();
					Debug.Assert(manifestNode.Children.Count >= 1);
					var moduleNode = manifestNode.Children.OfType<AssemblyTreeNode>().FirstOrDefault(a => moduleKey.Equals(a.DnSpyFile.Key));
					Debug.Assert(moduleNode == null);
					if (moduleNode == null) {
						var dict = new Dictionary<ModuleDef, MemoryModuleDefFile>();
						MemoryModuleDefFile newFile = null;
						try {
							newFile = MemoryModuleDefFile.Create(dict, module, UseDebugSymbols);
						}
						catch {
						}

						Debug.Assert(newFile != null);
						if (newFile != null) {
							UpdateResolver(newFile.ModuleDef);
							manifestNode.Children.Add(new AssemblyTreeNode(newFile));
						}
					}
				}
			}
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			if (e.Type == DebugCallbackType.LoadClass) {
				var lc = (LoadClassDebugCallbackEventArgs)e;
				var cls = lc.CorClass;
				var dnModule = dbg.TryGetModule(lc.CorAppDomain, cls);
				OnLoadClass(dnModule, cls);
			}
			else if (e.Type == DebugCallbackType.UnloadClass) {
				var uc = (UnloadClassDebugCallbackEventArgs)e;
				var cls = uc.CorClass;
				var dnModule = dbg.TryGetModule(uc.CorAppDomain, cls);
				OnUnloadClass(dnModule, cls);
			}
		}

		void OnLoadClass(DnModule dnModule, CorClass cls) {
			if (dnModule == null || !dnModule.IsDynamic || cls == null)
				return;
			var cmd = dnModule.CorModuleDef;
			if (cmd == null)
				return;

			Debug.Assert(classLoader != null);
			if (classLoader == null)
				return;
			classLoader.LoadClass(dnModule, cls.Token);
		}

		void OnUnloadClass(DnModule dnModule, CorClass cls) {
			if (dnModule == null || !dnModule.IsDynamic || cls == null)
				return;
			var cmd = dnModule.CorModuleDef;
			if (cmd == null)
				return;

			Debug.Assert(classLoader != null);
			if (classLoader == null)
				return;
			classLoader.UnloadClass(dnModule, cls.Token);
		}

		Dictionary<CorModuleDefFile, AssemblyTreeNode> GetVisibleAliveDynamicModuleNodes() {
			var dict = new Dictionary<CorModuleDefFile, AssemblyTreeNode>();
			foreach (var node in DnSpyFileListTreeNode.GetAllModuleNodes()) {
				var cmdf = node.DnSpyFile as CorModuleDefFile;
				if (cmdf == null)
					continue;
				if (cmdf.DnModule.Debugger.ProcessState == DebuggerProcessState.Terminated)
					continue;
				if (cmdf.DnModule.Process.HasExited || !cmdf.DnModule.IsDynamic)
					continue;
				bool b = dict.ContainsKey(cmdf);
				Debug.Assert(!b);
				if (b)
					continue;
				dict.Add(cmdf, node);
			}
			return dict;
		}

		void LoadNewClasses() {
			classLoader.LoadNewClasses(GetVisibleAliveDynamicModuleNodes());
		}

		void ReloadInMemoryModulesFromMemory() {
			foreach (var file in AllMemoryModuleDefFiles) {
				if (file.AutoUpdateMemory)
					UpdateModuleMemory(file);
			}
		}

		void UpdateModuleMemory(MemoryModuleDefFile file) {
			if (file.UpdateMemory())
				RefreshBodies(file);
		}

		void RefreshBodies(MemoryModuleDefFile file) {
			// Free all method bodies and clear cache so the new bodies are shown if any
			// got modified (eg. decrypted in memory)
			for (uint rid = 1; ; rid++) {
				var md = file.ModuleDef.ResolveToken(new MDToken(Table.Method, rid)) as MethodDef;
				if (md != null)
					break;
				MethodAnnotations.Instance.SetBodyModified(md, false);
				md.FreeMethodBody();
			}
			MainWindow.Instance.ModuleModified(file);
		}

		// Prevents memory leaks by using a weak reference to the current assembly resolver.
		// DnSpyFileList can be changed whenever the user picks a new assembly list.
		sealed class MyAssemblyResolver : IAssemblyResolver {
			readonly InMemoryModuleManager imMgr;

			public MyAssemblyResolver(InMemoryModuleManager imMgr) {
				this.imMgr = imMgr;
			}

			public bool AddToCache(AssemblyDef asm) {
				IAssemblyResolver ar = imMgr.DnSpyFileList.AssemblyResolver;
				return ar.AddToCache(asm);
			}

			public void Clear() {
				IAssemblyResolver ar = imMgr.DnSpyFileList.AssemblyResolver;
				ar.Clear();
			}

			public bool Remove(AssemblyDef asm) {
				IAssemblyResolver ar = imMgr.DnSpyFileList.AssemblyResolver;
				return ar.Remove(asm);
			}

			public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
				IAssemblyResolver ar = imMgr.DnSpyFileList.AssemblyResolver;
				return ar.Resolve(assembly, sourceModule);
			}
		}

		void UpdateResolver(ModuleDef module) {
			if (module != null)
				module.Context = DnSpyFile.CreateModuleContext(myAssemblyResolver);
		}

		void DnDebugger_OnCorModuleDefCreated(object sender, CorModuleDefCreatedEventArgs e) {
			UpdateResolver(e.CorModuleDef);
		}

		public DnSpyFile LoadFile(DnModule dnModule) {
			if (dnModule == null)
				return null;

			if (dnModule.IsDynamic)
				return LoadDynamic(dnModule);
			return LoadFromMemory(dnModule);
		}

		DnSpyFile LoadDynamic(DnModule dnModule) {
			var file = FindDynamic(dnModule);
			if (file != null)
				return file;

			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;

			var manifestDnModule = dnModule.Assembly.Modules[0];
			var manifestKey = CorModuleDefFile.CreateKey(manifestDnModule);
			var manMod = FindAssemblyByKey(manifestKey);
			Debug.Assert(manMod == null);
			if (manMod != null)
				return null;

			manMod = FindDynamic(manifestDnModule);
			Debug.Assert(manMod == null);
			if (manMod != null)
				return null;

			var modules = manifestDnModule.Assembly.Modules;
			var dict = new Dictionary<ModuleDef, CorModuleDefFile>(modules.Length);
			foreach (var module in modules) {
				UpdateResolver(module.GetOrCreateCorModuleDef());
				dict.Add(module.GetOrCreateCorModuleDef(), new CorModuleDefFile(dict, module, UseDebugSymbols));
			}
			Initialize(dict.Select(a => a.Value.DnModule.CorModuleDef));

			manMod = dict[manifestDnModule.CorModuleDef];
			DnSpyFileList.AddFile(manMod, true, true, false);

			return dict[dnModule.CorModuleDef];
		}

		CorModuleDefFile FindDynamic(DnModule dnModule) {
			var mod = dnModule.GetOrCreateCorModuleDef();
			var node = DnSpyFileListTreeNode.FindModuleNode(mod);
			if (node != null)
				return (CorModuleDefFile)node.DnSpyFile;
			return null;
		}

		DnSpyFile LoadFromMemory(DnModule dnModule) {
			Debug.Assert(!dnModule.IsDynamic);
			if (dnModule.Address == 0)
				return null;

			var file = FindMemory(dnModule);
			if (file != null)
				return file;

			var manifestDnModule = dnModule.Assembly.Modules[0];
			var manifestKey = MemoryModuleDefFile.CreateKey(manifestDnModule.Process, manifestDnModule.Address);
			var manMod = FindAssemblyByKey(manifestKey);
			Debug.Assert(manMod == null);
			if (manMod != null)
				return null;

			manMod = FindMemory(manifestDnModule);
			Debug.Assert(manMod == null);
			if (manMod != null)
				return null;

			var modules = manifestDnModule.Assembly.Modules;
			var dict = new Dictionary<ModuleDef, MemoryModuleDefFile>(modules.Length);
			var files = new List<MemoryModuleDefFile>(modules.Length);
			MemoryModuleDefFile result = null;
			foreach (var module in modules) {
				MemoryModuleDefFile mfile;
				try {
					mfile = MemoryModuleDefFile.Create(dict, module, UseDebugSymbols);
					UpdateResolver(mfile.ModuleDef);
					if (module == dnModule)
						result = mfile;
				}
				catch {
					// The PE headers and/or .NET headers are probably corrupt
					return LoadDynamic(dnModule);
				}
				files.Add(mfile);
				dict.Add(mfile.ModuleDef, mfile);
			}
			Debug.Assert(result != null);
			if (files.Count == 0)
				return null;
			var asm = files[0].AssemblyDef;
			if (asm == null) {
				if (files.Count > 1) {
					asm = files[0].ModuleDef.UpdateRowId(new AssemblyDefUser("???"));
					asm.Modules.Add(files[0].ModuleDef);
				}
			}
			asm.Modules.Clear();
			for (int i = 0; i < files.Count; i++)
				asm.Modules.Add(files[i].ModuleDef);

			DnSpyFileList.AddFile(files[0], true, true, false);

			return result;
		}

		MemoryModuleDefFile FindMemory(DnModule dnModule) {
			var key = MemoryModuleDefFile.CreateKey(dnModule.Process, dnModule.Address);
			return AllMemoryModuleDefFiles.FirstOrDefault(a => key.Equals(a.Key));
		}

		DnSpyFile FindAssemblyByKey(IDnSpyFilenameKey key) {
			return DnSpyFileList.FindByKey(key);
		}

		void Initialize(IEnumerable<CorModuleDef> modules) {
			Debug.Assert(DebugManager.Instance.ProcessState == DebuggerProcessState.Stopped);
			var list = modules.ToArray();
			foreach (var cmd in list)
				cmd.DisableMDAPICalls = true;
			classLoader.LoadEverything(modules);
		}
	}
}
