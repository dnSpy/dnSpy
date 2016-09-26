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
using System.Windows.Input;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;
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

		public override void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) {
			var spanData = GetReferenceAndUpdateCursor(e);
			if (spanData == null)
				return;
			var documentViewer = TryGetDocumentViewer();
			if (documentViewer == null)
				return;

			bool newTab = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
			e.Handled = documentViewer.GoTo(spanData, newTab, false, true, true);
		}

		public override void PostprocessMouseMove(MouseEventArgs e) => GetReferenceAndUpdateCursor(e);

		SpanData<ReferenceInfo>? GetReferenceAndUpdateCursor(MouseEventArgs e) {
			var newRef = GetReferenceCore(e);
			if (SameSpan(newRef, prevRef)) {
			}
			else if (newRef != null) {
				prevRef = newRef;
				if (oldCursor == null)
					oldCursor = wpfTextView.VisualElement.Cursor;
				wpfTextView.VisualElement.Cursor = Cursors.Hand;
			}
			else
				RestoreState();
			oldModifierKeys = Keyboard.Modifiers;
			return newRef;
		}

		void RestoreState() {
			if (oldCursor != null)
				wpfTextView.VisualElement.Cursor = oldCursor;
			prevRef = null;
			oldCursor = null;
			oldModifierKeys = Keyboard.Modifiers;
		}

		SpanData<ReferenceInfo>? GetReferenceCore(MouseEventArgs e) {
			if (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Control)
				return null;

			var documentViewer = TryGetDocumentViewer();
			if (documentViewer == null)
				return null;

			var loc = MouseLocation.TryCreateTextOnly(documentViewer.TextView, e);
			if (loc == null || loc.Position.IsInVirtualSpace)
				return null;
			int pos = loc.Position.Position.Position;
			var spanData = documentViewer.Content.ReferenceCollection.Find(pos, false);
			if (spanData == null)
				return null;
			if (spanData.Value.Data.Reference == null)
				return null;
			if (Keyboard.Modifiers != ModifierKeys.Control) {
				if (spanData.Value.Data.IsDefinition)
					return null;
				if (spanData.Value.Data.IsLocal)
					return null;
			}

			return spanData;
		}
		SpanData<ReferenceInfo>? prevRef;
		Cursor oldCursor;

		public override void PostprocessMouseLeave(MouseEventArgs e) => RestoreState();

		static bool SameSpan(SpanData<ReferenceInfo>? a, SpanData<ReferenceInfo>? b) {
			if (a == null && b == null)
				return true;
			if (a == null || b == null)
				return false;
			return a.Value.Span == b.Value.Span;
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) => RestoreState();

		void VisualElement_PreviewKeyDown(object sender, KeyEventArgs e) => UpdateModifiers();
		void VisualElement_PreviewKeyUp(object sender, KeyEventArgs e) => UpdateModifiers();
		void UpdateModifiers() {
			if (oldModifierKeys != Keyboard.Modifiers)
				GetReferenceAndUpdateCursor(new MouseEventArgs(Mouse.PrimaryDevice, 0));
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
