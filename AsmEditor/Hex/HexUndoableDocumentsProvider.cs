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
using System.Linq;
using dnSpy.AsmEditor.UndoRedo;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IUndoableDocumentsProvider))]
	sealed class HexUndoableDocumentsProvider : IUndoableDocumentsProvider {
		readonly Lazy<IHexDocumentManager> hexDocumentManager;

		[ImportingConstructor]
		HexUndoableDocumentsProvider(Lazy<IHexDocumentManager> hexDocumentManager) {
			this.hexDocumentManager = hexDocumentManager;
		}

		IEnumerable<IUndoObject> IUndoableDocumentsProvider.GetObjects() {
			return hexDocumentManager.Value.GetDocuments().Select(a => GetUndoObject(a));
		}

		IUndoObject IUndoableDocumentsProvider.GetUndoObject(object obj) {
			var doc = obj as AsmEdHexDocument;
			if (doc != null)
				return GetUndoObject(doc);
			return null;
		}

		bool IUndoableDocumentsProvider.OnExecutedOneCommand(IUndoObject obj) {
			return TryGetAsmEdHexDocument(obj) != null;
		}

		object IUndoableDocumentsProvider.GetDocument(IUndoObject obj) {
			return TryGetAsmEdHexDocument(obj);
		}

		static IUndoObject GetUndoObject(AsmEdHexDocument file) {
			return file.UndoObject;
		}

		internal static AsmEdHexDocument TryGetAsmEdHexDocument(IUndoObject iuo) {
			var uo = iuo as UndoObject;
			return uo == null ? null : uo.Value as AsmEdHexDocument;
		}
	}
}
