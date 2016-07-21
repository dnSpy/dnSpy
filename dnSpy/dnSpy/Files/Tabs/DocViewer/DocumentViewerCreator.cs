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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Files.Tabs.DocViewer {
	// Store DocumentViewer in a strong reference because it contains a IWpfTextViewHost that must
	// clean up after itself whenever the DocumentViewer instance gets closed
	[ExportFileTabUIContextCreator(Order = TabConstants.ORDER_DOCUMENTVIEWERCREATOR, UseStrongReference = true)]
	sealed class DocumentViewerCreator : IFileTabUIContextCreator {
		readonly IWpfCommandManager wpfCommandManager;
		readonly IMenuManager menuManager;
		readonly IDocumentViewerServiceImpl documentViewerServiceImpl;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService;

		[ImportingConstructor]
		DocumentViewerCreator(IWpfCommandManager wpfCommandManager, IMenuManager menuManager, IDocumentViewerServiceImpl documentViewerServiceImpl, ITextBufferFactoryService textBufferFactoryService, IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService) {
			this.wpfCommandManager = wpfCommandManager;
			this.menuManager = menuManager;
			this.documentViewerServiceImpl = documentViewerServiceImpl;
			this.textBufferFactoryService = textBufferFactoryService;
			this.dnSpyTextEditorFactoryService = dnSpyTextEditorFactoryService;
		}

		sealed class DocumentViewerHelper : IDocumentViewerHelper {
			public IDocumentViewerHelper RealInstance { get; set; }
			public void FollowReference(TextReference textRef, bool newTab) => RealInstance?.FollowReference(textRef, newTab);
			public void SetActive() => RealInstance?.SetActive();
			public void SetFocus() => RealInstance?.SetFocus();
		}

		public IFileTabUIContext Create<T>() where T : class, IFileTabUIContext {
			if (typeof(T) == typeof(IDocumentViewer)) {
				var helper = new DocumentViewerHelper();
				var uiCtxCtrl = new DocumentViewerControl(textBufferFactoryService, dnSpyTextEditorFactoryService, helper);
				var uiContext = new DocumentViewer(wpfCommandManager, documentViewerServiceImpl, menuManager, uiCtxCtrl);
				helper.RealInstance = uiContext;
				documentViewerServiceImpl.RaiseAddedEvent(uiContext);
				return uiContext;
			}
			return null;
		}
	}
}
