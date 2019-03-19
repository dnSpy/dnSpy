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
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract class ClassLoaderFactory {
		public abstract ClassLoader Create(DbgRuntime runtime, DbgDynamicModuleProvider dbgDynamicModuleProvider);
	}

	[Export(typeof(ClassLoaderFactory))]
	sealed class ClassLoaderFactoryImpl : ClassLoaderFactory {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IDocumentTreeView> documentTreeView;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<ShowModuleLoaderService> showModuleLoaderService;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		ClassLoaderFactoryImpl(UIDispatcher uiDispatcher, Lazy<IDocumentTreeView> documentTreeView, Lazy<IDocumentTabService> documentTabService, Lazy<ShowModuleLoaderService> showModuleLoaderService, Lazy<IMessageBoxService> messageBoxService) {
			this.uiDispatcher = uiDispatcher;
			this.documentTreeView = documentTreeView;
			this.documentTabService = documentTabService;
			this.showModuleLoaderService = showModuleLoaderService;
			this.messageBoxService = messageBoxService;
		}

		public override ClassLoader Create(DbgRuntime runtime, DbgDynamicModuleProvider dbgDynamicModuleProvider) =>
			new ClassLoaderImpl(uiDispatcher, documentTreeView, documentTabService, showModuleLoaderService, messageBoxService, runtime, dbgDynamicModuleProvider);
	}

	abstract class ClassLoader {
		public abstract void LoadClass(DbgModule module, uint token);
		public abstract void LoadNewClasses();
		public abstract void LoadEverything_UI(DynamicModuleDefDocument[] documents);
	}

	sealed class ClassLoaderImpl : ClassLoader {
		readonly object lockObj;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IDocumentTreeView> documentTreeView;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<ShowModuleLoaderService> showModuleLoaderService;
		readonly Lazy<IMessageBoxService> messageBoxService;
		readonly DbgRuntime runtime;
		readonly DbgDynamicModuleProvider dbgDynamicModuleProvider;
		readonly Dictionary<DbgModule, HashSet<uint>> loadedClasses;

		public ClassLoaderImpl(UIDispatcher uiDispatcher, Lazy<IDocumentTreeView> documentTreeView, Lazy<IDocumentTabService> documentTabService, Lazy<ShowModuleLoaderService> showModuleLoaderService, Lazy<IMessageBoxService> messageBoxService, DbgRuntime runtime, DbgDynamicModuleProvider dbgDynamicModuleProvider) {
			lockObj = new object();
			this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
			this.documentTreeView = documentTreeView ?? throw new ArgumentNullException(nameof(documentTreeView));
			this.documentTabService = documentTabService ?? throw new ArgumentNullException(nameof(documentTabService));
			this.showModuleLoaderService = showModuleLoaderService ?? throw new ArgumentNullException(nameof(showModuleLoaderService));
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.dbgDynamicModuleProvider = dbgDynamicModuleProvider ?? throw new ArgumentNullException(nameof(dbgDynamicModuleProvider));
			loadedClasses = new Dictionary<DbgModule, HashSet<uint>>();
		}

		public override void LoadClass(DbgModule module, uint token) {
			lock (lockObj) {
				if (!loadedClasses.TryGetValue(module, out var hash))
					loadedClasses.Add(module, hash = new HashSet<uint>());
				hash.Add(token);
			}
		}

		public override void LoadNewClasses() {
			lock (lockObj) {
				if (loadedClasses.Count == 0)
					return;
			}
			uiDispatcher.UI(() => LoadNewClasses_UI());
		}

		void LoadNewClasses_UI() {
			uiDispatcher.VerifyAccess();
			if (runtime.IsClosed || runtime.Process.State != DbgProcessState.Paused)
				return;

			Dictionary<DbgModule, HashSet<uint>> oldLoadedClasses;
			lock (lockObj) {
				if (loadedClasses.Count == 0)
					return;
				oldLoadedClasses = new Dictionary<DbgModule, HashSet<uint>>(loadedClasses);
				loadedClasses.Clear();
			}

			var visibleDocs = GetVisibleDocuments_UI();
			if (visibleDocs.Count == 0)
				return;

			var states = new List<ModuleState>(visibleDocs.Count);
			foreach (var info in visibleDocs) {
				oldLoadedClasses.TryGetValue(info.document.DbgModule, out var hash);
				states.Add(new ModuleState(info.document, info.documentNode, hash));
			}

			dbgDynamicModuleProvider.BeginInvoke(() => LoadNewClasses_EngineThread(states));
		}

		void LoadNewClasses_EngineThread(List<ModuleState> states) {
			if (runtime.IsClosed || runtime.Process.State != DbgProcessState.Paused)
				return;

			foreach (var state in states) {
				foreach (var token in dbgDynamicModuleProvider.GetModifiedTypes(state.Document.DbgModule))
					state.ModifiedTypes.Add(token);
				var nonLoadedTokens = GetNonLoadedTokens(state);
				dbgDynamicModuleProvider.InitializeNonLoadedClasses(state.Document.DbgModule, nonLoadedTokens);
			}

			var remaining = states.Where(a => a.ModifiedTypes.Count != 0 || (a.LoadClassHash != null && a.LoadClassHash.Count != 0)).Select(a => a.Document).ToArray();
			if (remaining.Length == 0)
				return;

			uiDispatcher.UI(() => {
				try {
					if (runtime.IsClosed)
						return;
					LoadEverything_UI(remaining);
					foreach (var state in states)
						new TreeViewUpdater(documentTabService.Value, state.Document, state.ModuleNode, state.ModifiedTypes, state.LoadClassHash).Update();
				}
				catch (Exception ex) {
					messageBoxService.Value.Show(ex);
				}
			});
		}

		uint[] GetNonLoadedTokens(ModuleState state) {
			var hash = new HashSet<uint>(state.ModifiedTypes);
			if (state.LoadClassHash != null) {
				foreach (var a in state.LoadClassHash)
					hash.Add(a);
			}
			var tokens = hash.ToList();
			tokens.Sort();
			var res = new List<uint>(tokens.Count);
			foreach (uint token in tokens) {
				bool loaded = state.LoadClassHash != null && state.LoadClassHash.Contains(token);
				if (loaded)
					continue;   // It has already been initialized

				res.Add(token);
			}
			return res.ToArray();
		}

		sealed class ModuleState {
			public DynamicModuleDefDocument Document { get; }
			public ModuleDocumentNode ModuleNode { get; }
			public HashSet<uint> LoadClassHash { get; }
			public HashSet<uint> ModifiedTypes { get; }

			public ModuleState(DynamicModuleDefDocument document, ModuleDocumentNode moduleNode, HashSet<uint> loadClassHash) {
				Document = document ?? throw new ArgumentNullException(nameof(document));
				ModuleNode = moduleNode ?? throw new ArgumentNullException(nameof(moduleNode));
				LoadClassHash = loadClassHash;
				ModifiedTypes = new HashSet<uint>();
			}
		}

		List<(DynamicModuleDefDocument document, ModuleDocumentNode documentNode)> GetVisibleDocuments_UI() {
			uiDispatcher.VerifyAccess();
			var list = new List<(DynamicModuleDefDocument document, ModuleDocumentNode documentNode)>();
			foreach (var node in documentTreeView.Value.GetAllModuleNodes()) {
				if (node.Document is DynamicModuleDefDocument doc && doc.DbgModule.Runtime == runtime && doc.DbgModule.IsDynamic)
					list.Add((doc, node));
			}
			return list;
		}

		public override void LoadEverything_UI(DynamicModuleDefDocument[] documents) {
			uiDispatcher.VerifyAccess();
			if (documents == null)
				throw new ArgumentNullException(nameof(documents));
			if (runtime.IsClosed)
				return;
			if (documents.Length == 0)
				return;

			using (var vm = new ModuleLoaderVM(uiDispatcher, messageBoxService.Value, runtime, dbgDynamicModuleProvider, documents))
				showModuleLoaderService.Value.Show(vm);
		}
	}
}
