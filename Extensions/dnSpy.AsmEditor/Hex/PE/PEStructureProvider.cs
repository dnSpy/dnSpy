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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class PEStructureProviderFactory {
		public abstract PEStructureProvider TryGetProvider(HexBuffer buffer, HexPosition pePosition);
	}

	[Export(typeof(PEStructureProviderFactory))]
	sealed class PEStructureProviderFactoryImpl : PEStructureProviderFactory {
		public override PEStructureProvider TryGetProvider(HexBuffer buffer, HexPosition pePosition) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (pePosition >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(pePosition));
			var imageLayout = GetImageLayout(buffer, pePosition);
			//TODO: The current code assumes the PE data is in file layout, not memory layout
			if (imageLayout != ImageLayout.File)
				return null;
			if (!buffer.Span.Contains(pePosition))
				return null;

			IPEImage peImage;
			try {
				var creator = new HexBufferImageStreamCreator(buffer, HexSpan.FromBounds(pePosition, buffer.Span.End));
				peImage = new PEImage(creator, imageLayout, true);
			}
			catch (BadImageFormatException) {
				return null;
			}
			catch (IOException) {
				return null;
			}

			var dict = buffer.Properties.GetOrCreateSingletonProperty(() => new Dict());
			return dict.GetOrCreate(buffer, pePosition, () => new PEStructureProviderImpl(buffer, pePosition, peImage));
		}

		static ImageLayout GetImageLayout(HexBuffer buffer, HexPosition pePosition) {
			//TODO: Checking IsMemory doesn't verify that it's memory layout, it just means that the underlying stream is a process stream
			if (buffer.IsMemory)
				return ImageLayout.Memory;
			return ImageLayout.File;
		}

		sealed class Dict {
			readonly Dictionary<Key, PEStructureProvider> dict;

			struct Key : IEquatable<Key> {
				readonly HexBuffer buffer;
				readonly HexPosition position;
				public Key(HexBuffer buffer, HexPosition position) {
					this.buffer = buffer;
					this.position = position;
				}

				public bool Equals(Key other) => buffer == other.buffer && position == other.position;
				public override bool Equals(object obj) => obj is Key && Equals((Key)obj);
				public override int GetHashCode() => (buffer?.GetHashCode() ?? 0) ^ position.GetHashCode();
			}

			public Dict() {
				dict = new Dictionary<Key, PEStructureProvider>();
			}

			public PEStructureProvider GetOrCreate(HexBuffer buffer, HexPosition position, Func<PEStructureProvider> create) {
				var key = new Key(buffer, position);
				PEStructureProvider provider;
				if (!dict.TryGetValue(key, out provider))
					dict.Add(key, provider = create());
				return provider;
			}
		}
	}

	abstract class PEStructureProvider {
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
		public override HexBuffer Buffer => buffer;
		public override HexSpan PESpan => HexSpan.FromBounds(pePosition, peEndPosition);
		public override ImageDosHeaderVM ImageDosHeader => imageDosHeader;
		public override ImageFileHeaderVM ImageFileHeader => imageFileHeader;
		public override ImageOptionalHeaderVM ImageOptionalHeader => imageOptionalHeader;
		public override ImageSectionHeaderVM[] Sections => sections;
		public override ImageCor20HeaderVM ImageCor20Header => imageCor20Header;
		public override StorageSignatureVM StorageSignature => storageSignature;
		public override StorageHeaderVM StorageHeader => storageHeader;
		public override StorageStreamVM[] StorageStreams => storageStreams;
		public override TablesStreamVM TablesStream => tablesStream;

		readonly HexBuffer buffer;
		readonly HexPosition pePosition;
		readonly HexPosition peEndPosition;
		readonly IPEImage peImage;
		readonly ImageDosHeaderVM imageDosHeader;
		readonly ImageFileHeaderVM imageFileHeader;
		readonly ImageOptionalHeaderVM imageOptionalHeader;
		readonly ImageSectionHeaderVM[] sections;
		readonly ImageCor20HeaderVM imageCor20Header;
		readonly StorageSignatureVM storageSignature;
		readonly StorageHeaderVM storageHeader;
		readonly StorageStreamVM[] storageStreams;
		readonly TablesStreamVM tablesStream;

		public PEStructureProviderImpl(HexBuffer buffer, HexPosition pePosition, IPEImage peImage) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (!buffer.Span.Contains(pePosition))
				throw new ArgumentOutOfRangeException(nameof(pePosition));
			if (peImage == null)
				throw new ArgumentNullException(nameof(peImage));
			this.buffer = buffer;
			this.pePosition = pePosition;
			this.peImage = peImage;

			imageDosHeader = new ImageDosHeaderVM(buffer, HexSpan.FromBounds((ulong)peImage.ImageDosHeader.StartOffset, (ulong)peImage.ImageDosHeader.EndOffset));
			imageFileHeader = new ImageFileHeaderVM(buffer, HexSpan.FromBounds((ulong)peImage.ImageNTHeaders.FileHeader.StartOffset, (ulong)peImage.ImageNTHeaders.FileHeader.EndOffset));
			if (peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32)
				imageOptionalHeader = new ImageOptionalHeader32VM(buffer, HexSpan.FromBounds((ulong)peImage.ImageNTHeaders.OptionalHeader.StartOffset, (ulong)peImage.ImageNTHeaders.OptionalHeader.EndOffset));
			else
				imageOptionalHeader = new ImageOptionalHeader64VM(buffer, HexSpan.FromBounds((ulong)peImage.ImageNTHeaders.OptionalHeader.StartOffset, (ulong)peImage.ImageNTHeaders.OptionalHeader.EndOffset));
			sections = new ImageSectionHeaderVM[peImage.ImageSectionHeaders.Count];
			for (int i = 0; i < sections.Length; i++)
				sections[i] = new ImageSectionHeaderVM(buffer, HexSpan.FromBounds((ulong)peImage.ImageSectionHeaders[i].StartOffset, (ulong)peImage.ImageSectionHeaders[i].EndOffset));
			imageCor20Header = TryCreateCor20(buffer, peImage);
			storageStreams = Array.Empty<StorageStreamVM>();
			if (imageCor20Header != null) {
				var md = TryCreateMetaData(peImage);
				if (md != null) {
					var mdHeader = md.MetaDataHeader;
					storageSignature = new StorageSignatureVM(buffer, (ulong)mdHeader.StartOffset, (int)(mdHeader.StorageHeaderOffset - mdHeader.StartOffset - 0x10));
					storageHeader = new StorageHeaderVM(buffer, (ulong)mdHeader.StorageHeaderOffset);
					var knownStreams = new List<DotNetStream> {
						md.StringsStream,
						md.USStream,
						md.BlobStream,
						md.GuidStream,
						md.TablesStream,
					};
					if (md.IsCompressed) {
						foreach (var stream in md.AllStreams) {
							if (stream.Name == "#!")
								knownStreams.Add(stream);
						}
					}
					storageStreams = new StorageStreamVM[md.MetaDataHeader.StreamHeaders.Count];
					for (int i = 0; i < storageStreams.Length; i++) {
						var sh = md.MetaDataHeader.StreamHeaders[i];
						var knownStream = knownStreams.FirstOrDefault(a => a.StreamHeader == sh);
						storageStreams[i] = new StorageStreamVM(buffer, knownStream, i, (ulong)sh.StartOffset, (int)(sh.EndOffset - sh.StartOffset - 8));
					}

					var metaDataTables = new MetaDataTableVM[0x40];
					tablesStream = new TablesStreamVM(buffer, md.TablesStream, metaDataTables);
					var stringsHeapSpan = HexSpan.FromBounds((ulong)md.StringsStream.StartOffset, (ulong)md.StringsStream.EndOffset);
					var guidHeapSpan = HexSpan.FromBounds((ulong)md.GuidStream.StartOffset, (ulong)md.GuidStream.EndOffset);
					foreach (var mdTable in md.TablesStream.MDTables) {
						if (mdTable.Rows != 0)
							metaDataTables[(int)mdTable.Table] = MetaDataTableVM.Create(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
					}
				}
			}

			using (var stream = peImage.CreateFullStream())
				peEndPosition = pePosition + (ulong)stream.Length;
		}

		static IMetaData TryCreateMetaData(IPEImage peImage) {
			var dnDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
			if (dnDir.VirtualAddress == 0 || dnDir.Size < 0x48)
				return null;
			try {
				return MetaDataCreator.CreateMetaData(peImage, true);
			}
			catch (BadImageFormatException) {
				return null;
			}
			catch (IOException) {
				return null;
			}
		}

		static ImageCor20HeaderVM TryCreateCor20(HexBuffer buffer, IPEImage peImage) {
			var dnDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
			const int COR20_HDR_SIZE = 0x48;
			if (dnDir.VirtualAddress == 0 || dnDir.Size < COR20_HDR_SIZE)
				return null;
			return new ImageCor20HeaderVM(buffer, new HexSpan((ulong)peImage.ToFileOffset(dnDir.VirtualAddress), COR20_HDR_SIZE));
		}

		public override HexPosition RvaToBufferPosition(uint rva) =>
			pePosition + (ulong)peImage.ToFileOffset((RVA)rva);

		public override uint BufferPositionToRva(HexPosition position) {
			if (position < pePosition)
				return 0;
			var offset = position - pePosition;
			if (offset > long.MaxValue)
				return 0;
			return (uint)peImage.ToRVA((FileOffset)offset.ToUInt64());
		}
	}
}
