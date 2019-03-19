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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageCor20HeaderVM : HexVM {
		public override string Name { get; }
		public UInt32HexField CbVM { get; }
		public UInt16HexField MajorRuntimeVersionVM { get; }
		public UInt16HexField MinorRuntimeVersionVM { get; }
		public DataDirectoryVM MetadataVM { get; }
		public UInt32FlagsHexField FlagsVM { get; }
		public UInt32HexField EntryPointTokenRVAVM { get; }
		public DataDirectoryVM ResourcesVM { get; }
		public DataDirectoryVM StrongNameSignatureVM { get; }
		public DataDirectoryVM CodeManagerTableVM { get; }
		public DataDirectoryVM VTableFixupsVM { get; }
		public DataDirectoryVM ExportAddressTableJumpsVM { get; }
		public DataDirectoryVM ManagedNativeHeaderVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public ImageCor20HeaderVM(HexBuffer buffer, DotNetCor20Data cor20)
			: base(cor20.Span) {
			Name = cor20.Name;
			CbVM = new UInt32HexField(cor20.Cb);
			MajorRuntimeVersionVM = new UInt16HexField(cor20.MajorRuntimeVersion, true);
			MinorRuntimeVersionVM = new UInt16HexField(cor20.MinorRuntimeVersion, true);
			MetadataVM = new DataDirectoryVM(cor20.Metadata);
			FlagsVM = new UInt32FlagsHexField(cor20.Flags);
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_IL_Only, 0));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_32BitReqd, 1));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_ILLibrary, 2));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_StrongNameSigned, 3));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_NativeEntryPoint, 4));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_TrackDebugData, 16));
			FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_32BitPref, 17));
			EntryPointTokenRVAVM = new UInt32HexField(cor20.EntryPointTokenOrRVA);
			ResourcesVM = new DataDirectoryVM(cor20.Resources);
			StrongNameSignatureVM = new DataDirectoryVM(cor20.StrongNameSignature);
			CodeManagerTableVM = new DataDirectoryVM(cor20.CodeManagerTable);
			VTableFixupsVM = new DataDirectoryVM(cor20.VTableFixups);
			ExportAddressTableJumpsVM = new DataDirectoryVM(cor20.ExportAddressTableJumps);
			ManagedNativeHeaderVM = new DataDirectoryVM(cor20.ManagedNativeHeader);

			hexFields = new HexField[] {
				CbVM,
				MajorRuntimeVersionVM,
				MinorRuntimeVersionVM,
				MetadataVM.RVAVM,
				MetadataVM.SizeVM,
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
