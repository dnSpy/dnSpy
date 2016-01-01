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

using dnSpy.Shared.UI.Properties;
using ICSharpCode.AvalonEdit.Search;

namespace dnSpy.Shared.UI.AvalonEdit {
	public sealed class AvalonEditSearchPanelLocalization : Localization {
		public override string MatchCaseText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_MatchCaseText; }
		}

		public override string MatchWholeWordsText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_MatchWholeWordsText; }
		}

		public override string UseRegexText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_UseRegexText; }
		}

		public override string FindNextText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_FindNextText; }
		}

		public override string FindPreviousText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_FindPreviousText; }
		}

		public override string ErrorText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_ErrorText; }
		}

		public override string NoMatchesFoundText {
			get { return dnSpy_Shared_UI_Resources.SearchPanel_NoMatchesFoundText; }
		}
	}
}
