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
	/// Adornment layer order constants
	/// </summary>
	public static class AdornmentLayerOrder {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const double BraceCompletion = 1000;
		public const double CurrentLineHighlighter = 2000;
		public const double TextMarker = 3000;
		public const double Selection = 4000;
		public const double InterLine = 5000;
		public const double Squiggle = 6000;
		public const double Text = 7000;
		public const double IntraText = 8000;
		public const double VisibleWhitespace = 9000;
		public const double Caret = 10000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
