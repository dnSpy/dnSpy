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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Predefined hex adornment layer names
	/// </summary>
	public static class PredefinedHexAdornmentLayers {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		const string prefix = "hex-";
		public const string BottomLayer = prefix + nameof(BottomLayer);
		public const string TopLayer = prefix + nameof(TopLayer);
		public const string Outlining = prefix + nameof(Outlining);
		public const string Caret = prefix + nameof(Caret);
		public const string CurrentLineHighlighter = prefix + nameof(CurrentLineHighlighter);
		public const string Selection = prefix + nameof(Selection);
		public const string Text = prefix + nameof(Text);
		public const string TextMarker = prefix + nameof(TextMarker);
		public const string NegativeTextMarker = prefix + nameof(NegativeTextMarker);
		public const string GlyphTextMarker = prefix + nameof(GlyphTextMarker);
		public const string BackgroundImage = prefix + nameof(BackgroundImage);
		public const string Search = prefix + nameof(Search);
		public const string IntraTextAdornment = prefix + nameof(IntraTextAdornment);
		public const string ColumnLineSeparator = prefix + nameof(ColumnLineSeparator);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
