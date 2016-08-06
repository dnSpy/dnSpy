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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// dnSpy adornment layers
	/// </summary>
	public static class PredefinedDnSpyAdornmentLayers {
		/// <summary>
		/// Bottom layer. All layers should normally be after this layer.
		/// </summary>
		public const string BottomLayer = "dnSpy-BottomLayer";

		/// <summary>
		/// Top layer. All layers should normally be before this layer.
		/// </summary>
		public const string TopLayer = "dnSpy-TopLayer";

		/// <summary>
		/// <see cref="IGlyphTextMarkerService"/>'s adornment layer
		/// </summary>
		public const string GlyphTextMarker = "dnSpy-GlyphTextMarker";

		/// <summary>
		/// Line separator adornment layer
		/// </summary>
		public const string LineSeparator = "dnSpy-LineSeparator";

		/// <summary>
		/// Background image adornment layer
		/// </summary>
		public const string BackgroundImage = "dnSpy-BackgroundImage";
	}
}
