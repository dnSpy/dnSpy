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

using System.Collections.Generic;
using System.Text;
using dnSpy.Contracts.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class StorageStreamVM : HexVM {
		public override string Name => "STORAGESTREAM";

		public UInt32HexField IOffsetVM { get; }
		public UInt32HexField ISizeVM { get; }
		public StringHexField RCNameVM { get; }
		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public StorageStreamVM(object owner, HexDocument doc, ulong startOffset, int stringLen)
			: base(owner) {
			this.IOffsetVM = new UInt32HexField(doc, Name, "iOffset", startOffset + 0);
			this.ISizeVM = new UInt32HexField(doc, Name, "iSize", startOffset + 4);
			this.RCNameVM = new StringHexField(doc, Name, "rcName", startOffset + 8, Encoding.ASCII, stringLen);

			this.hexFields = new HexField[] {
				IOffsetVM,
				ISizeVM,
				RCNameVM,
			};
		}
	}
}
