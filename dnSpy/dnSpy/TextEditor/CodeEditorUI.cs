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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;

namespace dnSpy.TextEditor {
	sealed class CodeEditorUI : ICodeEditorUI {
		public ITextBuffer TextBuffer => textEditor.TextBuffer;
		public object UIObject => textEditor;
		public IInputElement FocusedElement => textEditor.FocusedElement;
		public FrameworkElement ScaleElement => textEditor.ScaleElement;
		public object Tag { get; set; }

		readonly DnSpyTextEditor textEditor;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly CodeEditorUI codeEditor;
			readonly Func<GuidObject, bool, IEnumerable<GuidObject>> createGuidObjects;

			public GuidObjectsCreator(CodeEditorUI codeEditor, Func<GuidObject, bool, IEnumerable<GuidObject>> createGuidObjects) {
				this.codeEditor = codeEditor;
				this.createGuidObjects = createGuidObjects;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID, codeEditor);

				var textEditor = (DnSpyTextEditor)creatorObject.Object;
				foreach (var go in textEditor.GetGuidObjects(openedFromKeyboard))
					yield return go;

				if (createGuidObjects != null) {
					foreach (var guidObject in createGuidObjects(creatorObject, openedFromKeyboard))
						yield return guidObject;
				}
			}
		}

		public CodeEditorUI(CodeEditorOptions options, IThemeManager themeManager, IWpfCommandManager wpfCommandManager, IMenuManager menuManager, ITextEditorSettings textEditorSettings, ITextSnapshotColorizerCreator textBufferColorizerCreator, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService) {
			options = options ?? new CodeEditorOptions();
			var buffer = options.TextBuffer ?? textBufferFactoryService.CreateTextBuffer(contentTypeRegistryService.GetContentType((object)options.ContentType ?? options.ContentTypeGuid) ?? textBufferFactoryService.TextContentType);
			this.textEditor = new DnSpyTextEditor(themeManager, textEditorSettings, textBufferColorizerCreator, buffer, true);

			if (options.TextEditorCommandGuid != null)
				wpfCommandManager.Add(options.TextEditorCommandGuid.Value, this.textEditor);
			if (options.TextAreaCommandGuid != null)
				wpfCommandManager.Add(options.TextAreaCommandGuid.Value, this.textEditor.TextArea);
			if (options.MenuGuid != null)
				menuManager.InitializeContextMenu(this.textEditor, options.MenuGuid.Value, new GuidObjectsCreator(this, options.CreateGuidObjects), new ContextMenuInitializer(this.textEditor, this.textEditor));
		}
	}
}
