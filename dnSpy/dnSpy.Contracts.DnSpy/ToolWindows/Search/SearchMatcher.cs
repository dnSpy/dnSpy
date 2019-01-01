/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Text;
using dnSpy.Contracts.DnSpy.Properties;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.ToolWindows.Search {
	sealed class SearchMatcher {
		const StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase;
		readonly List<Span> spans;
		readonly SearchColumnDefinition[] definitions;
		readonly SearchColumnCommandParser parser;
		SearchCommand[] searchCommands;

		public SearchMatcher(SearchColumnDefinition[] definitions) {
			this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
			spans = new List<Span>();
			parser = new SearchColumnCommandParser(definitions);
			searchCommands = Array.Empty<SearchCommand>();
		}

		public string GetHelpText() {
			var sb = new StringBuilder();
			foreach (var def in definitions) {
				sb.Append("-" + def.ShortOptionName);
				sb.Append(" <text> ");
				sb.Append(string.Format(dnSpy_Contracts_DnSpy_Resources.Search_SearchColumnHelpText, def.LocalizedName));
				sb.AppendLine();
			}
			Debug.Assert(definitions.Length > 0);
			if (definitions.Length > 0) {
				sb.AppendLine();
				var def = definitions[0];
				sb.AppendLine(string.Format(dnSpy_Contracts_DnSpy_Resources.Search_SearchColumnInvertMatchHelpText, "-option!", "-" + def.ShortOptionName + "! <text>"));
			}
			return sb.ToString();
		}

		public void SetSearchText(string searchText) => searchCommands = parser.GetSearchCommands(searchText);

		public bool IsMatchAll(string[] columnText) {
			if (columnText.Length != definitions.Length)
				throw new InvalidOperationException();
			foreach (var cmd in searchCommands) {
				if (cmd.ColumnId == null) {
					bool match = false;
					foreach (var text in columnText) {
						bool b = text.IndexOf(cmd.SearchText, stringComparison) >= 0;
						if (cmd.Negate)
							b = !b;
						if (b) {
							match = true;
							break;
						}
					}
					if (!match)
						return false;
				}
				else {
					int index = GetColumnIndex(cmd.ColumnId);
					Debug.Assert(index >= 0);
					if (index < 0)
						return false;
					bool b = columnText[index].IndexOf(cmd.SearchText, stringComparison) >= 0;
					if (cmd.Negate)
						b = !b;
					if (!b)
						return false;
				}
			}
			return true;
		}

		int GetColumnIndex(string id) {
			for (int i = 0; i < definitions.Length; i++) {
				if (definitions[i].Id == id)
					return i;
			}
			return -1;
		}

		public List<Span> GetMatchSpans(string columnId, string text) {
			spans.Clear();
			if (searchCommands.Length == 0 || string.IsNullOrEmpty(text))
				return spans;

			foreach (var cmd in searchCommands) {
				if (cmd.ColumnId != null && cmd.ColumnId != columnId)
					continue;
				if (cmd.Negate)
					continue;
				int searchLength = cmd.SearchText.Length;
				if (searchLength == 0)
					continue;
				for (int index = 0; index < text.Length;) {
					index = text.IndexOf(cmd.SearchText, index, stringComparison);
					if (index < 0)
						break;
					spans.Add(new Span(index, searchLength));
					// "aa" should match "aaa" at index 0 and 1
					index++;
				}
			}

			return spans;
		}
	}
}
