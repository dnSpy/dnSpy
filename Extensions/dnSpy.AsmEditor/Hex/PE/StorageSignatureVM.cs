/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

		public StorageSignatureVM(HexBuffer buffer, DotNetMetadataHeaderData mdHeader)
			: base(HexSpan.FromBounds(mdHeader.Span.Start, mdHeader.VersionString.Data.Span.End)) {
			LSignatureVM = new UInt32HexField(mdHeader.Signature);
			IMajorVerVM = new UInt16HexField(mdHeader.MajorVersion, true);
			IMinorVerVM = new UInt16HexField(mdHeader.MinorVersion, true);
			IExtraDataVM = new UInt32HexField(mdHeader.ExtraData);
			IVersionStringVM = new UInt32HexField(mdHeader.VersionStringCount);
			VersionStringVM = new StringHexField(mdHeader.VersionString);

			hexFields = new HexField[] {
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
