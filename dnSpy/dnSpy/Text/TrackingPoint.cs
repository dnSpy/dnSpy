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
	sealed class TrackingPoint : ITrackingPoint {
		public ITextBuffer TextBuffer { get; }
		public TrackingFidelityMode TrackingFidelity { get; }
		public PointTrackingMode TrackingMode { get; }
		ITextVersion textVersion;
		int position;

		public TrackingPoint(ITextVersion textVersion, int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) {
			if (textVersion is null)
				throw new ArgumentNullException(nameof(textVersion));
			if ((uint)position > (uint)textVersion.Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			TextBuffer = textVersion.TextBuffer;
			TrackingMode = trackingMode;
			TrackingFidelity = trackingFidelity;
			this.textVersion = textVersion;
			this.position = position;
		}

		public char GetCharacter(ITextSnapshot snapshot) => GetPoint(snapshot).GetChar();
		public SnapshotPoint GetPoint(ITextSnapshot snapshot) => new SnapshotPoint(snapshot, GetPosition(snapshot.Version));
		public int GetPosition(ITextSnapshot snapshot) => GetPosition(snapshot.Version);
		public int GetPosition(ITextVersion version) {
			if (version is null)
				throw new ArgumentNullException(nameof(version));
			if (version == textVersion)
				return position;
			if (version.TextBuffer != textVersion.TextBuffer)
				throw new ArgumentException();

			// Rewrite the values since it's common to just move in one direction. This can lose
			// information: if we move forward and then backward, we might get another position.
			position = version.VersionNumber > textVersion.VersionNumber ?
				Tracking.TrackPositionForwardInTime(TrackingMode, position, textVersion, version) :
				Tracking.TrackPositionBackwardInTime(TrackingMode, position, textVersion, version);
			textVersion = version;
			return position;
		}
	}
}
