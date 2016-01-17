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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.Shared;
using dnSpy.Events;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Files.Tabs.TextEditor {
	interface ITextEditorHelper {
		void FollowReference(CodeReference refSeg, bool newTab);
		void SetFocus();
		void SetActive();
	}

	interface ITextEditorUIContextImpl : ITextEditorUIContext {
		/// <summary>
		/// Called when 'use new renderer' option has been changed. <see cref="SetOutput(ITextOutput, IHighlightingDefinition)"/>
		/// will be called after this method has been called.
		/// </summary>
		void OnUseNewRendererChanged();

		/// <summary>
		/// Raised after the text editor has gotten new text (<see cref="SetOutput(ITextOutput, IHighlightingDefinition)"/>)
		/// </summary>
		event EventHandler<EventArgs> NewTextContent;
	}

	sealed class TextEditorUIContext : ITextEditorUIContextImpl, ITextEditorHelper, IDisposable {
		readonly IWpfCommandManager wpfCommandManager;
		readonly ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl;
		TextEditorControl textEditorControl;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly TextEditorUIContext uiContext;

			public GuidObjectsCreator(TextEditorUIContext uiContext) {
				this.uiContext = uiContext;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORUICONTEXT_GUID, uiContext);

				var teCtrl = (TextEditorControl)creatorObject.Object;
				var position = openedFromKeyboard ? teCtrl.TextEditor.TextArea.Caret.Position : teCtrl.TextEditor.GetPositionFromMousePosition();
				if (position != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORLOCATION_GUID, new TextEditorLocation(position.Value.Line, position.Value.Column));

				var @ref = teCtrl.GetReferenceSegmentAt(position);
				if (@ref != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, @ref.ToCodeReference());
			}
		}

		sealed class ContextMenuInitializer : IContextMenuInitializer {
			public void Initialize(IMenuItemContext context, ContextMenu menu) {
				var teCtrl = (TextEditorControl)context.CreatorObject.Object;
				if (context.OpenedFromKeyboard) {
					IScrollInfo scrollInfo = teCtrl.TextEditor.TextArea.TextView;
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

		public TextEditorUIContext(IWpfCommandManager wpfCommandManager, ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl) {
			this.wpfCommandManager = wpfCommandManager;
			this.textEditorUIContextManagerImpl = textEditorUIContextManagerImpl;
			this.newTextContentEvent = new WeakEventList<EventArgs>();
		}

		public void Initialize(IMenuManager menuManager, TextEditorControl textEditorControl) {
			this.textEditorControl = textEditorControl;
			this.wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorControl);
			this.wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTEDITOR, textEditorControl.TextEditor);
			this.wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTAREA, textEditorControl.TextEditor.TextArea);
			menuManager.InitializeContextMenu(this.textEditorControl, MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID, new GuidObjectsCreator(this), new ContextMenuInitializer());
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

		public IInputElement FocusedElement {
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

		public bool HasSelectedText {
			get { return textEditorControl.TextEditor.SelectionLength > 0; }
		}

		public TextEditorLocation Location {
			get {
				var caret = textEditorControl.TextEditor.TextArea.Caret;
				return new TextEditorLocation(caret.Line, caret.Column);
			}
		}

		public void OnShow() {
		}

		public void OnHide() {
			textEditorControl.Clear();
			outputData.Clear();
		}

		public object Serialize() {
			if (cachedEditorPositionState != null)
				return cachedEditorPositionState;
			return new EditorPositionState(textEditorControl.TextEditor);
		}

		public void Deserialize(object obj) {
			var state = obj as EditorPositionState;
			if (state == null)
				return;

			// It can't scroll until it's gotten its scrollviewer
			if (textEditorControl.TextEditor.Template == null) {
				bool start = cachedEditorPositionState == null;
				cachedEditorPositionState = state;
				if (start) {
					textEditorControl.TextEditor.IsVisibleChanged -= TextEditor_IsVisibleChanged;
					textEditorControl.TextEditor.IsVisibleChanged += TextEditor_IsVisibleChanged;
				}
			}
			else
				InitializeState(state);
		}
		EditorPositionState cachedEditorPositionState;

		void InitializeState(EditorPositionState state) {
			textEditorControl.TextEditor.ScrollToVerticalOffset(state.VerticalOffset);
			textEditorControl.TextEditor.ScrollToHorizontalOffset(state.HorizontalOffset);
			textEditorControl.TextEditor.TextArea.Caret.Position = state.TextViewPosition;
			textEditorControl.TextEditor.TextArea.Caret.DesiredXPos = state.DesiredXPos;
		}

		void TextEditor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			textEditorControl.TextEditor.IsVisibleChanged -= TextEditor_IsVisibleChanged;
			if (cachedEditorPositionState == null)
				return;
			InitializeState(cachedEditorPositionState);
			cachedEditorPositionState = null;
		}

		public object CreateSerialized(ISettingsSection section) {
			double? verticalOffset = section.Attribute<double?>("VerticalOffset");
			double? horizontalOffset = section.Attribute<double?>("HorizontalOffset");
			double? desiredXPos = section.Attribute<double?>("DesiredXPos");
			int? textViewPosition_Line = section.Attribute<int?>("TextViewPosition.Line");
			int? textViewPosition_Column = section.Attribute<int?>("TextViewPosition.Column");
			int? textViewPosition_VisualColumn = section.Attribute<int?>("TextViewPosition.VisualColumn");
			bool? textViewPosition_IsAtEndOfLine = section.Attribute<bool?>("TextViewPosition.IsAtEndOfLine");
			Debug.Assert(verticalOffset != null);
			if (verticalOffset == null || horizontalOffset == null || desiredXPos == null)
				return null;
			if (textViewPosition_Line == null || textViewPosition_Column == null)
				return null;
			if (textViewPosition_VisualColumn == null || textViewPosition_IsAtEndOfLine == null)
				return null;
			return new EditorPositionState(verticalOffset.Value, horizontalOffset.Value, new TextViewPosition(textViewPosition_Line.Value, textViewPosition_Column.Value, textViewPosition_VisualColumn.Value) { IsAtEndOfLine = textViewPosition_IsAtEndOfLine.Value }, desiredXPos.Value);
		}

		public void SaveSerialized(ISettingsSection section, object obj) {
			var state = obj as EditorPositionState;
			Debug.Assert(state != null);
			if (state == null)
				return;

			section.Attribute("VerticalOffset", state.VerticalOffset);
			section.Attribute("HorizontalOffset", state.HorizontalOffset);
			section.Attribute("DesiredXPos", state.DesiredXPos);
			section.Attribute("TextViewPosition.Line", state.TextViewPosition.Line);
			section.Attribute("TextViewPosition.Column", state.TextViewPosition.Column);
			section.Attribute("TextViewPosition.VisualColumn", state.TextViewPosition.VisualColumn);
			section.Attribute("TextViewPosition.IsAtEndOfLine", state.TextViewPosition.IsAtEndOfLine);
		}

		public void SetOutput(ITextOutput output, IHighlightingDefinition highlighting) {
			outputData.Clear();
			textEditorControl.SetOutput(output, highlighting);
			this.textEditorUIContextManagerImpl.RaiseNewContentEvent(this, output, (a, b, c) => newTextContentEvent.Raise(this, EventArgs.Empty), TextEditorUIContextManagerConstants.ORDER_TEXTMARKERSERVICE);
		}

		public void AddOutputData(object key, object data) {
			if (key == null)
				throw new ArgumentNullException();
			outputData.Add(key, data);
		}

		public object GetOutputData(object key) {
			if (key == null)
				throw new ArgumentNullException();
			object data;
			outputData.TryGetValue(key, out data);
			return data;
		}
		readonly Dictionary<object, object> outputData = new Dictionary<object, object>();

		public event EventHandler<EventArgs> NewTextContent {
			add { newTextContentEvent.Add(value); }
			remove { newTextContentEvent.Remove(value); }
		}
		readonly WeakEventList<EventArgs> newTextContentEvent;

		public void OnUseNewRendererChanged() {
			textEditorControl.OnUseNewRendererChanged();
		}

		void ITextEditorHelper.FollowReference(CodeReference codeRef, bool newTab) {
			Debug.Assert(FileTab != null);
			if (FileTab == null)
				return;
			FileTab.FollowReference(codeRef, newTab);
		}

		void ITextEditorHelper.SetFocus() {
			FileTab.TrySetFocus();
		}

		public void SetActive() {
			FileTab.FileTabManager.ActiveTab = FileTab;
		}

		public void ShowCancelButton(Action onCancel, string msg) {
			textEditorControl.ShowCancelButton(onCancel, msg);
		}

		public void HideCancelButton() {
			textEditorControl.HideCancelButton();
		}

		public void MoveCaretTo(object @ref) {
			textEditorControl.GoToLocation(@ref);
		}

		public object GetReferenceSegmentAt(MouseEventArgs e) {
			return textEditorControl.GetReferenceSegmentAt(e);
		}

		public void Dispose() {
			textEditorUIContextManagerImpl.RaiseRemovedEvent(this);
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorControl);
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTEDITOR, textEditorControl.TextEditor);
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT_TEXTAREA, textEditorControl.TextEditor.TextArea);
			textEditorControl.Dispose();
			outputData.Clear();
		}

		public void ScrollAndMoveCaretTo(int line, int column) {
			textEditorControl.ScrollAndMoveCaretTo(line, column);
		}

		public object SelectedReference {
			get {
				var refSeg = textEditorControl.GetCurrentReferenceSegment();
				return refSeg == null ? null : refSeg.Reference;
			}
		}

		public CodeReference SelectedCodeReference {
			get {
				var refSeg = textEditorControl.GetCurrentReferenceSegment();
				return refSeg == null ? null : refSeg.ToCodeReference();
			}
		}

		public IEnumerable<CodeReference> GetSelectedCodeReferences() {
			return textEditorControl.GetSelectedCodeReferences();
		}

		public IEnumerable<object> References {
			get { return textEditorControl.AllReferences; }
		}

		public IEnumerable<Tuple<CodeReference, TextEditorLocation>> GetCodeReferences(int line, int column) {
			return textEditorControl.GetCodeReferences(line, column);
		}
	}
}
