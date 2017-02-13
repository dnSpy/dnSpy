/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Contracts.Language.Intellisense {
	struct BestMatchSelector {
		enum MatchPriority {
			/// <summary>
			/// Full match, case sensitive
			/// </summary>
			Full,

			/// <summary>
			/// Full acronym match, eg. <c>TA</c> matches <c>TaskAwaiter</c>.
			/// This has less priority than <see cref="Full"/> because if there's an
			/// identifier named <c>TA</c>, then it should be selected, and not <c>TaskAwaiter</c>.
			/// </summary>
			FullAcronym,

			/// <summary>
			/// Full match, case insensitive
			/// </summary>
			FullIgnoringCase,

			/// <summary>
			/// Matches start of filter, case sensitive
			/// </summary>
			Start,

			/// <summary>
			/// Matches acronyms at start of filter.
			/// </summary>
			StartAcronym,

			/// <summary>
			/// Matches start of filter, case insensitive
			/// </summary>
			StartIgnoringCase,

			/// <summary>
			/// Matches any location, case sensitive
			/// </summary>
			AnyLocation,

			/// <summary>
			/// Matches acronyms at any location.
			/// </summary>
			AnyLocationAcronym,

			/// <summary>
			/// Matches any location, case insensitive
			/// </summary>
			AnyLocationIgnoringCase,

			/// <summary>
			/// Other match
			/// </summary>
			Other,

			/// <summary>
			/// Not a match
			/// </summary>
			Nothing = int.MaxValue,
		}

		public Completion Result => bestCompletion;

		readonly string searchText;
		MatchPriority matchPriority;
		Completion bestCompletion;
		readonly int[] acronymMatchIndexes;

		public BestMatchSelector(string searchText) {
			this.searchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
			matchPriority = MatchPriority.Nothing;
			bestCompletion = null;
			acronymMatchIndexes = AcronymSearchHelpers.TryCreateMatchIndexes(searchText);
		}

		public void Select(Completion completion) {
			var newMatchPriority = GetMatchPriority(completion);
			if (newMatchPriority < matchPriority) {
				bestCompletion = completion;
				matchPriority = newMatchPriority;
			}
		}

		int CountUpperCaseLetters(string text) {
			int count = 0;
			foreach (var c in text) {
				if (char.IsUpper(c))
					count++;
			}
			return count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		MatchPriority GetMatchPriority(Completion completion) {
			var filterText = completion.TryGetFilterText();
			Debug.Assert(filterText != null);
			if (filterText == null)
				return MatchPriority.Other;

			if (filterText.Equals(searchText, StringComparison.CurrentCulture))
				return MatchPriority.Full;

			bool matchedAcronym = acronymMatchIndexes != null && AcronymSearchHelpers.TryUpdateAcronymIndexes(acronymMatchIndexes, searchText, filterText);
			if (matchedAcronym && CountUpperCaseLetters(filterText) == acronymMatchIndexes.Length)
				return MatchPriority.FullAcronym;

			if (filterText.Equals(searchText, StringComparison.CurrentCultureIgnoreCase))
				return MatchPriority.FullIgnoringCase;

			int index = filterText.IndexOf(searchText, StringComparison.CurrentCulture);
			if (index == 0)
				return MatchPriority.Start;

			if (matchedAcronym && acronymMatchIndexes[0] == 0)
				return MatchPriority.StartAcronym;

			int indexIgnoringCase = filterText.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase);
			if (indexIgnoringCase == 0)
				return MatchPriority.StartIgnoringCase;

			if (index > 0)
				return MatchPriority.AnyLocation;
			if (matchedAcronym)
				return MatchPriority.AnyLocationAcronym;
			if (indexIgnoringCase > 0)
				return MatchPriority.AnyLocationIgnoringCase;

			return MatchPriority.Other;
		}
	}
}
