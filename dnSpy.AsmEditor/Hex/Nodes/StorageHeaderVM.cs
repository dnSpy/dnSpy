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
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class StorageHeaderVM : HexVM {
		public override string Name {
			get { return "STORAGEHEADER"; }
		}

		public ByteFlagsHexField FFlagsVM {
			get { return fFlagsVM; }
		}
		readonly ByteFlagsHexField fFlagsVM;

		public ByteHexField PadVM {
			get { return padVM; }
		}
		readonly ByteHexField padVM;

		public UInt16HexField IStreamsVM {
			get { return iStreamsVM; }
		}
		readonly UInt16HexField iStreamsVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		public StorageHeaderVM(object owner, HexDocument doc, ulong startOffset)
			: base(owner) {
			this.fFlagsVM = new ByteFlagsHexField(doc, Name, "fFlags", startOffset + 0);
			this.fFlagsVM.Add(new BooleanHexBitField("ExtraData", 0));
			this.padVM = new ByteHexField(doc, Name, "pad", startOffset + 1);
			this.iStreamsVM = new UInt16HexField(doc, Name, "iStreams", startOffset + 2);

			this.hexFields = new HexField[] {
				fFlagsVM,
				padVM,
				iStreamsVM,
			};
		}
	}
}
