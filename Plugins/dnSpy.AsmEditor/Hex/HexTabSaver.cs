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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.SaveModule;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.AsmEditor.Hex {
	[ExportTabSaverCreator(Order = TabConstants.ORDER_HEXTABSAVERCREATOR)]
	sealed class HexTabSaverCreator : ITabSaverCreator {
		readonly Lazy<IDocumentSaver> documentSaver;

		[ImportingConstructor]
		HexTabSaverCreator(Lazy<IDocumentSaver> documentSaver) {
			this.documentSaver = documentSaver;
		}

		public ITabSaver Create(IFileTab tab) => HexTabSaver.TryCreate(documentSaver, tab);
	}

	sealed class HexTabSaver : ITabSaver {
		public bool CanSave => true;
		public string MenuHeader => dnSpy_AsmEditor_Resources.Save;

		public static ITabSaver TryCreate(Lazy<IDocumentSaver> documentSaver, IFileTab tab) {
			var uiContext = tab.UIContext as HexBoxFileTabUIContext;
			if (uiContext == null)
				return null;
			var doc = uiContext.DnHexBox.Document as AsmEdHexDocument;
			Debug.Assert(doc != null);
			if (doc == null)
				return null;

			return new HexTabSaver(documentSaver, doc);
		}

		readonly Lazy<IDocumentSaver> documentSaver;
		readonly AsmEdHexDocument doc;

		HexTabSaver(Lazy<IDocumentSaver> documentSaver, AsmEdHexDocument doc) {
			this.documentSaver = documentSaver;
			this.doc = doc;
		}

		public void Save() => documentSaver.Value.Save(new[] { doc });
	}
}
