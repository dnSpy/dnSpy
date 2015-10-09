/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dnSpy.HexEditor;

namespace dnSpy.Hex {
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
			this.BytesGroupCount = dnHexBox.BytesGroupCount;
			this.BytesPerLine = dnHexBox.BytesPerLine;
			this.UseHexPrefix = dnHexBox.UseHexPrefix;
			this.ShowAscii = dnHexBox.ShowAscii;
			this.LowerCaseHex = dnHexBox.LowerCaseHex;
			this.AsciiEncoding = dnHexBox.AsciiEncoding;
			this.HexOffsetSize = dnHexBox.HexOffsetSize;
			this.UseRelativeOffsets = dnHexBox.UseRelativeOffsets;
			this.BaseOffset = dnHexBox.BaseOffset;
			this.StartOffset = dnHexBox.StartOffset == dnHexBox.DocumentStartOffset ? (ulong?)null : dnHexBox.StartOffset;
			this.EndOffset = dnHexBox.EndOffset == dnHexBox.DocumentEndOffset ? (ulong?)null : dnHexBox.EndOffset;
		}

		public void CopyTo(DnHexBox dnHexBox) {
			dnHexBox.BytesGroupCount = this.BytesGroupCount;
			dnHexBox.BytesPerLine = this.BytesPerLine;
			dnHexBox.UseHexPrefix = this.UseHexPrefix;
			dnHexBox.ShowAscii = this.ShowAscii;
			dnHexBox.LowerCaseHex = this.LowerCaseHex;
			dnHexBox.AsciiEncoding = this.AsciiEncoding;
			dnHexBox.HexOffsetSize = this.HexOffsetSize;
			dnHexBox.UseRelativeOffsets = this.UseRelativeOffsets;
			dnHexBox.BaseOffset = this.BaseOffset;
			dnHexBox.StartOffset = this.StartOffset ?? dnHexBox.DocumentStartOffset;
			dnHexBox.EndOffset = this.EndOffset ?? dnHexBox.DocumentEndOffset;
		}
	}
}
