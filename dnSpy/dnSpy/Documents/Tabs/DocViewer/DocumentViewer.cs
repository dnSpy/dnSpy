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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	interface IDocumentViewerHelper {
		void FollowReference(TextReference textRef, bool newTab);
		void SetFocus();
		void SetActive();
	}

	sealed class DocumentViewer : DocumentTabUIContext, IDocumentViewer, IDocumentViewerHelper, IZoomable, IDisposable {
		readonly IWpfCommandService wpfCommandService;
		readonly IDocumentViewerServiceImpl documentViewerServiceImpl;
		readonly DocumentViewerControl documentViewerControl;

		public event EventHandler<DocumentViewerGotNewContentEventArgs> GotNewContent;
		public event EventHandler<DocumentViewerRemovedEventArgs> Removed;

		FrameworkElement IDocumentViewer.UIObject => documentViewerControl;
		double IZoomable.ZoomValue => documentViewerControl.TextView.ZoomLevel / 100.0;
		IDsWpfTextViewHost IDocumentViewer.TextViewHost => documentViewerControl.TextViewHost;
		public IDsWpfTextView TextView => documentViewerControl.TextView;
		ITextCaret IDocumentViewer.Caret => documentViewerControl.TextView.Caret;
		ITextSelection IDocumentViewer.Selection => documentViewerControl.TextView.Selection;
		public DocumentViewerContent Content => documentViewerControl.Content;
		SpanDataCollection<ReferenceInfo> IDocumentViewer.ReferenceCollection => documentViewerControl.Content.ReferenceCollection;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly DocumentViewer documentViewer;

			public GuidObjectsProvider(DocumentViewer documentViewer) {
				this.documentViewer = documentViewer;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_DOCUMENTVIEWER_GUID, documentViewer);

				var dvCtrl = documentViewer.documentViewerControl;
				var loc = dvCtrl.TextView.GetTextEditorPosition(args.OpenedFromKeyboard);
				if (loc != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORPOSITION_GUID, loc);

					var @ref = dvCtrl.GetReferenceInfo(loc.Position);
					if (@ref != null)
						yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, @ref.Value.ToTextReference());
				}
			}
		}

		public DocumentViewer(IWpfCommandService wpfCommandService, IDocumentViewerServiceImpl documentViewerServiceImpl, IMenuService menuService, DocumentViewerControl documentViewerControl) {
			if (wpfCommandService == null)
				throw new ArgumentNullException(nameof(wpfCommandService));
			if (documentViewerServiceImpl == null)
				throw new ArgumentNullException(nameof(documentViewerServiceImpl));
			if (menuService == null)
				throw new ArgumentNullException(nameof(menuService));
			if (documentViewerControl == null)
				throw new ArgumentNullException(nameof(documentViewerControl));
			this.wpfCommandService = wpfCommandService;
			this.documentViewerServiceImpl = documentViewerServiceImpl;
			this.documentViewerControl = documentViewerControl;
			menuService.InitializeContextMenu(documentViewerControl.TextView.VisualElement, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID, new GuidObjectsProvider(this), new ContextMenuInitializer(documentViewerControl.TextView));
			// Prevent the tab control's context menu from popping up when right-clicking in the textview host margin
			menuService.InitializeContextMenu(documentViewerControl, Guid.NewGuid());
			wpfCommandService.Add(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT, documentViewerControl);
			documentViewerControl.TextView.Properties.AddProperty(typeof(DocumentViewer), this);
			documentViewerControl.TextView.TextBuffer.Properties.AddProperty(DocumentViewerExtensions.DocumentViewerTextBufferKey, this);
		}

		internal static DocumentViewer TryGetInstance(ITextView textView) {
			DocumentViewer documentViewer;
			textView.Properties.TryGetProperty(typeof(DocumentViewer), out documentViewer);
			return documentViewer;
		}

		public override IInputElement FocusedElement {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				var button = documentViewerControl.CancelButton;
				if (button?.IsVisible == true)
					return button;
				return documentViewerControl.TextView.VisualElement;
			}
		}

		public override object UIObject {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl;
			}
		}

		public override FrameworkElement ZoomElement {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl.TextView.VisualElement;
			}
		}

		public override void OnShow() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
		}

		public override void OnHide() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.Clear();
			outputData.Clear();
		}

		public override object CreateUIState() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (cachedEditorPositionState != null)
				return cachedEditorPositionState;
			return new EditorPositionState(documentViewerControl.TextView);
		}

		public override void RestoreUIState(object obj) {
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
			textView.Selection.Clear();
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

		public override object DeserializeUIState(ISettingsSection section) {
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

		public override void SerializeUIState(ISettingsSection section, object obj) {
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

		public bool SetContent(DocumentViewerContent content, IContentType contentType) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if (documentViewerControl.SetContent(content, contentType)) {
				outputData.Clear();
				var newContentType = documentViewerControl.TextView.TextBuffer.ContentType;
				GotNewContent?.Invoke(this, new DocumentViewerGotNewContentEventArgs(this, content, newContentType));
				documentViewerServiceImpl.RaiseNewContentEvent(this, content, newContentType);
				return true;
			}
			else
				return false;
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
			Debug.Assert(DocumentTab != null);
			if (DocumentTab == null)
				return;
			DocumentTab.FollowReference(textRef, newTab);
		}

		void IDocumentViewerHelper.SetFocus() {
			Debug.Assert(!isDisposed);
			if (isDisposed)
				return;
			DocumentTab.TrySetFocus();
		}

		void IDocumentViewerHelper.SetActive() {
			Debug.Assert(!isDisposed);
			if (isDisposed)
				return;
			DocumentTab.DocumentTabService.ActiveTab = DocumentTab;
		}

		public void HideCancelButton() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.HideCancelButton();
		}

		public void MoveCaretToReference(object @ref, MoveCaretOptions options) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.GoToLocation(@ref, options);
		}

		public void ShowCancelButton(string message, Action onCancel) {
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
			documentViewerControl.TextView.VisualElement.Loaded -= VisualElement_Loaded;
			Removed?.Invoke(this, new DocumentViewerRemovedEventArgs(this));
			documentViewerServiceImpl.RaiseRemovedEvent(this);
			wpfCommandService.Remove(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT, documentViewerControl);
			documentViewerControl.Dispose();
			outputData.Clear();
			isDisposed = true;
		}

		public void MoveCaretToPosition(int position, MoveCaretOptions options) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.MoveCaretToPosition(position, options);
		}

		public void MoveCaretToSpan(int position, int length, MoveCaretOptions options) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.MoveCaretToSpan(new Span(position, length), options);
		}

		public void MoveCaretToSpan(Span span, MoveCaretOptions options) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.MoveCaretToSpan(span, options);
		}

		public void MoveCaretToSpan(SpanData<ReferenceInfo> refInfo, MoveCaretOptions options) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.MoveCaretToSpan(refInfo.Span, options);
		}

		public SpanData<ReferenceInfo>? SelectedReference {
			get {
				if (isDisposed)
					throw new ObjectDisposedException(nameof(IDocumentViewer));
				return documentViewerControl.GetCurrentReferenceInfo();
			}
		}

		public IEnumerable<SpanData<ReferenceInfo>> GetSelectedReferences() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.GetSelectedTextReferences();
		}

		public object SaveReferencePosition() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.SaveReferencePosition(this.GetMethodDebugService());
		}

		public bool RestoreReferencePosition(object obj) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.RestoreReferencePosition(this.GetMethodDebugService(), obj);
		}

		public void MoveReference(bool forward) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.MoveReference(forward);
		}

		public void MoveToNextDefinition(bool forward) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.MoveToNextDefinition(forward);
		}

		public void FollowReference() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.FollowReference();
		}

		public void FollowReferenceNewTab() {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			documentViewerControl.FollowReferenceNewTab();
		}

		internal bool GoTo(SpanData<ReferenceInfo>? spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canFollowReference, MoveCaretOptions options) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(IDocumentViewer));
			return documentViewerControl.GoTo(spanData, newTab, followLocalRefs, canRecordHistory, canFollowReference, options);
		}
	}
}
