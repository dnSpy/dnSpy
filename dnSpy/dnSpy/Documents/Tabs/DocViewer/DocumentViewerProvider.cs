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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	// Store DocumentViewer in a strong reference because it contains a IWpfTextViewHost that must
	// clean up after itself whenever the DocumentViewer instance gets closed
	[ExportDocumentTabUIContextProvider(Order = TabConstants.ORDER_DOCUMENTVIEWERPROVIDER, UseStrongReference = true)]
	sealed class DocumentViewerProvider : IDocumentTabUIContextProvider {
		readonly IWpfCommandService wpfCommandService;
		readonly IMenuService menuService;
		readonly IDocumentViewerServiceImpl documentViewerServiceImpl;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IDsTextEditorFactoryService dsTextEditorFactoryService;

		[ImportingConstructor]
		DocumentViewerProvider(IWpfCommandService wpfCommandService, IMenuService menuService, IDocumentViewerServiceImpl documentViewerServiceImpl, ITextBufferFactoryService textBufferFactoryService, IDsTextEditorFactoryService dsTextEditorFactoryService) {
			this.wpfCommandService = wpfCommandService;
			this.menuService = menuService;
			this.documentViewerServiceImpl = documentViewerServiceImpl;
			this.textBufferFactoryService = textBufferFactoryService;
			this.dsTextEditorFactoryService = dsTextEditorFactoryService;
		}

		sealed class DocumentViewerHelper : IDocumentViewerHelper {
			public IDocumentViewerHelper RealInstance { get; set; }
			public void FollowReference(TextReference textRef, bool newTab) => RealInstance?.FollowReference(textRef, newTab);
			public void SetActive() => RealInstance?.SetActive();
			public void SetFocus() => RealInstance?.SetFocus();
		}

		public DocumentTabUIContext Create<T>() where T : class {
			if (typeof(T) == typeof(IDocumentViewer)) {
				var helper = new DocumentViewerHelper();
				var uiCtxCtrl = new DocumentViewerControl(textBufferFactoryService, dsTextEditorFactoryService, helper);
				var uiContext = new DocumentViewer(wpfCommandService, documentViewerServiceImpl, menuService, uiCtxCtrl);
				helper.RealInstance = uiContext;
				documentViewerServiceImpl.RaiseAddedEvent(uiContext);
				return uiContext;
			}
			return null;
		}
	}
}
