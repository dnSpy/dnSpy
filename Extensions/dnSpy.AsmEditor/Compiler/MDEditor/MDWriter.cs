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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class MDWriter {
		readonly RawModuleBytes moduleData;
		readonly MetadataEditor mdEditor;
		readonly MDWriterStream stream;
		readonly List<PESection> sections;
		PESection? textSection;
		long dataDirPosition;

		public MetadataEditor MetadataEditor => mdEditor;
		public RawModuleBytes ModuleData => moduleData;

		const uint SectionAlignment = 0x2000;
		const uint FileAlignment = 0x200;

		static readonly byte[] dosHeader = new byte[0x80] {
			0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
			0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
			0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
			0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
			0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
			0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72,
			0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
			0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E,
			0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
			0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A,
			0x24, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
		};

		sealed class PESection {
			public byte[] Name { get; }
			public uint Characteristics { get; }
			public List<PESectionData> SectionData { get; }
			public uint VirtualAddress { get; set; }
			public uint PointerToRawData { get; set; }
			public uint VirtualSize { get; set; }
			public uint SizeOfRawData { get; set; }

			public PESection(string name, uint characteristics) {
				var bytes = Encoding.ASCII.GetBytes(name);
				if (bytes.Length != 8)
					Array.Resize(ref bytes, 8);
				Name = bytes;
				Characteristics = characteristics;
				SectionData = new List<PESectionData>();
			}
		}

		public MDWriter(RawModuleBytes moduleData, MetadataEditor mdEditor, MDWriterStream stream) {
			this.moduleData = moduleData;
			this.mdEditor = mdEditor;
			this.stream = stream;
			sections = new List<PESection>();
		}

		public void Write() {
			var peImage = mdEditor.RealMetadata.PEImage;
			bool is32Bit = peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32;

			sections.Add(textSection = new PESection(".text", 0x60000020));

			StrongNameSignatureSectionData? snData = null;
			var cor20 = mdEditor.RealMetadata.ImageCor20Header;
			if ((cor20.Flags & ComImageFlags.StrongNameSigned) != 0 && cor20.StrongNameSignature.Size != 0 && cor20.StrongNameSignature.VirtualAddress != 0)
				snData = new StrongNameSignatureSectionData(cor20.StrongNameSignature.Size);

			if (!(snData is null))
				textSection.SectionData.Add(snData);
			var mdData = new DotNetMetadataSectionData(mdEditor);
			textSection.SectionData.Add(new ImageCor20HeaderSectionData(mdData, snData));
			textSection.SectionData.Add(mdData);

			// DOS MZ header
			stream.Write(dosHeader);

			// PE\0\0
			Debug.Assert(stream.Position == BitConverter.ToUInt32(dosHeader, 0x7C));
			stream.Write(0x00004550);

			// IMAGE_FILE_HEADER
			var ifh = peImage.ImageNTHeaders.FileHeader;
			stream.Write((ushort)ifh.Machine);
			Debug.Assert(sections.Count <= ushort.MaxValue);
			stream.Write((ushort)sections.Count);
			stream.Write(ifh.TimeDateStamp);
			stream.Position += 8;// PointerToSymbolTable & NumberOfSymbols
			stream.Write((ushort)(is32Bit ? 0xE0 : 0xF0));
			stream.Write((ushort)ifh.Characteristics);

			// IMAGE_OPTIONAL_HEADER
			var optHeaderPos = stream.Position;
			if (is32Bit) {
				var opt = (ImageOptionalHeader32)peImage.ImageNTHeaders.OptionalHeader;
				var start = stream.Position;
				stream.Write(opt.Magic);
				stream.Write(opt.MajorLinkerVersion);
				stream.Write(opt.MinorLinkerVersion);
				// SizeOfCode, SizeOfInitializedData, SizeOfUninitializedData
				// AddressOfEntryPoint, BaseOfCode, BaseOfData
				stream.Position += 6 * 4;
				stream.Write((uint)opt.ImageBase);
				stream.Write((uint)SectionAlignment);
				stream.Write((uint)FileAlignment);
				stream.Write(opt.MajorOperatingSystemVersion);
				stream.Write(opt.MinorOperatingSystemVersion);
				stream.Write(opt.MajorImageVersion);
				stream.Write(opt.MinorImageVersion);
				stream.Write(opt.MajorSubsystemVersion);
				stream.Write(opt.MinorSubsystemVersion);
				stream.Write(opt.Win32VersionValue);
				// SizeOfImage, SizeOfHeaders, CheckSum
				stream.Position += 3 * 4;
				stream.Write((ushort)opt.Subsystem);
				stream.Write((ushort)opt.DllCharacteristics);
				stream.Write((uint)opt.SizeOfStackReserve);
				stream.Write((uint)opt.SizeOfStackCommit);
				stream.Write((uint)opt.SizeOfHeapReserve);
				stream.Write((uint)opt.SizeOfHeapCommit);
				stream.Write(opt.LoaderFlags);
				stream.Write(0x10);// NumberOfRvaAndSizes
				Debug.Assert((stream.Position - start) == 0x60);
			}
			else {
				var opt = (ImageOptionalHeader64)peImage.ImageNTHeaders.OptionalHeader;
				var start = stream.Position;
				stream.Write(opt.Magic);
				stream.Write(opt.MajorLinkerVersion);
				stream.Write(opt.MinorLinkerVersion);
				// SizeOfCode, SizeOfInitializedData, SizeOfUninitializedData
				// AddressOfEntryPoint, BaseOfCode
				stream.Position += 5 * 4;
				stream.Write(opt.ImageBase);
				stream.Write((uint)SectionAlignment);
				stream.Write((uint)FileAlignment);
				stream.Write(opt.MajorOperatingSystemVersion);
				stream.Write(opt.MinorOperatingSystemVersion);
				stream.Write(opt.MajorImageVersion);
				stream.Write(opt.MinorImageVersion);
				stream.Write(opt.MajorSubsystemVersion);
				stream.Write(opt.MinorSubsystemVersion);
				stream.Write(opt.Win32VersionValue);
				// SizeOfImage, SizeOfHeaders, CheckSum
				stream.Position += 3 * 4;
				stream.Write((ushort)opt.Subsystem);
				stream.Write((ushort)opt.DllCharacteristics);
				stream.Write(opt.SizeOfStackReserve);
				stream.Write(opt.SizeOfStackCommit);
				stream.Write(opt.SizeOfHeapReserve);
				stream.Write(opt.SizeOfHeapCommit);
				stream.Write(opt.LoaderFlags);
				stream.Write(0x10);// NumberOfRvaAndSizes
				Debug.Assert((stream.Position - start) == 0x70);
			}

			// IMAGE_DATA_DIRECTORY
			dataDirPosition = stream.Position;
			stream.Position += 0x10 * 8;

			// IMAGE_SECTION_HEADER
			var sectionPos = stream.Position;
			foreach (var section in sections) {
				Debug.Assert(section.Name.Length == 8);
				stream.Write(section.Name);
				// VirtualSize, VirtualAddress, SizeOfRawData, PointerToRawData
				// PointerToRelocations, PointerToLinenumbers, NumberOfRelocations, NumberOfLinenumbers
				stream.Position += 6 * 4 + 2 * 2;
				stream.Write(section.Characteristics);
			}
			uint headerLength = (uint)stream.Position;

			AlignUp(FileAlignment);

			// Write all sections
			uint rva = SectionAlignment;
			foreach (var section in sections) {
				section.VirtualAddress = rva;
				section.PointerToRawData = (uint)stream.Position;
				foreach (var data in section.SectionData) {
					var pos = stream.Position;
					AlignUp(data.Alignment);
					rva += (uint)(stream.Position - pos);

					pos = stream.Position;
					data.Write(this, rva, stream);
					rva += (uint)(stream.Position - pos);
				}
				Debug.Assert(stream.Position != section.PointerToRawData);
				if (stream.Position == section.PointerToRawData)
					stream.Position++;
				section.VirtualSize = (uint)stream.Position - section.PointerToRawData;
				section.SizeOfRawData = ((uint)stream.Position - section.PointerToRawData + FileAlignment - 1) & ~(FileAlignment - 1);
				rva = (rva + SectionAlignment - 1) & ~(SectionAlignment - 1);
				AlignUp(FileAlignment);
			}

			stream.Length = stream.Position;

			// Update IMAGE_SECTION_HEADER
			stream.Position = sectionPos;
			foreach (var section in sections) {
				stream.Position += 8;
				stream.Write(section.VirtualSize);
				stream.Write(section.VirtualAddress);
				stream.Write(section.SizeOfRawData);
				stream.Write(section.PointerToRawData);
				stream.Position += 16;
			}

			// Update IMAGE_OPTIONAL_HEADER
			var sectionSizes = new SectionSizes(FileAlignment, SectionAlignment, headerLength, () => GetSectionSizeInfos());
			stream.Position = optHeaderPos;
			if (is32Bit) {
				var opt = (ImageOptionalHeader32)peImage.ImageNTHeaders.OptionalHeader;
				stream.Position += 4;
				stream.Write(sectionSizes.SizeOfCode);
				stream.Write(sectionSizes.SizeOfInitdData);
				stream.Write(sectionSizes.SizeOfUninitdData);
				stream.Position += 4;
				stream.Write(sectionSizes.BaseOfCode);
				stream.Write(sectionSizes.BaseOfData);
				stream.Position += 0x1C;
				stream.Write(sectionSizes.SizeOfImage);
				stream.Write(sectionSizes.SizeOfHeaders);
			}
			else {
				var opt = (ImageOptionalHeader64)peImage.ImageNTHeaders.OptionalHeader;
				stream.Position += 4;
				stream.Write(sectionSizes.SizeOfCode);
				stream.Write(sectionSizes.SizeOfInitdData);
				stream.Write(sectionSizes.SizeOfUninitdData);
				stream.Position += 4;
				stream.Write(sectionSizes.BaseOfCode);
				stream.Position += 0x20;
				stream.Write(sectionSizes.SizeOfImage);
				stream.Write(sectionSizes.SizeOfHeaders);
			}

			foreach (var section in sections) {
				foreach (var data in section.SectionData)
					data.Finish(this, stream);
			}
		}

		IEnumerable<SectionSizeInfo> GetSectionSizeInfos() {
			foreach (var section in sections)
				yield return new SectionSizeInfo(section.VirtualSize, section.Characteristics);
		}

		void AlignUp(uint alignment) => stream.Position = (stream.Position + alignment - 1) & ~(alignment - 1);

		public void WriteDataDirectory(int index, uint rva, uint size) {
			if ((uint)index >= 0x10)
				throw new ArgumentOutOfRangeException(nameof(index));

			var oldPos = stream.Position;

			stream.Position = dataDirPosition + index * 8;
			stream.Write(rva);
			stream.Write(size);

			stream.Position = oldPos;
		}
	}
}
