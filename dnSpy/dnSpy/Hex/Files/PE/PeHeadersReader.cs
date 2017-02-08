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

using System;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.PE {
	sealed class PeHeadersReader {
		public PeDosHeaderData DosHeader { get; private set; }
		public PeFileHeaderData FileHeader { get; private set; }
		public PeOptionalHeaderData OptionalHeader { get; private set; }
		public PeSectionsData Sections { get; private set; }
		public ImageSectionHeader[] SectionHeaders { get; private set; }
		public bool IsFileLayout { get; private set; }

		readonly HexBufferFile file;
		readonly Lazy<PeFileLayoutProvider, VSUTIL.IOrderable>[] peFileLayoutProviders;
		readonly bool initFileLayout;

		public PeHeadersReader(HexBufferFile file, Lazy<PeFileLayoutProvider, VSUTIL.IOrderable>[] peFileLayoutProviders) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (peFileLayoutProviders == null)
				throw new ArgumentNullException(nameof(peFileLayoutProviders));
			this.file = file;
			this.peFileLayoutProviders = peFileLayoutProviders;

			if (file.Tags.Contains(PredefinedBufferFileTags.FileLayout))
				IsFileLayout = true;
			else if (file.Tags.Contains(PredefinedBufferFileTags.MemoryLayout))
				IsFileLayout = false;
			else
				initFileLayout = true;
		}

		public bool Read() {
			var pos = file.Span.Start;
			DosHeader = PeDosHeaderDataImpl.TryCreate(file, pos);
			if (DosHeader == null)
				return false;

			uint ntHeaderOffset = DosHeader.Lfanew.Data.ReadValue();
			pos = file.Span.Start + ntHeaderOffset;
			if (pos + 4 > file.Span.End)
				return false;
			if (file.Buffer.ReadUInt32(pos) != 0x4550)
				return false;
			pos += 4;
			FileHeader = PeFileHeaderDataImpl.TryCreate(file, pos);
			if (FileHeader == null)
				return false;
			pos = FileHeader.Span.End;
			uint sizeOfOptionalHeader = FileHeader.SizeOfOptionalHeader.Data.ReadValue();
			OptionalHeader = CreateOptionalHeader(pos, sizeOfOptionalHeader);
			if (OptionalHeader == null)
				return false;
			pos = OptionalHeader.Span.Span.Start + sizeOfOptionalHeader;
			int sects = FileHeader.NumberOfSections.Data.ReadValue();
			Sections = CreateSections(pos, sects);
			if (Sections == null)
				return false;

			var headers = new ImageSectionHeader[Sections.FieldCount];
			for (int i = 0; i < headers.Length; i++) {
				var h = Sections[i].Data;
				headers[i] = new ImageSectionHeader(h.VirtualSize.Data.ReadValue(), h.VirtualAddress.Data.ReadValue(), h.SizeOfRawData.Data.ReadValue(), h.PointerToRawData.Data.ReadValue());
			}
			SectionHeaders = headers;

			if (initFileLayout)
				IsFileLayout = GuessIsFileLayout();
			return true;
		}

		PeOptionalHeaderData CreateOptionalHeader(HexPosition position, uint size) {
			switch (file.Buffer.ReadUInt16(position)) {
			case 0x010B: return PeOptionalHeader32DataImpl.TryCreate(file, position, size);
			case 0x020B: return PeOptionalHeader64DataImpl.TryCreate(file, position, size);
			default: return null;
			}
		}

		PeSectionsData CreateSections(HexPosition position, int sects) {
			if (sects != 0 && (!file.Span.Contains(position) || !file.Span.Contains(position + ((ulong)sects * 0x28 - 1))))
				return null;
			var fields = new ArrayField<PeSectionData>[sects];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var data = PeSectionDataImpl.TryCreate(file, currPos);
				if (data == null)
					return null;
				var field = new ArrayField<PeSectionData>(data, (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new PeSectionsData(new HexBufferSpan(file.Buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		bool GuessIsFileLayout() {
			foreach (var lz in peFileLayoutProviders) {
				var kind = lz.Value.GetLayout(file);
				if (kind == PeFileLayout.File)
					return true;
				if (kind == PeFileLayout.Memory)
					return false;
				Debug.Assert(kind == PeFileLayout.Unknown);
			}

			var b = DotNetCheckIsFileLayout();
			if (b != null)
				return b.Value;

			if (!file.IsNestedFile && file.Buffer.IsMemory) {
				// If there's unmapped memory, it's probably a file loaded by the OS loader
				var pos = file.Span.Start;
				while (pos < file.Span.End) {
					var info = file.Buffer.GetSpanInfo(pos);
					if (!info.HasData)
						return false;
					pos = info.Span.End;
				}
			}

			return DefaultIsFileLayout;
		}

		bool? DotNetCheckIsFileLayout() {
			if (OptionalHeader.DataDirectory.Data.FieldCount <= 14)
				return null;
			var dataDir = OptionalHeader.DataDirectory.Data[14];
			var rva = dataDir.Data.VirtualAddress.Data.ReadValue();
			var size = dataDir.Data.Size.Data.ReadValue();
			if (rva == 0 || size < 0x48)
				return null;

			bool mem = CheckDotNet(rva, MemoryLayout_ToBufferPosition);
			bool file = CheckDotNet(rva, FileLayout_ToBufferPosition);
			if (mem && file)
				return DefaultIsFileLayout;
			if (mem)
				return false;
			if (file)
				return true;

			return null;
		}

		// If it's a nested file, it's most likely file layout
		bool DefaultIsFileLayout => file.IsNestedFile || !file.Buffer.IsMemory;

		bool CheckDotNet(uint cor20Rva, Func<uint, HexPosition> rvaToPos) {
			var pos = rvaToPos(cor20Rva);
			if (!file.Span.Contains(pos) || !file.Span.Contains(pos + (0x48 - 1)))
				return false;
			if (file.Buffer.ReadUInt32(pos) < 0x48)
				return false;
			var hdrRva = file.Buffer.ReadUInt32(pos + 8);
			var hdrSize = file.Buffer.ReadUInt32(pos + 0x0C);
			if (hdrSize < 0x20)
				return false;
			pos = rvaToPos(hdrRva);
			if (!file.Span.Contains(pos) || !file.Span.Contains(pos + (hdrSize - 1)))
				return false;
			if (file.Buffer.ReadUInt32(pos) != 0x424A5342)
				return false;

			// Looks OK
			return true;
		}

		HexPosition MemoryLayout_ToBufferPosition(uint rva) => file.Span.Start + rva;

		HexPosition FileLayout_ToBufferPosition(uint rva) {
			var fileSpan = file.Span;
			foreach (var sect in SectionHeaders) {
				if (rva >= sect.VirtualAddress && rva < sect.VirtualAddress + Math.Max(sect.VirtualSize, sect.SizeOfRawData))
					return fileSpan.Start + ((rva - sect.VirtualAddress) + sect.PointerToRawData);
			}
			return fileSpan.Start + rva;
		}
	}
}
