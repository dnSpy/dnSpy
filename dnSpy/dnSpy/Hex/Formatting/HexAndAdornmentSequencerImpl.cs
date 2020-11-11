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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Tagging;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex.Formatting {
	sealed class HexAndAdornmentSequencerImpl : HexAndAdornmentSequencer {
		public override HexBuffer Buffer => hexView.Buffer;

		readonly HexTagAggregator<HexSpaceNegotiatingAdornmentTag> hexTagAggregator;
		readonly HexView hexView;

		public HexAndAdornmentSequencerImpl(HexView hexView, HexTagAggregator<HexSpaceNegotiatingAdornmentTag> hexTagAggregator) {
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			this.hexTagAggregator = hexTagAggregator ?? throw new ArgumentNullException(nameof(hexTagAggregator));
			hexView.Closed += HexView_Closed;
			hexTagAggregator.TagsChanged += HexTagAggregator_TagsChanged;
		}

		public override event EventHandler<HexAndAdornmentSequenceChangedEventArgs>? SequenceChanged;

		void HexTagAggregator_TagsChanged(object? sender, HexTagsChangedEventArgs e) =>
			SequenceChanged?.Invoke(this, new HexAndAdornmentSequenceChangedEventArgs(e.Span));

		public override HexAndAdornmentCollection CreateHexAndAdornmentCollection(HexBufferPoint position) {
			var line = hexView.BufferLines.GetLineFromPosition(position);
			return CreateHexAndAdornmentCollection(line);
		}

		public override HexAndAdornmentCollection CreateHexAndAdornmentCollection(HexBufferLine line) {
			if (line is null)
				throw new ArgumentNullException(nameof(line));
			if (line.Buffer != hexView.Buffer)
				throw new ArgumentException();
			var lineSpan = line.TextSpan;

			List<AdornmentElementAndSpan>? adornmentList = null;
			foreach (var tagSpan in hexTagAggregator.GetAllTags(new HexTaggerContext(line, lineSpan))) {
				if (adornmentList is null)
					adornmentList = new List<AdornmentElementAndSpan>();
				adornmentList.Add(new AdornmentElementAndSpan(new HexAdornmentElementImpl(tagSpan), tagSpan.Span));
			}

			// Common case
			if (adornmentList is null) {
				var elem = new HexSequenceElementImpl(lineSpan);
				return new HexAndAdornmentCollectionImpl(this, new[] { elem });
			}

			var sequenceList = new List<HexSequenceElement>();
			adornmentList.Sort(AdornmentElementAndSpanComparer.Instance);
			int start = lineSpan.Start;
			int end = lineSpan.End;
			int curr = start;
			AdornmentElementAndSpan? lastAddedAdornment = null;
			for (int i = 0; i < adornmentList.Count; i++) {
				var info = adornmentList[i];
				int spanStart = info.Span.Length == 0 && info.AdornmentElement.Affinity == VST.PositionAffinity.Predecessor ? info.Span.Start - 1 : info.Span.Start;
				if (spanStart < start)
					continue;
				if (info.Span.Start > end)
					break;
				var textSpan = VST.Span.FromBounds(curr, info.Span.Start);
				if (!textSpan.IsEmpty)
					sequenceList.Add(new HexSequenceElementImpl(textSpan));
				if (info.Span.Start != end || (info.Span.Length == 0 && info.AdornmentElement.Affinity == VST.PositionAffinity.Predecessor)) {
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
				var textSpan = VST.Span.FromBounds(curr, end);
				Debug.Assert(!textSpan.IsEmpty);
				sequenceList.Add(new HexSequenceElementImpl(textSpan));
			}

			return new HexAndAdornmentCollectionImpl(this, sequenceList.ToArray());
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
				return (x.AdornmentElement.Affinity == VST.PositionAffinity.Predecessor ? 0 : 1) - (y.AdornmentElement.Affinity == VST.PositionAffinity.Predecessor ? 0 : 1);
			}
		}

		readonly struct AdornmentElementAndSpan {
			public VST.Span Span { get; }
			public HexAdornmentElementImpl AdornmentElement { get; }
			public AdornmentElementAndSpan(HexAdornmentElementImpl adornmentElement, VST.Span span) {
				AdornmentElement = adornmentElement;
				Span = span;
			}
		}

		sealed class HexAdornmentElementImpl : HexAdornmentElement {
			public override VST.Span Span => tagSpan.Span;
			public override bool ShouldRenderText => false;
			public override double Width => tagSpan.Tag.Width;
			public override double TopSpace => tagSpan.Tag.TopSpace;
			public override double Baseline => tagSpan.Tag.Baseline;
			public override double TextHeight => tagSpan.Tag.TextHeight;
			public override double BottomSpace => tagSpan.Tag.BottomSpace;
			public override object IdentityTag => tagSpan.Tag.IdentityTag;
			public override object ProviderTag => tagSpan.Tag.ProviderTag;
			public override VST.PositionAffinity Affinity => tagSpan.Tag.Affinity;

			readonly IHexTextTagSpan<HexSpaceNegotiatingAdornmentTag> tagSpan;

			public HexAdornmentElementImpl(IHexTextTagSpan<HexSpaceNegotiatingAdornmentTag> tagSpan) => this.tagSpan = tagSpan ?? throw new ArgumentNullException(nameof(tagSpan));
		}

		sealed class HexSequenceElementImpl : HexSequenceElement {
			public override bool ShouldRenderText => true;
			public override VST.Span Span { get; }

			public HexSequenceElementImpl(VST.Span span) => Span = span;
		}

		sealed class HexAndAdornmentCollectionImpl : HexAndAdornmentCollection {
			public override HexAndAdornmentSequencer Sequencer { get; }
			public override int Count => elements.Length;
			public override HexSequenceElement this[int index] => elements[index];
			readonly HexSequenceElement[] elements;

			public HexAndAdornmentCollectionImpl(HexAndAdornmentSequencer sequencer, HexSequenceElement[] elements) {
				Sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
				this.elements = elements ?? throw new ArgumentNullException(nameof(elements));
			}
		}

		void HexView_Closed(object? sender, EventArgs e) {
			Debug.Assert(hexView.Properties.ContainsProperty(typeof(HexAndAdornmentSequencer)));
			hexView.Properties.RemoveProperty(typeof(HexAndAdornmentSequencer));
			hexView.Closed -= HexView_Closed;
			hexTagAggregator.TagsChanged -= HexTagAggregator_TagsChanged;
			hexTagAggregator.Dispose();
		}
	}
}
