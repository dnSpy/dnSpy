/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace dnSpy.Text.Projection {
	sealed class BufferGraph : IBufferGraph {
		public ITextBuffer TopBuffer { get; }
		public event EventHandler<GraphBufferContentTypeChangedEventArgs> GraphBufferContentTypeChanged;
#pragma warning disable 0067
		public event EventHandler<GraphBuffersChangedEventArgs> GraphBuffersChanged;
#pragma warning restore 0067

		public BufferGraph(ITextBuffer textBuffer) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			TopBuffer = textBuffer;
			TopBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
			if (textBuffer is IProjectionBuffer)
				throw new NotSupportedException();
		}

		void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			var buffer = (ITextBuffer)sender;
			GraphBufferContentTypeChanged?.Invoke(this, new GraphBufferContentTypeChangedEventArgs(buffer, e.BeforeContentType, e.AfterContentType));
		}

		public Collection<ITextBuffer> GetTextBuffers(Predicate<ITextBuffer> match) {
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			if (match(TopBuffer))
				return new Collection<ITextBuffer> { TopBuffer };
			return new Collection<ITextBuffer>();
		}

		public IMappingPoint CreateMappingPoint(SnapshotPoint point, PointTrackingMode trackingMode) {
			if (point.Snapshot == null)
				throw new ArgumentException();
			return new MappingPoint(this, point, trackingMode);
		}

		public IMappingSpan CreateMappingSpan(SnapshotSpan span, SpanTrackingMode trackingMode) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			return new MappingSpan(this, span, trackingMode);
		}

		public SnapshotPoint? MapDownToBuffer(SnapshotPoint position, PointTrackingMode trackingMode, ITextBuffer targetBuffer, PositionAffinity affinity) {
			if (position.Snapshot == null)
				throw new ArgumentException();
			if (targetBuffer == null)
				throw new ArgumentNullException(nameof(targetBuffer));

			if (position.Snapshot.TextBuffer != TopBuffer)
				return null;
			if (TopBuffer != targetBuffer)
				return null;
			return position.TranslateTo(targetBuffer.CurrentSnapshot, trackingMode);
		}

		public SnapshotPoint? MapDownToSnapshot(SnapshotPoint position, PointTrackingMode trackingMode, ITextSnapshot targetSnapshot, PositionAffinity affinity) {
			if (position.Snapshot == null)
				throw new ArgumentException();
			if (targetSnapshot == null)
				throw new ArgumentNullException(nameof(targetSnapshot));

			var res = MapDownToBuffer(position, trackingMode, targetSnapshot.TextBuffer, affinity);
			if (res == null)
				return null;
			return res.Value.TranslateTo(targetSnapshot, trackingMode);
		}

		public SnapshotPoint? MapDownToFirstMatch(SnapshotPoint position, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity) {
			if (position.Snapshot == null)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (position.Snapshot.TextBuffer != TopBuffer)
				return null;
			if (!match(TopBuffer.CurrentSnapshot))
				return null;
			return position.TranslateTo(TopBuffer.CurrentSnapshot, trackingMode);
		}

		public SnapshotPoint? MapDownToInsertionPoint(SnapshotPoint position, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match) {
			if (position.Snapshot == null)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (position.Snapshot.TextBuffer != TopBuffer)
				return null;
			if (!match(TopBuffer.CurrentSnapshot))
				return null;
			return position.TranslateTo(TopBuffer.CurrentSnapshot, trackingMode);
		}

		public NormalizedSnapshotSpanCollection MapDownToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, ITextBuffer targetBuffer) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (targetBuffer == null)
				throw new ArgumentNullException(nameof(targetBuffer));

			if (span.Snapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			if (targetBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			return new NormalizedSnapshotSpanCollection(span.TranslateTo(targetBuffer.CurrentSnapshot, trackingMode));
		}

		public NormalizedSnapshotSpanCollection MapDownToSnapshot(SnapshotSpan span, SpanTrackingMode trackingMode, ITextSnapshot targetSnapshot) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (targetSnapshot == null)
				throw new ArgumentNullException(nameof(targetSnapshot));

			if (span.Snapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			if (targetSnapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			return new NormalizedSnapshotSpanCollection(span.TranslateTo(targetSnapshot, trackingMode));
		}

		public NormalizedSnapshotSpanCollection MapDownToFirstMatch(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (span.Snapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			if (!match(TopBuffer.CurrentSnapshot))
				return NormalizedSnapshotSpanCollection.Empty;
			return new NormalizedSnapshotSpanCollection(span.TranslateTo(TopBuffer.CurrentSnapshot, trackingMode));
		}

		public SnapshotPoint? MapUpToBuffer(SnapshotPoint point, PointTrackingMode trackingMode, PositionAffinity affinity, ITextBuffer targetBuffer) {
			if (point.Snapshot == null)
				throw new ArgumentException();
			if (targetBuffer == null)
				throw new ArgumentNullException(nameof(targetBuffer));

			if (point.Snapshot.TextBuffer != TopBuffer)
				return null;
			if (TopBuffer != targetBuffer)
				return null;
			return point.TranslateTo(targetBuffer.CurrentSnapshot, trackingMode);
		}

		public SnapshotPoint? MapUpToSnapshot(SnapshotPoint point, PointTrackingMode trackingMode, PositionAffinity affinity, ITextSnapshot targetSnapshot) {
			if (point.Snapshot == null)
				throw new ArgumentException();
			if (targetSnapshot == null)
				throw new ArgumentNullException(nameof(targetSnapshot));

			var res = MapUpToBuffer(point, trackingMode, affinity, targetSnapshot.TextBuffer);
			if (res == null)
				return null;
			return res.Value.TranslateTo(targetSnapshot, trackingMode);
		}

		public SnapshotPoint? MapUpToFirstMatch(SnapshotPoint point, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity) {
			if (point.Snapshot == null)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (point.Snapshot.TextBuffer != TopBuffer)
				return null;
			if (!match(TopBuffer.CurrentSnapshot))
				return null;
			return point.TranslateTo(TopBuffer.CurrentSnapshot, trackingMode);
		}

		public NormalizedSnapshotSpanCollection MapUpToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, ITextBuffer targetBuffer) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (targetBuffer == null)
				throw new ArgumentNullException(nameof(targetBuffer));

			if (span.Snapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			if (targetBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			return new NormalizedSnapshotSpanCollection(span.TranslateTo(targetBuffer.CurrentSnapshot, trackingMode));
		}

		public NormalizedSnapshotSpanCollection MapUpToSnapshot(SnapshotSpan span, SpanTrackingMode trackingMode, ITextSnapshot targetSnapshot) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (targetSnapshot == null)
				throw new ArgumentNullException(nameof(targetSnapshot));

			if (span.Snapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			if (targetSnapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			return new NormalizedSnapshotSpanCollection(span.TranslateTo(targetSnapshot, trackingMode));
		}

		public NormalizedSnapshotSpanCollection MapUpToFirstMatch(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (span.Snapshot.TextBuffer != TopBuffer)
				return NormalizedSnapshotSpanCollection.Empty;
			if (!match(TopBuffer.CurrentSnapshot))
				return NormalizedSnapshotSpanCollection.Empty;
			return new NormalizedSnapshotSpanCollection(span.TranslateTo(TopBuffer.CurrentSnapshot, trackingMode));
		}
	}
}
