/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Documents.Tabs.DocViewer;
using dnSpy.Properties;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Disassembly.Viewer {
	sealed class DisassemblyDocumentTabContent : DocumentTabContent, IDisposable {
		public override string Title { get; }

		readonly IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider;
		readonly DisassemblyContentProvider contentProvider;
		readonly IContentType asmContentType;
		DocumentViewerContent cachedDocumentViewerContent;
		IContentType cachedContentType;
		bool isVisible;
		bool disposed;

		public DisassemblyDocumentTabContent(IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider, IContentType contentType, DisassemblyContentProvider contentProvider, string title) {
			this.documentViewerContentFactoryProvider = documentViewerContentFactoryProvider;
			this.contentProvider = contentProvider;
			asmContentType = contentType;
			Title = title ?? dnSpy_Resources.Disassembly_TabTitle;
			contentProvider.OnContentChanged += DisassemblyContentProvider_OnContentChanged;
		}

		void DisassemblyContentProvider_OnContentChanged(object sender, EventArgs e) {
			if (disposed)
				return;
			cachedDocumentViewerContent = null;
			if (isVisible)
				Refresh();
		}

		void Refresh() {
			var tab = DocumentTab;
			tab?.DocumentTabService.Refresh(new[] { tab });
		}

		public override DocumentTabContent Clone() =>
			new DisassemblyDocumentTabContent(documentViewerContentFactoryProvider, asmContentType, contentProvider.Clone(), Title);
		public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			(DocumentTabUIContext)locator.Get<IDocumentViewer>();

		public override void OnShow(IShowContext ctx) {
			Debug.Assert(!isVisible);
			isVisible = true;
			var documentViewer = (IDocumentViewer)ctx.UIContext;
			if (cachedDocumentViewerContent == null)
				(cachedDocumentViewerContent, cachedContentType) = CreateContent(documentViewer);
			documentViewer.SetContent(cachedDocumentViewerContent, cachedContentType);
		}

		(DocumentViewerContent content, IContentType contentType) CreateContent(IDocumentViewer documentViewer) {
			var factory = documentViewerContentFactoryProvider.Create();
			var disasmContent = contentProvider.GetContent();
			var output = factory.Output;
			foreach (var text in disasmContent.Text) {
				if (text.Reference != null)
					output.Write(text.Text, text.Reference, ToDecompilerReferenceFlags(text.ReferenceFlags), text.Color);
				else
					output.Write(text.Text, text.Color);
			}
			var contentType = GetContentType(disasmContent.Kind);
			return (factory.CreateContent(documentViewer, contentType), contentType);
		}

		IContentType GetContentType(DisassemblyContentKind kind) => asmContentType;

		static DecompilerReferenceFlags ToDecompilerReferenceFlags(DisassemblyReferenceFlags referenceFlags) {
			var flags = DecompilerReferenceFlags.None;
			if ((referenceFlags & DisassemblyReferenceFlags.Definition) != 0)
				flags |= DecompilerReferenceFlags.Definition;
			if ((referenceFlags & DisassemblyReferenceFlags.Local) != 0)
				flags |= DecompilerReferenceFlags.Local;
			return flags;
		}

		public override void OnHide() {
			Debug.Assert(isVisible);
			isVisible = false;
		}

		public void Dispose() {
			disposed = true;
			contentProvider.OnContentChanged -= DisassemblyContentProvider_OnContentChanged;
			contentProvider.Dispose();
		}
	}
}
