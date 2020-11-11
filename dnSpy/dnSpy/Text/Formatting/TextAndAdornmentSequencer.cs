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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Formatting {
	sealed class TextAndAdornmentSequencer : ITextAndAdornmentSequencer {
		public IBufferGraph BufferGraph => textView.BufferGraph;
		public ITextBuffer SourceBuffer => textView.TextViewModel.EditBuffer;
		public ITextBuffer TopBuffer => textView.TextViewModel.VisualBuffer;

		readonly ITextView textView;
		readonly ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator;

		public TextAndAdornmentSequencer(ITextView textView, ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.tagAggregator = tagAggregator ?? throw new ArgumentNullException(nameof(tagAggregator));
			textView.Closed += TextView_Closed;
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
		}

		public event EventHandler<TextAndAdornmentSequenceChangedEventArgs>? SequenceChanged;

		void TagAggregator_TagsChanged(object? sender, TagsChangedEventArgs e) =>
			SequenceChanged?.Invoke(this, new TextAndAdornmentSequenceChangedEventArgs(e.Span));

		public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(ITextSnapshotLine topLine, ITextSnapshot sourceTextSnapshot) {
			if (topLine is null)
				throw new ArgumentNullException(nameof(topLine));
			if (sourceTextSnapshot is null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (topLine.Snapshot.TextBuffer != TopBuffer)
				throw new InvalidOperationException();
			if (sourceTextSnapshot.TextBuffer != SourceBuffer)
				throw new InvalidOperationException();

			if (SourceBuffer != TopBuffer)
				throw new NotSupportedException();
			return CreateTextAndAdornmentCollection(topLine.ExtentIncludingLineBreak, sourceTextSnapshot);
		}

		public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(SnapshotSpan topSpan, ITextSnapshot sourceTextSnapshot) {
			if (topSpan.Snapshot is null)
				throw new ArgumentException();
			if (sourceTextSnapshot is null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (topSpan.Snapshot.TextBuffer != TopBuffer)
				throw new InvalidOperationException();
			if (sourceTextSnapshot.TextBuffer != SourceBuffer)
				throw new InvalidOperationException();

			if (SourceBuffer != TopBuffer)
				throw new NotSupportedException();

			List<AdornmentElementAndSpan>? adornmentList = null;
			foreach (var tagSpan in tagAggregator.GetTags(topSpan)) {
				if (adornmentList is null)
					adornmentList = new List<AdornmentElementAndSpan>();
				var spans = tagSpan.Span.GetSpans(sourceTextSnapshot);
				Debug.Assert(spans.Count == 1);
				if (spans.Count != 1)
					continue;
				adornmentList.Add(new AdornmentElementAndSpan(new AdornmentElement(tagSpan), spans[0]));
			}

			// Common case
			if (adornmentList is null) {
				var elem = new TextSequenceElement(BufferGraph.CreateMappingSpan(topSpan, SpanTrackingMode.EdgeExclusive));
				return new TextAndAdornmentCollection(this, new[] { elem });
			}

			var sequenceList = new List<ISequenceElement>();
			adornmentList.Sort(AdornmentElementAndSpanComparer.Instance);
			int start = topSpan.Start;
			int end = topSpan.End;
			int curr = start;
			AdornmentElementAndSpan? lastAddedAdornment = null;
			for (int i = 0; i < adornmentList.Count; i++) {
				var info = adornmentList[i];
				int spanStart = info.Span.Length == 0 && info.AdornmentElement.Affinity == PositionAffinity.Predecessor ? info.Span.Start - 1 : info.Span.Start;
				if (spanStart < start)
					continue;
				if (info.Span.Start > end)
					break;
				var textSpan = new SnapshotSpan(topSpan.Snapshot, Span.FromBounds(curr, info.Span.Start));
				if (!textSpan.IsEmpty)
					sequenceList.Add(new TextSequenceElement(BufferGraph.CreateMappingSpan(textSpan, SpanTrackingMode.EdgeExclusive)));
				if (info.Span.Start != end || (info.Span.Length == 0 && info.AdornmentElement.Affinity == PositionAffinity.Predecessor)) {
					bool canAppend = true;
					if (lastAddedAdornment is not null && lastAddedAdornment.Value.Span.End > info.Span.Start)
						canAppend = false;
					if (canAppend) {
						sequenceList.Add(info.AdornmentElement);
						lastAddedAdornment = info;
					}
				}
				curr = info.Span.End;
			}
			if (curr < end) {
				var textSpan = new SnapshotSpan(topSpan.Snapshot, Span.FromBounds(curr, end));
				Debug.Assert(!textSpan.IsEmpty);
				sequenceList.Add(new TextSequenceElement(BufferGraph.CreateMappingSpan(textSpan, SpanTrackingMode.EdgeExclusive)));
			}

			return new TextAndAdornmentCollection(this, sequenceList);
		}

		sealed class AdornmentElementAndSpanComparer : IComparer<AdornmentElementAndSpan> {
			public static readonly AdornmentElementAndSpanComparer Instance = new AdornmentElementAndSpanComparer();
			public int Compare([AllowNull] AdornmentElementAndSpan x, [AllowNull] AdornmentElementAndSpan y) {
				int c = x.Span.Start - y.Span.Start;
				if (c != 0)
					return c;
				c = x.Span.Length - y.Span.Length;
				if (c != 0)
					return c;
				return (x.AdornmentElement.Affinity == PositionAffinity.Predecessor ? 0 : 1) - (y.AdornmentElement.Affinity == PositionAffinity.Predecessor ? 0 : 1);
			}
		}

		readonly struct AdornmentElementAndSpan {
			public Span Span { get; }
			public AdornmentElement AdornmentElement { get; }
			public AdornmentElementAndSpan(AdornmentElement adornmentElement, Span span) {
				AdornmentElement = adornmentElement;
				Span = span;
			}
		}

		sealed class AdornmentElement : IAdornmentElement {
			public IMappingSpan Span => tagSpan.Span;
			public bool ShouldRenderText => false;
			public double Width => tagSpan.Tag.Width;
			public double TopSpace => tagSpan.Tag.TopSpace;
			public double Baseline => tagSpan.Tag.Baseline;
			public double TextHeight => tagSpan.Tag.TextHeight;
			public double BottomSpace => tagSpan.Tag.BottomSpace;
			public object IdentityTag => tagSpan.Tag.IdentityTag;
			public object ProviderTag => tagSpan.Tag.ProviderTag;
			public PositionAffinity Affinity => tagSpan.Tag.Affinity;

			readonly IMappingTagSpan<SpaceNegotiatingAdornmentTag> tagSpan;

			public AdornmentElement(IMappingTagSpan<SpaceNegotiatingAdornmentTag> tagSpan) => this.tagSpan = tagSpan ?? throw new ArgumentNullException(nameof(tagSpan));
		}

		void TextView_Closed(object? sender, EventArgs e) {
			Debug.Assert(textView.Properties.ContainsProperty(typeof(ITextAndAdornmentSequencer)));
			textView.Properties.RemoveProperty(typeof(ITextAndAdornmentSequencer));
			textView.Closed -= TextView_Closed;
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
		}
	}
}
