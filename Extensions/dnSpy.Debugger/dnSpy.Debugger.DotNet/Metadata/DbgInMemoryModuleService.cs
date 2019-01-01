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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract class DbgInMemoryModuleService {
		public abstract ModuleDef LoadModule(DbgModule module);
		public abstract ModuleDef FindModule(DbgModule module);
	}

	[Export(typeof(DbgInMemoryModuleService))]
	[Export(typeof(IDbgManagerStartListener))]
	sealed class DbgInMemoryModuleServiceImpl : DbgInMemoryModuleService, IDbgManagerStartListener {
		readonly object lockObj;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IDocumentTreeView> documentTreeView;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly DsDocumentProvider documentProvider;
		readonly DbgAssemblyInfoProviderService dbgAssemblyInfoProviderService;
		readonly DbgDynamicModuleProviderService dbgDynamicModuleProviderService;
		readonly ClassLoaderFactory classLoaderFactory;
		readonly Lazy<DbgModuleMemoryRefreshedNotifier2> dbgModuleMemoryRefreshedNotifier;

		bool UseDebugSymbols => true;

		IEnumerable<MemoryModuleDefDocument> AllMemoryModuleDefDocuments => documentProvider.Documents.OfType<MemoryModuleDefDocument>();
		IEnumerable<DynamicModuleDefDocument> AllDynamicModuleDefDocuments => documentProvider.Documents.OfType<DynamicModuleDefDocument>();

		sealed class RuntimeInfo {
			readonly DbgInMemoryModuleServiceImpl owner;
			public DbgAssemblyInfoProvider AssemblyInfoProvider { get; }
			public DbgDynamicModuleProvider DynamicModuleProvider { get; }
			public ClassLoader ClassLoader { get; }
			public RuntimeInfo(DbgInMemoryModuleServiceImpl owner, DbgAssemblyInfoProvider dbgAssemblyInfoProvider, DbgDynamicModuleProvider dbgDynamicModuleProvider, ClassLoader classLoader) {
				this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
				AssemblyInfoProvider = dbgAssemblyInfoProvider ?? throw new ArgumentNullException(nameof(dbgAssemblyInfoProvider));
				DynamicModuleProvider = dbgDynamicModuleProvider;
				ClassLoader = classLoader;
				if (dbgDynamicModuleProvider != null)
					dbgDynamicModuleProvider.ClassLoaded += DbgDynamicModuleProvider_ClassLoaded;
			}

			void DbgDynamicModuleProvider_ClassLoaded(object sender, ClassLoadedEventArgs e) => owner.DbgDynamicModuleProvider_ClassLoaded(this, e);
		}

		[ImportingConstructor]
		DbgInMemoryModuleServiceImpl(UIDispatcher uiDispatcher, Lazy<IDocumentTreeView> documentTreeView, Lazy<IDocumentTabService> documentTabService, Lazy<IMethodAnnotations> methodAnnotations, DsDocumentProvider documentProvider, DbgAssemblyInfoProviderService dbgAssemblyInfoProviderService, DbgDynamicModuleProviderService dbgDynamicModuleProviderService, ClassLoaderFactory classLoaderFactory, Lazy<DbgModuleMemoryRefreshedNotifier2> dbgModuleMemoryRefreshedNotifier) {
			lockObj = new object();
			this.uiDispatcher = uiDispatcher;
			this.documentTreeView = documentTreeView;
			this.documentTabService = documentTabService;
			this.methodAnnotations = methodAnnotations;
			this.documentProvider = documentProvider;
			this.dbgAssemblyInfoProviderService = dbgAssemblyInfoProviderService;
			this.dbgDynamicModuleProviderService = dbgDynamicModuleProviderService;
			this.classLoaderFactory = classLoaderFactory;
			this.dbgModuleMemoryRefreshedNotifier = dbgModuleMemoryRefreshedNotifier;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.ProcessesChanged += DbgManager_ProcessesChanged;

		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added) {
				foreach (var p in e.Objects) {
					p.RuntimesChanged += DbgProcess_RuntimesChanged;
					p.IsRunningChanged += DbgProcess_IsRunningChanged;
				}
			}
			else {
				foreach (var p in e.Objects) {
					p.RuntimesChanged -= DbgProcess_RuntimesChanged;
					p.IsRunningChanged -= DbgProcess_IsRunningChanged;
				}
			}
		}

		void DbgProcess_IsRunningChanged(object sender, EventArgs e) {
			var process = (DbgProcess)sender;
			if (process.State == DbgProcessState.Paused) {
				foreach (var r in process.Runtimes) {
					if (!TryGetRuntimeInfo(r, out var info))
						continue;
					info.ClassLoader?.LoadNewClasses();
				}
			}
		}

		void DbgProcess_RuntimesChanged(object sender, DbgCollectionChangedEventArgs<DbgRuntime> e) {
			if (e.Added) {
				foreach (var r in e.Objects) {
					var assemblyInfoProvider = dbgAssemblyInfoProviderService.Create(r);
					if (assemblyInfoProvider == null)
						continue;

					ClassLoader classLoader;
					var dynamicModuleProvider = dbgDynamicModuleProviderService.Create(r);
					if (dynamicModuleProvider == null)
						classLoader = null;
					else
						classLoader = classLoaderFactory.Create(r, dynamicModuleProvider);

					r.GetOrCreateData(() => new RuntimeInfo(this, assemblyInfoProvider, dynamicModuleProvider, classLoader));
					r.ModulesChanged += DbgRuntime_ModulesChanged;
				}
			}
			else {
				foreach (var r in e.Objects)
					r.ModulesChanged -= DbgRuntime_ModulesChanged;
			}
		}

		void DbgDynamicModuleProvider_ClassLoaded(RuntimeInfo info, ClassLoadedEventArgs e) => info.ClassLoader?.LoadClass(e.Module, e.LoadedClassToken);
		bool TryGetRuntimeInfo(DbgRuntime runtime, out RuntimeInfo info) => runtime.TryGetData(out info);

		void DbgRuntime_ModulesChanged(object sender, DbgCollectionChangedEventArgs<DbgModule> e) {
			if (e.Added) {
				if (!TryGetRuntimeInfo((DbgRuntime)sender, out var info))
					return;

				List<(DbgModule manifestModule, DbgModule module)> list = null;
				foreach (var module in e.Objects) {
					var manifestModule = info.AssemblyInfoProvider.GetManifestModule(module);
					// If it's the manifest module, it can't possibly have been inserted in the treeview
					if (manifestModule == null || manifestModule == module)
						continue;

					if (list == null)
						list = new List<(DbgModule, DbgModule)>();
					list.Add((manifestModule, module));
				}
				if (list != null) {
					uiDispatcher.UI(() => {
						foreach (var t in list)
							OnModuleAdded_UI(info, t.manifestModule, t.module);
					});
				}
			}
		}

		void OnModuleAdded_UI(RuntimeInfo info, DbgModule manifestModule, DbgModule module) {
			uiDispatcher.VerifyAccess();

			// If an assembly is visible in the treeview, and a new netmodule gets added, add a
			// new netmodule node to the assembly in the treeview.

			// Update a dynamic assembly, if one exists
			if (info.DynamicModuleProvider != null) {
				var manifestKey = DynamicModuleDefDocument.CreateKey(manifestModule);
				var asmFile = FindDocument(manifestKey);
				if (documentTreeView.Value.FindNode(asmFile) is AssemblyDocumentNode asmNode) {
					var moduleKey = DynamicModuleDefDocument.CreateKey(module);
					asmNode.TreeNode.EnsureChildrenLoaded();
					Debug.Assert(asmNode.TreeNode.Children.Count >= 1);
					var moduleNode = asmNode.TreeNode.DataChildren.OfType<ModuleDocumentNode>().FirstOrDefault(a => moduleKey.Equals(a.Document.Key));
					Debug.Assert(moduleNode == null);
					if (moduleNode == null) {
						var md = info.DynamicModuleProvider.GetDynamicMetadata(module, out var moduleId);
						if (md != null) {
							UpdateResolver(md);
							var newFile = new DynamicModuleDefDocument(moduleId, module, md, UseDebugSymbols);
							asmNode.Document.Children.Add(newFile);
							Initialize_UI(info, new[] { newFile });
							asmNode.TreeNode.Children.Add(documentTreeView.Value.TreeView.Create(documentTreeView.Value.CreateNode(asmNode, newFile)));
						}
					}
				}
			}

			// Update an in-memory assembly, if one exists
			if (manifestModule.HasAddress && module.HasAddress) {
				var manifestKey = MemoryModuleDefDocument.CreateKey(manifestModule.Process, manifestModule.Address);
				var asmFile = FindDocument(manifestKey);
				if (documentTreeView.Value.FindNode(asmFile) is AssemblyDocumentNode asmNode) {
					var moduleKey = MemoryModuleDefDocument.CreateKey(module.Process, module.Address);
					asmNode.TreeNode.EnsureChildrenLoaded();
					Debug.Assert(asmNode.TreeNode.Children.Count >= 1);
					var moduleNode = asmNode.TreeNode.DataChildren.OfType<ModuleDocumentNode>().FirstOrDefault(a => moduleKey.Equals(a.Document.Key));
					Debug.Assert(moduleNode == null);
					if (moduleNode == null) {
						MemoryModuleDefDocument newFile = null;
						try {
							newFile = MemoryModuleDefDocument.Create(this, module, UseDebugSymbols);
						}
						catch {
						}

						Debug.Assert(newFile != null);
						if (newFile != null) {
							UpdateResolver(newFile.ModuleDef);
							asmNode.Document.Children.Add(newFile);
							RemoveFromAssembly(newFile.ModuleDef);
							asmNode.Document.ModuleDef.Assembly.Modules.Add(newFile.ModuleDef);
							asmNode.TreeNode.Children.Add(documentTreeView.Value.TreeView.Create(documentTreeView.Value.CreateNode(asmNode, newFile)));
						}
					}
				}
			}
		}

		static void RemoveFromAssembly(ModuleDef module) {
			// It could be a netmodule that contains an AssemblyDef row, if so remove it from the assembly
			if (module.Assembly != null)
				module.Assembly.Modules.Remove(module);
		}

		void UpdateResolver(ModuleDef module) {
			if (module != null)
				module.Context = DsDotNetDocumentBase.CreateModuleContext(documentProvider.AssemblyResolver);
		}

		IDsDocument FindDocument(IDsDocumentNameKey key) => documentProvider.Find(key);

		public override ModuleDef LoadModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (module.IsDynamic)
				return LoadDynamicModule(module);
			return LoadMemoryModule(module);
		}

		ModuleDef LoadDynamicModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));

			var doc = FindDynamicModule(module);
			if (doc != null)
				return doc;

			if (!TryGetRuntimeInfo(module.Runtime, out var info))
				return null;
			if (info.DynamicModuleProvider == null)
				return null;

			if (module.Process.State != DbgProcessState.Paused)
				return null;

			// Can happen if the breakpoints window just opened and a dynamic assembly is requested.
			if (uiDispatcher.IsProcessingDisabled())
				return null;

			lock (lockObj) {
				doc = FindDynamicModule(module);
				if (doc != null)
					return doc;

				var modules = info.AssemblyInfoProvider.GetAssemblyModules(module);
				if (modules.Length == 0)
					return null;
				var manifestDnModule = modules[0];
				var manifestKey = DynamicModuleDefDocument.CreateKey(manifestDnModule);
				var manMod = FindDocument(manifestKey);
				Debug.Assert(manMod == null);
				if (manMod != null)
					return null;

				var manDoc = FindDynamicModule(manifestDnModule);
				Debug.Assert(manDoc == null);
				if (manDoc != null)
					return null;

				var files = new List<DynamicModuleDefDocument>(modules.Length);
				DynamicModuleDefDocument resDoc = null;
				foreach (var m in modules) {
					var md = info.DynamicModuleProvider.GetDynamicMetadata(m, out var moduleId);
					if (md == null)
						continue;
					UpdateResolver(md);
					var newDoc = new DynamicModuleDefDocument(moduleId, m, md, UseDebugSymbols);
					if (m == module)
						resDoc = newDoc;
					files.Add(newDoc);
				}
				if (files.Count == 0)
					return null;
				Initialize(info, files.ToArray());

				var asmFile = DynamicModuleDefDocument.CreateAssembly(files);
				var addedFile = documentProvider.GetOrAdd(asmFile);
				Debug.Assert(addedFile == asmFile);

				return resDoc?.ModuleDef;
			}
		}

		ModuleDef LoadMemoryModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));

			if (!TryGetRuntimeInfo(module.Runtime, out var info))
				return null;

			Debug.Assert(!module.IsDynamic);
			if (!module.HasAddress)
				return null;

			var doc = FindMemoryModule(module);
			if (doc != null)
				return doc;

			MemoryModuleDefDocument result = null;
			lock (lockObj) {
				doc = FindMemoryModule(module);
				if (doc != null)
					return doc;

				var modules = info.AssemblyInfoProvider.GetAssemblyModules(module);
				if (modules.Length == 0)
					return null;
				var manifestModule = modules[0];
				var manifestKey = MemoryModuleDefDocument.CreateKey(manifestModule.Process, manifestModule.Address);
				var manMod = FindDocument(manifestKey);
				Debug.Assert(manMod == null);
				if (manMod != null)
					return null;

				var manDoc = FindMemoryModule(manifestModule);
				Debug.Assert(manDoc == null);
				if (manDoc != null)
					return null;

				var docs = new List<MemoryModuleDefDocument>(modules.Length);
				foreach (var m in modules) {
					MemoryModuleDefDocument modDoc;
					try {
						modDoc = MemoryModuleDefDocument.Create(this, m, UseDebugSymbols);
						UpdateResolver(modDoc.ModuleDef);
						if (m == module)
							result = modDoc;
					}
					catch {
						// The PE headers and/or .NET headers are probably corrupt
						return LoadDynamicModule(module);
					}
					docs.Add(modDoc);
				}
				Debug.Assert(result != null);
				if (docs.Count == 0 || result == null)
					return null;
				var asmFile = MemoryModuleDefDocument.CreateAssembly(docs);
				var asm = docs[0].AssemblyDef;
				if (asm == null) {
					if (docs.Count > 1) {
						asm = docs[0].ModuleDef.UpdateRowId(new AssemblyDefUser("???"));
						asm.Modules.Add(docs[0].ModuleDef);
					}
				}
				asm.Modules.Clear();
				for (int i = 0; i < docs.Count; i++) {
					RemoveFromAssembly(docs[i].ModuleDef);
					asm.Modules.Add(docs[i].ModuleDef);
				}

				var addedFile = documentProvider.GetOrAdd(asmFile);
				Debug.Assert(addedFile == asmFile);
			}

			// The modules got loaded for the first time, but it's possible that the debugger is using the
			// old disk file modules. Raise an event so the debugger rereads the memory.
			var newModules = GetModules(result.Process, result.Address);
			if (newModules.Length > 0)
				dbgModuleMemoryRefreshedNotifier.Value.RaiseModulesRefreshed(newModules);

			return result.ModuleDef;
		}

		public override ModuleDef FindModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (module.IsDynamic)
				return FindDynamicModule(module);
			// It could be a dynamic module if LoadMemoryModule() failed and called LoadDynamicModule()
			return FindMemoryModule(module) ?? FindDynamicModule(module);
		}

		ModuleDef FindDynamicModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			return AllDynamicModuleDefDocuments.FirstOrDefault(a => a.DbgModule == module)?.ModuleDef;
		}

		ModuleDef FindMemoryModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (!module.HasAddress)
				return null;
			var key = MemoryModuleDefDocument.CreateKey(module.Process, module.Address);
			return AllMemoryModuleDefDocuments.FirstOrDefault(a => key.Equals(a.Key))?.ModuleDef;
		}

		void Initialize(RuntimeInfo info, DynamicModuleDefDocument[] docs) =>
			uiDispatcher.Invoke(() => Initialize_UI(info, docs));

		void Initialize_UI(RuntimeInfo info, DynamicModuleDefDocument[] docs) {
			uiDispatcher.VerifyAccess();
			Debug.Assert(info.DynamicModuleProvider != null);
			if (info.DynamicModuleProvider == null)
				return;
			info.ClassLoader?.LoadEverything_UI(docs);
		}

		internal void UpdateModuleMemory(MemoryModuleDefDocument document) {
			uiDispatcher.VerifyAccess();
			if (document.TryUpdateMemory())
				RefreshBodies(document);

			// Always reset all breakpoints. If we set breakpoints (and fail) and later the module
			// gets decrypted and we then open the in-memory copy, TryUpdateMemory() will return
			// false ("no changes"), but the breakpoints will still need to be refreshed.
			var modules = GetModules(document.Process, document.Address);
			if (modules.Length > 0)
				dbgModuleMemoryRefreshedNotifier.Value.RaiseModulesRefreshed(modules);
		}

		void RefreshBodies(MemoryModuleDefDocument document) {
			uiDispatcher.VerifyAccess();

			if (document.ModuleDef.EnableTypeDefFindCache) {
				document.ModuleDef.EnableTypeDefFindCache = false;
				document.ModuleDef.EnableTypeDefFindCache = true;
			}

			// Free all method bodies and clear cache so the new bodies are shown if any
			// got modified (eg. decrypted in memory)
			for (uint rid = 1; ; rid++) {
				var md = document.ModuleDef.ResolveToken(new MDToken(Table.Method, rid)) as MethodDef;
				if (md == null)
					break;
				methodAnnotations.Value.SetBodyModified(md, false);
				md.FreeMethodBody();
			}
			documentTabService.Value.RefreshModifiedDocument(document);
		}

		static DbgModule[] GetModules(DbgProcess process, ulong address) =>
			process.Runtimes.SelectMany(a => a.Modules).Where(a => a.HasAddress && a.Address == address).ToArray();
	}
}
