/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Shared.UI.Decompiler;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.Tabs.TextEditor {
	interface IClickedReferenceHandler {
		bool GoTo(ReferenceSegment refSeg, bool newTab, bool followLocalRefs);
	}

	sealed class TextEditorUIContext : ITextEditorUIContext, IClickedReferenceHandler {
		readonly TextEditorControl textEditorControl;

		public TextEditorUIContext(TextEditorControl textEditorControl) {
			this.textEditorControl = textEditorControl;
			this.textEditorControl.ClickedReferenceHandler = this;
		}

		public IFileTab FileTab {
			get { return fileTab; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (fileTab == null)
					fileTab = value;
				else if (fileTab != value)
					throw new InvalidOperationException();
			}
		}
		IFileTab fileTab;

		public UIElement FocusedElement {
			get { return textEditorControl.TextEditor.TextArea; }
		}

		public object UIObject {
			get { return textEditorControl; }
		}

		public void Clear() {
			textEditorControl.Clear();
			//TODO: Debugger plugin should clear CodeMappings
		}

		public void Deserialize(object obj) {
			var state = obj as EditorPositionState;
			Debug.Assert(state != null);
			if (state == null)
				return;

			textEditorControl.TextEditor.ScrollToVerticalOffset(state.VerticalOffset);
			textEditorControl.TextEditor.ScrollToHorizontalOffset(state.HorizontalOffset);
			textEditorControl.TextEditor.TextArea.Caret.Position = state.TextViewPosition;
			textEditorControl.TextEditor.TextArea.Caret.DesiredXPos = state.DesiredXPos;
		}

		public object Serialize() {
			return new EditorPositionState(textEditorControl.TextEditor);
		}

		public void SetOutput(ITextOutput output, IHighlightingDefinition newHighlighting) {
			textEditorControl.SetOutput(output, newHighlighting);
			//TODO: CodeMappings should be init'd by the debugger plugin
		}

		void FollowReference(object @ref, bool newTab) {
			Debug.Assert(FileTab != null);
			if (FileTab == null)
				return;
			FileTab.FollowReference(@ref, newTab);
		}

		bool IClickedReferenceHandler.GoTo(ReferenceSegment refSeg, bool newTab, bool followLocalRefs) {
			if (refSeg == null)
				return false;

			if (newTab) {
				FollowReference(refSeg.Reference, newTab);
				return true;
			}

			if (followLocalRefs) {
				if (!textEditorControl.IsOwnerOf(refSeg)) {
					FollowReference(refSeg.Reference, newTab);
					return true;
				}

				var localTarget = textEditorControl.FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				if (refSeg.IsLocalTarget) {
					//TODO: RecordHistory(this);
					var line = textEditorControl.TextEditor.Document.GetLineByOffset(refSeg.StartOffset);
					int column = refSeg.StartOffset - line.Offset + 1;
					textEditorControl.ScrollAndMoveCaretTo(line.LineNumber, column);
					return true;
				}

				if (refSeg.IsLocal)
					return false;
				FollowReference(refSeg.Reference, newTab);
				return true;
			}
			else {
				var localTarget = textEditorControl.FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				int pos = -1;
				if (!refSeg.IsLocal) {
					if (refSeg.IsLocalTarget)
						pos = refSeg.EndOffset;
					if (pos < 0 && textEditorControl.DefinitionLookup != null)
						pos = textEditorControl.DefinitionLookup.GetDefinitionPosition(refSeg.Reference);
				}
				if (pos >= 0) {
					//TODO: RecordHistory(this);
					textEditorControl.MarkReferences(refSeg);
					//TODO: SetTextEditorFocus(this);
					textEditorControl.TextEditor.Select(pos, 0);
					textEditorControl.TextEditor.ScrollTo(textEditorControl.TextEditor.TextArea.Caret.Line, textEditorControl.TextEditor.TextArea.Caret.Column);
					return true;
				}

				if (refSeg.IsLocal && textEditorControl.MarkReferences(refSeg))
					return false;   // Allow another handler to set a new caret position

				//TODO: SetTextEditorFocus(this);
				FollowReference(refSeg.Reference, newTab);
				return true;
			}
		}
	}
}
