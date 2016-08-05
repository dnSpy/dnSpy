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
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedDnSpyTextViewRoles.DocumentViewer)]
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
		readonly Dictionary<string, Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata>> documentViewerReferenceEnablerProviders;

		[ImportingConstructor]
		HighlightReferencesDocumentViewerListener([ImportMany] Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata>[] documentViewerReferenceEnablerProviders) {
			this.documentViewerReferenceEnablerProviders = new Dictionary<string, Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata>>(documentViewerReferenceEnablerProviders.Length, StringComparer.Ordinal);
			foreach (var lazy in documentViewerReferenceEnablerProviders) {
				string id = lazy.Metadata.Id;
				Debug.Assert(id != null);
				if (id == null)
					continue;
				bool b = this.documentViewerReferenceEnablerProviders.ContainsKey(id);
				Debug.Assert(!b);
				if (b)
					continue;
				this.documentViewerReferenceEnablerProviders.Add(id, lazy);
			}
		}

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.Added)
				DocumentViewerHighlightReferencesTagger.OnDocumentViewerCreated(e.DocumentViewer, documentViewerReferenceEnablerProviders);
		}
	}

	sealed class DocumentViewerHighlightReferencesTagger : ITagger<ITextMarkerTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly ITextView textView;
		SpanData<ReferenceInfo>? currentReference;
		SpanData<ReferenceAndId>? currentSpanReference;
		SpanDataCollection<ReferenceAndId> spanReferenceCollection;
		IDocumentViewer documentViewer;
		Dictionary<string, Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata>> documentViewerReferenceEnablerProviders;
		Dictionary<string, IDocumentViewerReferenceEnabler> documentViewerReferenceEnablers;
		bool canHighlightReferences;

		DocumentViewerHighlightReferencesTagger(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			this.spanReferenceCollection = SpanDataCollection<ReferenceAndId>.Empty;
			textView.Closed += TextView_Closed;
			textView.Options.OptionChanged += Options_OptionChanged;
			textView.Caret.PositionChanged += Caret_PositionChanged;
			UpdateReferenceHighlighting();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultDnSpyTextViewOptions.ReferenceHighlightingId.Name)
				UpdateReferenceHighlighting();
		}

		void UpdateReferenceHighlighting() {
			canHighlightReferences = textView.Options.IsReferenceHighlightingEnabled();
			currentReference = GetCurrentReference();
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
			if (documentViewer == null)
				return;
			if (currentReference == null && currentSpanReference == null)
				return;
			currentReference = null;
			currentSpanReference = null;
			RefreshAllTags();
		}

		public static void OnDocumentViewerCreated(IDocumentViewer documentViewer, Dictionary<string, Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata>> documentViewerReferenceEnablerProviders) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			GetOrCreate(documentViewer.TextView).SetDocumentViewer(documentViewer, documentViewerReferenceEnablerProviders);
		}

		void SetDocumentViewer(IDocumentViewer documentViewer, Dictionary<string, Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata>> documentViewerReferenceEnablerProviders) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			if (documentViewerReferenceEnablerProviders == null)
				throw new ArgumentNullException(nameof(documentViewerReferenceEnablerProviders));
			if (this.documentViewer != null)
				throw new InvalidOperationException();
			this.documentViewer = documentViewer;
			this.documentViewerReferenceEnablerProviders = documentViewerReferenceEnablerProviders;
			this.documentViewerReferenceEnablers = new Dictionary<string, IDocumentViewerReferenceEnabler>(documentViewerReferenceEnablerProviders.Count, StringComparer.Ordinal);
			documentViewer.GotNewContent += DocumentViewer_GotNewContent;
		}

		void DocumentViewer_GotNewContent(object sender, DocumentViewerGotNewContentEventArgs e) {
			spanReferenceCollection = documentViewer?.Content.GetCustomData<SpanDataCollection<ReferenceAndId>>(DocumentViewerContentDataIds.SpanReference) ?? SpanDataCollection<ReferenceAndId>.Empty;
			currentReference = GetCurrentReference();
			currentSpanReference = GetCurrentSpanReference();
		}

		SpanData<ReferenceInfo>? GetCurrentReference() => canHighlightReferences ? documentViewer?.SelectedReference : null;

		SpanData<ReferenceAndId>? GetCurrentSpanReference() {
			if (documentViewer == null)
				return null;
			var spanData = SpanDataCollectionUtilities.GetCurrentSpanReference(spanReferenceCollection, documentViewer.TextView);
			return spanData?.Data.Reference == null ? null : spanData;
		}

		static readonly ITextMarkerTag HighlightedDefinitionTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.HighlightedDefinition);
		static readonly ITextMarkerTag HighlightedWrittenReferenceTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.HighlightedWrittenReference);
		static readonly ITextMarkerTag HighlightedReferenceTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.HighlightedReference);

		ITextMarkerTag TryGetTextMarkerTag(SpanData<ReferenceInfo> spanData) {
			if (spanData.Data.Reference == null)
				return null;
			if (spanData.Data.IsDefinition)
				return HighlightedDefinitionTag;
			return spanData.Data.IsWrite ? HighlightedWrittenReferenceTag : HighlightedReferenceTag;
		}

		bool IsEnabled(string id) {
			// A null id is always enabled
			if (id == null)
				return true;

			IDocumentViewerReferenceEnabler refChecker;
			if (!documentViewerReferenceEnablers.TryGetValue(id, out refChecker)) {
				Lazy<IDocumentViewerReferenceEnablerProvider, IDocumentViewerReferenceEnablerProviderMetadata> lazy;
				bool b = documentViewerReferenceEnablerProviders.TryGetValue(id, out lazy);
				Debug.Assert(b, $"Missing {nameof(IDocumentViewerReferenceEnablerProvider)} for reference id = {id}");
				if (b) {
					refChecker = lazy.Value.Create(documentViewer);
					if (refChecker != null)
						refChecker.IsEnabledChanged += DocumentViewerReferenceEnabler_IsEnabledChanged;
				}
				else
					refChecker = null;
				documentViewerReferenceEnablers.Add(id, refChecker);
			}

			return refChecker?.IsEnabled ?? true;
		}

		void DocumentViewerReferenceEnabler_IsEnabledChanged(object sender, EventArgs e) {
			if (documentViewer.TextView.IsClosed)
				return;
			RefreshAllTags();
		}

		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (documentViewer == null)
				yield break;
			if (documentViewer.TextView.IsClosed)
				yield break;
			Debug.Assert(documentViewerReferenceEnablerProviders != null);
			Debug.Assert(documentViewerReferenceEnablers != null);

			// It's not common for both references to be non-null but it does happen if it's VB and the reference
			// is at eg. a Get keyword. For that reason, check for span refs first or we won't see the definition
			// highlight because it's hidden behind another span reference.
			if (currentSpanReference != null) {
				if (spans.Count == 0)
					yield break;
				var snapshot = spans[0].Snapshot;
				var theRef = currentSpanReference.Value;
				foreach (var span in spans) {
					foreach (var spanData in spanReferenceCollection.Find(span.Span)) {
						if (spanData.Span.End > snapshot.Length)
							continue;
						if (!IsEnabled(spanData.Data.Id))
							continue;
						if (spanData.Data.Reference == null)
							continue;
						if (!object.Equals(spanData.Data.Reference, theRef.Data.Reference))
							continue;
						yield return new TagSpan<ITextMarkerTag>(new SnapshotSpan(snapshot, spanData.Span), HighlightedReferenceTag);
					}
				}
			}

			if (currentReference != null) {
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
		}

		static bool IsSameReference(SpanData<ReferenceInfo>? a, SpanData<ReferenceInfo>? b) {
			if (a == null && b == null)
				return true;
			if (a == null || b == null)
				return false;
			return SpanDataReferenceInfoExtensions.CompareReferences(a.Value.Data, b.Value.Data);
		}

		static bool IsSameReference(SpanData<ReferenceAndId>? a, SpanData<ReferenceAndId>? b) {
			if (a == null && b == null)
				return true;
			if (a == null || b == null)
				return false;
			return object.Equals(a.Value.Data.Reference, b.Value.Data.Reference);
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (documentViewer == null)
				return;

			bool refresh = false;

			var newRef = GetCurrentReference();
			if (newRef != null) {
				if (!IsSameReference(newRef, currentReference))
					refresh = true;
			}

			var newSpanRef = GetCurrentSpanReference();
			if (newSpanRef != null) {
				if (!IsSameReference(newSpanRef, currentSpanReference))
					refresh = true;
			}

			if (((currentReference == null) != (newRef == null)) || ((currentSpanReference == null) != (newSpanRef == null)))
				refresh = true;

			if (refresh) {
				currentReference = newRef;
				currentSpanReference = newSpanRef;
				RefreshAllTags();
			}
		}

		void RefreshAllTags() {
			var snapshot = textView.TextSnapshot;
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
		}

		void TextView_Closed(object sender, EventArgs e) {
			if (documentViewerReferenceEnablers != null) {
				foreach (var v in documentViewerReferenceEnablers.Values) {
					v.IsEnabledChanged -= DocumentViewerReferenceEnabler_IsEnabledChanged;
					v.Dispose();
				}
			}
			documentViewerReferenceEnablers = null;
			documentViewerReferenceEnablerProviders = null;
			currentReference = null;
			currentSpanReference = null;
			spanReferenceCollection = SpanDataCollection<ReferenceAndId>.Empty;
			textView.Closed -= TextView_Closed;
			textView.Options.OptionChanged -= Options_OptionChanged;
			textView.Caret.PositionChanged -= Caret_PositionChanged;
			if (documentViewer != null)
				documentViewer.GotNewContent -= DocumentViewer_GotNewContent;
		}
	}
}
