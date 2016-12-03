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
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class StorageHeaderVM : HexVM {
		public override string Name => "STORAGEHEADER";
		public ByteFlagsHexField FFlagsVM { get; }
		public ByteHexField PadVM { get; }
		public UInt16HexField IStreamsVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public StorageHeaderVM(object owner, HexBuffer buffer, HexPosition startOffset)
			: base(owner) {
			FFlagsVM = new ByteFlagsHexField(buffer, Name, "fFlags", startOffset + 0);
			FFlagsVM.Add(new BooleanHexBitField("ExtraData", 0));
			PadVM = new ByteHexField(buffer, Name, "pad", startOffset + 1);
			IStreamsVM = new UInt16HexField(buffer, Name, "iStreams", startOffset + 2);

			hexFields = new HexField[] {
				FFlagsVM,
				PadVM,
				IStreamsVM,
			};
		}
	}
}
