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

using System.ComponentModel.Composition;
using System.Windows;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class DnlibTypePicker : IDnlibTypePicker {
		static IAppWindow appWindow;
		static IDocumentTreeView documentTreeView;
		static IImageService imageService;
		static IDocumentSearcherProvider fileSearcherProvider;
		static IDecompilerService decompilerService;
		static IDocumentTreeViewProvider documentTreeViewProvider;
		static IDocumentTreeViewSettings documentTreeViewSettings;

		[ExportAutoLoaded]
		sealed class Loader : IAutoLoaded {
			[ImportingConstructor]
			Loader(IAppWindow appWindow, IImageService imageService, IDocumentTreeView documentTreeView, IDocumentSearcherProvider fileSearcherProvider, IDecompilerService decompilerService, IDocumentTreeViewProvider documentTreeViewProvider, IDocumentTreeViewSettings documentTreeViewSettings) {
				DnlibTypePicker.appWindow = appWindow;
				DnlibTypePicker.imageService = imageService;
				DnlibTypePicker.documentTreeView = documentTreeView;
				DnlibTypePicker.fileSearcherProvider = fileSearcherProvider;
				DnlibTypePicker.decompilerService = decompilerService;
				DnlibTypePicker.documentTreeViewProvider = documentTreeViewProvider;
				DnlibTypePicker.documentTreeViewSettings = documentTreeViewSettings;
			}
		}

		readonly Window ownerWindow;

		public DnlibTypePicker()
			: this(null) {
		}

		public DnlibTypePicker(Window ownerWindow) {
			this.ownerWindow = ownerWindow;
		}

		public T GetDnlibType<T>(string title, IDocumentTreeNodeFilter filter, T selectedObject, ModuleDef ownerModule) where T : class {
			var newDocumentTreeView = documentTreeViewProvider.Create(filter);
			try {
				var data = new MemberPickerVM(fileSearcherProvider, newDocumentTreeView, decompilerService, filter, title, documentTreeView.DocumentService.GetDocuments());
				data.SyntaxHighlight = documentTreeViewSettings.SyntaxHighlight;
				var win = new MemberPickerDlg(documentTreeView, newDocumentTreeView, imageService);
				win.DataContext = data;
				win.Owner = ownerWindow ?? appWindow.MainWindow;
				data.SelectItem(selectedObject);
				if (win.ShowDialog() != true)
					return null;

				return ImportObject(ownerModule, data.SelectedDnlibObject) as T;
			}
			finally {
				newDocumentTreeView.Dispose();
			}
		}

		static object ImportObject(ModuleDef ownerModule, object obj) {
			var importer = new Importer(ownerModule, ImporterOptions.TryToUseDefs);

			var type = obj as IType;
			if (type != null)
				return importer.Import(type);

			var field = obj as IField;
			if (field != null && field.IsField)
				return importer.Import(field);

			var method = obj as IMethod;
			if (method != null && method.IsMethod)
				return importer.Import(method);

			// DsDocument, namespace, PropertyDef, EventDef, AssemblyRef, ModuleRef
			return obj;
		}
	}
}
