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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class TextImageVersion : ITextImageVersion {
		public ITextImageVersion Next { get; private set; }
		public int Length { get; }
		public INormalizedTextChangeCollection Changes { get; private set; }
		public int VersionNumber { get; }
		public int ReiteratedVersionNumber { get; }
		public object Identifier { get; }

		public TextImageVersion(int length, int versionNumber, int reiteratedVersionNumber, object identifier) {
			VersionNumber = versionNumber;
			ReiteratedVersionNumber = reiteratedVersionNumber;
			Length = length;
			Identifier = identifier;
		}

		internal void SetChanges(INormalizedTextChangeCollection changes, ITextImageVersion next) {
			Next = next;
			Changes = changes;
		}

		public int TrackTo(VersionedPosition other, PointTrackingMode mode) {
			if (other.Version == null)
				throw new ArgumentException(nameof(other));
			if (other.Version.VersionNumber == VersionNumber)
				return other.Position;
			if (other.Version.VersionNumber > VersionNumber)
				return Tracking.TrackPositionForwardInTime(mode, other.Position, this, other.Version);
			return Tracking.TrackPositionBackwardInTime(mode, other.Position, this, other.Version);
		}

		public Span TrackTo(VersionedSpan span, SpanTrackingMode mode) {
			if (span.Version == null)
				throw new ArgumentException(nameof(span));
			if (span.Version.VersionNumber == VersionNumber)
				return span.Span;
			if (span.Version.VersionNumber > VersionNumber)
				return Tracking.TrackSpanForwardInTime(mode, span.Span, this, span.Version);
			return Tracking.TrackSpanBackwardInTime(mode, span.Span, this, span.Version);
		}
	}
}
