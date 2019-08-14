/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_LINESEPARATORSERVICE)]
	sealed class LineSeparatorDocumentViewerListener : IDocumentViewerListener {
		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.GotNewContent)
				LineSeparatorViewTagger.GetInstance(e.DocumentViewer.TextView).SetLineSeparatorCollection(e.DocumentViewer.Content.GetCustomData<LineSeparatorCollection>(DocumentViewerContentDataIds.LineSeparator));
		}
	}

	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveLineSeparator)]
	[TagType(typeof(ILineSeparatorTag))]
	sealed class LineSeparatorViewTaggerProvider : IViewTaggerProvider {
		public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (textView.TextBuffer != buffer)
				return null;
			return LineSeparatorViewTagger.GetInstance(textView) as ITagger<T>;
		}
	}

	sealed class LineSeparatorViewTagger : ITagger<ILineSeparatorTag> {
		public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

		readonly ITextView textView;
		LineSeparatorCollection lineSeparatorCollection;

		public LineSeparatorViewTagger(ITextView textView) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			lineSeparatorCollection = LineSeparatorCollection.Empty;
			textView.Closed += TextView_Closed;
		}

		public static LineSeparatorViewTagger GetInstance(ITextView textView) =>
			textView.Properties.GetOrCreateSingletonProperty(typeof(LineSeparatorViewTagger), () => new LineSeparatorViewTagger(textView));

		public void SetLineSeparatorCollection(LineSeparatorCollection? coll) {
			if (textView.IsClosed)
				return;
			if (coll is null)
				coll = LineSeparatorCollection.Empty;
			if (lineSeparatorCollection == coll)
				return;
			lineSeparatorCollection = coll;
			RefreshAllTags();
		}

		void RefreshAllTags() {
			var snapshot = textView.TextSnapshot;
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
		}

		public IEnumerable<ITagSpan<ILineSeparatorTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (textView.IsClosed)
				yield break;
			if (spans.Count == 0)
				yield break;
			var snapshot = spans[0].Snapshot;
			foreach (var span in spans) {
				foreach (var pos in lineSeparatorCollection.Find(span.Span)) {
					if (pos > snapshot.Length)
						yield break;// Old data, we'll get called again
					yield return new TagSpan<ILineSeparatorTag>(new SnapshotSpan(snapshot, pos, 0), lineSeparatorTag);
				}
			}
		}
		static readonly ILineSeparatorTag lineSeparatorTag = new LineSeparatorTag(true);

		void TextView_Closed(object? sender, EventArgs e) {
			lineSeparatorCollection = LineSeparatorCollection.Empty;
			textView.Closed -= TextView_Closed;
		}
	}
}
