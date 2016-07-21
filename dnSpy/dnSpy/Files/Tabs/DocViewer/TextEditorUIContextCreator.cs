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
	[ExportFileTabUIContextCreator(Order = TabConstants.ORDER_TEXTEDITORUICONTEXTCREATOR)]
	sealed class TextEditorUIContextCreator : IFileTabUIContextCreator {
		readonly IWpfCommandManager wpfCommandManager;
		readonly IMenuManager menuManager;
		readonly ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService;

		[ImportingConstructor]
		TextEditorUIContextCreator(IWpfCommandManager wpfCommandManager, IMenuManager menuManager, ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl, ITextBufferFactoryService textBufferFactoryService, IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService) {
			this.wpfCommandManager = wpfCommandManager;
			this.menuManager = menuManager;
			this.textEditorUIContextManagerImpl = textEditorUIContextManagerImpl;
			this.textBufferFactoryService = textBufferFactoryService;
			this.dnSpyTextEditorFactoryService = dnSpyTextEditorFactoryService;
		}

		sealed class TextEditorHelper : ITextEditorHelper {
			public ITextEditorHelper RealInstance { get; set; }
			public void FollowReference(TextReference textRef, bool newTab) => RealInstance?.FollowReference(textRef, newTab);
			public void SetActive() => RealInstance?.SetActive();
			public void SetFocus() => RealInstance?.SetFocus();
		}

		public IFileTabUIContext Create<T>() where T : class, IFileTabUIContext {
			if (typeof(T) == typeof(ITextEditorUIContext)) {
				var helper = new TextEditorHelper();
				var uiCtxCtrl = new TextEditorUIContextControl(textBufferFactoryService, dnSpyTextEditorFactoryService, helper);
				var uiContext = new TextEditorUIContext(wpfCommandManager, textEditorUIContextManagerImpl, menuManager, uiCtxCtrl);
				helper.RealInstance = uiContext;
				textEditorUIContextManagerImpl.RaiseAddedEvent(uiContext);
				return uiContext;
			}
			return null;
		}
	}
}
