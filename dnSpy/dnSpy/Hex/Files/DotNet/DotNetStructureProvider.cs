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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.DotNet {
	[Export(typeof(StructureProviderFactory))]
	[VSUTIL.Name(PredefinedStructureProviderFactoryNames.DotNet)]
	[VSUTIL.Order(After = PredefinedStructureProviderFactoryNames.PE)]
	sealed class DotNetStructureProviderFactory : StructureProviderFactory {
		public override StructureProvider? Create(HexBufferFile file) => new DotNetStructureProvider(file);
	}

	sealed class DotNetStructureProvider : StructureProvider {
		readonly HexBufferFile file;
		DotNetCor20Data? cor20;
		DotNetMetadataHeaderData? mdHeader;
		HexSpan metadataSpan;
		DotNetHeap[]? dotNetHeaps;
		DotNetMetadataHeaders? dotNetMetadataHeaders;
		DotNetHeaders? dotNetHeaders;
		VirtualArrayData<ByteData>? strongNameSignature;
		DotNetMethodProvider? dotNetMethodProvider;
		DotNetResourceProvider? dotNetResourceProvider;

		public DotNetStructureProvider(HexBufferFile file) => this.file = file ?? throw new ArgumentNullException(nameof(file));

		public override bool Initialize() {
			HexSpan? resourcesSpan = null;
			var peHeaders = file.GetHeaders<PeHeaders>();
			if (peHeaders is not null) {
				if (peHeaders.OptionalHeader.DataDirectory.Data.FieldCount < 15)
					return false;
				// Mono ignores the size field
				var cor20Span = Read(peHeaders, peHeaders.OptionalHeader.DataDirectory.Data[14].Data, allowZeroSize: true);
				if (cor20Span is null)
					return false;
				cor20 = DotNetCor20DataImpl.TryCreate(file, cor20Span.Value.Start);
				if (cor20 is null)
					return false;

				// Mono ignores the size field
				var mdSpan = Read(peHeaders, cor20.Metadata.Data, allowZeroSize: true);
				resourcesSpan = Read(peHeaders, cor20.Resources.Data, allowZeroSize: false);
				var snSpan = Read(peHeaders, cor20.StrongNameSignature.Data, allowZeroSize: false);

				ReadDotNetMetadataHeader(peHeaders, mdSpan);
				ReadStrongNameSignature(peHeaders, snSpan);
			}
			else {
				// Could be a portable PDB file (or a metadata only file)
				ReadDotNetMetadataHeader(file.Span);
			}

			if (mdHeader is not null && dotNetHeaps is not null)
				dotNetMetadataHeaders = new DotNetMetadataHeadersImpl(metadataSpan, mdHeader, dotNetHeaps);
			if (peHeaders is not null && cor20 is not null) {
				dotNetMethodProvider = new DotNetMethodProviderImpl(file, peHeaders, dotNetMetadataHeaders?.TablesStream);
				dotNetResourceProvider = new DotNetResourceProviderImpl(file, peHeaders, dotNetMetadataHeaders, resourcesSpan);
				dotNetHeaders = new DotNetHeadersImpl(peHeaders, cor20, dotNetMetadataHeaders, strongNameSignature, dotNetMethodProvider, dotNetResourceProvider);
			}
			return cor20 is not null || !metadataSpan.IsEmpty;
		}

		HexSpan? Read(PeHeaders peHeaders, DataDirectoryData dir, bool allowZeroSize) {
			uint rva = dir.VirtualAddress.Data.ReadValue();
			uint size = dir.Size.Data.ReadValue();
			if (rva == 0 || (!allowZeroSize && size == 0))
				return null;
			var position = peHeaders.RvaToBufferPosition(rva);
			var end = position + size;
			if (end > HexPosition.MaxEndPosition)
				return null;
			var span = HexSpan.FromBounds(position, end);
			if (!file.Span.Contains(span))
				return null;
			return span;
		}

		void ReadDotNetMetadataHeader(PeHeaders peHeaders, HexSpan? dir) {
			if (dir is null)
				return;
			// Mono ignores the size field so pass in the full size
			ReadDotNetMetadataHeader(HexSpan.FromBounds(dir.Value.Start, file.Span.End));
		}

		void ReadDotNetMetadataHeader(HexSpan span) {
			var mdReader = DotNetMetadataHeaderReader.TryCreate(file, span);
			if (mdReader is null)
				return;
			Debug2.Assert(mdReader.StorageStreamHeaders is not null);
			mdHeader = DotNetMetadataHeaderDataImpl.TryCreate(file, mdReader.MetadataHeaderSpan, (int)mdReader.VersionStringSpan.Length.ToUInt64(), mdReader.StorageStreamHeaders);
			if (mdHeader is null)
				return;
			metadataSpan = mdReader.MetadataSpan;
			var dnReader = new DotNetHeapsReader(file, mdHeader, mdReader.StorageStreamHeaders);
			if (dnReader.Read())
				dotNetHeaps = dnReader.Streams;
		}

		void ReadStrongNameSignature(PeHeaders peHeaders, HexSpan? span) {
			if (span is null)
				return;
			strongNameSignature = ArrayData.CreateVirtualByteArray(new HexBufferSpan(file.Buffer, span.Value), name: "STRONGNAMESIGNATURE");
		}

		public override ComplexData? GetStructure(HexPosition position) {
			var cor20 = this.cor20;
			if (cor20 is not null) {
				if (cor20.Span.Span.Contains(position))
					return cor20;
				if (strongNameSignature?.Span.Span.Contains(position) == true)
					return strongNameSignature;
				var body = dotNetMethodProvider?.GetMethodBody(position);
				if (body is not null)
					return body;
				var resource = dotNetResourceProvider?.GetResource(position);
				if (resource is not null)
					return resource;
			}

			if (metadataSpan.Contains(position)) {
				if (mdHeader?.Span.Span.Contains(position) == true)
					return mdHeader;
				return dotNetMetadataHeaders?.GetStructure(position);
			}

			return null;
		}

		public override ComplexData? GetStructure(string id) {
			switch (id) {
			case PredefinedDotNetDataIds.Cor20:
				return cor20;

			case PredefinedDotNetDataIds.MetadataHeader:
				return mdHeader;

			case PredefinedDotNetDataIds.StrongNameSignature:
				return strongNameSignature;
			}
			return null;
		}

		public override THeader? GetHeaders<THeader>() where THeader : class =>
			dotNetMetadataHeaders as THeader ??
			dotNetHeaders as THeader ??
			dotNetMethodProvider as THeader ??
			dotNetResourceProvider as THeader;
	}
}
