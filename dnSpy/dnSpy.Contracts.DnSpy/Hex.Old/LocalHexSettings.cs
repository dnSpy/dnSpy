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

using dnSpy.Contracts.HexEditor;

namespace dnSpy.Contracts.Hex {
	sealed class LocalHexSettings {
		public int? BytesGroupCount;
		public int? BytesPerLine;
		public bool? UseHexPrefix;
		public bool? ShowAscii;
		public bool? LowerCaseHex;
		public AsciiEncoding? AsciiEncoding;

		public int HexOffsetSize;
		public bool UseRelativeOffsets;
		public ulong BaseOffset;

		public ulong? StartOffset;
		public ulong? EndOffset;

		public LocalHexSettings() {
		}

		public LocalHexSettings(DnHexBox dnHexBox) {
			BytesGroupCount = dnHexBox.BytesGroupCount;
			BytesPerLine = dnHexBox.BytesPerLine;
			UseHexPrefix = dnHexBox.UseHexPrefix;
			ShowAscii = dnHexBox.ShowAscii;
			LowerCaseHex = dnHexBox.LowerCaseHex;
			AsciiEncoding = dnHexBox.AsciiEncoding;
			HexOffsetSize = dnHexBox.HexOffsetSize;
			UseRelativeOffsets = dnHexBox.UseRelativeOffsets;
			BaseOffset = dnHexBox.BaseOffset;
			StartOffset = dnHexBox.StartOffset == dnHexBox.DocumentStartOffset ? (ulong?)null : dnHexBox.StartOffset;
			EndOffset = dnHexBox.EndOffset == dnHexBox.DocumentEndOffset ? (ulong?)null : dnHexBox.EndOffset;
		}

		public void CopyTo(DnHexBox dnHexBox) {
			dnHexBox.BytesGroupCount = BytesGroupCount;
			dnHexBox.BytesPerLine = BytesPerLine;
			dnHexBox.UseHexPrefix = UseHexPrefix;
			dnHexBox.ShowAscii = ShowAscii;
			dnHexBox.LowerCaseHex = LowerCaseHex;
			dnHexBox.AsciiEncoding = AsciiEncoding;
			dnHexBox.HexOffsetSize = HexOffsetSize;
			dnHexBox.UseRelativeOffsets = UseRelativeOffsets;
			dnHexBox.BaseOffset = BaseOffset;
			dnHexBox.StartOffset = StartOffset ?? dnHexBox.DocumentStartOffset;
			dnHexBox.EndOffset = EndOffset ?? dnHexBox.DocumentEndOffset;
		}
	}
}
