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
using System.ComponentModel.Composition;

namespace dnSpy.Hex.Editor.Search {
	abstract class SearchSettings {
		public abstract string SearchString { get; }
		public abstract string ReplaceString { get; }
		public abstract bool MatchCase { get; }
		public abstract bool BigEndian { get; }
		public abstract HexDataKind DataKind { get; }
		public abstract void SaveSettings(string searchString, string replaceString, bool matchCase, bool bigEndian, HexDataKind dataKind);
	}

	[Export(typeof(SearchSettings))]
	sealed class SearchSettingsImpl : SearchSettings {
		public override string SearchString => searchString;
		public override string ReplaceString => replaceString;
		public override bool MatchCase => matchCase;
		public override bool BigEndian => bigEndian;
		public override HexDataKind DataKind => dataKind;

		string searchString = string.Empty;
		string replaceString = string.Empty;
		bool matchCase = false;
		bool bigEndian = false;
		HexDataKind dataKind = HexDataKind.Bytes;

		public override void SaveSettings(string searchString, string replaceString, bool matchCase, bool bigEndian, HexDataKind dataKind) {
			if (searchString == null)
				throw new ArgumentNullException(nameof(searchString));
			if (replaceString == null)
				throw new ArgumentNullException(nameof(replaceString));
			this.searchString = searchString;
			this.replaceString = replaceString;
			this.matchCase = matchCase;
			this.bigEndian = bigEndian;
			this.dataKind = dataKind;
		}
	}
}
