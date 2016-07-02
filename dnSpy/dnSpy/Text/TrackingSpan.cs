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

namespace dnSpy.Text {
	sealed class TrackingSpan : ITrackingSpan {
		public ITextBuffer TextBuffer { get; }
		public TrackingFidelityMode TrackingFidelity { get; }
		public SpanTrackingMode TrackingMode { get; }
		ITextVersion textVersion;
		Span span;

		public TrackingSpan(ITextVersion textVersion, Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) {
			if (textVersion == null)
				throw new ArgumentNullException(nameof(textVersion));
			if ((uint)span.End > (uint)textVersion.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			TextBuffer = textVersion.TextBuffer;
			TrackingFidelity = trackingFidelity;
			TrackingMode = trackingMode;
			this.textVersion = textVersion;
			this.span = span;
		}

		public string GetText(ITextSnapshot snapshot) => GetSpan(snapshot).GetText();
		public SnapshotSpan GetSpan(ITextSnapshot snapshot) => new SnapshotSpan(snapshot, GetSpan(snapshot.Version));
		public SnapshotPoint GetStartPoint(ITextSnapshot snapshot) => new SnapshotPoint(snapshot, GetSpan(snapshot.Version).Start);
		public SnapshotPoint GetEndPoint(ITextSnapshot snapshot) => new SnapshotPoint(snapshot, GetSpan(snapshot.Version).End);
		public Span GetSpan(ITextVersion version) {
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			if (version == textVersion)
				return span;
			if (version.TextBuffer != textVersion.TextBuffer)
				throw new ArgumentException();

			// Rewrite the values since it's common to just move in one direction. This can lose
			// information: if we move forward and then backward, we might get another span.
			span = version.VersionNumber > textVersion.VersionNumber ?
				Tracking.TrackSpanForwardInTime(TrackingMode, span, textVersion, version) :
				Tracking.TrackSpanBackwardInTime(TrackingMode, span, textVersion, version);
			textVersion = version;
			return span;
		}
	}
}
