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
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Operations {
	sealed class TextSearchNavigator : ITextSearchNavigator {
		public string? SearchTerm { get; set; }
		public string? ReplaceTerm { get; set; }
		public FindOptions SearchOptions { get; set; }
		public SnapshotSpan? CurrentResult => currentResult;
		SnapshotSpan? currentResult;

		public SnapshotPoint? StartPoint {
			get {
				if (!(startPoint is null))
					startPoint = startPoint.Value.TranslateTo(buffer.CurrentSnapshot, (SearchOptions & FindOptions.SearchReverse) != 0 ? PointTrackingMode.Negative : PointTrackingMode.Positive);
				return startPoint;
			}
			set {
				if (!(value is null) && value.Value.Snapshot.TextBuffer != buffer)
					throw new ArgumentException();
				startPoint = value;
			}
		}
		SnapshotPoint? startPoint;

		public ITrackingSpan? SearchSpan {
			get => searchSpan;
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (value.TextBuffer != buffer)
					throw new ArgumentException();
				searchSpan = value;
			}
		}
		ITrackingSpan? searchSpan;

		readonly ITextBuffer buffer;
		readonly ITextSearchService2 textSearchService2;

		public TextSearchNavigator(ITextBuffer buffer, ITextSearchService2 textSearchService2) {
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			this.textSearchService2 = textSearchService2 ?? throw new ArgumentNullException(nameof(textSearchService2));
		}

		bool IsValidStartingPosition(SnapshotSpan range, SnapshotPoint startingPosition) =>
			range.Snapshot == startingPosition.Snapshot && range.Start <= startingPosition && startingPosition <= range.End;

		bool FindFailed() {
			currentResult = null;
			return false;
		}

		public bool Find() {
			if (SearchTerm is null)
				throw new InvalidOperationException();
			if (SearchTerm.Length == 0)
				throw new InvalidOperationException();

			SnapshotPoint startingPosition;
			if (!(CurrentResult is null)) {
				if ((SearchOptions & FindOptions.SearchReverse) != 0) {
					if (CurrentResult.Value.End.Position > 0)
						startingPosition = CurrentResult.Value.End - 1;
					else if ((SearchOptions & FindOptions.Wrap) != 0)
						startingPosition = new SnapshotPoint(CurrentResult.Value.Snapshot, CurrentResult.Value.Snapshot.Length);
					else
						return FindFailed();
				}
				else {
					if (CurrentResult.Value.Start.Position != CurrentResult.Value.Snapshot.Length)
						startingPosition = CurrentResult.Value.Start + 1;
					else if ((SearchOptions & FindOptions.Wrap) != 0)
						startingPosition = new SnapshotPoint(CurrentResult.Value.Snapshot, 0);
					else
						return FindFailed();
				}
			}
			else if (!(StartPoint is null))
				startingPosition = StartPoint.Value;
			else
				startingPosition = new SnapshotPoint(buffer.CurrentSnapshot, 0);
			startingPosition = startingPosition.TranslateTo(buffer.CurrentSnapshot, (SearchOptions & FindOptions.SearchReverse) != 0 ? PointTrackingMode.Negative : PointTrackingMode.Positive);

			var spanToUse = searchSpan?.GetSpan(buffer.CurrentSnapshot) ?? new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length);
			if (!IsValidStartingPosition(spanToUse, startingPosition))
				return FindFailed();
			foreach (var result in textSearchService2.FindAll(spanToUse, startingPosition, SearchTerm, SearchOptions)) {
				currentResult = result;
				return true;
			}

			return FindFailed();
		}

		public bool Replace() {
			if (SearchTerm is null)
				throw new InvalidOperationException();
			if (SearchTerm.Length == 0)
				throw new InvalidOperationException();
			if (CurrentResult is null)
				throw new InvalidOperationException();
			if (ReplaceTerm is null)
				throw new InvalidOperationException();

			var spanToUse = searchSpan?.GetSpan(CurrentResult.Value.Snapshot) ?? new SnapshotSpan(CurrentResult.Value.Snapshot, 0, CurrentResult.Value.Snapshot.Length);
			SnapshotSpan searchRange;
			if ((SearchOptions & FindOptions.SearchReverse) != 0) {
				Debug.Assert(spanToUse.Start <= CurrentResult.Value.End);
				if (spanToUse.Start > CurrentResult.Value.End)
					return false;
				searchRange = new SnapshotSpan(spanToUse.Start, CurrentResult.Value.End);
			}
			else {
				Debug.Assert(CurrentResult.Value.Start <= spanToUse.End);
				if (CurrentResult.Value.Start > spanToUse.End)
					return false;
				searchRange = new SnapshotSpan(CurrentResult.Value.Start, spanToUse.End);
			}

			var span = textSearchService2.FindForReplace(searchRange, SearchTerm, ReplaceTerm, SearchOptions, out string expandedReplacePattern);
			if (span is null)
				return false;
			using (var ed = buffer.CreateEdit()) {
				var currSpan = span.Value.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
				if (!ed.Replace(currSpan.Span, expandedReplacePattern))
					return false;
				ed.Apply();
				// Don't check the snapshot returned by Apply() since we could've replaced a 0-length span with the
				// empty string if regex search was used. In that case, no new snapshot is created.
				return !ed.Canceled;
			}
		}

		public void ClearCurrentResult() => currentResult = null;
	}
}
