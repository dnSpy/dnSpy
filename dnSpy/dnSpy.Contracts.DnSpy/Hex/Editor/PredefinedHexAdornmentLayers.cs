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
		const string prefix = "hex-";

		/// <summary>
		/// Bottom layer. All layers should normally be after this layer.
		/// </summary>
		public const string BottomLayer = prefix + nameof(BottomLayer);

		/// <summary>
		/// Top layer. All layers should normally be before this layer.
		/// </summary>
		public const string TopLayer = prefix + nameof(TopLayer);

		/// <summary>
		/// Caret adornment layer
		/// </summary>
		public const string Caret = prefix + nameof(Caret);

		/// <summary>
		/// Current line highlighter adornment layer
		/// </summary>
		public const string CurrentLineHighlighter = prefix + nameof(CurrentLineHighlighter);

		/// <summary>
		/// Selection adornment layer
		/// </summary>
		public const string Selection = prefix + nameof(Selection);

		/// <summary>
		/// Text adornment layer
		/// </summary>
		public const string Text = prefix + nameof(Text);

		/// <summary>
		/// Text marker adornment layer for markers with a negative z-index
		/// </summary>
		public const string NegativeTextMarker = prefix + nameof(NegativeTextMarker);

		/// <summary>
		/// Text marker adornment layer
		/// </summary>
		public const string TextMarker = prefix + nameof(TextMarker);

		/// <summary>
		/// Background image adornment layer
		/// </summary>
		public const string BackgroundImage = prefix + nameof(BackgroundImage);

		/// <summary>
		/// Search adornment layer
		/// </summary>
		public const string Search = prefix + nameof(Search);
	}
}
