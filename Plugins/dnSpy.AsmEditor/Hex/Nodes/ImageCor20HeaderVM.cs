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
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
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

		public ImageCor20HeaderVM(object owner, HexDocument doc, ulong startOffset)
			: base(owner) {
			this.CbVM = new UInt32HexField(doc, Name, "cb", startOffset + 0);
			this.MajorRuntimeVersionVM = new UInt16HexField(doc, Name, "MajorRuntimeVersion", startOffset + 4, true);
			this.MinorRuntimeVersionVM = new UInt16HexField(doc, Name, "MinorRuntimeVersion", startOffset + 6, true);
			this.MetaDataVM = new DataDirVM(doc, Name, "MetaData", startOffset + 8);
			this.FlagsVM = new UInt32FlagsHexField(doc, Name, "Flags", startOffset + 0x10);
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_IL_Only, 0));
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_32BitReqd, 1));
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_ILLibrary, 2));
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_StrongNameSigned, 3));
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_NativeEntryPoint, 4));
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_TrackDebugData, 16));
			this.FlagsVM.Add(new BooleanHexBitField(dnSpy_AsmEditor_Resources.HexNode_Cor20Header_Flags_32BitPref, 17));
			this.EntryPointTokenRVAVM = new UInt32HexField(doc, Name, "EntryPoint Token/RVA", startOffset + 0x14);
			this.ResourcesVM = new DataDirVM(doc, Name, "Resources", startOffset + 0x18);
			this.StrongNameSignatureVM = new DataDirVM(doc, Name, "StrongNameSignature", startOffset + 0x20);
			this.CodeManagerTableVM = new DataDirVM(doc, Name, "CodeManagerTable", startOffset + 0x28);
			this.VTableFixupsVM = new DataDirVM(doc, Name, "VTableFixups", startOffset + 0x30);
			this.ExportAddressTableJumpsVM = new DataDirVM(doc, Name, "ExportAddressTableJumps", startOffset + 0x38);
			this.ManagedNativeHeaderVM = new DataDirVM(doc, Name, "ManagedNativeHeader", startOffset + 0x40);

			this.hexFields = new HexField[] {
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
