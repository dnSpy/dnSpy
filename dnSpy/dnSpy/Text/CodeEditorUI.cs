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
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	sealed class CodeEditorUI : ICodeEditorUI {
		public ITextBuffer TextBuffer => textEditor.TextBuffer;
		public object UIObject => textEditor;
		public IInputElement FocusedElement => textEditor.FocusedElement;
		public FrameworkElement ScaleElement => textEditor.ScaleElement;
		public object Tag { get; set; }

		readonly DnSpyTextEditor textEditor;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly CodeEditorUI codeEditorUI;
			readonly Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects;

			public GuidObjectsCreator(CodeEditorUI codeEditorUI, Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects) {
				this.codeEditorUI = codeEditorUI;
				this.createGuidObjects = createGuidObjects;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID, codeEditorUI);

				var textEditor = (DnSpyTextEditor)args.CreatorObject.Object;
				foreach (var go in textEditor.GetGuidObjects(args.OpenedFromKeyboard))
					yield return go;

				if (createGuidObjects != null) {
					foreach (var guidObject in createGuidObjects(args))
						yield return guidObject;
				}
			}
		}

		public CodeEditorUI(CodeEditorOptions options, IDnSpyTextEditorCreator dnSpyTextEditorCreator) {
			options = options ?? new CodeEditorOptions();
			this.textEditor = dnSpyTextEditorCreator.Create(new DnSpyTextEditorOptions(options.Options, options.TextBuffer, true, () => new GuidObjectsCreator(this, options.Options.CreateGuidObjects)));
		}
	}
}
