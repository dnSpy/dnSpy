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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer {
	interface IDocumentViewerHelper {
		void FollowReference(TextReference textRef, bool newTab);
		void SetFocus();
		void SetActive();
	}

	sealed class DocumentViewer : IDocumentViewer, IDocumentViewerHelper, IZoomable, IDisposable {
		readonly IWpfCommandManager wpfCommandManager;
		readonly IDocumentViewerServiceImpl documentViewerServiceImpl;
		readonly DocumentViewerControl documentViewerControl;

		public event EventHandler<DocumentViewerNewContentEventArgs> NewContent;
		public event EventHandler<DocumentViewerRemovedEventArgs> Removed;

		FrameworkElement IDocumentViewer.UIObject => documentViewerControl;
		double IZoomable.ScaleValue => documentViewerControl.TextView.ZoomLevel / 100.0;
		IDnSpyWpfTextViewHost IDocumentViewer.TextViewHost => documentViewerControl.TextViewHost;
		IDnSpyTextView IDocumentViewer.TextView => documentViewerControl.TextView;
		ITextCaret IDocumentViewer.Caret => documentViewerControl.TextView.Caret;
		ITextSelection IDocumentViewer.Selection => documentViewerControl.TextView.Selection;
		DocumentViewerContent IDocumentViewer.Content => documentViewerControl.Content;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly DocumentViewer uiContext;

			public GuidObjectsCreator(DocumentViewer uiContext) {
				this.uiContext = uiContext;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_DOCUMENTVIEWER_GUID, uiContext);

				var teCtrl = (DocumentViewerControl)args.CreatorObject.Object;
				var loc = teCtrl.TextView.GetTextEditorLocation(args.OpenedFromKeyboard);
				if (loc != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORLOCATION_GUID, loc);

					int pos = teCtrl.TextView.LineColumnToPosition(loc.Value.Line, loc.Value.Column);
					var @ref = teCtrl.GetTextReferenceAt(pos);
					if (@ref != null)
						yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, @ref.Value.ToTextReference());
				}
			}
		}

		public DocumentViewer(IWpfCommandManager wpfCommandManager, IDocumentViewerServiceImpl documentViewerServiceImpl, IMenuManager menuManager, DocumentViewerControl documentViewerControl) {
			if (wpfCommandManager == null)
				throw new ArgumentNullException(nameof(wpfCommandManager));
			if (documentViewerServiceImpl == null)
				throw new ArgumentNullException(nameof(documentViewerServiceImpl));
			if (menuManager == null)
				throw new ArgumentNullException(nameof(menuManager));
			if (documentViewerControl == null)
				throw new ArgumentNullException(nameof(documentViewerControl));
			this.wpfCommandManager = wpfCommandManager;
			this.documentViewerServiceImpl = documentViewerServiceImpl;
			this.documentViewerControl = documentViewerControl;
			menuManager.InitializeContextMenu(documentViewerControl, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID, new GuidObjectsCreator(this), new ContextMenuInitializer(documentViewerControl.TextView, documentViewerControl));
			wpfCommandManager.Add(CommandConstants.GUID_DOCUMENTVIEWER_UICONTEXT, documentViewerControl);
		}

		public IFileTab FileTab {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return fileTab;
			}
			set {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
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
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				var button = documentViewerControl.CancelButton;
				if (button?.IsVisible == true)
					return button;
				return documentViewerControl.TextView.VisualElement;
			}
		}

		public object UIObject {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl;
			}
		}

		public FrameworkElement ScaleElement {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl.TextView.VisualElement; ;
			}
		}

		public TextEditorLocation Location {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl.TextView.GetTextEditorLocation(); ;
			}
		}

		public void OnShow() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
		}

		public void OnHide() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.Clear();
			outputData.Clear();
		}

		public object Serialize() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (cachedEditorPositionState != null)
				return cachedEditorPositionState;
			return new EditorPositionState(documentViewerControl.TextView);
		}

		public void Deserialize(object obj) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			var state = obj as EditorPositionState;
			if (state == null)
				return;

			var textView = documentViewerControl.TextView;
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
			var textView = documentViewerControl.TextView;

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
			var textView = documentViewerControl.TextView;
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
			documentViewerControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
			if (cachedEditorPositionState == null)
				return;
			InitializeState(cachedEditorPositionState);
			cachedEditorPositionState = null;
		}

		public object CreateSerialized(ISettingsSection section) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (section == null)
				throw new ArgumentNullException(nameof(section));
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
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (section == null)
				throw new ArgumentNullException(nameof(section));
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

		public void SetContent(DocumentViewerContent content, IContentType contentType) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if (documentViewerControl.SetContent(content, contentType)) {
				outputData.Clear();
				var newContentType = documentViewerControl.TextView.TextBuffer.ContentType;
				NewContent?.Invoke(this, new DocumentViewerNewContentEventArgs(this, content, newContentType));
				documentViewerServiceImpl.RaiseNewContentEvent(this, content, newContentType);
			}
		}

		public void AddContentData(object key, object data) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			outputData.Add(key, data);
		}

		public object GetContentData(object key) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			object data;
			outputData.TryGetValue(key, out data);
			return data;
		}
		readonly Dictionary<object, object> outputData = new Dictionary<object, object>();

		void IDocumentViewerHelper.FollowReference(TextReference textRef, bool newTab) {
			Debug.Assert(!isDisposed);
			if (isDisposed)
				return;
			Debug.Assert(FileTab != null);
			if (FileTab == null)
				return;
			FileTab.FollowReference(textRef, newTab);
		}

		void IDocumentViewerHelper.SetFocus() {
			Debug.Assert(!isDisposed);
			if (isDisposed)
				return;
			FileTab.TrySetFocus();
		}

		void IDocumentViewerHelper.SetActive() {
			Debug.Assert(!isDisposed);
			if (isDisposed)
				return;
			FileTab.FileTabManager.ActiveTab = FileTab;
		}

		public void HideCancelButton() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.HideCancelButton();
		}

		public void MoveCaretTo(object @ref) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.GoToLocation(@ref);
		}

		public void ShowCancelButton(Action onCancel, string message) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (onCancel == null)
				throw new ArgumentNullException(nameof(onCancel));
			documentViewerControl.ShowCancelButton(onCancel, message);
		}

		bool isDisposed;
		public void Dispose() {
			if (isDisposed)
				return;
			isDisposed = true;
			documentViewerControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
			Removed?.Invoke(this, new DocumentViewerRemovedEventArgs(this));
			documentViewerServiceImpl.RaiseRemovedEvent(this);
			wpfCommandManager.Remove(CommandConstants.GUID_DOCUMENTVIEWER_UICONTEXT, documentViewerControl);
			documentViewerControl.Dispose();
			outputData.Clear();
		}

		public void ScrollAndMoveCaretTo(int line, int column) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (line < 0)
				throw new ArgumentOutOfRangeException(nameof(line));
			if (column < 0)
				throw new ArgumentOutOfRangeException(nameof(column));
			documentViewerControl.ScrollAndMoveCaretTo(line, column);
		}

		public SpanData<ReferenceInfo>? SelectedReferenceInfo {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl.GetCurrentReferenceInfo();
			}
		}

		public IEnumerable<SpanData<ReferenceInfo>> GetSelectedTextReferences() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.GetSelectedTextReferences(); ;
		}

		public object SaveReferencePosition() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.SaveReferencePosition(this.GetCodeMappings());
		}

		public bool RestoreReferencePosition(object obj) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.RestoreReferencePosition(this.GetCodeMappings(), obj);
		}
	}
}
