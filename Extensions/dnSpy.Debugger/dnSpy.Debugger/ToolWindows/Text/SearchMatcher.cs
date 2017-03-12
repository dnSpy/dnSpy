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
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Debugger.ToolWindows.Text {
	sealed class SearchMatcher {
		string[] searchParts;
		readonly List<Span> spans;

		public SearchMatcher() {
			searchParts = Array.Empty<string>();
			spans = new List<Span>();
		}
		static readonly char[] searchSeparators = new char[] { ' ', '\t', '\r', '\n' };

		public void SetSearchText(string searchText) => searchParts = (searchText ?? string.Empty).Split(searchSeparators, StringSplitOptions.RemoveEmptyEntries);

		public bool IsMatchAll(string[] text) {
			foreach (var part in searchParts) {
				bool match = false;
				foreach (var t in text) {
					if (t.IndexOf(part, StringComparison.CurrentCultureIgnoreCase) >= 0) {
						match = true;
						break;
					}
				}
				if (!match)
					return false;
			}
			return true;
		}

		public List<Span> GetMatchSpans(string text) {
			spans.Clear();
			if (searchParts.Length == 0 || string.IsNullOrEmpty(text))
				return spans;

			foreach (var part in searchParts) {
				for (int index = 0; index < text.Length;) {
					index = text.IndexOf(part, index, StringComparison.CurrentCultureIgnoreCase);
					if (index < 0)
						break;
					spans.Add(new Span(index, part.Length));
					// "aa" should match "aaa" at index 0 and 1
					index++;
				}
			}

			return spans;
		}
	}
}
