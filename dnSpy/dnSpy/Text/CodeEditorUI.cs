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
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;

namespace dnSpy.Text {
	sealed class CodeEditorUI : ICodeEditorUI {
		public IWpfTextView TextView { get; }
		public ITextBuffer TextBuffer => TextView.TextViewModel.EditBuffer;
		public object UIObject => TextView.UIObject;
		public IInputElement FocusedElement => TextView.FocusedElement;
		public FrameworkElement ScaleElement => TextView.ScaleElement;
		public object Tag { get; set; }

		readonly IWpfTextView wpfTextView;
		readonly DnSpyTextEditor textEditor;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly CodeEditorUI codeEditorUI;

			public GuidObjectsCreator(CodeEditorUI codeEditorUI) {
				this.codeEditorUI = codeEditorUI;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID, codeEditorUI);
			}
		}

		public CodeEditorUI(CodeEditorOptions options, ITextEditorFactoryService2 textEditorFactoryService2) {
			options = options ?? new CodeEditorOptions();
			var wpfTextView = textEditorFactoryService2.CreateTextView(options.TextBuffer, options, (object)options.ContentType ?? options.ContentTypeGuid, true, () => new GuidObjectsCreator(this));
			this.wpfTextView = wpfTextView;
			TextView = wpfTextView;
			this.textEditor = wpfTextView.DnSpyTextEditor;
		}

		public void Dispose() {
			if (!wpfTextView.IsClosed)
				wpfTextView.Close();
		}
	}
}
