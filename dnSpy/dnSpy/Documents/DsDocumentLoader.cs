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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.MVVM.Dialogs;

namespace dnSpy.Documents {
	sealed class DsDocumentLoader : IProgressTask, IDsDocumentLoader {
		readonly IDsDocumentService documentService;
		readonly Window ownerWindow;
		readonly HashSet<IDsDocument> hash;
		readonly List<IDsDocument> loadedDocuments;
		DocumentToLoad[] documentsToLoad;

		public bool IsIndeterminate => false;
		public double ProgressMinimum => 0;
		public double ProgressMaximum { get; set; }

		public DsDocumentLoader(IDsDocumentService documentService, Window ownerWindow) {
			this.documentService = documentService;
			this.ownerWindow = ownerWindow;
			this.loadedDocuments = new List<IDsDocument>();
			this.hash = new HashSet<IDsDocument>();
		}

		public IDsDocument[] Load(IEnumerable<DocumentToLoad> documents) {
			documentsToLoad = documents.ToArray();
			ProgressMaximum = documentsToLoad.Length;

			const int MAX_NUM_DOCUMENTS_NO_DLG_BOX = 10;
			if (documentsToLoad.Length <= MAX_NUM_DOCUMENTS_NO_DLG_BOX) {
				foreach (var f in documentsToLoad)
					Load(f);
			}
			else
				ProgressDlg.Show(this, "dnSpy", ownerWindow);

			return loadedDocuments.ToArray();
		}

		void Load(DocumentToLoad f) {
			if (f.Info.Type == DocumentConstants.DOCUMENTTYPE_FILE && string.IsNullOrEmpty(f.Info.Name))
				return;
			var document = documentService.TryGetOrCreate(f.Info, f.IsAutoLoaded);
			if (document != null && !hash.Contains(document)) {
				loadedDocuments.Add(document);
				hash.Add(document);
			}
		}

		public void Execute(IProgress progress) {
			for (int i = 0; i < documentsToLoad.Length; i++) {
				progress.ThrowIfCancellationRequested();
				var f = documentsToLoad[i];
				progress.SetTotalProgress(i);
				progress.SetDescription(GetDescription(f.Info));
				Load(f);
			}

			progress.SetTotalProgress(documentsToLoad.Length);
		}

		string GetDescription(DsDocumentInfo info) {
			if (info.Type == DocumentConstants.DOCUMENTTYPE_REFASM) {
				int index = info.Name.LastIndexOf(DocumentConstants.REFERENCE_ASSEMBLY_SEPARATOR);
				if (index >= 0)
					return info.Name.Substring(0, index);
			}
			return info.Name;
		}
	}
}
