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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace dnSpy.Text {
	sealed class MappingSpan : IMappingSpan {
		/*readonly*/ SnapshotSpan snapshotSpan;
		readonly SpanTrackingMode spanTrackingMode;
		IMappingPoint start, end;

		public ITextBuffer AnchorBuffer => snapshotSpan.Snapshot.TextBuffer;
		public IBufferGraph BufferGraph { get; }
		public IMappingPoint Start => start ?? (start = new MappingPoint(BufferGraph, snapshotSpan.Start, PointTrackingMode));
		public IMappingPoint End => end ?? (end = new MappingPoint(BufferGraph, snapshotSpan.End, PointTrackingMode));
		PointTrackingMode PointTrackingMode => spanTrackingMode == SpanTrackingMode.EdgeExclusive || spanTrackingMode == SpanTrackingMode.EdgeNegative ? PointTrackingMode.Negative : PointTrackingMode.Positive;

		public MappingSpan(IBufferGraph bufferGraph, SnapshotSpan snapshotSpan, SpanTrackingMode trackingMode) {
			if (bufferGraph == null)
				throw new ArgumentNullException(nameof(bufferGraph));
			if (snapshotSpan.Snapshot == null)
				throw new ArgumentException();
			BufferGraph = bufferGraph;
			this.snapshotSpan = snapshotSpan;
			spanTrackingMode = trackingMode;
		}

		public NormalizedSnapshotSpanCollection GetSpans(Predicate<ITextBuffer> match) {
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			if (match(AnchorBuffer))
				return GetSpans(AnchorBuffer.CurrentSnapshot);
			return NormalizedSnapshotSpanCollection.Empty;
		}

		public NormalizedSnapshotSpanCollection GetSpans(ITextSnapshot targetSnapshot) {
			if (targetSnapshot == null)
				throw new ArgumentNullException(nameof(targetSnapshot));
			var newSpan = snapshotSpan.TranslateTo(targetSnapshot, spanTrackingMode);
			return new NormalizedSnapshotSpanCollection(newSpan);
		}

		public NormalizedSnapshotSpanCollection GetSpans(ITextBuffer targetBuffer) {
			if (targetBuffer == null)
				throw new ArgumentNullException(nameof(targetBuffer));
			return GetSpans(targetBuffer.CurrentSnapshot);
		}

		public override string ToString() => nameof(IMappingSpan) + "@" + snapshotSpan.ToString();
	}
}
