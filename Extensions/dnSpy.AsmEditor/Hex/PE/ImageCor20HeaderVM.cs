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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageCor20HeaderVM : HexVM {
		public override string Name => "IMAGE_COR20_HEADER";
		public UInt32HexField CbVM { get; }
		public UInt16HexField MajorRuntimeVersionVM { get; }
		public UInt16HexField MinorRuntimeVersionVM { get; }
		public DataDirVM MetaDataVM { get; }
		public UInt32FlagsHexField FlagsVM { get; }
		public UInt32HexField EntryPointTokenRVAVM { get; }
		public DataDirVM ResourcesVM { get; }
		public DataDirVM StrongNameSignatureVM { get; }
		public DataDirVM CodeManagerTableVM { get; }
		public DataDirVM VTableFixupsVM { get; }
		public DataDirVM ExportAddressTableJumpsVM { get; }
		public DataDirVM ManagedNativeHeaderVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public ImageCor20HeaderVM(HexBuffer buffer, HexSpan span)
			: base(span) {
			var startOffset = span.Start;
			CbVM = new UInt32HexField(buffer, Name, "cb", startOffset + 0);
			MajorRuntimeVersionVM = new UInt16HexField(buffer, Name, "MajorRuntimeVersion", startOffset + 4, true);
			MinorRuntimeVersionVM = new UInt16HexField(buffer, Name, "MinorRuntimeVersion", startOffset + 6, true);
			MetaDataVM = new DataDirVM(buffer, Name, "MetaData", startOffset + 8);
			FlagsVM = new UInt32FlagsHexField(buffer, Name, "Flags", startOffset + 0x10);
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_IL_Only, 0));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_32BitReqd, 1));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_ILLibrary, 2));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_StrongNameSigned, 3));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_NativeEntryPoint, 4));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_TrackDebugData, 16));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_32BitPref, 17));
			EntryPointTokenRVAVM = new UInt32HexField(buffer, Name, "EntryPoint Token/RVA", startOffset + 0x14);
			ResourcesVM = new DataDirVM(buffer, Name, "Resources", startOffset + 0x18);
			StrongNameSignatureVM = new DataDirVM(buffer, Name, "StrongNameSignature", startOffset + 0x20);
			CodeManagerTableVM = new DataDirVM(buffer, Name, "CodeManagerTable", startOffset + 0x28);
			VTableFixupsVM = new DataDirVM(buffer, Name, "VTableFixups", startOffset + 0x30);
			ExportAddressTableJumpsVM = new DataDirVM(buffer, Name, "ExportAddressTableJumps", startOffset + 0x38);
			ManagedNativeHeaderVM = new DataDirVM(buffer, Name, "ManagedNativeHeader", startOffset + 0x40);

			hexFields = new HexField[] {
				CbVM,
				MajorRuntimeVersionVM,
				MinorRuntimeVersionVM,
				MetaDataVM.RVAVM,
				MetaDataVM.SizeVM,
				FlagsVM,
				EntryPointTokenRVAVM,
				ResourcesVM.RVAVM,
				ResourcesVM.SizeVM,
				StrongNameSignatureVM.RVAVM,
				StrongNameSignatureVM.SizeVM,
				CodeManagerTableVM.RVAVM,
				CodeManagerTableVM.SizeVM,
				VTableFixupsVM.RVAVM,
				VTableFixupsVM.SizeVM,
				ExportAddressTableJumpsVM.RVAVM,
				ExportAddressTableJumpsVM.SizeVM,
				ManagedNativeHeaderVM.RVAVM,
				ManagedNativeHeaderVM.SizeVM,
			};
		}
	}
}
