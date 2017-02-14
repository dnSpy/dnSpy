/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	sealed class CodeEditor : ICodeEditor {
		public IDsWpfTextView TextView => TextViewHost.TextView;
		public IDsWpfTextViewHost TextViewHost { get; }
		public ITextBuffer TextBuffer => TextViewHost.TextView.TextBuffer;
		public object UIObject => TextViewHost.HostControl;
		public IInputElement FocusedElement => TextViewHost.TextView.VisualElement;
		public FrameworkElement ZoomElement => TextViewHost.TextView.VisualElement;
		public object Tag { get; set; }

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly CodeEditor codeEditor;

			public GuidObjectsProvider(CodeEditor codeEditorUI) => codeEditor = codeEditorUI;

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID, codeEditor);
			}
		}

		public CodeEditor(CodeEditorOptions options, IDsTextEditorFactoryService dsTextEditorFactoryService, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService, IEditorOptionsFactoryService editorOptionsFactoryService) {
			options = options?.Clone() ?? new CodeEditorOptions();
			options.CreateGuidObjects = CommonGuidObjectsProvider.Create(options.CreateGuidObjects, new GuidObjectsProvider(this));
			var contentType = contentTypeRegistryService.GetContentType(options.ContentType, options.ContentTypeString) ?? textBufferFactoryService.TextContentType;
			var textBuffer = options.TextBuffer;
			if (textBuffer == null)
				textBuffer = textBufferFactoryService.CreateTextBuffer(contentType);
			var roles = dsTextEditorFactoryService.CreateTextViewRoleSet(options.Roles);
			var textView = dsTextEditorFactoryService.CreateTextView(textBuffer, roles, editorOptionsFactoryService.GlobalOptions, options);
			TextViewHost = dsTextEditorFactoryService.CreateTextViewHost(textView, false);
			TextViewHost.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.TextEditor);
			TextViewHost.TextView.Options.SetOptionValue(DefaultDsTextViewOptions.RefreshScreenOnChangeId, true);
		}

		public void Dispose() {
			if (!TextViewHost.IsClosed)
				TextViewHost.Close();
		}
	}
}
