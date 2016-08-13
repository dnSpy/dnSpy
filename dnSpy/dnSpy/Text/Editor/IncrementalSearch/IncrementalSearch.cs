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
using Microsoft.VisualStudio.Text.IncrementalSearch;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor.IncrementalSearch {
	sealed class IncrementalSearch : IIncrementalSearch {
		public bool IsActive { get; private set; }
		public IncrementalSearchDirection SearchDirection { get; set; }
		public ITextView TextView { get; }

		public string SearchString {
			get { return searchString; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				searchString = value;
			}
		}
		string searchString;

		SnapshotPoint CaretStartPosition {
			get {
				caretStartPosition = caretStartPosition.TranslateTo(TextView.TextSnapshot, PointTrackingMode.Negative);
				return caretStartPosition;
			}
			set { caretStartPosition = value; }
		}
		SnapshotPoint caretStartPosition;

		FindOptions FindOptions {
			get {
				var options = FindOptions.None;
				if (SearchDirection == IncrementalSearchDirection.Backward)
					options |= FindOptions.SearchReverse;
				foreach (var c in SearchString) {
					if (char.IsUpper(c)) {
						options |= FindOptions.MatchCase;
						break;
					}
				}
				return options;
			}
		}

		static readonly IncrementalSearchResult searchFailedResult = new IncrementalSearchResult(false, false, false, false);

		readonly ITextSearchService textSearchService;
		readonly IEditorOperations editorOperations;

		public IncrementalSearch(ITextView textView, ITextSearchService textSearchService, IEditorOperationsFactoryService editorOperationsFactoryService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (textSearchService == null)
				throw new ArgumentNullException(nameof(textSearchService));
			if (editorOperationsFactoryService == null)
				throw new ArgumentNullException(nameof(editorOperationsFactoryService));
			TextView = textView;
			this.textSearchService = textSearchService;
			this.editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
			SearchString = string.Empty;
		}

		public void Start() {
			if (IsActive)
				throw new InvalidOperationException();
			IsActive = true;
			CaretStartPosition = TextView.Caret.Position.BufferPosition;
		}

		public void Dismiss() {
			if (!IsActive)
				throw new InvalidOperationException();
			IsActive = false;
			// Don't hold a strong reference to the snapshot
			CaretStartPosition = default(SnapshotPoint);
		}

		public IncrementalSearchResult AppendCharAndSearch(char toAppend) {
			if (!IsActive)
				throw new InvalidOperationException();
			SearchString += toAppend.ToString();
			return SelectNextResult();
		}

		public IncrementalSearchResult DeleteCharAndSearch() {
			if (!IsActive)
				throw new InvalidOperationException();
			if (SearchString.Length == 0)
				throw new InvalidOperationException();

			if (SearchString.Length == 1) {
				SearchString = string.Empty;
				return searchFailedResult;
			}

			SearchString = SearchString.Substring(0, SearchString.Length - 1);
			return SelectNextResult();
		}

		public IncrementalSearchResult SelectNextResult() {
			if (!IsActive)
				throw new InvalidOperationException();
			if (SearchString.Length == 0)
				return searchFailedResult;
			TextView.Selection.Clear();
			var res = textSearchService.FindNext(CaretStartPosition.Position, true, new FindData(SearchString, CaretStartPosition.Snapshot, FindOptions, null));
			if (res == null)
				return searchFailedResult;
			editorOperations.SelectAndMoveCaret(new VirtualSnapshotPoint(res.Value.Start), new VirtualSnapshotPoint(res.Value.End));
			if (SearchDirection == IncrementalSearchDirection.Forward)
				return new IncrementalSearchResult(passedEndOfBuffer: res.Value.Start < CaretStartPosition, passedStartOfBuffer: false, passedStartOfSearch: res.Value.Start != CaretStartPosition, resultFound: true);
			return new IncrementalSearchResult(passedEndOfBuffer: false, passedStartOfBuffer: res.Value.End > CaretStartPosition, passedStartOfSearch: res.Value.End != CaretStartPosition, resultFound: true);
		}

		public void Clear() => SearchString = string.Empty;
	}
}
