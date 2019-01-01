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

using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class ImageCor20HeaderSectionData : PESectionData {
		public override uint Alignment => 8;
		const uint headerSize = 0x48;

		readonly DotNetMetadataSectionData mdData;
		readonly StrongNameSignatureSectionData snData;
		long cor20HeaderMetadataDataDirPosition;
		long cor20HeaderStrongnameDataDirPosition;

		public ImageCor20HeaderSectionData(DotNetMetadataSectionData mdData, StrongNameSignatureSectionData snData) {
			this.mdData = mdData;
			this.snData = snData;
		}

		public override void Write(MDWriter mdWriter, uint rva, MDWriterStream stream) {
			mdWriter.WriteDataDirectory(14, rva, headerSize);
			stream.Write(headerSize);
			var cor20 = mdWriter.MetadataEditor.RealMetadata.ImageCor20Header;
			stream.Write(cor20.MajorRuntimeVersion);
			stream.Write(cor20.MinorRuntimeVersion);
			cor20HeaderMetadataDataDirPosition = stream.Position;
			stream.Position += 8;// Metadata data directory, updated later
			var flags = cor20.Flags;
			flags &= ~ComImageFlags.NativeEntryPoint;
			if (snData == null)
				flags &= ~ComImageFlags.StrongNameSigned;
			else
				flags |= ComImageFlags.StrongNameSigned;
			stream.Write((uint)flags);
			if ((cor20.Flags & ComImageFlags.NativeEntryPoint) == 0)
				stream.Write(cor20.EntryPointToken_or_RVA);
			else
				stream.Position += 4;
			stream.Position += 8;// .NET resources
			cor20HeaderStrongnameDataDirPosition = stream.Position;
			stream.Position += 8;// Strong name signature
			stream.Position += 8;// Code manager table
			stream.Position += 8;// Vtable fixups
			stream.Position += 8;// Export address table jumps
			stream.Position += 8;// Managed native header
		}

		public override void Finish(MDWriter mdWriter, MDWriterStream stream) {
			stream.Position = cor20HeaderMetadataDataDirPosition;
			Debug.Assert(mdData.RVA != 0);
			Debug.Assert(mdData.Size != 0);
			stream.Write(mdData.RVA);
			stream.Write(mdData.Size);

			if (snData != null) {
				stream.Position = cor20HeaderStrongnameDataDirPosition;
				Debug.Assert(snData.RVA != 0);
				Debug.Assert(snData.Size != 0);
				stream.Write(snData.RVA);
				stream.Write(snData.Size);
			}
		}
	}
}
