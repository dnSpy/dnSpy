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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.TextEditor {
	interface ITextEditorHelper {
		void FollowReference(CodeReference refSeg, bool newTab);
		void SetFocus();
		void SetActive();
	}

	sealed class TextEditorUIContext : ITextEditorUIContext, ITextEditorHelper, IZoomable, IDisposable {
		readonly IWpfCommandManager wpfCommandManager;
		readonly ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl;
		readonly TextEditorUIContextControl textEditorUIContextControl;

		double IZoomable.ScaleValue => textEditorUIContextControl.TextView.ZoomLevel / 100.0;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly TextEditorUIContext uiContext;

			public GuidObjectsCreator(TextEditorUIContext uiContext) {
				this.uiContext = uiContext;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORUICONTEXT_GUID, uiContext);

				var teCtrl = (TextEditorUIContextControl)args.CreatorObject.Object;
				var loc = teCtrl.TextView.GetTextEditorLocation(args.OpenedFromKeyboard);
				if (loc != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORLOCATION_GUID, loc);

					int pos = teCtrl.TextView.LineColumnToPosition(loc.Value.Line, loc.Value.Column);
					var @ref = teCtrl.GetCodeReferenceAt(pos);
					if (@ref != null)
						yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, @ref.Value.Data.ToCodeReference());
				}
			}
		}

		public TextEditorUIContext(IWpfCommandManager wpfCommandManager, ITextEditorUIContextManagerImpl textEditorUIContextManagerImpl, IMenuManager menuManager, TextEditorUIContextControl textEditorUIContextControl) {
			if (wpfCommandManager == null)
				throw new ArgumentNullException(nameof(wpfCommandManager));
			if (textEditorUIContextManagerImpl == null)
				throw new ArgumentNullException(nameof(textEditorUIContextManagerImpl));
			if (menuManager == null)
				throw new ArgumentNullException(nameof(menuManager));
			if (textEditorUIContextControl == null)
				throw new ArgumentNullException(nameof(textEditorUIContextControl));
			this.wpfCommandManager = wpfCommandManager;
			this.textEditorUIContextManagerImpl = textEditorUIContextManagerImpl;
			this.textEditorUIContextControl = textEditorUIContextControl;
			menuManager.InitializeContextMenu(textEditorUIContextControl, MenuConstants.GUIDOBJ_TEXTEDITORUICONTEXTCONTROL_GUID, new GuidObjectsCreator(this), new ContextMenuInitializer(textEditorUIContextControl.TextView, textEditorUIContextControl));
			wpfCommandManager.Add(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorUIContextControl);
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
				var button = textEditorUIContextControl.CancelButton;
				if (button?.IsVisible == true)
					return button;
				return textEditorUIContextControl.TextView.VisualElement;
			}
		}

		public object UIObject => textEditorUIContextControl;
		public FrameworkElement ScaleElement => textEditorUIContextControl.TextView.VisualElement;
		public bool HasSelectedText => !textEditorUIContextControl.TextView.Selection.IsEmpty;

		public TextEditorLocation Location {
			get {
				int caretPos = textEditorUIContextControl.TextView.Caret.Position.BufferPosition.Position;
				var line = textEditorUIContextControl.TextView.TextSnapshot.GetLineFromPosition(caretPos);
				return new TextEditorLocation(line.LineNumber, caretPos - line.Start.Position);
			}
		}

		public void OnShow() { }

		public void OnHide() {
			textEditorUIContextControl.Clear();
			outputData.Clear();
		}

		public object Serialize() {
			if (cachedEditorPositionState != null)
				return cachedEditorPositionState;
			return new EditorPositionState(textEditorUIContextControl.TextView);
		}

		public void Deserialize(object obj) {
			var state = obj as EditorPositionState;
			if (state == null)
				return;

			var textView = textEditorUIContextControl.TextView;
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
			var textView = textEditorUIContextControl.TextView;

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
			var textView = textEditorUIContextControl.TextView;
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
			textEditorUIContextControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
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

		public void SetOutput(DnSpyTextOutputResult result, IContentType contentType) {
			outputData.Clear();
			textEditorUIContextControl.SetOutput(result, contentType);
			textEditorUIContextManagerImpl.RaiseNewContentEvent(this, result);
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

		void ITextEditorHelper.FollowReference(CodeReference codeRef, bool newTab) {
			Debug.Assert(FileTab != null);
			if (FileTab == null)
				return;
			FileTab.FollowReference(codeRef, newTab);
		}

		void ITextEditorHelper.SetFocus() => FileTab.TrySetFocus();
		public void SetActive() => FileTab.FileTabManager.ActiveTab = FileTab;
		public void ShowCancelButton(Action onCancel, string msg) => textEditorUIContextControl.ShowCancelButton(onCancel, msg);
		public void HideCancelButton() => textEditorUIContextControl.HideCancelButton();
		public void MoveCaretTo(object @ref) => textEditorUIContextControl.GoToLocation(@ref);

		public void Dispose() {
			textEditorUIContextControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
			textEditorUIContextManagerImpl.RaiseRemovedEvent(this);
			wpfCommandManager.Remove(CommandConstants.GUID_TEXTEDITOR_UICONTEXT, textEditorUIContextControl);
			textEditorUIContextControl.Dispose();
			outputData.Clear();
		}

		public void ScrollAndMoveCaretTo(int line, int column) => textEditorUIContextControl.ScrollAndMoveCaretTo(line, column);
		public object SelectedReference => textEditorUIContextControl.GetCurrentReferenceInfo()?.Data.Reference;
		public CodeReference SelectedCodeReference => textEditorUIContextControl.GetCurrentReferenceInfo()?.Data.ToCodeReference();
		public IEnumerable<CodeReference> GetSelectedCodeReferences() => textEditorUIContextControl.GetSelectedCodeReferences();
		public IEnumerable<object> References => textEditorUIContextControl.AllReferences;
		public IEnumerable<Tuple<CodeReference, TextEditorLocation>> GetCodeReferences(int line, int column) =>
			textEditorUIContextControl.GetCodeReferences(line, column);
		public object SaveReferencePosition() => textEditorUIContextControl.SaveReferencePosition(this.GetCodeMappings());
		public bool RestoreReferencePosition(object obj) => textEditorUIContextControl.RestoreReferencePosition(this.GetCodeMappings(), obj);
	}
}
