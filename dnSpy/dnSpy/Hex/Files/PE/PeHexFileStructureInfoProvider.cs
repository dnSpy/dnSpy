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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.PE {
	[Export(typeof(HexFileStructureInfoProviderFactory))]
	[VSUTIL.Name(PredefinedHexFileStructureInfoProviderFactoryNames.PE)]
	[VSUTIL.Order(Before = PredefinedHexFileStructureInfoProviderFactoryNames.Default)]
	sealed class PeHexFileStructureInfoProviderFactory : HexFileStructureInfoProviderFactory {
		public override HexFileStructureInfoProvider? Create(HexView hexView) =>
			new PeHexFileStructureInfoProvider();
	}

	sealed class PeHexFileStructureInfoProvider : HexFileStructureInfoProvider {
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file, ComplexData structure, HexPosition position) {
			if (structure is PeOptionalHeaderData optHdr)
				return GetFieldReferenceSpan(file, optHdr, position);

			if (structure is PeSectionsData sections)
				return GetFieldReferenceSpan(file, sections, position);

			return null;
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, PeOptionalHeaderData optHdr, HexPosition position) {
			if (optHdr.DataDirectory.Data.Span.Contains(position)) {
				var data = (DataDirectoryData)optHdr.DataDirectory.Data.GetFieldByPosition(position)!.Data;
				return DataDirectoryDataUtils.TryGetSpan(file, data, position);
			}

			return null;
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, PeSectionsData sections, HexPosition position) {
			var data = (PeSectionData?)sections.GetFieldByPosition(position)?.Data;
			if (data is null)
				return null;

			HexSpan? span;
			if (!((span = TryGetRvaSpan(file, position, data.VirtualAddress.Data, data.VirtualSize.Data)) is null))
				return span;
			if (!((span = TryGetFileSpan(file, position, data.PointerToRawData.Data, data.SizeOfRawData.Data)) is null))
				return span;

			return null;
		}

		static HexSpan? TryGetRvaSpan(HexBufferFile file, HexPosition position, RvaData rvaData, UInt32Data sizeData) {
			if (!rvaData.Span.Span.Contains(position))
				return null;
			var peHeaders = file.GetHeaders<PeHeaders>();
			if (peHeaders is null)
				return null;
			uint rva = rvaData.ReadValue();
			if (rva == 0)
				return null;
			uint size = sizeData.ReadValue();
			if (size == 0)
				return null;
			var pos = peHeaders.RvaToBufferPosition(rva);
			if (pos + size > file.Span.End)
				return new HexSpan(pos, 0);
			return new HexSpan(pos, size);
		}

		static HexSpan? TryGetFileSpan(HexBufferFile file, HexPosition position, FileOffsetData offsetData, UInt32Data sizeData) {
			if (!offsetData.Span.Span.Contains(position))
				return null;
			var peHeaders = file.GetHeaders<PeHeaders>();
			if (peHeaders is null)
				return null;
			uint offset = offsetData.ReadValue();
			if (offset == 0)
				return null;
			uint size = sizeData.ReadValue();
			if (size == 0)
				return null;
			var pos = peHeaders.FilePositionToBufferPosition(offset);
			if (pos + size > file.Span.End)
				return new HexSpan(pos, 0);
			return new HexSpan(pos, size);
		}
	}
}
