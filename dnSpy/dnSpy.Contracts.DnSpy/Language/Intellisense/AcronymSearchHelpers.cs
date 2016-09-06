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

using System.Diagnostics;

namespace dnSpy.Contracts.Language.Intellisense {
	static class AcronymSearchHelpers {
		public static int[] TryCreateMatchIndexes(string searchText) {
			// Don't treat eg. "I" as an acronym search. We don't want "I"
			// to match Int16 intead of IEnumerable.
			if (searchText.Length <= 1)
				return null;
			foreach (var c in searchText) {
				if (!char.IsUpper(c))
					return null;
			}
			return new int[searchText.Length];
		}

		public static bool TryUpdateAcronymIndexes(int[] acronymMatchIndexes, string searchText, string completionText) {
			if (acronymMatchIndexes == null)
				return false;
			for (int acronymIndex = 0, textIndex = 0; acronymIndex < searchText.Length; acronymIndex++) {
				textIndex = completionText.IndexOf(searchText[acronymIndex], textIndex);
				if (textIndex < 0)
					return false;
				acronymMatchIndexes[acronymIndex] = textIndex;
				textIndex++;
			}
			return true;
		}
	}
}
