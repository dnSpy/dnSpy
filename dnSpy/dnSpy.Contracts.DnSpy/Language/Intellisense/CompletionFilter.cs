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
			if (searchText == null)
				throw new ArgumentNullException(nameof(searchText));
			this.searchText = searchText;

			bool acronymSearch = true;
			foreach (var c in searchText) {
				if (!char.IsUpper(c)) {
					acronymSearch = false;
					break;
				}
			}
			if (acronymSearch && searchText.Length > 0)
				acronymMatchIndexes = new int[searchText.Length];
		}

		bool TryUpdateAcronymIndexes(string completionText) {
			Debug.Assert(acronymMatchIndexes != null);
			if (acronymMatchIndexes == null)
				return false;
			var searchTextLocal = searchText;
			var acronymMatchIndexesLocal = acronymMatchIndexes;
			for (int acronymIndex = 0, textIndex = 0; acronymIndex < searchTextLocal.Length; acronymIndex++) {
				textIndex = completionText.IndexOf(searchTextLocal[acronymIndex], textIndex);
				if (textIndex < 0)
					return false;
				acronymMatchIndexesLocal[acronymIndex] = textIndex;
				textIndex++;
			}
			return true;
		}

		public bool IsMatch(Completion completion) {
			var completionText = completion.FilterText;

			if (completionText.IndexOf(searchText, stringComparison) >= 0)
				return true;
			if (acronymMatchIndexes != null && TryUpdateAcronymIndexes(completionText))
				return true;

			return false;
		}

		public IEnumerable<Span> GetMatchSpans(Completion completion, string completionText) {
			int index = completionText.IndexOf(searchText, stringComparison);
			bool useAcronymIndexes = index != 0 && acronymMatchIndexes != null && TryUpdateAcronymIndexes(completionText);

			Debug.Assert(acronymMatchIndexes == null || acronymMatchIndexes.Length > 0);
			if (index >= 0 && useAcronymIndexes && acronymMatchIndexes[0] < index)
				index = -1;

			if (index >= 0) {
				yield return new Span(index, completionText.Length);
				yield break;
			}

			if (useAcronymIndexes) {
				foreach (var i in acronymMatchIndexes)
					yield return new Span(i, 1);
				yield break;
			}
		}
	}
}
