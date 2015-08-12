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
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class ImageCor20HeaderVM : HexVM {
		public override string Name {
			get { return "IMAGE_COR20_HEADER"; }
		}

		public UInt32HexField CbVM {
			get { return cbVM; }
		}
		readonly UInt32HexField cbVM;

		public UInt16HexField MajorRuntimeVersionVM {
			get { return majorRuntimeVersionVM; }
		}
		readonly UInt16HexField majorRuntimeVersionVM;

		public UInt16HexField MinorRuntimeVersionVM {
			get { return minorRuntimeVersionVM; }
		}
		readonly UInt16HexField minorRuntimeVersionVM;

		public DataDirVM MetaDataVM {
			get { return metaDataVM; }
		}
		readonly DataDirVM metaDataVM;

		public UInt32HexField FlagsVM {
			get { return flagsVM; }
		}
		readonly UInt32HexField flagsVM;

		public UInt32HexField EntryPointTokenRVAVM {
			get { return entryPointTokenRVAVM; }
		}
		readonly UInt32HexField entryPointTokenRVAVM;

		public DataDirVM ResourcesVM {
			get { return resourcesVM; }
		}
		readonly DataDirVM resourcesVM;

		public DataDirVM StrongNameSignatureVM {
			get { return strongNameSignatureVM; }
		}
		readonly DataDirVM strongNameSignatureVM;

		public DataDirVM CodeManagerTableVM {
			get { return codeManagerTableVM; }
		}
		readonly DataDirVM codeManagerTableVM;

		public DataDirVM VTableFixupsVM {
			get { return vtableFixupsVM; }
		}
		readonly DataDirVM vtableFixupsVM;

		public DataDirVM ExportAddressTableJumpsVM {
			get { return exportAddressTableJumpsVM; }
		}
		readonly DataDirVM exportAddressTableJumpsVM;

		public DataDirVM ManagedNativeHeaderVM {
			get { return managedNativeHeaderVM; }
		}
		readonly DataDirVM managedNativeHeaderVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		public ImageCor20HeaderVM(HexDocument doc, ulong startOffset) {
			this.cbVM = new UInt32HexField(doc, Name, "cb", startOffset + 0);
			this.majorRuntimeVersionVM = new UInt16HexField(doc, Name, "MajorRuntimeVersion", startOffset + 4);
			this.minorRuntimeVersionVM = new UInt16HexField(doc, Name, "MinorRuntimeVersion", startOffset + 6);
			this.metaDataVM = new DataDirVM(doc, Name, "MetaData", startOffset + 8);
			this.flagsVM = new UInt32HexField(doc, Name, "Flags", startOffset + 0x10);
			this.entryPointTokenRVAVM = new UInt32HexField(doc, Name, "EntryPoint Token/RVA", startOffset + 0x14);
			this.resourcesVM = new DataDirVM(doc, Name, "Resources", startOffset + 0x18);
			this.strongNameSignatureVM = new DataDirVM(doc, Name, "StrongNameSignature", startOffset + 0x20);
			this.codeManagerTableVM = new DataDirVM(doc, Name, "CodeManagerTable", startOffset + 0x28);
			this.vtableFixupsVM = new DataDirVM(doc, Name, "VTableFixups", startOffset + 0x30);
			this.exportAddressTableJumpsVM = new DataDirVM(doc, Name, "ExportAddressTableJumps", startOffset + 0x38);
			this.managedNativeHeaderVM = new DataDirVM(doc, Name, "ManagedNativeHeader", startOffset + 0x40);

			this.hexFields = new HexField[] {
				cbVM,
				majorRuntimeVersionVM,
				minorRuntimeVersionVM,
				metaDataVM.RVAVM,
				metaDataVM.SizeVM,
				flagsVM,
				entryPointTokenRVAVM,
				resourcesVM.RVAVM,
				resourcesVM.SizeVM,
				strongNameSignatureVM.RVAVM,
				strongNameSignatureVM.SizeVM,
				codeManagerTableVM.RVAVM,
				codeManagerTableVM.SizeVM,
				vtableFixupsVM.RVAVM,
				vtableFixupsVM.SizeVM,
				exportAddressTableJumpsVM.RVAVM,
				exportAddressTableJumpsVM.SizeVM,
				managedNativeHeaderVM.RVAVM,
				managedNativeHeaderVM.SizeVM,
			};
		}
	}
}
