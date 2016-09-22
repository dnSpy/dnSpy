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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	static class IntellisenseSessionHelper {
		public static ITrackingPoint GetTriggerPoint(ITextView textView, ITrackingPoint triggerPoint, ITextBuffer textBuffer) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));

			var point = GetTriggerPoint(textView, triggerPoint, textBuffer.CurrentSnapshot);
			if (point == null)
				return null;
			return point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Negative);
		}

		public static SnapshotPoint? GetTriggerPoint(ITextView textView, ITrackingPoint triggerPoint, ITextSnapshot textSnapshot) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			if (textSnapshot == null)
				throw new ArgumentNullException(nameof(textSnapshot));

			return triggerPoint.GetPoint(textView.TextSnapshot);
		}
	}
}
