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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer {
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.TEXT)]
	[TextViewRole(DocumentViewerConstants.TextViewRole)]
	[TagType(typeof(ITextMarkerTag))]
	sealed class HighlightReferencesViewTaggerProvider : IViewTaggerProvider {
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (textView.TextBuffer != buffer)
				return null;
			if (!textView.Roles.Contains(PredefinedTextViewRoles.Interactive))
				return null;
			return DocumentViewerHighlightReferencesTagger.GetOrCreate(textView) as ITagger<T>;
		}
	}

	[ExportDocumentViewerListener]
	sealed class HighlightReferencesDocumentViewerListener : IDocumentViewerListener {
		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.Added)
				DocumentViewerHighlightReferencesTagger.OnDocumentViewerCreated(e.DocumentViewer);
		}
	}

	sealed class DocumentViewerHighlightReferencesTagger : ITagger<ITextMarkerTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly ITextView textView;
		SpanData<ReferenceInfo>? currentReference;
		IDocumentViewer documentViewer;
		bool canHighlightReferences;

		DocumentViewerHighlightReferencesTagger(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			textView.Closed += TextView_Closed;
			textView.Options.OptionChanged += Options_OptionChanged;
			UpdateReferenceHighlighting();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultDnSpyTextViewOptions.ReferenceHighlightingId.Name)
				UpdateReferenceHighlighting();
		}

		void UpdateReferenceHighlighting() {
			canHighlightReferences = textView.Options.IsReferenceHighlightingEnabled();
			if (canHighlightReferences) {
				textView.Caret.PositionChanged += Caret_PositionChanged;
				currentReference = GetCurrentReference();
			}
			else {
				textView.Caret.PositionChanged -= Caret_PositionChanged;
				currentReference = null;
			}
			RefreshAllTags();
		}

		public static DocumentViewerHighlightReferencesTagger GetOrCreate(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.TextBuffer.Properties.GetOrCreateSingletonProperty(typeof(DocumentViewerHighlightReferencesTagger), () => new DocumentViewerHighlightReferencesTagger(textView));
		}

		public static void ClearMarkedReferences(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			GetOrCreate(textView).ClearMarkedReferences();
		}

		void ClearMarkedReferences() {
			if (!canHighlightReferences)
				return;
			if (documentViewer == null)
				return;
			if (currentReference == null)
				return;
			currentReference = null;
			RefreshAllTags();
		}

		public static void OnDocumentViewerCreated(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			GetOrCreate(documentViewer.TextView).SetDocumentViewer(documentViewer);
		}

		void SetDocumentViewer(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			if (this.documentViewer != null)
				throw new InvalidOperationException();
			this.documentViewer = documentViewer;
			documentViewer.GotNewContent += DocumentViewer_GotNewContent;
		}

		void DocumentViewer_GotNewContent(object sender, DocumentViewerGotNewContentEventArgs e) {
			if (canHighlightReferences)
				currentReference = GetCurrentReference();
		}

		SpanData<ReferenceInfo>? GetCurrentReference() => documentViewer?.SelectedReference;

		static readonly ITextMarkerTag HighlightedDefinitionTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.HighlightedDefinition);
		static readonly ITextMarkerTag HighlightedWrittenReferenceTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.HighlightedWrittenReference);
		static readonly ITextMarkerTag HighlightedReferenceTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.HighlightedReference);

		ITextMarkerTag TryGetTextMarkerTag(SpanData<ReferenceInfo> spanData) {
			if (spanData.Data.Reference == null)
				return null;
			if (spanData.Data.IsDefinition)
				return HighlightedDefinitionTag;
			const bool isWrittenReference = false;//TODO:
			return isWrittenReference ? HighlightedWrittenReferenceTag : HighlightedReferenceTag;
		}

		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (documentViewer == null)
				yield break;
			if (currentReference == null)
				yield break;
			if (spans.Count == 0)
				yield break;
			var snapshot = spans[0].Snapshot;
			var theRef = currentReference.Value;
			foreach (var span in spans) {
				foreach (var spanData in documentViewer.Content.ReferenceCollection.Find(span.Span)) {
					Debug.Assert(spanData.Span.End <= snapshot.Length);
					if (spanData.Span.End > snapshot.Length)
						continue;
					var tag = TryGetTextMarkerTag(spanData);
					if (tag == null)
						continue;
					if (!SpanDataReferenceInfoExtensions.CompareReferences(spanData.Data, theRef.Data))
						continue;
					yield return new TagSpan<ITextMarkerTag>(new SnapshotSpan(snapshot, spanData.Span), tag);
				}
			}
		}

		static bool IsSameReference(SpanData<ReferenceInfo>? a, SpanData<ReferenceInfo>? b) {
			if (a == null && b == null)
				return true;
			if (a == null || b == null)
				return false;
			return SpanDataReferenceInfoExtensions.CompareReferences(a.Value.Data, b.Value.Data);
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (documentViewer == null)
				return;
			var newRef = GetCurrentReference();
			if (IsSameReference(newRef, currentReference))
				return;
			currentReference = newRef;
			RefreshAllTags();
		}

		void RefreshAllTags() {
			var snapshot = textView.TextSnapshot;
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
		}

		void TextView_Closed(object sender, EventArgs e) {
			currentReference = null;
			textView.Closed -= TextView_Closed;
			textView.Caret.PositionChanged -= Caret_PositionChanged;
			textView.Options.OptionChanged -= Options_OptionChanged;
			if (documentViewer != null)
				documentViewer.GotNewContent -= DocumentViewer_GotNewContent;
		}
	}
}
