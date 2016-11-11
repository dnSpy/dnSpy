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

using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Default <see cref="WpfHexView"/> options
	/// </summary>
	public static class DefaultWpfHexViewOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string ForceClearTypeIfNeededName = "WpfHexView/ForceClearTypeIfNeeded";
		public static readonly EditorOptionKey<bool> ForceClearTypeIfNeededId = new EditorOptionKey<bool>(ForceClearTypeIfNeededName);
		public const string AppearanceCategoryName = "WpfHexView/Appearance/Category";
		public static readonly EditorOptionKey<string> AppearanceCategoryId = new EditorOptionKey<string>(AppearanceCategoryName);
		public const string EnableHighlightCurrentLineName = "WpfHexView/Adornments/HighlightCurrentLine/Enable";
		public static readonly EditorOptionKey<bool> EnableHighlightCurrentLineId = new EditorOptionKey<bool>(EnableHighlightCurrentLineName);
		public const string EnableMouseWheelZoomName = "WpfHexView/MouseWheelZoom";
		public static readonly EditorOptionKey<bool> EnableMouseWheelZoomId = new EditorOptionKey<bool>(EnableMouseWheelZoomName);
		public const string EnableSimpleGraphicsName = "WpfHexView/Graphics/Simple/Enable";
		public static readonly EditorOptionKey<bool> EnableSimpleGraphicsId = new EditorOptionKey<bool>(EnableSimpleGraphicsName);
		public const string UseReducedOpacityForHighContrastOptionName = "WpfHexView/UseReducedOpacityForHighContrast";
		public static readonly EditorOptionKey<bool> UseReducedOpacityForHighContrastOptionId = new EditorOptionKey<bool>(UseReducedOpacityForHighContrastOptionName);
		public const string ZoomLevelName = "WpfHexView/ZoomLevel";
		public static readonly EditorOptionKey<double> ZoomLevelId = new EditorOptionKey<double>(ZoomLevelName);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
