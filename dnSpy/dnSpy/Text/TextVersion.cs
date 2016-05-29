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
using System.Diagnostics;
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	sealed class TextVersion : ITextVersion {
		public ITextBuffer TextBuffer { get; }
		public int VersionNumber { get; }
		public int ReiteratedVersionNumber { get; }
		public int Length { get; }
		public ITextVersion Next { get; private set; }
		public INormalizedTextChangeCollection Changes { get; private set; }

		public TextVersion(ITextBuffer textBuffer, int length, int versionNumber, int reiteratedVersionNumber) {
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			TextBuffer = textBuffer;
			VersionNumber = versionNumber;
			ReiteratedVersionNumber = reiteratedVersionNumber;
			Length = length;
		}

		public TextVersion SetChanges(IList<ITextChange> changes, int? reiteratedVersionNumber = null) {
			var normalizedChanges = NormalizedTextChangeCollection.Create(changes);
			if (reiteratedVersionNumber == null)
				reiteratedVersionNumber = changes.Count == 0 ? ReiteratedVersionNumber : VersionNumber + 1;
			int newLength = Length;
			foreach (var c in normalizedChanges)
				newLength += c.Delta;
			Debug.Assert(newLength >= 0);
			var newVersion = new TextVersion(TextBuffer, newLength, VersionNumber + 1, reiteratedVersionNumber.Value);
			Changes = normalizedChanges;
			Next = newVersion;
			return newVersion;
		}

		public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode) =>
			CreateTrackingPoint(position, trackingMode, TrackingFidelityMode.Forward);
		public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) {
			if (trackingFidelity == TrackingFidelityMode.UndoRedo)
				throw new NotSupportedException();
			return new TrackingPoint(this, position, trackingMode, trackingFidelity);
		}

		public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode) =>
			CreateTrackingSpan(new Span(start, length), trackingMode);
		public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode) =>
			CreateTrackingSpan(span, trackingMode, TrackingFidelityMode.Forward);
		public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) =>
			CreateTrackingSpan(new Span(start, length), trackingMode, trackingFidelity);
		public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) {
			if (trackingMode == SpanTrackingMode.Custom)
				throw new NotSupportedException();
			if (trackingFidelity == TrackingFidelityMode.UndoRedo)
				throw new NotSupportedException();
			return new TrackingSpan(this, span, trackingMode, trackingFidelity);
		}

		public override string ToString() => $"V{VersionNumber} (r{ReiteratedVersionNumber})";
	}
}
