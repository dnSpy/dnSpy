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
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Editor {
	static class UriHelper {
		public static SnapshotSpan? GetUri(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, ITextView textView, SnapshotPoint point) {
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			if (point.Snapshot == null)
				throw new ArgumentException();
			var pointSpan = new SnapshotSpan(point.Snapshot, point.Position, 0);
			using (var tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<IUriTag>(textView)) {
				foreach (var tagSpan in tagAggregator.GetTags(new SnapshotSpan(point.Snapshot, point.Position, 0))) {
					foreach (var span in tagSpan.Span.GetSpans(point.Snapshot)) {
						if (span.IntersectsWith(pointSpan))
							return span;
					}
				}
			}
			return null;
		}
	}
}
