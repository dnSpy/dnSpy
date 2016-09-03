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
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// <see cref="Completion"/> collection
	/// </summary>
	public class CompletionCollection {
		readonly Completion[] allCompletions;
		readonly FilteredCompletionCollection filteredCompletions;

		/// <summary>
		/// Gets the filtered collection
		/// </summary>
		public IFilteredCompletionCollection FilteredCollection => filteredCompletions;

		/// <summary>
		/// Current <see cref="Completion"/>
		/// </summary>
		public CurrentCompletion CurrentCompletion {
			get { return currentCompletion; }
			set {
				if ((object)value == null)
					throw new ArgumentNullException(nameof(value));
				if (value.Equals(currentCompletion))
					return;
				if (value.Completion != null && !filteredCompletions.Contains(value.Completion))
					throw new ArgumentException();
				currentCompletion = value;
				CurrentCompletionChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		CurrentCompletion currentCompletion;

		/// <summary>
		/// Raised when <see cref="CurrentCompletion"/> has changed
		/// </summary>
		public event EventHandler CurrentCompletionChanged;

		/// <summary>
		/// Span that will be modified when a <see cref="Completion"/> gets committed
		/// </summary>
		public ITrackingSpan ApplicableTo { get; protected set; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected CompletionCollection()
			: this(null, Array.Empty<Completion>()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="applicableTo">Span that will be modified when a <see cref="Completion"/> gets committed</param>
		/// <param name="completions">Completion items</param>
		public CompletionCollection(ITrackingSpan applicableTo, IEnumerable<Completion> completions) {
			if (completions == null)
				throw new ArgumentNullException(nameof(completions));
			currentCompletion = CurrentCompletion.Empty;
			allCompletions = completions.ToArray();
			filteredCompletions = new FilteredCompletionCollection(allCompletions);
			ApplicableTo = applicableTo;
		}

		/// <summary>
		/// Creates a <see cref="ICompletionFilter"/>
		/// </summary>
		/// <param name="searchText">Search text</param>
		/// <returns></returns>
		protected virtual ICompletionFilter CreateCompletionFilter(string searchText) => new CompletionFilter(searchText);

		/// <summary>
		/// Filters the list. <see cref="SelectBestMatch"/> should be called after this method
		/// </summary>
		public virtual void Filter() {
			Debug.Assert(ApplicableTo != null, "You must initialize " + nameof(ApplicableTo) + " before calling this method");
			var inputText = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
			IList<Completion> newList;
			if (inputText.Length < CompletionConstants.MimimumSearchLengthForFilter)
				newList = allCompletions;
			else {
				var list = new List<Completion>();
				newList = list;
				var completionFilter = CreateCompletionFilter(inputText);
				foreach (var c in allCompletions) {
					if (completionFilter.IsMatch(c))
						list.Add(c);
				}
			}
			if (newList.Count != 0)
				filteredCompletions.SetNewFilteredCollection(newList);
		}

		/// <summary>
		/// Selects the best match and should be called after <see cref="Filter"/>
		/// </summary>
		public void SelectBestMatch() =>
			CurrentCompletion = GetBestMatch() ?? CurrentCompletion.Empty;

		enum MatchPriority {
			/// <summary>
			/// Full match, case sensitive
			/// </summary>
			Full,

			/// <summary>
			/// Full match, case insensitive
			/// </summary>
			FullIgnoringCase,

			/// <summary>
			/// Matches start of filter, case sensitive
			/// </summary>
			Start,

			/// <summary>
			/// Matches start of filter, case insensitive
			/// </summary>
			StartIgnoringCase,

			/// <summary>
			/// Matches any location, case sensitive
			/// </summary>
			AnyLocation,

			/// <summary>
			/// Matches any location, case insensitive
			/// </summary>
			AnyLocationIgnoringCase,

			/// <summary>
			/// Other match, eg. an acronym match
			/// </summary>
			Other,

			/// <summary>
			/// Not a match
			/// </summary>
			Nothing = int.MaxValue,
		}

		/// <summary>
		/// Gets the best match in <see cref="FilteredCollection"/>
		/// </summary>
		/// <returns></returns>
		protected virtual CurrentCompletion GetBestMatch() {
			Debug.Assert(ApplicableTo != null, "You must initialize " + nameof(ApplicableTo) + " before calling this method");
			var inputText = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
			var completionFilter = CreateCompletionFilter(inputText);
			int matches = 0;
			Completion bestCompletion = null;
			var matchPriority = MatchPriority.Nothing;
			if (inputText.Length > 0) {
				foreach (var completion in filteredCompletions) {
					int index;
					if (!completionFilter.IsMatch(completion))
						continue;
					matches++;

					MatchPriority newMatchPriority;
					if (completion.FilterText.Equals(inputText, StringComparison.CurrentCulture))
						newMatchPriority = MatchPriority.Full;
					else if (completion.FilterText.Equals(inputText, StringComparison.CurrentCultureIgnoreCase))
						newMatchPriority = MatchPriority.FullIgnoringCase;
					else if ((index = completion.FilterText.IndexOf(inputText, StringComparison.CurrentCulture)) >= 0)
						newMatchPriority = index == 0 ? MatchPriority.Start : MatchPriority.AnyLocation;
					else if ((index = completion.FilterText.IndexOf(inputText, StringComparison.CurrentCultureIgnoreCase)) >= 0)
						newMatchPriority = index == 0 ? MatchPriority.StartIgnoringCase : MatchPriority.AnyLocationIgnoringCase;
					else
						newMatchPriority = MatchPriority.Other;
					if (newMatchPriority < matchPriority) {
						bestCompletion = completion;
						matchPriority = newMatchPriority;
					}
				}
			}
			bool isSelected = bestCompletion != null;
			return new CurrentCompletion(bestCompletion, isSelected, matches == 1);
		}

		/// <summary>
		/// Commits the currently selected <see cref="Completion"/>
		/// </summary>
		public virtual void Commit() => CurrentCompletion.Completion?.Commit(ApplicableTo);
	}
}
