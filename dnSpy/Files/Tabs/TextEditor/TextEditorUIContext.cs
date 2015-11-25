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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Decompiler;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.Tabs.TextEditor {
	interface ITextEditorHelper {
		bool GoTo(ReferenceSegment refSeg, bool newTab, bool followLocalRefs, bool canRecordHistory);
		void SetFocus();
		void SetActive();
	}

	sealed class TextEditorUIContext : ITextEditorUIContext, ITextEditorHelper, IDisposable {
		readonly IWpfCommandManager wpfCommandManager;
		readonly TextEditorControl textEditorControl;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly TextEditorUIContext textEditorUIContext;

			public GuidObjectsCreator(TextEditorUIContext textEditorUIContext) {
				this.textEditorUIContext = textEditorUIContext;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORUICONTEXT, textEditorUIContext);

				var teCtrl = (TextEditorControl)creatorObject.Object;
				var position = openedFromKeyboard ? teCtrl.TextEditor.TextArea.Caret.Position : teCtrl.TextEditor.GetPositionFromMousePosition();
				if (position != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTVIEWPOSITION_GUID, position);

				var @ref = teCtrl.GetReferenceSegmentAt(position);
				if (@ref != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, @ref.ToCodeReferenceSegment());
			}
		}

		sealed class ContextMenuInitializer : IContextMenuInitializer {
			public void Initialize(IMenuItemContext context, ContextMenu menu) {
				var teCtrl = (TextEditorControl)context.CreatorObject.Object;
				if (context.OpenedFromKeyboard) {
					var scrollInfo = (IScrollInfo)teCtrl.TextEditor.TextArea.TextView;
					var pos = teCtrl.TextEditor.TextArea.TextView.GetVisualPosition(teCtrl.TextEditor.TextArea.Caret.Position, VisualYPosition.TextBottom);
					pos = new Point(pos.X - scrollInfo.HorizontalOffset, pos.Y - scrollInfo.VerticalOffset);

					menu.HorizontalOffset = pos.X;
					menu.VerticalOffset = pos.Y;
					ContextMenuService.SetPlacement(teCtrl, PlacementMode.Relative);
					ContextMenuService.SetPlacementTarget(teCtrl, teCtrl.TextEditor.TextArea.TextView);
					menu.Closed += (s, e2) => {
						teCtrl.ClearValue(ContextMenuService.PlacementProperty);
						teCtrl.ClearValue(ContextMenuService.PlacementTargetProperty);
					};
				}
				else {
					teCtrl.ClearValue(ContextMenuService.PlacementProperty);
					teCtrl.ClearValue(ContextMenuService.PlacementTargetProperty);
				}
			}
		}

		public TextEditorUIContext(IWpfCommandManager wpfCommandManager, IMenuManager menuManager, TextEditorControl textEditorControl) {
			this.wpfCommandManager = wpfCommandManager;
			this.textEditorControl = textEditorControl;
			this.textEditorControl.TextEditorHelper = this;
			this.wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorControl);
			this.wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTEDITOR, textEditorControl.TextEditor);
			this.wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTAREA, textEditorControl.TextEditor.TextArea);
			menuManager.InitializeContextMenu(this.textEditorControl, MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID, new GuidObjectsCreator(this), new ContextMenuInitializer());
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
			get {
				var button = textEditorControl.CancelButton;
				if (button != null && button.IsVisible)
					return button;
				return textEditorControl.TextEditor.TextArea;
			}
		}

		public object UIObject {
			get { return textEditorControl; }
		}

		public FrameworkElement ScaleElement {
			get { return textEditorControl.TextEditor.TextArea; }
		}

		public void OnShow() {
		}

		public void OnHide() {
			textEditorControl.Clear();
			//TODO: Debugger plugin should clear CodeMappings
		}

		public void Deserialize(object obj) {
			var state = obj as EditorPositionState;
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

		void FollowReference(ReferenceSegment refSeg, bool newTab) {
			Debug.Assert(FileTab != null);
			if (FileTab == null)
				return;
			FileTab.FollowReference(refSeg.ToCodeReferenceSegment(), newTab);
		}

		bool ITextEditorHelper.GoTo(ReferenceSegment refSeg, bool newTab, bool followLocalRefs, bool canRecordHistory) {
			if (refSeg == null)
				return false;

			if (newTab) {
				FollowReference(refSeg, newTab);
				return true;
			}

			if (followLocalRefs) {
				if (!textEditorControl.IsOwnerOf(refSeg)) {
					FollowReference(refSeg, newTab);
					return true;
				}

				var localTarget = textEditorControl.FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				if (refSeg.IsLocalTarget) {
					//TODO: if (canRecordHistory) RecordHistory(this);
					var line = textEditorControl.TextEditor.Document.GetLineByOffset(refSeg.StartOffset);
					int column = refSeg.StartOffset - line.Offset + 1;
					textEditorControl.ScrollAndMoveCaretTo(line.LineNumber, column);
					return true;
				}

				if (refSeg.IsLocal)
					return false;
				FollowReference(refSeg, newTab);
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
					//TODO: if (canRecordHistory) RecordHistory(this);
					textEditorControl.MarkReferences(refSeg);
					((ITextEditorHelper)this).SetFocus();
					textEditorControl.TextEditor.Select(pos, 0);
					textEditorControl.TextEditor.ScrollTo(textEditorControl.TextEditor.TextArea.Caret.Line, textEditorControl.TextEditor.TextArea.Caret.Column);
					return true;
				}

				if (refSeg.IsLocal && textEditorControl.MarkReferences(refSeg))
					return false;   // Allow another handler to set a new caret position

				((ITextEditorHelper)this).SetFocus();
				FollowReference(refSeg, newTab);
				return true;
			}
		}

		void ITextEditorHelper.SetFocus() {
			FileTab.SetFocus();
		}

		public void SetActive() {
			FileTab.FileTabManager.ActiveTab = FileTab;
		}

		public void Dispose() {
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorControl);
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTEDITOR, textEditorControl.TextEditor);
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTAREA, textEditorControl.TextEditor.TextArea);
		}

		public void ShowCancelButton(Action onCancel, string msg) {
			textEditorControl.ShowCancelButton(onCancel, msg);
		}

		public void MoveCaretTo(object @ref) {
			textEditorControl.GoToLocation(@ref);
		}
	}
}
