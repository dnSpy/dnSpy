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

using dnSpy.Tabs;

namespace dnSpy.AsmEditor.Hex {
	sealed class LocalHexSettings {
		public int? BytesGroupCount;
		public int? BytesPerLine;
		public bool? UseHexPrefix;
		public bool? ShowAscii;
		public bool? LowerCaseHex;

		public int HexOffsetSize;
		public bool UseRelativeOffsets;
		public ulong BaseOffset;

		public ulong? StartOffset;
		public ulong? EndOffset;

		public LocalHexSettings() {
		}

		public LocalHexSettings(HexTabState tabState) {
			this.BytesGroupCount = tabState.BytesGroupCount;
			this.BytesPerLine = tabState.BytesPerLine;
			this.UseHexPrefix = tabState.UseHexPrefix;
			this.ShowAscii = tabState.ShowAscii;
			this.LowerCaseHex = tabState.LowerCaseHex;
			this.HexOffsetSize = tabState.HexBox.HexOffsetSize;
			this.UseRelativeOffsets = tabState.HexBox.UseRelativeOffsets;
			this.BaseOffset = tabState.HexBox.BaseOffset;
			this.StartOffset = tabState.HexBox.StartOffset == 0 ? (ulong?)null : tabState.HexBox.StartOffset;
			this.EndOffset = tabState.HexBox.EndOffset == tabState.DocumentEndOffset ? (ulong?)null : tabState.HexBox.EndOffset;
		}

		public void CopyTo(HexTabState tabState) {
			tabState.BytesGroupCount = this.BytesGroupCount;
			tabState.BytesPerLine = this.BytesPerLine;
			tabState.UseHexPrefix = this.UseHexPrefix;
			tabState.ShowAscii = this.ShowAscii;
			tabState.LowerCaseHex = this.LowerCaseHex;
			tabState.HexBox.HexOffsetSize = this.HexOffsetSize;
			tabState.HexBox.UseRelativeOffsets = this.UseRelativeOffsets;
			tabState.HexBox.BaseOffset = this.BaseOffset;
			tabState.HexBox.StartOffset = this.StartOffset ?? 0;
			tabState.HexBox.EndOffset = this.EndOffset ?? tabState.DocumentEndOffset;
		}
	}
}
