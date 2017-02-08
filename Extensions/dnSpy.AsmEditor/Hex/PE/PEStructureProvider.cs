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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class PEStructureProviderFactory {
		public abstract PEStructureProvider TryGetProvider(HexBufferFile file);
	}

	[Export(typeof(PEStructureProviderFactory))]
	sealed class PEStructureProviderFactoryImpl : PEStructureProviderFactory {
		public override PEStructureProvider TryGetProvider(HexBufferFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			var peHeaders = file.GetHeaders<PeHeaders>();
			if (peHeaders == null)
				return null;
			return file.Properties.GetOrCreateSingletonProperty(typeof(PEStructureProviderImpl), () => new PEStructureProviderImpl(file, peHeaders));
		}
	}

	abstract class PEStructureProvider {
		public abstract HexBufferFile BufferFile { get; }
		public abstract HexBuffer Buffer { get; }
		public abstract HexSpan PESpan { get; }
		public abstract ImageDosHeaderVM ImageDosHeader { get; }
		public abstract ImageFileHeaderVM ImageFileHeader { get; }
		public abstract ImageOptionalHeaderVM ImageOptionalHeader { get; }
		public abstract ImageSectionHeaderVM[] Sections { get; }
		/// <summary>Can be null if it's not a .NET file</summary>
		public abstract ImageCor20HeaderVM ImageCor20Header { get; }
		/// <summary>Can be null if it's not a .NET file</summary>
		public abstract StorageSignatureVM StorageSignature { get; }
		/// <summary>Can be null if it's not a .NET file</summary>
		public abstract StorageHeaderVM StorageHeader { get; }
		public abstract StorageStreamVM[] StorageStreams { get; }
		/// <summary>Can be null if it's not a .NET file</summary>
		public abstract TablesStreamVM TablesStream { get; }
		public abstract HexPosition RvaToBufferPosition(uint rva);
		public abstract uint BufferPositionToRva(HexPosition position);
	}

	sealed class PEStructureProviderImpl : PEStructureProvider {
		public override HexBufferFile BufferFile => file;
		public override HexBuffer Buffer => file.Buffer;
		public override HexSpan PESpan => file.Span;
		public override ImageDosHeaderVM ImageDosHeader => imageDosHeader;
		public override ImageFileHeaderVM ImageFileHeader => imageFileHeader;
		public override ImageOptionalHeaderVM ImageOptionalHeader => imageOptionalHeader;
		public override ImageSectionHeaderVM[] Sections => sections;
		public override ImageCor20HeaderVM ImageCor20Header => imageCor20Header;
		public override StorageSignatureVM StorageSignature => storageSignature;
		public override StorageHeaderVM StorageHeader => storageHeader;
		public override StorageStreamVM[] StorageStreams => storageStreams;
		public override TablesStreamVM TablesStream => tablesStream;

		readonly HexBufferFile file;
		readonly PeHeaders peHeaders;
		readonly ImageDosHeaderVM imageDosHeader;
		readonly ImageFileHeaderVM imageFileHeader;
		readonly ImageOptionalHeaderVM imageOptionalHeader;
		readonly ImageSectionHeaderVM[] sections;
		readonly ImageCor20HeaderVM imageCor20Header;
		readonly StorageSignatureVM storageSignature;
		readonly StorageHeaderVM storageHeader;
		readonly StorageStreamVM[] storageStreams;
		readonly TablesStreamVM tablesStream;

		public PEStructureProviderImpl(HexBufferFile file, PeHeaders peHeaders) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (peHeaders == null)
				throw new ArgumentNullException(nameof(peHeaders));
			if (peHeaders != file.GetHeaders<PeHeaders>())
				throw new ArgumentException();
			this.file = file;
			this.peHeaders = peHeaders;
			var buffer = file.Buffer;

			imageDosHeader = new ImageDosHeaderVM(buffer, peHeaders.DosHeader);
			imageFileHeader = new ImageFileHeaderVM(buffer, peHeaders.FileHeader);
			if (peHeaders.OptionalHeader.Is32Bit)
				imageOptionalHeader = new ImageOptionalHeader32VM(buffer, (PeOptionalHeader32Data)peHeaders.OptionalHeader);
			else
				imageOptionalHeader = new ImageOptionalHeader64VM(buffer, (PeOptionalHeader64Data)peHeaders.OptionalHeader);
			sections = new ImageSectionHeaderVM[peHeaders.Sections.FieldCount];
			for (int i = 0; i < sections.Length; i++)
				sections[i] = new ImageSectionHeaderVM(buffer, peHeaders.Sections[i].Data);
			var dnHeaders = file.GetHeaders<DotNetHeaders>();
			storageStreams = Array.Empty<StorageStreamVM>();
			if (dnHeaders != null) {
				imageCor20Header = new ImageCor20HeaderVM(buffer, dnHeaders.Cor20);
				var mdHeaders = dnHeaders.MetadataHeaders;
				if (mdHeaders != null) {
					storageSignature = new StorageSignatureVM(buffer, mdHeaders.MetadataHeader);
					storageHeader = new StorageHeaderVM(buffer, mdHeaders.MetadataHeader);
					storageStreams = new StorageStreamVM[mdHeaders.Streams.Count];
					for (int i = 0; i < storageStreams.Length; i++) {
						var ssh = mdHeaders.MetadataHeader.StreamHeaders.Data[i].Data;
						var heap = mdHeaders.Streams[i];
						storageStreams[i] = new StorageStreamVM(buffer, heap, ssh, i);
					}

					var metaDataTables = new MetaDataTableVM[0x40];
					if (mdHeaders.TablesStream != null) {
						tablesStream = new TablesStreamVM(buffer, mdHeaders.TablesStream, metaDataTables);
						var stringsHeapSpan = GetSpan(mdHeaders.StringsStream);
						var guidHeapSpan = GetSpan(mdHeaders.GUIDStream);
						foreach (var mdTable in mdHeaders.TablesStream.MDTables) {
							if (mdTable.Rows != 0)
								metaDataTables[(int)mdTable.Table] = MetaDataTableVM.Create(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
						}
					}
				}
			}
		}

		static HexSpan GetSpan(DotNetHeap heap) => heap?.Span.Span ?? default(HexSpan);

		public override HexPosition RvaToBufferPosition(uint rva) =>
			peHeaders.RvaToBufferPosition(rva);
		public override uint BufferPositionToRva(HexPosition position) =>
			peHeaders.BufferPositionToRva(position);
	}
}
