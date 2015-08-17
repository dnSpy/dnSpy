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

using System.Collections.Generic;
using System.Text;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class StorageSignatureVM : HexVM {
		public override string Name {
			get { return "STORAGESIGNATURE"; }
		}

		public UInt32HexField LSignatureVM {
			get { return lSignatureVM; }
		}
		readonly UInt32HexField lSignatureVM;

		public UInt16HexField IMajorVerVM {
			get { return iMajorVerVM; }
		}
		readonly UInt16HexField iMajorVerVM;

		public UInt16HexField IMinorVerVM {
			get { return iMinorVerVM; }
		}
		readonly UInt16HexField iMinorVerVM;

		public UInt32HexField IExtraDataVM {
			get { return iExtraDataVM; }
		}
		readonly UInt32HexField iExtraDataVM;

		public UInt32HexField IVersionStringVM {
			get { return iVersionStringVM; }
		}
		readonly UInt32HexField iVersionStringVM;

		public StringHexField VersionStringVM {
			get { return versionStringVM; }
		}
		readonly StringHexField versionStringVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		public StorageSignatureVM(HexDocument doc, ulong startOffset, int stringLen) {
			this.lSignatureVM = new UInt32HexField(doc, Name, "lSignature", startOffset + 0);
			this.iMajorVerVM = new UInt16HexField(doc, Name, "iMajorVer", startOffset + 4, true);
			this.iMinorVerVM = new UInt16HexField(doc, Name, "iMinorVer", startOffset + 6, true);
			this.iExtraDataVM = new UInt32HexField(doc, Name, "iExtraData", startOffset + 8);
			this.iVersionStringVM = new UInt32HexField(doc, Name, "iVersionString", startOffset + 0x0C);
			this.versionStringVM = new StringHexField(doc, Name, "VersionString", startOffset + 0x10, Encoding.UTF8, stringLen);

			this.hexFields = new HexField[] {
				lSignatureVM,
				iMajorVerVM,
				iMinorVerVM,
				iExtraDataVM,
				iVersionStringVM,
				versionStringVM,
			};
		}
	}
}
