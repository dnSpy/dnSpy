/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class StorageHeaderVM : HexVM {
		public override string Name => "STORAGEHEADER";
		public ByteFlagsHexField FFlagsVM { get; }
		public ByteHexField PadVM { get; }
		public UInt16HexField IStreamsVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public StorageHeaderVM(HexBuffer buffer, DotNetMetadataHeaderData mdHeader)
			: base(HexSpan.FromBounds(mdHeader.Flags.Data.Span.Start, mdHeader.StreamCount.Data.Span.End)) {
			FFlagsVM = new ByteFlagsHexField(mdHeader.Flags);
			FFlagsVM.Add(new BooleanHexBitField(mdHeader.ExtraData.Name, 0));
			PadVM = new ByteHexField(mdHeader.Pad);
			IStreamsVM = new UInt16HexField(mdHeader.StreamCount);

			hexFields = new HexField[] {
				FFlagsVM,
				PadVM,
				IStreamsVM,
			};
		}
	}
}
