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

		public SearchMatcher() {
			this.searchParts = Array.Empty<string>();
			this.spans = new List<Span>();
		}
		static readonly char[] searchSeparators = new char[] { ' ', '\t', '\r', '\n' };

		public void SetSearchText(string searchText) {
			searchParts = (searchText ?? string.Empty).Split(searchSeparators, StringSplitOptions.RemoveEmptyEntries);
		}

		public bool IsMatchAll(List<string> list) {
			if (searchParts.Length == 0)
				return false;
			foreach (var part in searchParts) {
				bool match = false;
				foreach (var listString in list) {
					if (listString.IndexOf(part, StringComparison.CurrentCultureIgnoreCase) >= 0) {
						match = true;
						break;
					}
				}
				if (!match)
					return false;
			}
			return true;
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
