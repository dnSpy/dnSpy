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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// <see cref="Completion"/> collection
	/// </summary>
	class CompletionCollection {
		readonly Completion[] allCompletions;
		readonly FilteredCompletionCollection filteredCompletions;

		/// <summary>
		/// Gets the filters
		/// </summary>
		public virtual IReadOnlyList<IIntellisenseFilter> Filters { get; }

		/// <summary>
		/// Gets the filtered collection
		/// </summary>
		public IFilteredCompletionCollection Completions => filteredCompletions;

		/// <summary>
		/// Current <see cref="Completion"/>
		/// </summary>
		public CompletionSelectionStatus SelectionStatus {
			get { return selectionStatus; }
			set {
				if ((object)value == null)
					throw new ArgumentNullException(nameof(value));
				if (value == selectionStatus)
					return;
				if (value.Completion != null && !filteredCompletions.Contains(value.Completion))
					throw new ArgumentException();
				var oldValue = selectionStatus;
				selectionStatus = value;
				SelectionStatusChanged?.Invoke(this, new ValueChangedEventArgs<CompletionSelectionStatus>(oldValue, selectionStatus));
			}
		}
		CompletionSelectionStatus selectionStatus;

		/// <summary>
		/// Raised when <see cref="SelectionStatus"/> has changed
		/// </summary>
		public event EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>> SelectionStatusChanged;

		/// <summary>
		/// Span that will be modified when a <see cref="Completion"/> gets committed
		/// </summary>
		public ITrackingSpan ApplicableTo {
			get { return applicableTo; }
			protected set {
				searchText = null;
				applicableTo = value;
			}
		}
		ITrackingSpan applicableTo;

		/// <summary>
		/// Constructor
		/// </summary>
		protected CompletionCollection()
			: this(null, Array.Empty<Completion>(), null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="applicableTo">Span that will be modified when a <see cref="Completion"/> gets committed</param>
		/// <param name="completions">Completion items</param>
		/// <param name="filters">Filters or null</param>
		public CompletionCollection(ITrackingSpan applicableTo, IEnumerable<Completion> completions, IReadOnlyList<IIntellisenseFilter> filters = null) {
			if (completions == null)
				throw new ArgumentNullException(nameof(completions));
			selectionStatus = CompletionSelectionStatus.Empty;
			allCompletions = completions.ToArray();
			filteredCompletions = new FilteredCompletionCollection(allCompletions);
			ApplicableTo = applicableTo;
			Filters = filters ?? Array.Empty<IIntellisenseFilter>();
		}

		/// <summary>
		/// Recalculates the items
		/// </summary>
		public virtual void Recalculate() {
		}

		string SearchText {
			get {
				var atSpan = ApplicableTo;
				if (atSpan == null)
					return string.Empty;
				if (searchText == null || atSpan.TextBuffer.CurrentSnapshot.Version.VersionNumber != searchTextVersion) {
					searchTextVersion = atSpan.TextBuffer.CurrentSnapshot.Version.VersionNumber;
					searchText = atSpan.GetText(atSpan.TextBuffer.CurrentSnapshot);
				}
				return searchText;
			}
		}
		string searchText;
		int searchTextVersion = -1;

		/// <summary>
		/// Gets highlighted text spans or null
		/// </summary>
		/// <param name="displayText">Text shown in the UI</param>
		/// <returns></returns>
		public virtual IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText) =>
			new ReadOnlyCollection<Span>(CreateCompletionFilter(SearchText).GetMatchSpans(displayText));

		/// <summary>
		/// Creates a <see cref="ICompletionFilter"/>
		/// </summary>
		/// <param name="searchText">Search text</param>
		/// <returns></returns>
		public virtual ICompletionFilter CreateCompletionFilter(string searchText) => new CompletionFilter(searchText);

		/// <summary>
		/// Uses <see cref="Filters"/> to filter <paramref name="completions"/>
		/// </summary>
		/// <param name="filteredResult">Result</param>
		/// <param name="completions">Completion items to filter</param>
		protected virtual void Filter(List<Completion> filteredResult, IList<Completion> completions) => filteredResult.AddRange(completions);

		/// <summary>
		/// Filters the list. <see cref="SelectBestMatch"/> should be called after this method
		/// </summary>
		public virtual void Filter() {
			Debug.Assert(ApplicableTo != null, "You must initialize " + nameof(ApplicableTo) + " before calling this method");
			var inputText = SearchText;
			var filteredList = new List<Completion>(allCompletions.Length);
			Filter(filteredList, allCompletions);
			IList<Completion> finalList;
			if (inputText.Length < CompletionConstants.MimimumSearchLengthForFilter)
				finalList = filteredList;
			else {
				var list = new List<Completion>(filteredList.Count);
				finalList = list;
				var completionFilter = CreateCompletionFilter(inputText);
				foreach (var c in filteredList) {
					if (completionFilter.IsMatch(c))
						list.Add(c);
				}
			}
			if (finalList.Count != 0)
				filteredCompletions.SetNewFilteredCollection(finalList);
		}

		/// <summary>
		/// Selects the best match and should be called after <see cref="Filter()"/>
		/// </summary>
		public void SelectBestMatch() =>
			SelectionStatus = GetBestMatch() ?? CompletionSelectionStatus.Empty;

		/// <summary>
		/// Gets the best match in <see cref="Completions"/>
		/// </summary>
		/// <returns></returns>
		protected virtual CompletionSelectionStatus GetBestMatch() {
			Debug.Assert(ApplicableTo != null, "You must initialize " + nameof(ApplicableTo) + " before calling this method");
			var inputText = SearchText;
			var completionFilter = CreateCompletionFilter(inputText);
			int matches = 0;
			var selector = new BestMatchSelector(inputText);
			if (inputText.Length > 0) {
				foreach (var completion in filteredCompletions) {
					if (!completionFilter.IsMatch(completion))
						continue;
					matches++;
					selector.Select(completion);
				}
			}
			bool isSelected = selector.Result != null;
			return new CompletionSelectionStatus(selector.Result, isSelected: isSelected, isUnique: matches == 1);
		}

		/// <summary>
		/// Commits the currently selected <see cref="Completion"/>
		/// </summary>
		public virtual void Commit() => SelectionStatus.Completion?.Commit(ApplicableTo);
	}
}
