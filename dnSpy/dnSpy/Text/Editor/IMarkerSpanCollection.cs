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
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	interface IMarkerSpanCollection {
		/// <summary>
		/// true if it's box selection
		/// </summary>
		bool IsBoxMode { get; }

		/// <summary>
		/// true if there are no spans
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Gets all spans that overlap the visible region
		/// </summary>
		IEnumerable<VirtualSnapshotSpan> VisibleSpans { get; }
	}

	sealed class NullMarkerSpanCollection : IMarkerSpanCollection {
		public static readonly IMarkerSpanCollection Instance = new NullMarkerSpanCollection();
		bool IMarkerSpanCollection.IsBoxMode => false;
		bool IMarkerSpanCollection.IsEmpty => true;
		IEnumerable<VirtualSnapshotSpan> IMarkerSpanCollection.VisibleSpans => Enumerable.Empty<VirtualSnapshotSpan>();
	}

	sealed class StreamMarkerSpanCollection : IMarkerSpanCollection {
		bool IMarkerSpanCollection.IsBoxMode => false;

		bool IMarkerSpanCollection.IsEmpty {
			get {
				if (textView.TextSnapshot != span.Snapshot)
					throw new InvalidOperationException();
				// An empty span is not an empty collection
				return false;
			}
		}

		IEnumerable<VirtualSnapshotSpan> IMarkerSpanCollection.VisibleSpans {
			get {
				if (textView.TextSnapshot != span.Snapshot)
					throw new InvalidOperationException();
				if (span.OverlapsWith(new VirtualSnapshotSpan(textView.TextViewLines.FormattedSpan)))
					yield return span;
			}
		}

		readonly ITextView textView;
		readonly VirtualSnapshotSpan span;

		public StreamMarkerSpanCollection(ITextView textView, VirtualSnapshotSpan span) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (span.Snapshot == null)
				throw new ArgumentException();
			this.textView = textView;
			this.span = span;
		}
	}

	sealed class BoxMarkerSpanCollection : IMarkerSpanCollection {
		bool IMarkerSpanCollection.IsBoxMode => true;

		bool IMarkerSpanCollection.IsEmpty {
			get {
				if (textSelection.TextView.TextSnapshot != textSnapshot)
					throw new InvalidOperationException();
				// If it was empty, NullMarkerSpanCollection should've been used
				return false;
			}
		}

		IEnumerable<VirtualSnapshotSpan> IMarkerSpanCollection.VisibleSpans {
			get {
				if (textSelection.TextView.TextSnapshot != textSnapshot)
					throw new InvalidOperationException();
				return textSelection.VisibleSpans;
			}
		}

		readonly TextSelection textSelection;
		readonly ITextSnapshot textSnapshot;

		public BoxMarkerSpanCollection(TextSelection textSelection) {
			if (textSelection == null)
				throw new ArgumentNullException(nameof(textSelection));
			this.textSelection = textSelection;
			this.textSnapshot = textSelection.TextView.TextSnapshot;
		}
	}
}
