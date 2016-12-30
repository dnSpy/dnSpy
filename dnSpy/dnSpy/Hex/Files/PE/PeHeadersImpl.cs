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

using System;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.PE {
	sealed class PeHeadersImpl : PeHeaders {
		public override PeDosHeaderData DosHeader { get; }
		public override PeFileHeaderData FileHeader { get; }
		public override PeOptionalHeaderData OptionalHeader { get; }
		public override PeSectionsData Sections { get; }
		public override bool IsFileLayout { get; }
		ImageSectionHeader[] SectionHeaders { get; }
		readonly HexSpan fileSpan;

		public PeHeadersImpl(PeHeadersReader reader, HexSpan fileSpan) {
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));
			DosHeader = reader.DosHeader;
			FileHeader = reader.FileHeader;
			OptionalHeader = reader.OptionalHeader;
			Sections = reader.Sections;
			IsFileLayout = reader.IsFileLayout;
			SectionHeaders = reader.SectionHeaders;
			this.fileSpan = fileSpan;
		}

		public override HexPosition RvaToBufferPosition(uint rva) {
			var fileSpan = this.fileSpan;
			if (IsFileLayout) {
				foreach (var sect in SectionHeaders) {
					if (rva >= sect.VirtualAddress && rva < sect.VirtualAddress + Math.Max(sect.VirtualSize, sect.SizeOfRawData))
						return fileSpan.Start + ((rva - sect.VirtualAddress) + sect.PointerToRawData);
				}
			}

			return fileSpan.Start + rva;
		}

		public override uint BufferPositionToRva(HexPosition position) {
			var fileSpan = this.fileSpan;
			if (!fileSpan.Contains(position))
				return 0;
			if (IsFileLayout) {
				ulong offset = (position - fileSpan.Start).ToUInt64();
				foreach (var sect in SectionHeaders) {
					if (offset >= sect.PointerToRawData && offset < sect.PointerToRawData + sect.SizeOfRawData)
						return (uint)(offset - sect.PointerToRawData) + sect.VirtualAddress;
				}
			}

			return (uint)(position - fileSpan.Start).ToUInt64();
		}

		public override ulong BufferPositionToFilePosition(HexPosition position) {
			var fileSpan = this.fileSpan;
			if (!fileSpan.Contains(position))
				return 0;

			ulong pos = (position - fileSpan.Start).ToUInt64();
			if (!IsFileLayout) {
				foreach (var sect in SectionHeaders) {
					if (pos >= sect.VirtualAddress && pos < sect.VirtualAddress + Math.Max(sect.VirtualSize, sect.SizeOfRawData))
						return (pos - sect.VirtualAddress) + sect.PointerToRawData;
				}
			}

			return pos;
		}

		public override HexPosition FilePositionToBufferPosition(ulong position) {
			var fileSpan = this.fileSpan;
			if (!IsFileLayout) {
				foreach (var sect in SectionHeaders) {
					if (position >= sect.PointerToRawData && position < sect.PointerToRawData + sect.SizeOfRawData)
						return fileSpan.Start + position - sect.PointerToRawData + sect.VirtualAddress;
				}
			}

			if (position >= fileSpan.Length)
				return 0;
			return fileSpan.Start + position;
		}
	}
}
