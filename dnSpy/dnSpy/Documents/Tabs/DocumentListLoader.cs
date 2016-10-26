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
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs {
	interface IDocumentListLoader {
		IEnumerable<object> Load(ISettingsSection section, bool loadDocuments);
		void Save(ISettingsSection section);
		bool CanLoad { get; }
		bool Load(DocumentList documentList, IDsDocumentLoader documentLoader = null);
		bool CanReload { get; }
		bool Reload(IDsDocumentLoader documentLoader = null);
		bool CanCloseAll { get; }
		void CloseAll();
		void SaveCurrentDocumentsToList();
	}

	[Export(typeof(IDocumentListLoader))]
	sealed class DocumentListLoader : IDocumentListLoader {
		readonly DocumentListService documentListService;
		readonly DocumentTabService documentTabService;
		readonly DocumentTabSerializer documentTabSerializer;
		readonly Lazy<IDocumentListListener, IDocumentListListenerMetadata>[] documentListListeners;

		[ImportingConstructor]
		DocumentListLoader(IAppWindow appWindow, DocumentListService documentListService, DocumentTabService documentTabService, DocumentTabSerializer documentTabSerializer, [ImportMany] IEnumerable<Lazy<IDocumentListListener, IDocumentListListenerMetadata>> documentListListeners) {
			this.documentListService = documentListService;
			this.documentTabService = documentTabService;
			this.documentTabSerializer = documentTabSerializer;
			this.documentListListeners = documentListListeners.OrderBy(a => a.Metadata.Order).ToArray();
			appWindow.MainWindowClosed += AppWindow_MainWindowClosed;
		}

		void AppWindow_MainWindowClosed(object sender, EventArgs e) => SaveCurrentDocumentsToList();

		struct Disable_SaveCurrentDocumentsToList : IDisposable {
			readonly DocumentListLoader documentListLoader;
			readonly bool oldValue;

			public Disable_SaveCurrentDocumentsToList(DocumentListLoader documentListLoader) {
				this.documentListLoader = documentListLoader;
				this.oldValue = documentListLoader.disable_SaveCurrentDocumentsToList;
				documentListLoader.disable_SaveCurrentDocumentsToList = true;
			}

			public void Dispose() {
				documentListLoader.disable_SaveCurrentDocumentsToList = oldValue;
				documentListLoader.SaveCurrentDocumentsToList();
			}
		}

		Disable_SaveCurrentDocumentsToList DisableSaveToList() => new Disable_SaveCurrentDocumentsToList(this);

		public void SaveCurrentDocumentsToList() {
			if (disable_SaveCurrentDocumentsToList)
				return;
			documentListService.SelectedDocumentList.Update(documentTabService.DocumentTreeView.TreeView.Root.DataChildren.OfType<DsDocumentNode>().Select(a => a.Document));
		}
		bool disable_SaveCurrentDocumentsToList;

		public IEnumerable<object> Load(ISettingsSection section, bool loadDocuments) {
			var disable = DisableSaveToList();
			documentListService.Load(section);
			yield return null;

			if (loadDocuments) {
				foreach (var f in documentListService.SelectedDocumentList.Documents) {
					if (!(f.Type == DocumentConstants.DOCUMENTTYPE_FILE && string.IsNullOrEmpty(f.Name)))
						documentTabService.DocumentTreeView.DocumentService.TryGetOrCreate(f);
					yield return null;
				}
			}
			disable.Dispose();
		}

		public void Save(ISettingsSection section) {
			SaveCurrentDocumentsToList();
			documentListService.Save(section);
		}

		bool CheckCanLoad(bool isReload) {
			foreach (var listener in documentListListeners) {
				if (!listener.Value.CheckCanLoad(isReload))
					return false;
			}
			return true;
		}

		void NotifyBeforeLoad(bool isReload) {
			foreach (var listener in documentListListeners)
				listener.Value.BeforeLoad(isReload);
		}

		void NotifyAfterLoad(bool isReload) {
			foreach (var listener in documentListListeners)
				listener.Value.AfterLoad(isReload);
		}

		public bool CanLoad => !disableLoadAndReload && documentListListeners.All(a => a.Value.CanLoad);

		public bool Load(DocumentList documentList, IDsDocumentLoader documentLoader) {
			const bool isReload = false;
			if (documentLoader == null)
				documentLoader = new DefaultDsDocumentLoader(documentTabService.DocumentTreeView.DocumentService);
			if (!CanLoad)
				return false;
			if (!CheckCanLoad(isReload))
				return false;
			if (documentList != documentListService.SelectedDocumentList)
				SaveCurrentDocumentsToList();

			NotifyBeforeLoad(isReload);
			using (DisableSaveToList()) {
				documentTabService.CloseAll();
				documentTabService.DocumentTreeView.DocumentService.Clear();
				documentLoader.Load(documentList.Documents.Select(a => new DocumentToLoad(a)));
			}
			NotifyAfterLoad(isReload);

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}));
			return true;
		}

		public bool CanReload => !disableLoadAndReload && documentListListeners.All(a => a.Value.CanReload);

		public bool Reload(IDsDocumentLoader documentLoader) {
			const bool isReload = true;
			if (documentLoader == null)
				documentLoader = new DefaultDsDocumentLoader(documentTabService.DocumentTreeView.DocumentService);
			if (!CanReload)
				return false;
			if (!CheckCanLoad(isReload))
				return false;
			SaveCurrentDocumentsToList();

			NotifyBeforeLoad(isReload);
			var tgws = documentTabSerializer.SaveTabs();
			using (DisableSaveToList())
			using (documentTabService.OnReloadAll()) {
				documentTabService.CloseAll();
				documentTabService.DocumentTreeView.DocumentService.Clear();
				var documents = documentListService.SelectedDocumentList.Documents.Select(a => new DocumentToLoad(a)).ToList();
				foreach (var tgw in tgws) {
					foreach (var g in tgw.TabGroups) {
						foreach (var t in g.Tabs) {
							foreach (var f in t.AutoLoadedDocuments)
								documents.Add(new DocumentToLoad(f, true));
						}
					}
				}
				documentLoader.Load(documents);
			}
			NotifyAfterLoad(isReload);

			// The documentss in the TV is loaded with a delay so make sure we delay before restoring
			// or the code that tries to find the nodes might fail to find them.
			disableLoadAndReload = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				foreach (var o in documentTabSerializer.Restore(tgws)) {
				}
				disableLoadAndReload = false;
			}));
			return true;
		}
		bool disableLoadAndReload;

		public bool CanCloseAll => documentTabService.DocumentTreeView.TreeView.Root.Children.Count > 0;

		public void CloseAll() {
			const bool isReload = false;
			if (!CanCloseAll)
				return;
			if (!CheckCanLoad(isReload))
				return;

			NotifyBeforeLoad(isReload);
			documentTabService.CloseAll();
			documentTabService.DocumentTreeView.DocumentService.Clear();
			NotifyAfterLoad(isReload);

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}));
		}
	}
}
