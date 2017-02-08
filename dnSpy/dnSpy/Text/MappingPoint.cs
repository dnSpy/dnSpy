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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace dnSpy.Text {
	sealed class MappingPoint : IMappingPoint {
		public ITextBuffer AnchorBuffer => snapshotPoint.Snapshot.TextBuffer;
		public IBufferGraph BufferGraph { get; }

		/*readonly*/ SnapshotPoint snapshotPoint;
		readonly PointTrackingMode trackingMode;

		public MappingPoint(IBufferGraph bufferGraph, SnapshotPoint snapshotPoint, PointTrackingMode trackingMode) {
			if (bufferGraph == null)
				throw new ArgumentNullException(nameof(bufferGraph));
			if (snapshotPoint.Snapshot == null)
				throw new ArgumentException();
			BufferGraph = bufferGraph;
			this.snapshotPoint = snapshotPoint;
			this.trackingMode = trackingMode;
		}

		public SnapshotPoint? GetPoint(Predicate<ITextBuffer> match, PositionAffinity affinity) {
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			if (match(AnchorBuffer))
				return GetPoint(AnchorBuffer.CurrentSnapshot, affinity);
			return null;
		}

		public SnapshotPoint? GetPoint(ITextSnapshot targetSnapshot, PositionAffinity affinity) {
			if (targetSnapshot == null)
				throw new ArgumentNullException(nameof(targetSnapshot));
			return snapshotPoint.TranslateTo(targetSnapshot, trackingMode);
		}

		public SnapshotPoint? GetPoint(ITextBuffer targetBuffer, PositionAffinity affinity) {
			if (targetBuffer == null)
				throw new ArgumentNullException(nameof(targetBuffer));
			return GetPoint(targetBuffer.CurrentSnapshot, affinity);
		}

		public SnapshotPoint? GetInsertionPoint(Predicate<ITextBuffer> match) {
			throw new NotImplementedException();//TODO:
		}

		public override string ToString() => nameof(IMappingPoint) + "@" + snapshotPoint.ToString();
	}
}
