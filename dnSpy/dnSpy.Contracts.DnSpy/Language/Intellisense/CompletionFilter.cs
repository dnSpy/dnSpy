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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Default <see cref="Completion"/> filter
	/// </summary>
	sealed class CompletionFilter : ICompletionFilter {
		readonly string searchText;
		readonly int[] acronymMatchIndexes;
		const StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="searchText">Search text</param>
		public CompletionFilter(string searchText) {
			this.searchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
			acronymMatchIndexes = AcronymSearchHelpers.TryCreateMatchIndexes(searchText);
		}

		bool TryUpdateAcronymIndexes(string completionText) =>
			AcronymSearchHelpers.TryUpdateAcronymIndexes(acronymMatchIndexes, searchText, completionText);

		public bool IsMatch(Completion completion) {
			var completionText = completion.TryGetFilterText();
			if (completionText == null)
				return false;

			if (completionText.IndexOf(searchText, stringComparison) >= 0)
				return true;
			if (acronymMatchIndexes != null && TryUpdateAcronymIndexes(completionText))
				return true;

			return false;
		}

		public Span[] GetMatchSpans(string completionText) {
			Debug.Assert(acronymMatchIndexes == null || acronymMatchIndexes.Length > 0);

			// Acronyms have higher priority, eg. TA should match |T|ask|A|waiter
			// and not |Ta|skAwaiter.
			if (acronymMatchIndexes != null && TryUpdateAcronymIndexes(completionText)) {
				var localIndexes = acronymMatchIndexes;
				var res = new Span[localIndexes.Length];
				for (int i = 0; i < localIndexes.Length; i++)
					res[i] = new Span(localIndexes[i], 1);
				return res;
			}

			int index = completionText.IndexOf(searchText, stringComparison);
			if (index >= 0)
				return new[] { new Span(index, searchText.Length) };

			return Array.Empty<Span>();
		}
	}
}
