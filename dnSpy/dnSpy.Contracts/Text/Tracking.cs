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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Tracking helper class
	/// </summary>
	public static class Tracking {
		/// <summary>
		/// Track span backward in time
		/// </summary>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="span">Span</param>
		/// <param name="currentVersion">Version</param>
		/// <param name="targetVersion">Target version (older version)</param>
		/// <returns></returns>
		public static Span TrackSpanBackwardInTime(SpanTrackingMode trackingMode, Span span, ITextVersion currentVersion, ITextVersion targetVersion) {
			int newStart = TrackPositionBackwardInTime(trackingMode == SpanTrackingMode.EdgeExclusive || trackingMode == SpanTrackingMode.EdgePositive ? PointTrackingMode.Positive : PointTrackingMode.Negative, span.Start, currentVersion, targetVersion);
			int newEnd = TrackPositionBackwardInTime(trackingMode == SpanTrackingMode.EdgeExclusive || trackingMode == SpanTrackingMode.EdgeNegative ? PointTrackingMode.Negative : PointTrackingMode.Positive, span.End, currentVersion, targetVersion);
			return Span.FromBounds(newStart, Math.Max(newStart, newEnd));
		}

		/// <summary>
		/// Track position backward in time
		/// </summary>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="currentPosition">Position</param>
		/// <param name="currentVersion">Version</param>
		/// <param name="targetVersion">Target version (older version)</param>
		/// <returns></returns>
		public static int TrackPositionBackwardInTime(PointTrackingMode trackingMode, int currentPosition, ITextVersion currentVersion, ITextVersion targetVersion) {
			if (currentVersion == null || targetVersion == null || currentVersion.VersionNumber < targetVersion.VersionNumber)
				throw new ArgumentException();
			var changesArray = new IList<ITextChange>[currentVersion.VersionNumber - targetVersion.VersionNumber];
			var v = targetVersion;
			for (int i = changesArray.Length - 1; i >= 0; i--) {
				changesArray[i] = v.Changes;
				v = v.Next;
			}
			if (v != currentVersion)
				throw new InvalidOperationException();

			for (int j = 0; j < changesArray.Length; j++) {
				var changes = changesArray[j];

				int i;
				for (i = 0; i < changes.Count; i++) {
					var c = changes[i];
					if (c.NewPosition <= currentPosition && currentPosition <= c.NewEnd) {
						currentPosition = trackingMode == PointTrackingMode.Negative ? c.OldPosition : c.OldEnd;
						break;
					}
					if (currentPosition > c.NewEnd) {
						currentPosition += -c.NewEnd + c.OldEnd;
						break;
					}
				}
				if (i == changes.Count && i != 0) {
					var c = changes[i - 1];
					if (currentPosition > c.NewEnd)
						currentPosition += -c.NewEnd + c.OldEnd;
				}
			}
			return currentPosition;
		}

		/// <summary>
		/// Track span forward in time
		/// </summary>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="span">Span</param>
		/// <param name="currentVersion">Version</param>
		/// <param name="targetVersion">Target version (later version)</param>
		/// <returns></returns>
		public static Span TrackSpanForwardInTime(SpanTrackingMode trackingMode, Span span, ITextVersion currentVersion, ITextVersion targetVersion) {
			int newStart = TrackPositionForwardInTime(trackingMode == SpanTrackingMode.EdgeExclusive || trackingMode == SpanTrackingMode.EdgePositive ? PointTrackingMode.Positive : PointTrackingMode.Negative, span.Start, currentVersion, targetVersion);
			int newEnd = TrackPositionForwardInTime(trackingMode == SpanTrackingMode.EdgeExclusive || trackingMode == SpanTrackingMode.EdgeNegative ? PointTrackingMode.Negative : PointTrackingMode.Positive, span.End, currentVersion, targetVersion);
			return Span.FromBounds(newStart, Math.Max(newStart, newEnd));
		}

		/// <summary>
		/// Tracks position forward in time
		/// </summary>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="currentPosition">Position</param>
		/// <param name="currentVersion">Version</param>
		/// <param name="targetVersion">Target version (later version)</param>
		/// <returns></returns>
		public static int TrackPositionForwardInTime(PointTrackingMode trackingMode, int currentPosition, ITextVersion currentVersion, ITextVersion targetVersion) {
			if (currentVersion == null || targetVersion == null || currentVersion.VersionNumber > targetVersion.VersionNumber)
				throw new ArgumentException();
			while (currentVersion != targetVersion) {
				var changes = currentVersion.Changes;
				int i;
				for (i = 0; i < changes.Count; i++) {
					var c = changes[i];
					if (c.OldPosition <= currentPosition && currentPosition <= c.OldEnd) {
						currentPosition = trackingMode == PointTrackingMode.Negative ? c.NewPosition : c.NewEnd;
						break;
					}
					if (currentPosition > c.OldEnd) {
						currentPosition += -c.OldEnd + c.NewEnd;
						break;
					}
				}
				if (i == changes.Count && i != 0) {
					var c = changes[i - 1];
					if (currentPosition > c.OldEnd)
						currentPosition += -c.OldEnd + c.NewEnd;
				}
				currentVersion = currentVersion.Next;
			}
			return currentPosition;
		}
	}
}
