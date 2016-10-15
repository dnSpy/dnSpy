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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// <see cref="Completion"/> collection
	/// </summary>
	public class DsCompletionSet : CompletionSet2 {
		readonly Completion[] allCompletions;
		readonly Completion[] allCompletionBuilders;
		readonly FilteredCompletionCollection filteredCompletions;
		readonly FilteredCompletionCollection filteredCompletionBuilders;

		/// <summary>
		/// Gets the filtered <see cref="Completion"/>s
		/// </summary>
		public override IList<Completion> Completions => filteredCompletions;

		/// <summary>
		/// Gets the filtered <see cref="Completion"/> builders
		/// </summary>
		public override IList<Completion> CompletionBuilders => filteredCompletionBuilders;

		/// <summary>
		/// Gets or sets the text tracking span to which this completion applies
		/// </summary>
		public override ITrackingSpan ApplicableTo {
			get { return base.ApplicableTo; }
			protected set {
				searchText = null;
				base.ApplicableTo = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		protected DsCompletionSet() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="moniker">Unique non-localized identifier</param>
		/// <param name="displayName">Name shown in the UI if there are multiple <see cref="CompletionSet"/>s</param>
		/// <param name="applicableTo">Span that will be modified when a <see cref="Completion"/> gets committed</param>
		/// <param name="completions">Completion items</param>
		/// <param name="completionBuilders">Completion builders</param>
		/// <param name="filters">Filters or null</param>
		public DsCompletionSet(string moniker, string displayName, ITrackingSpan applicableTo, IEnumerable<Completion> completions, IEnumerable<Completion> completionBuilders, IReadOnlyList<IIntellisenseFilter> filters)
			: base(moniker, displayName, applicableTo, Array.Empty<Completion>(), Array.Empty<Completion>(), filters ?? Array.Empty<IIntellisenseFilter>()) {
			allCompletions = completions.ToArray();
			allCompletionBuilders = completionBuilders.ToArray();
			filteredCompletions = new FilteredCompletionCollection(allCompletions);
			filteredCompletionBuilders = new FilteredCompletionCollection(allCompletionBuilders);
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
		public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText) =>
			CreateCompletionFilter(SearchText).GetMatchSpans(displayText);

		/// <summary>
		/// Creates a <see cref="ICompletionFilter"/>
		/// </summary>
		/// <param name="searchText">Search text</param>
		/// <returns></returns>
		public virtual ICompletionFilter CreateCompletionFilter(string searchText) => new CompletionFilter(searchText);

		/// <summary>
		/// Uses <see cref="CompletionSet2.Filters"/> to filter <paramref name="completions"/>
		/// </summary>
		/// <param name="filteredResult">Result</param>
		/// <param name="completions">Completion items to filter</param>
		protected virtual void Filter(List<Completion> filteredResult, IList<Completion> completions) => filteredResult.AddRange(completions);

		/// <summary>
		/// Filters the list. <see cref="SelectBestMatch"/> should be called after this method
		/// </summary>
		public override void Filter() {
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
		public override void SelectBestMatch() =>
			SelectionStatus = GetBestMatch() ?? new CompletionSelectionStatus(null, false, false);

		struct MruSelection {
			public Completion Completion { get; }
			public int Index { get; }
			public MruSelection(Completion completion, int index) {
				Completion = completion;
				Index = index;
			}
		}

		/// <summary>
		/// Gets the best match in <see cref="CompletionSet.Completions"/>
		/// </summary>
		/// <returns></returns>
		protected virtual CompletionSelectionStatus GetBestMatch() {
			Debug.Assert(ApplicableTo != null, "You must initialize " + nameof(ApplicableTo) + " before calling this method");
			var inputText = SearchText;
			var completionFilter = CreateCompletionFilter(inputText);
			int matches = 0;
			var selector = new BestMatchSelector(inputText);
			var mruSelectionCase = default(MruSelection);
			var mruSelection = default(MruSelection);
			if (inputText.Length > 0) {
				foreach (var completion in Completions) {
					if (!completionFilter.IsMatch(completion))
						continue;
					matches++;
					selector.Select(completion);

					if (completion.DisplayText.StartsWith(searchText, StringComparison.Ordinal)) {
						int currentMruIndex = GetMruIndex(completion);
						if (mruSelectionCase.Completion == null || currentMruIndex < mruSelectionCase.Index)
							mruSelectionCase = new MruSelection(completion, currentMruIndex);
					}
					else if (completion.DisplayText.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) {
						int currentMruIndex = GetMruIndex(completion);
						if (mruSelection.Completion == null || currentMruIndex < mruSelection.Index)
							mruSelection = new MruSelection(completion, currentMruIndex);
					}
				}
			}

			// If it was an exact match, don't use the MRU-selected completion. Eg.
			// local 'i' exists, and we previously typed 'int', and we've just typed 'i',
			// then select 'i' and not 'int'
			var selectedCompletion = mruSelectionCase.Completion ?? mruSelection.Completion ?? selector.Result;
			if (selector.Result != null && inputText.Equals(selector.Result.TryGetFilterText(), StringComparison.OrdinalIgnoreCase))
				selectedCompletion = selector.Result;

			bool isSelected = selectedCompletion != null;
			bool isUnique = matches == 1;
			return new CompletionSelectionStatus(selectedCompletion, isSelected, isUnique);
		}

		/// <summary>
		/// Gets the MRU index of <paramref name="completion"/>
		/// </summary>
		/// <param name="completion">Completion item</param>
		/// <returns></returns>
		protected virtual int GetMruIndex(Completion completion) => int.MaxValue;
	}
}
