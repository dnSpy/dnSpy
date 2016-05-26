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
	/// Default <see cref="IWpfTextViewHost"/> options
	/// </summary>
	public static class DefaultTextViewHostOptions {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static readonly EditorOptionKey<bool> HorizontalScrollBarId = new EditorOptionKey<bool>("IWpfTextViewHost/HorizontalScrollBar");
		public static readonly EditorOptionKey<bool> VerticalScrollBarId = new EditorOptionKey<bool>("IWpfTextViewHost/VerticalScrollBar");
		public static readonly EditorOptionKey<bool> LineNumberMarginId = new EditorOptionKey<bool>("IWpfTextViewHost/LineNumberMargin");
		public static readonly EditorOptionKey<bool> SelectionMarginId = new EditorOptionKey<bool>("IWpfTextViewHost/SelectionMargin");
		public static readonly EditorOptionKey<bool> GlyphMarginId = new EditorOptionKey<bool>("IWpfTextViewHost/GlyphMargin");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
