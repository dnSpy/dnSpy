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

using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	sealed class HexBoxUIState {
		public HexBoxState HexBoxState = new HexBoxState();
		public int? BytesGroupCount;
		public int? BytesPerLine;
		public bool? UseHexPrefix;
		public bool? ShowAscii;
		public bool? LowerCaseHex;
		public AsciiEncoding? AsciiEncoding;
		public int HexOffsetSize;
		public bool UseRelativeOffsets;
		public ulong BaseOffset;
	}
}
