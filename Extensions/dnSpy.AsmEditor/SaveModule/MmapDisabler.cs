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
using dnlib.PE;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.SaveModule {
	interface IMmapDisabler {
		void Disable(IEnumerable<string> filenames);
	}

	[Export(typeof(IMmapDisabler))]
	sealed class MmapDisabler : IMmapDisabler {
		readonly IDocumentTreeView documentTreeView;
		readonly Lazy<IUndoCommandService> undoCommandService;

		[ImportingConstructor]
		MmapDisabler(IDocumentTreeView documentTreeView, Lazy<IUndoCommandService> undoCommandService) {
			this.documentTreeView = documentTreeView;
			this.undoCommandService = undoCommandService;
		}

		public void Disable(IEnumerable<string> filenames) {
			var hash = new HashSet<string>(filenames, StringComparer.OrdinalIgnoreCase);

			var documentsHash = new HashSet<IDsDocument>(GetDocuments());
			foreach (var d in documentsHash)
				DisableMemoryMappedIO(hash, d.PEImage);
		}

		IEnumerable<IDsDocument> GetDocuments() {
			foreach (var n in documentTreeView.GetAllCreatedDocumentNodes())
				yield return n.Document;
			foreach (var f in documentTreeView.DocumentService.GetDocuments()) {
				foreach (var c in f.GetAllChildrenAndSelf())
					yield return c;
			}
			foreach (var uo in undoCommandService.Value.GetAllObjects()) {
				var f = DsDocumentUndoableDocumentsProvider.TryGetDocument(uo);
				if (f != null)
					yield return f;
			}
		}

		static void DisableMemoryMappedIO(HashSet<string> filenames, IPEImage peImage) {
			if (peImage != null && filenames.Contains(peImage.FileName))
				MemoryMappedIOHelper.DisableMemoryMappedIO(peImage);
		}
	}
}
