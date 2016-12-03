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
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class StorageSignatureVM : HexVM {
		public override string Name => "STORAGESIGNATURE";

		public UInt32HexField LSignatureVM { get; }
		public UInt16HexField IMajorVerVM { get; }
		public UInt16HexField IMinorVerVM { get; }
		public UInt32HexField IExtraDataVM { get; }
		public UInt32HexField IVersionStringVM { get; }
		public StringHexField VersionStringVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public StorageSignatureVM(object owner, HexBuffer buffer, HexPosition startOffset, int stringLen)
			: base(owner) {
			this.LSignatureVM = new UInt32HexField(buffer, Name, "lSignature", startOffset + 0);
			this.IMajorVerVM = new UInt16HexField(buffer, Name, "iMajorVer", startOffset + 4, true);
			this.IMinorVerVM = new UInt16HexField(buffer, Name, "iMinorVer", startOffset + 6, true);
			this.IExtraDataVM = new UInt32HexField(buffer, Name, "iExtraData", startOffset + 8);
			this.IVersionStringVM = new UInt32HexField(buffer, Name, "iVersionString", startOffset + 0x0C);
			this.VersionStringVM = new StringHexField(buffer, Name, "VersionString", startOffset + 0x10, Encoding.UTF8, stringLen);

			this.hexFields = new HexField[] {
				LSignatureVM,
				IMajorVerVM,
				IMinorVerVM,
				IExtraDataVM,
				IVersionStringVM,
				VersionStringVM,
			};
		}
	}
}
