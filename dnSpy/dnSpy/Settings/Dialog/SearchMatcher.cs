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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Settings.Dialog {
	sealed class SearchMatcher {
		string[] searchParts;
		readonly List<Span> spans;
		readonly List<string> remaining;

		public SearchMatcher() {
			searchParts = Array.Empty<string>();
			spans = new List<Span>();
			remaining = new List<string>();
		}
		static readonly char[] searchSeparators = new char[] { ' ', '\t', '\r', '\n' };

		public void SetSearchText(string searchText) {
			searchParts = (searchText ?? string.Empty).Split(searchSeparators, StringSplitOptions.RemoveEmptyEntries);
		}

		public bool IsMatchAll(List<string> pageTitles, List<string> pageStrings) {
			if (searchParts.Length == 0)
				return false;

			// For each page title that matches a searched string, remove it from the
			// list we should check. Whatever is remaining must all be present in at
			// least one string for this method to return true.

			remaining.Clear();
			remaining.AddRange(searchParts);
			for (int i = remaining.Count - 1; i >= 0; i--) {
				var part = remaining[i];
				foreach (var title in pageTitles) {
					if (title.IndexOf(part, StringComparison.CurrentCultureIgnoreCase) >= 0) {
						remaining.RemoveAt(i);
						break;
					}
				}
			}
			if (remaining.Count == 0)
				return true;

			foreach (var s in pageStrings) {
				bool match = true;
				foreach (var part in remaining) {
					if (s.IndexOf(part, StringComparison.CurrentCultureIgnoreCase) < 0) {
						match = false;
						break;
					}
				}
				if (match)
					return true;
			}

			return false;
		}

		public bool IsMatchAny(string text) {
			if (string.IsNullOrEmpty(text))
				return false;
			foreach (var part in searchParts) {
				if (text.IndexOf(part, StringComparison.CurrentCultureIgnoreCase) >= 0)
					return true;
			}
			return false;
		}

		public List<Span> GetMatchSpans(string text) {
			spans.Clear();
			if (searchParts.Length == 0 || string.IsNullOrEmpty(text))
				return spans;

			foreach (var part in searchParts) {
				int index = text.IndexOf(part, StringComparison.CurrentCultureIgnoreCase);
				if (index >= 0)
					spans.Add(new Span(index, part.Length));
			}

			return spans;
		}
	}
}
