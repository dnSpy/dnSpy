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
using System.ComponentModel.Composition;

namespace dnSpy.Text.Editor.Search {
	interface ISearchSettings {
		string SearchString { get; }
		string ReplaceString { get; }
		bool MatchCase { get; }
		bool MatchWholeWords { get; }
		bool UseRegularExpressions { get; }
		void SaveSettings(string searchString, string replaceString, bool matchCase, bool matchWholeWords, bool useRegularExpressions);
	}

	[Export(typeof(ISearchSettings))]
	sealed class SearchSettings : ISearchSettings {
		public string SearchString { get; private set; } = string.Empty;
		public string ReplaceString { get; private set; } = string.Empty;
		public bool MatchCase { get; private set; } = false;
		public bool MatchWholeWords { get; private set; } = false;
		public bool UseRegularExpressions { get; private set; } = false;

		public void SaveSettings(string searchString, string replaceString, bool matchCase, bool matchWholeWords, bool useRegularExpressions) {
			SearchString = searchString ?? throw new ArgumentNullException(nameof(searchString));
			ReplaceString = replaceString ?? throw new ArgumentNullException(nameof(replaceString));
			MatchCase = matchCase;
			MatchWholeWords = matchWholeWords;
			UseRegularExpressions = useRegularExpressions;
		}
	}
}
