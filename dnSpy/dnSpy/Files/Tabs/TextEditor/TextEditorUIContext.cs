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
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using dnSpy.Events;
using dnSpy.Text.Editor;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.TextEditor {
	interface ITextEditorHelper {
		void FollowReference(CodeReference refSeg, bool newTab);
		void SetFocus();
		void SetActive();
	}

	interface ITextEditorUIContextImpl : ITextEditorUIContext {
		/// <summary>
		/// Called when 'use new renderer' option has been changed. <see cref="ITextEditorUIContext.SetOutput(ITextOutput, IHighlightingDefinition, IContentType)"/>
		/// will be called after this method has been called.
		/// </summary>
		void OnUseNewRendererChanged();

		/// <summary>
		/// Raised after the text editor has gotten new text (<see cref="ITextEditorUIContext.SetOutput(ITextOutput, IHighlightingDefinition, IContentType)"/>)
		/// </summary>
		event EventHandler<EventArgs> NewTextContent;
	}

	sealed class TextEditorUIContext : ITextEditorUIContextImpl, ITextEditorHelper, IZoomable, IDisposable {
		readonly IWpfCommandManager wpfCommandManager;
		readonly ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl;
		TextEditorControl textEditorControl;

		double IZoomable.ScaleValue => textEditorControl.TextView.ZoomLevel / 100.0;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly TextEditorUIContext uiContext;

			public GuidObjectsCreator(TextEditorUIContext uiContext) {
				this.uiContext = uiContext;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORUICONTEXT_GUID, uiContext);

				var teCtrl = (TextEditorControl)args.CreatorObject.Object;
				foreach (var go in teCtrl.TextEditor.GetGuidObjects(args.OpenedFromKeyboard))
					yield return go;

				var position = args.OpenedFromKeyboard ? teCtrl.TextEditor.TextArea.Caret.Position : teCtrl.TextEditor.GetPositionFromMousePosition();
				var @ref = teCtrl.GetReferenceSegmentAt(position);
				if (@ref != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, @ref.ToCodeReference());
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
			menuManager.InitializeContextMenu(this.textEditorControl, MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID, new GuidObjectsCreator(this), new ContextMenuInitializer(textEditorControl.TextView, textEditorControl));
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
				if (button?.IsVisible == true)
					return button;
				return textEditorControl.TextView.VisualElement;
			}
		}

		public object UIObject => textEditorControl;
		public FrameworkElement ScaleElement => textEditorControl.TextView.VisualElement;
		public bool HasSelectedText => !textEditorControl.TextView.Selection.IsEmpty;

		public TextEditorLocation Location {
			get {
				int caretPos = textEditorControl.TextView.Caret.Position.BufferPosition.Position;
				var line = textEditorControl.TextView.TextSnapshot.GetLineFromPosition(caretPos);
				return new TextEditorLocation(line.LineNumber, caretPos - line.Start.Position);
			}
		}

		public void OnShow() { }

		public void OnHide() {
			textEditorControl.Clear();
			outputData.Clear();
		}

		public object Serialize() {
			if (cachedEditorPositionState != null)
				return cachedEditorPositionState;
			return new EditorPositionState(textEditorControl.TextView);
		}

		public void Deserialize(object obj) {
			var state = obj as EditorPositionState;
			if (state == null)
				return;

			var textView = textEditorControl.TextView;
			if (!textView.VisualElement.IsLoaded) {
				bool start = cachedEditorPositionState == null;
				cachedEditorPositionState = state;
				if (start)
					textView.VisualElement.Loaded += VisualElement_Loaded;
			}
			else
				InitializeState(state);
		}
		EditorPositionState cachedEditorPositionState;

		void InitializeState(EditorPositionState state) {
			var textView = textEditorControl.TextView;

			if (IsValid(state)) {
				textView.ViewportLeft = state.ViewportLeft;
				textView.DisplayTextLineContainingBufferPosition(new SnapshotPoint(textView.TextSnapshot, state.TopLinePosition), state.TopLineVerticalDistance, ViewRelativePosition.Top);
				var newPos = new VirtualSnapshotPoint(new SnapshotPoint(textView.TextSnapshot, state.CaretPosition), state.CaretVirtualSpaces);
				textView.Caret.MoveTo(newPos, state.CaretAffinity, true);
			}
			else
				textView.Caret.MoveTo(new VirtualSnapshotPoint(textView.TextSnapshot, 0));
		}

		bool IsValid(EditorPositionState state) {
			var textView = textEditorControl.TextView;
			if (state.CaretAffinity != PositionAffinity.Successor && state.CaretAffinity != PositionAffinity.Predecessor)
				return false;
			if (state.CaretVirtualSpaces < 0)
				return false;
			if (state.CaretPosition < 0 || state.CaretPosition > textView.TextSnapshot.Length)
				return false;
			if (double.IsNaN(state.ViewportLeft) || state.ViewportLeft < 0)
				return false;
			if (state.TopLinePosition < 0 || state.TopLinePosition > textView.TextSnapshot.Length)
				return false;
			if (double.IsNaN(state.TopLineVerticalDistance))
				return false;

			return true;
		}

		void VisualElement_Loaded(object sender, RoutedEventArgs e) {
			textEditorControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
			if (cachedEditorPositionState == null)
				return;
			InitializeState(cachedEditorPositionState);
			cachedEditorPositionState = null;
		}

		public object CreateSerialized(ISettingsSection section) {
			var caretAffinity = section.Attribute<PositionAffinity?>("CaretAffinity");
			var caretVirtualSpaces = section.Attribute<int?>("CaretVirtualSpaces");
			var caretPosition = section.Attribute<int?>("CaretPosition");
			var viewportLeft = section.Attribute<double?>("ViewportLeft");
			var topLinePosition = section.Attribute<int?>("TopLinePosition");
			var topLineVerticalDistance = section.Attribute<double?>("TopLineVerticalDistance");

			if (caretAffinity == null || caretVirtualSpaces == null || caretPosition == null)
				return null;
			if (viewportLeft == null || topLinePosition == null || topLineVerticalDistance == null)
				return null;
			return new EditorPositionState(caretAffinity.Value, caretVirtualSpaces.Value, caretPosition.Value, viewportLeft.Value, topLinePosition.Value, topLineVerticalDistance.Value);
		}

		public void SaveSerialized(ISettingsSection section, object obj) {
			var state = obj as EditorPositionState;
			Debug.Assert(state != null);
			if (state == null)
				return;

			section.Attribute("CaretAffinity", state.CaretAffinity);
			section.Attribute("CaretVirtualSpaces", state.CaretVirtualSpaces);
			section.Attribute("CaretPosition", state.CaretPosition);
			section.Attribute("ViewportLeft", state.ViewportLeft);
			section.Attribute("TopLinePosition", state.TopLinePosition);
			section.Attribute("TopLineVerticalDistance", state.TopLineVerticalDistance);
		}

		public void SetOutput(ITextOutput output, IHighlightingDefinition highlighting, IContentType contentType) {
			outputData.Clear();
			textEditorControl.SetOutput(output, highlighting, contentType);
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

		public void OnUseNewRendererChanged() => textEditorControl.OnUseNewRendererChanged();

		void ITextEditorHelper.FollowReference(CodeReference codeRef, bool newTab) {
			Debug.Assert(FileTab != null);
			if (FileTab == null)
				return;
			FileTab.FollowReference(codeRef, newTab);
		}

		void ITextEditorHelper.SetFocus() => FileTab.TrySetFocus();
		public void SetActive() => FileTab.FileTabManager.ActiveTab = FileTab;
		public void ShowCancelButton(Action onCancel, string msg) => textEditorControl.ShowCancelButton(onCancel, msg);
		public void HideCancelButton() => textEditorControl.HideCancelButton();
		public void MoveCaretTo(object @ref) => textEditorControl.GoToLocation(@ref);
		public object GetReferenceSegmentAt(MouseEventArgs e) => textEditorControl.GetReferenceSegmentAt(e);

		public void Dispose() {
			this.textEditorControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
			textEditorUIContextManagerImpl.RaiseRemovedEvent(this);
			this.wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorControl);
			textEditorControl.Dispose();
			outputData.Clear();
		}

		public void ScrollAndMoveCaretTo(int line, int column) => textEditorControl.ScrollAndMoveCaretTo(line, column);
		public object SelectedReference => textEditorControl.GetCurrentReferenceSegment()?.Reference;
		public CodeReference SelectedCodeReference => textEditorControl.GetCurrentReferenceSegment()?.ToCodeReference();
		public IEnumerable<CodeReference> GetSelectedCodeReferences() => textEditorControl.GetSelectedCodeReferences();
		public IEnumerable<object> References => textEditorControl.AllReferences;
		public IEnumerable<Tuple<CodeReference, TextEditorLocation>> GetCodeReferences(int line, int column) =>
			textEditorControl.GetCodeReferences(line, column);
		public object SaveReferencePosition() => textEditorControl.SaveReferencePosition(this.GetCodeMappings());
		public bool RestoreReferencePosition(object obj) => textEditorControl.RestoreReferencePosition(this.GetCodeMappings(), obj);
	}
}
