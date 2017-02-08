/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	/// Predefined hex margin names
	/// </summary>
	public static class PredefinedHexMarginNames {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		const string prefix = "hex-";
		public const string Left = prefix + nameof(Left);
		public const string Right = prefix + nameof(Right);
		public const string Top = prefix + nameof(Top);
		public const string Bottom = prefix + nameof(Bottom);
		public const string LeftSelection = prefix + nameof(LeftSelection);
		public const string Outlining = prefix + nameof(Outlining);
		public const string LineNumber = prefix + nameof(LineNumber);
		public const string CustomLineNumber = prefix + nameof(CustomLineNumber);
		public const string HorizontalScrollBar = prefix + nameof(HorizontalScrollBar);
		public const string HorizontalScrollBarContainer = prefix + nameof(HorizontalScrollBarContainer);
		public const string VerticalScrollBar = prefix + nameof(VerticalScrollBar);
		public const string VerticalScrollBarContainer = prefix + nameof(VerticalScrollBarContainer);
		public const string RightControl = prefix + nameof(RightControl);
		public const string BottomControl = prefix + nameof(BottomControl);
		public const string Spacer = prefix + nameof(Spacer);
		public const string Glyph = prefix + nameof(Glyph);
		public const string Suggestion = prefix + nameof(Suggestion);
		public const string ZoomControl = prefix + nameof(ZoomControl);
		public const string BottomRightCorner = prefix + nameof(BottomRightCorner);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
