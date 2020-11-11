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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.PE {
	[Export(typeof(StructureProviderFactory))]
	[VSUTIL.Name(PredefinedStructureProviderFactoryNames.PE)]
	sealed class PeStructureProviderFactory : StructureProviderFactory {
		readonly Lazy<PeFileLayoutProvider, VSUTIL.IOrderable>[] peFileLayoutProviders;

		[ImportingConstructor]
		PeStructureProviderFactory([ImportMany] IEnumerable<Lazy<PeFileLayoutProvider, VSUTIL.IOrderable>> peFileLayoutProviders) => this.peFileLayoutProviders = VSUTIL.Orderer.Order(peFileLayoutProviders).ToArray();

		public override StructureProvider? Create(HexBufferFile file) => new PeStructureProvider(file, peFileLayoutProviders);
	}

	sealed class PeStructureProvider : StructureProvider {
		readonly HexBufferFile file;
		readonly Lazy<PeFileLayoutProvider, VSUTIL.IOrderable>[] peFileLayoutProviders;
		PeHeadersImpl? peHeadersImpl;
		HexSpan peHeadersSpan;

		public PeStructureProvider(HexBufferFile file, Lazy<PeFileLayoutProvider, VSUTIL.IOrderable>[] peFileLayoutProviders) {
			this.file = file ?? throw new ArgumentNullException(nameof(file));
			this.peFileLayoutProviders = peFileLayoutProviders ?? throw new ArgumentNullException(nameof(peFileLayoutProviders));
		}

		public override bool Initialize() {
			var reader = new PeHeadersReader(file, peFileLayoutProviders);
			if (reader.Read()) {
				peHeadersImpl = new PeHeadersImpl(reader, file.Span);
				peHeadersSpan = GetSpan(peHeadersImpl.DosHeader.Span.Span, peHeadersImpl.FileHeader.Span.Span, peHeadersImpl.OptionalHeader.Span.Span, peHeadersImpl.Sections.Span.Span);
			}
			return peHeadersImpl is not null;
		}

		HexSpan GetSpan(params HexSpan[] spans) {
			HexPosition start = HexPosition.MaxEndPosition;
			HexPosition end = HexPosition.Zero;
			foreach (var span in spans) {
				if (span.Start < start)
					start = span.Start;
				if (span.End > end)
					end = span.End;
			}
			if (start < end)
				return HexSpan.FromBounds(start, end);
			return default;
		}

		public override ComplexData? GetStructure(HexPosition position) {
			if (!peHeadersSpan.Contains(position))
				return null;

			var peHeaders = peHeadersImpl;
			if (peHeaders is null)
				return null;

			if (peHeaders.DosHeader.Span.Span.Contains(position))
				return peHeaders.DosHeader;
			if (peHeaders.FileHeader.Span.Span.Contains(position))
				return peHeaders.FileHeader;
			if (peHeaders.OptionalHeader.Span.Span.Contains(position))
				return peHeaders.OptionalHeader;
			if (peHeaders.Sections.Span.Span.Contains(position))
				return peHeaders.Sections;

			return null;
		}

		public override ComplexData? GetStructure(string id) {
			var peHeaders = peHeadersImpl;
			if (peHeaders is null)
				return null;

			switch (id) {
			case PredefinedPeDataIds.PeDosHeader:
				return peHeaders.DosHeader;

			case PredefinedPeDataIds.PeFileHeader:
				return peHeaders.FileHeader;

			case PredefinedPeDataIds.PeOptionalHeader:
				return peHeaders.OptionalHeader;

			case PredefinedPeDataIds.PeSections:
				return peHeaders.Sections;
			}

			return null;
		}

		public override THeader? GetHeaders<THeader>() where THeader : class =>
			peHeadersImpl as THeader;
	}
}
