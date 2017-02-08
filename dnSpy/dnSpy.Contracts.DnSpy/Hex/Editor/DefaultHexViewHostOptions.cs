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

using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Default <see cref="WpfHexViewHost"/> options
	/// </summary>
	public static class DefaultHexViewHostOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string HorizontalScrollBarName = "HexViewHost/HorizontalScrollBar";
		public static readonly VSTE.EditorOptionKey<bool> HorizontalScrollBarId = new VSTE.EditorOptionKey<bool>(HorizontalScrollBarName);
		public const string VerticalScrollBarName = "HexViewHost/VerticalScrollBar";
		public static readonly VSTE.EditorOptionKey<bool> VerticalScrollBarId = new VSTE.EditorOptionKey<bool>(VerticalScrollBarName);
		public const string SelectionMarginName = "HexViewHost/SelectionMargin";
		public static readonly VSTE.EditorOptionKey<bool> SelectionMarginId = new VSTE.EditorOptionKey<bool>(SelectionMarginName);
		public const string ZoomControlName = "HexViewHost/ZoomControl";
		public static readonly VSTE.EditorOptionKey<bool> ZoomControlId = new VSTE.EditorOptionKey<bool>(ZoomControlName);
		public const string GlyphMarginName = "HexViewHost/GlyphMargin";
		public static readonly VSTE.EditorOptionKey<bool> GlyphMarginId = new VSTE.EditorOptionKey<bool>(GlyphMarginName);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
