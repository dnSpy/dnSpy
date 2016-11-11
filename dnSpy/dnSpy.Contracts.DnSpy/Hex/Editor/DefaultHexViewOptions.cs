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
	/// Default <see cref="HexView"/> options
	/// </summary>
	public static class DefaultHexViewOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string HexBytesDisplayFormatName = "HexView/HexBytesDisplayFormat";
		public static readonly EditorOptionKey<HexBytesDisplayFormat> HexBytesDisplayFormatId = new EditorOptionKey<HexBytesDisplayFormat>(HexBytesDisplayFormatName);
		public const string HexOffsetFormatName = "HexView/HexOffsetFormat";
		public static readonly EditorOptionKey<HexOffsetFormat> HexOffsetFormatId = new EditorOptionKey<HexOffsetFormat>(HexOffsetFormatName);
		public const string LowerCaseHexName = "HexView/LowerCaseHex";
		public static readonly EditorOptionKey<bool> LowerCaseHexId = new EditorOptionKey<bool>(LowerCaseHexName);
		public const string BytesPerLineName = "HexView/BytesPerLine";
		public static readonly EditorOptionKey<int> BytesPerLineId = new EditorOptionKey<int>(BytesPerLineName);
		public const string EnableColorizationName = "HexView/EnableColorization";
		public static readonly EditorOptionKey<bool> EnableColorizationId = new EditorOptionKey<bool>(EnableColorizationName);
		public const string ViewProhibitUserInputName = "HexView/ProhibitUserInput";
		public static readonly EditorOptionKey<bool> ViewProhibitUserInputId = new EditorOptionKey<bool>(ViewProhibitUserInputName);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
