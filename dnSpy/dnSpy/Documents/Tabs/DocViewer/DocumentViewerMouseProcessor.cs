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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	[Export(typeof(IMouseProcessorProvider))]
	[Name(PredefinedDsMouseProcessorProviders.DocumentViewer)]
	[ContentType(ContentTypes.Any)]
	[TextViewRole(PredefinedDsTextViewRoles.DocumentViewer)]
	sealed class DocumentViewerMouseProcessorProvider : IMouseProcessorProvider {
		public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView) =>
			wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(DocumentViewerMouseProcessor), () => new DocumentViewerMouseProcessor(wpfTextView));
	}

	sealed class DocumentViewerMouseProcessor : MouseProcessorBase {
		readonly IWpfTextView wpfTextView;

		public DocumentViewerMouseProcessor(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			this.wpfTextView = wpfTextView;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			wpfTextView.VisualElement.PreviewKeyDown += VisualElement_PreviewKeyDown;
			wpfTextView.VisualElement.PreviewKeyUp += VisualElement_PreviewKeyUp;
		}

		DocumentViewer TryGetDocumentViewer() {
			if (__documentViewer == null)
				__documentViewer = DocumentViewer.TryGetInstance(wpfTextView);
			return __documentViewer;
		}
		DocumentViewer __documentViewer;

		struct MouseReferenceInfo {
			public SpanData<ReferenceInfo>? SpanData { get; }
			public SpanData<ReferenceInfo>? RealSpanData { get; }
			readonly int virtualSpaces;
			readonly int position;
			readonly int versionNumber;

			public bool IsClickable => SpanData != null || (RealSpanData != null && Keyboard.Modifiers == ModifierKeys.Control);

			public MouseReferenceInfo(SpanData<ReferenceInfo>? spanData, SpanData<ReferenceInfo>? realSpanData, VirtualSnapshotPoint point) {
				SpanData = spanData;
				RealSpanData = realSpanData;
				virtualSpaces = point.VirtualSpaces;
				position = point.Position.Position;
				versionNumber = point.Position.Snapshot.Version.VersionNumber;
			}

			public bool IsSamePoint(MouseReferenceInfo other) =>
				other.virtualSpaces == virtualSpaces &&
				other.position == position &&
				other.versionNumber == versionNumber;
		}

		public override void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) {
			RestoreState();
			clickedRef = GetReferenceCore(e);
			if (clickedRef != null)
				UpdateCursor(clickedRef.Value.IsClickable);
		}

		void UpdateCursor(bool canClick) {
			if (oldCursor == null)
				oldCursor = wpfTextView.VisualElement.Cursor;
			wpfTextView.VisualElement.Cursor = canClick ? Cursors.Hand : oldCursor;
			oldModifierKeys = Keyboard.Modifiers;
		}

		void RestoreState() {
			if (oldCursor != null)
				wpfTextView.VisualElement.Cursor = oldCursor;
			clickedRef = null;
			oldCursor = null;
			oldModifierKeys = Keyboard.Modifiers;
		}

		bool CanClick(MouseEventArgs e, MouseReferenceInfo? newRef) {
			if (newRef == null || !newRef.Value.IsClickable)
				return false;
			if (clickedRef == null)
				return true;
			if (!clickedRef.Value.IsClickable)
				return false;
			return clickedRef.Value.IsSamePoint(newRef.Value);
		}

		public override void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e) {
			try {
				var newRef = GetReferenceCore(e);
				if (!CanClick(e, newRef))
					return;
				Debug.Assert(newRef != null);
				var documentViewer = TryGetDocumentViewer();
				if (documentViewer == null)
					return;
				if (newRef?.RealSpanData == null)
					return;

				bool newTab = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
				e.Handled = documentViewer.GoTo(newRef.Value.RealSpanData, newTab, false, true, true, MoveCaretOptions.None);
			}
			finally {
				RestoreState();
				if (CanClick(e, GetReferenceCore(e)))
					UpdateCursor(true);
			}
		}

		void UpdateMouseCursor(MouseEventArgs e) =>
			UpdateCursor(CanClick(e, GetReferenceCore(e)));

		public override void PostprocessMouseMove(MouseEventArgs e) => UpdateMouseCursor(e);

		MouseReferenceInfo? GetReferenceCore(MouseEventArgs e) {
			if (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Control)
				return null;

			var documentViewer = TryGetDocumentViewer();
			if (documentViewer == null)
				return null;

			var loc = MouseLocation.Create(documentViewer.TextView, e, insertionPosition: false);
			if (loc == null)
				return null;
			if (loc.Position.IsInVirtualSpace)
				return new MouseReferenceInfo(null, null, loc.Position);
			int pos = loc.Position.Position.Position;
			var spanData = documentViewer.Content.ReferenceCollection.Find(pos, false);
			if (spanData == null)
				return new MouseReferenceInfo(null, spanData, loc.Position);
			if (spanData.Value.Data.Reference == null)
				return new MouseReferenceInfo(null, spanData, loc.Position);
			if (Keyboard.Modifiers != ModifierKeys.Control) {
				if (spanData.Value.Data.IsDefinition)
					return new MouseReferenceInfo(null, spanData, loc.Position);
				if (spanData.Value.Data.IsLocal)
					return new MouseReferenceInfo(null, spanData, loc.Position);
			}

			return new MouseReferenceInfo(spanData, spanData, loc.Position);
		}
		MouseReferenceInfo? clickedRef;
		Cursor oldCursor;

		public override void PostprocessMouseLeave(MouseEventArgs e) => RestoreState();

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldSnapshot != e.NewSnapshot)
				RestoreState();
		}

		void VisualElement_PreviewKeyDown(object sender, KeyEventArgs e) => UpdateModifiers();
		void VisualElement_PreviewKeyUp(object sender, KeyEventArgs e) => UpdateModifiers();
		void UpdateModifiers() {
			if (oldModifierKeys != Keyboard.Modifiers)
				UpdateMouseCursor(new MouseEventArgs(Mouse.PrimaryDevice, 0));
		}
		ModifierKeys oldModifierKeys;

		void WpfTextView_Closed(object sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			wpfTextView.VisualElement.PreviewKeyDown -= VisualElement_PreviewKeyDown;
			wpfTextView.VisualElement.PreviewKeyUp -= VisualElement_PreviewKeyUp;
		}
	}
}
