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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dndbg.DotNet;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Shared.UI.Files;

namespace dnSpy.Debugger.IMModules {
	interface IInMemoryModuleManager {
		event EventHandler DynamicModulesLoaded;
		void UpdateModuleMemory(MemoryModuleDefFile file);
		IDnSpyFile LoadFile(DnModule dnModule, bool canLoadDynFile);
		IDnSpyFile FindFile(DnModule dnModule);
		IEnumerable<IDnSpyFile> AllDnSpyFiles { get; }
	}

	[Export, Export(typeof(IInMemoryModuleManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class InMemoryModuleManager : IInMemoryModuleManager {
		ClassLoader classLoader;

		bool UseDebugSymbols {
			get { return fileManager.Settings.LoadPDBFiles; }
		}

		public IEnumerable<IDnSpyFile> AllDnSpyFiles {
			get {
				var hash = new HashSet<IDnSpyFile>(fileTreeView.GetAllModuleNodes().Select(a => a.DnSpyFile));
				foreach (var c in fileManager.GetFiles())
					hash.Add(c);
				foreach (var f in hash.ToArray()) {
					foreach (var c in f.Children)
						hash.Add(c);
				}

				return hash;
			}
		}

		IEnumerable<MemoryModuleDefFile> AllMemoryModuleDefFiles {
			get { return AllDnSpyFiles.OfType<MemoryModuleDefFile>(); }
		}

		IEnumerable<CorModuleDefFile> AllCorModuleDefFiles {
			get { return AllDnSpyFiles.OfType<CorModuleDefFile>(); }
		}

		readonly IFileTreeView fileTreeView;
		readonly IFileManager fileManager;
		readonly IAppWindow appWindow;
		readonly ITheDebugger theDebugger;

		[ImportingConstructor]
		InMemoryModuleManager(ITheDebugger theDebugger, IFileTreeView fileTreeView, IAppWindow appWindow) {
			this.fileTreeView = fileTreeView;
			this.fileManager = fileTreeView.FileManager;
			this.appWindow = appWindow;
			this.theDebugger = theDebugger;
			theDebugger.OnProcessStateChanged2 += TheDebugger_OnProcessStateChanged2;
		}

		public event EventHandler DynamicModulesLoaded;

		void TheDebugger_OnProcessStateChanged2(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				classLoader = new ClassLoader(fileTreeView, appWindow.MainWindow);
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
				if (DynamicModulesLoaded != null)
					DynamicModulesLoaded(this, EventArgs.Empty);
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

		static IAssemblyFileNode GetParentAssembly(IModuleFileNode modNode) {
			var parent = modNode == null ? null : modNode.TreeNode.Parent;
			return parent == null ? null : parent.Data as IAssemblyFileNode;
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
				var asmFile = FindAssemblyByKey(manifestKey);
				var asmNode = fileTreeView.FindNode(asmFile) as IAssemblyFileNode;
				if (asmNode != null) {
					var cmdf = (CorModuleDefFile)asmNode.DnSpyFile;
					var moduleKey = CorModuleDefFile.CreateKey(module);
					asmNode.TreeNode.EnsureChildrenLoaded();
					Debug.Assert(asmNode.TreeNode.Children.Count >= 1);
					var moduleNode = asmNode.TreeNode.DataChildren.OfType<IModuleFileNode>().FirstOrDefault(a => moduleKey.Equals(a.DnSpyFile.Key));
					Debug.Assert(moduleNode == null);
					if (moduleNode == null) {
						var newFile = new CorModuleDefFile(module, UseDebugSymbols);
						UpdateResolver(module.GetOrCreateCorModuleDef());
						cmdf.Children.Add(newFile);
						Initialize(module.Debugger, new[] { newFile.DnModule.CorModuleDef });
						asmNode.TreeNode.Children.Add(fileTreeView.CreateNode(asmNode, newFile).TreeNode);
					}
				}
			}

			// Update an in-memory assembly, if one exists
			if (manifestModule.Address != 0 && module.Address != 0) {
				var manifestKey = MemoryModuleDefFile.CreateKey(manifestModule.Process, manifestModule.Address);
				var asmFile = FindAssemblyByKey(manifestKey);
				var asmNode = fileTreeView.FindNode(asmFile) as IAssemblyFileNode;
				if (asmNode != null) {
					var mmdf = (MemoryModuleDefFile)asmNode.DnSpyFile;
					var moduleKey = MemoryModuleDefFile.CreateKey(module.Process, module.Address);
					asmNode.TreeNode.EnsureChildrenLoaded();
					Debug.Assert(asmNode.TreeNode.Children.Count >= 1);
					var moduleNode = asmNode.TreeNode.DataChildren.OfType<IModuleFileNode>().FirstOrDefault(a => moduleKey.Equals(a.DnSpyFile.Key));
					Debug.Assert(moduleNode == null);
					if (moduleNode == null) {
						MemoryModuleDefFile newFile = null;
						try {
							newFile = MemoryModuleDefFile.Create(module, UseDebugSymbols);
						}
						catch {
						}

						Debug.Assert(newFile != null);
						if (newFile != null) {
							UpdateResolver(newFile.ModuleDef);
							mmdf.Children.Add(newFile);
							asmNode.DnSpyFile.ModuleDef.Assembly.Modules.Add(newFile.ModuleDef);
							asmNode.TreeNode.Children.Add(fileTreeView.CreateNode(asmNode, newFile).TreeNode);
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

		Dictionary<CorModuleDefFile, IModuleFileNode> GetVisibleAliveDynamicModuleNodes() {
			var dict = new Dictionary<CorModuleDefFile, IModuleFileNode>();
			foreach (var node in fileTreeView.GetAllModuleNodes()) {
				var cmdf = node.DnSpyFile as CorModuleDefFile;
				if (cmdf == null)
					continue;
				if (cmdf.DnModule.Debugger.ProcessState == DebuggerProcessState.Terminated)
					continue;
				if (cmdf.DnModule.Process.HasExited || cmdf.DnModule.HasUnloaded || !cmdf.DnModule.IsDynamic)
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

		public void UpdateModuleMemory(MemoryModuleDefFile file) {
			if (file.UpdateMemory())
				RefreshBodies(file);
		}

		void RefreshBodies(MemoryModuleDefFile file) {
			// Free all method bodies and clear cache so the new bodies are shown if any
			// got modified (eg. decrypted in memory)
			for (uint rid = 1; ; rid++) {
				var md = file.ModuleDef.ResolveToken(new MDToken(Table.Method, rid)) as MethodDef;
				if (md == null)
					break;
				//TODO: MethodAnnotations.Instance.SetBodyModified(md, false);
				md.FreeMethodBody();
			}
			//TODO: ModuleModified(file);

			// A breakpoint in an encrypted method will fail to be created. Now's a good time to
			// re-add any failed breakpoints.
			if (!file.Process.HasExited && file.Process.Debugger.ProcessState != DebuggerProcessState.Terminated) {
				foreach (var module in file.Process.Modules) {
					if (module.Address == file.Address)
						file.Process.Debugger.AddBreakpoints(module);
				}
			}
		}

		void UpdateResolver(ModuleDef module) {
			if (module != null)
				module.Context = DnSpyDotNetFileBase.CreateModuleContext(fileTreeView.FileManager.AssemblyResolver);
		}

		void DnDebugger_OnCorModuleDefCreated(object sender, CorModuleDefCreatedEventArgs e) {
			UpdateResolver(e.CorModuleDef);
		}

		public IDnSpyFile FindFile(DnModule dnModule) {
			if (dnModule == null)
				return null;
			if (dnModule.IsDynamic)
				return FindDynamic(dnModule);
			// It could be a CorModuleDefFile if LoadFromMemory() failed and called LoadDynamic()
			return FindMemory(dnModule) ?? FindDynamic(dnModule);
		}

		public IDnSpyFile LoadFile(DnModule dnModule, bool canLoadDynFile) {
			if (dnModule == null)
				return null;

			if (dnModule.IsDynamic)
				return LoadDynamic(dnModule, canLoadDynFile);
			return LoadFromMemory(dnModule, canLoadDynFile);
		}

		IDnSpyFile LoadDynamic(DnModule dnModule, bool canLoadDynFile) {
			var file = FindDynamic(dnModule);
			if (file != null)
				return file;

			if (dnModule.Debugger.ProcessState != DebuggerProcessState.Stopped)
				return null;
			if (!canLoadDynFile)
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
			var files = new List<CorModuleDefFile>(modules.Length);
			foreach (var module in modules) {
				UpdateResolver(module.GetOrCreateCorModuleDef());
				var newFile = new CorModuleDefFile(module, UseDebugSymbols);
				dict.Add(module.GetOrCreateCorModuleDef(), newFile);
				files.Add(newFile);
			}
			Debug.Assert(files.Count != 0);
			Initialize(dnModule.Debugger, dict.Select(a => a.Value.DnModule.CorModuleDef));

			var asmFile = CorModuleDefFile.CreateAssembly(files);
			var addedFile = fileManager.GetOrAdd(asmFile);
			Debug.Assert(addedFile == asmFile);

			return dict[dnModule.CorModuleDef];
		}

		IDnSpyFile FindDynamic(DnModule dnModule) {
			if (dnModule == null)
				return null;
			var mod = dnModule.GetOrCreateCorModuleDef();
			return AllCorModuleDefFiles.FirstOrDefault(a => a.ModuleDef == mod);
		}

		IDnSpyFile LoadFromMemory(DnModule dnModule, bool canLoadDynFile) {
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
					mfile = MemoryModuleDefFile.Create(module, UseDebugSymbols);
					UpdateResolver(mfile.ModuleDef);
					if (module == dnModule)
						result = mfile;
				}
				catch {
					// The PE headers and/or .NET headers are probably corrupt
					return LoadDynamic(dnModule, canLoadDynFile);
				}
				files.Add(mfile);
				dict.Add(mfile.ModuleDef, mfile);
			}
			Debug.Assert(result != null);
			if (files.Count == 0)
				return null;
			var asmFile = MemoryModuleDefFile.CreateAssembly(files);
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

			var addedFile = fileManager.GetOrAdd(asmFile);
			Debug.Assert(addedFile == asmFile);

			return result;
		}

		MemoryModuleDefFile FindMemory(DnModule dnModule) {
			if (dnModule == null)
				return null;
			var key = MemoryModuleDefFile.CreateKey(dnModule.Process, dnModule.Address);
			return AllMemoryModuleDefFiles.FirstOrDefault(a => key.Equals(a.Key));
		}

		IDnSpyFile FindAssemblyByKey(IDnSpyFilenameKey key) {
			return fileManager.Find(key);
		}

		void Initialize(DnDebugger dbg, IEnumerable<CorModuleDef> modules) {
			Debug.Assert(dbg.ProcessState == DebuggerProcessState.Stopped);
			var list = modules.ToArray();
			foreach (var cmd in list)
				cmd.DisableMDAPICalls = true;
			classLoader.LoadEverything(modules);
		}
	}
}
