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

using System.Collections.Generic;
using System.Windows;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class CodeEditor : ICodeEditor {
		public IWpfTextView TextView { get; }
		public ITextBuffer TextBuffer => TextView.TextBuffer;
		public object UIObject => TextView.UIObject;
		public IInputElement FocusedElement => TextView.FocusedElement;
		public FrameworkElement ScaleElement => TextView.ScaleElement;
		public object Tag { get; set; }

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly CodeEditor codeEditor;

			public GuidObjectsCreator(CodeEditor codeEditorUI) {
				this.codeEditor = codeEditorUI;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID, codeEditor);
			}
		}

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Zoomable,
			CodeEditorTextViewRoles.CODE,
		};

		public CodeEditor(CodeEditorOptions options, ITextEditorFactoryService2 textEditorFactoryService2, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService) {
			options = options ?? new CodeEditorOptions();
			var contentType = contentTypeRegistryService.GetContentType((object)options.ContentType ?? options.ContentTypeGuid) ?? textBufferFactoryService.TextContentType;
			var textBuffer = options.TextBuffer;
			if (textBuffer == null)
				textBuffer = textBufferFactoryService.CreateTextBuffer(contentType);
			var roles = textEditorFactoryService2.CreateTextViewRoleSet(defaultRoles);
			TextView = textEditorFactoryService2.CreateTextView(textBuffer, roles, options, () => new GuidObjectsCreator(this));
			TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.CodeEditor);
		}

		public void Dispose() {
			if (!TextView.IsClosed)
				TextView.Close();
		}
	}
}
