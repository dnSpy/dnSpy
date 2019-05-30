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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class TextVersion : ITextVersion2 {
		public ITextBuffer TextBuffer { get; }
		public int VersionNumber => textImageVersion.VersionNumber;
		public int ReiteratedVersionNumber => textImageVersion.ReiteratedVersionNumber;
		public int Length => textImageVersion.Length;
		public ITextVersion? Next { get; private set; }
		public INormalizedTextChangeCollection? Changes => textImageVersion.Changes;
		public ITextImageVersion ImageVersion => textImageVersion;

		readonly TextImageVersion textImageVersion;

		public TextVersion(ITextBuffer textBuffer, int length, int versionNumber, int reiteratedVersionNumber, object identifier) {
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			TextBuffer = textBuffer;
			textImageVersion = new TextImageVersion(length, versionNumber, reiteratedVersionNumber, identifier);
		}

		public TextVersion SetChanges(IList<ITextChange> changes, int? reiteratedVersionNumber = null) {
			var normalizedChanges = NormalizedTextChangeCollection.Create(changes);
			int reiterVerNum = reiteratedVersionNumber ?? (changes.Count == 0 ? ReiteratedVersionNumber : VersionNumber + 1);
			int newLength = Length;
			foreach (var c in normalizedChanges)
				newLength += c.Delta;
			Debug.Assert(newLength >= 0);
			var newVersion = new TextVersion(TextBuffer, newLength, VersionNumber + 1, reiterVerNum, textImageVersion.Identifier);
			textImageVersion.SetChanges(normalizedChanges, newVersion.textImageVersion);
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

		public ITrackingSpan CreateCustomTrackingSpan(Span span, TrackingFidelityMode trackingFidelity, object customState, CustomTrackToVersion behavior) => throw new NotImplementedException();//TODO:

		public override string ToString() => $"V{VersionNumber} (r{ReiteratedVersionNumber})";
	}
}
