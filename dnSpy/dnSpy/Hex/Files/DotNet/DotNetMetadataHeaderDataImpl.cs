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
using System.Collections.ObjectModel;
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetMetadataHeaderDataImpl : DotNetMetadataHeaderData {
		public override StructField<UInt32Data> Signature { get; }
		public override StructField<UInt16Data> MajorVersion { get; }
		public override StructField<UInt16Data> MinorVersion { get; }
		public override StructField<UInt32Data> ExtraData { get; }
		public override StructField<UInt32Data> VersionStringCount { get; }
		public override StructField<StringData> VersionString { get; }
		public override StructField<ByteFlagsData> Flags { get; }
		public override StructField<ByteData> Pad { get; }
		public override StructField<UInt16Data> StreamCount { get; }
		public override StructField<VariableLengthArrayData<DotNetStorageStream>> StreamHeaders { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> flagsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x01, "ExtraData"),
		});

		DotNetMetadataHeaderDataImpl(HexBufferSpan span, int stringLength, StorageStreamHeader[] storageStreamHeaders)
			: base(span) {

			var buffer = span.Buffer;
			var pos    = span.Start.Position;

			Signature          = new StructField<UInt32Data>(   "Signature"   , new UInt32Data(   buffer, pos        )); // Always    0x424A5342 "BSJB"
			MajorVersion       = new StructField<UInt16Data>(   "MajorVer"    , new UInt16Data(   buffer, pos +    4 )); // currently 1
			MinorVersion       = new StructField<UInt16Data>(   "MinorVer"    , new UInt16Data(   buffer, pos +    6 )); // currently 1
			ExtraData          = new StructField<UInt32Data>(   "Reserved"    , new UInt32Data(   buffer, pos +    8 ));
			VersionStringCount = new StructField<UInt32Data>(   "CLRVerStrLen", new UInt32Data(   buffer, pos + 0x0C )); // The CLRVerStrLen is rounded up to a multiple of 4.
			VersionString      = new StructField<StringData>(   "CLRVerStr"   , new StringData(   buffer, pos + 0x10, stringLength, Encoding.UTF8));
			pos                = pos + 0x10 + stringLength;

			Flags              = new StructField<ByteFlagsData>("Flags"       , new ByteFlagsData(buffer, pos, flagsFlagInfos));
			Pad                = new StructField<ByteData>  (   "Flags_pad"   , new ByteData  (   buffer, pos + 1));
			StreamCount        = new StructField<UInt16Data>(   "Streams"     , new UInt16Data(   buffer, pos + 2)); // currently 5
			pos += 4;

         // Read the 5 DotNetStorageStream; #~,  #Strings,  #US,  #Blob and  #GUID   into StreamHeaders[]
			var fields = new ArrayField<DotNetStorageStream>[storageStreamHeaders.Length];
			for (int i = 0; i < storageStreamHeaders.Length; i++) {
				var field = new ArrayField<DotNetStorageStream>(new DotNetStorageStreamImpl(new HexBufferSpan(buffer, storageStreamHeaders[i].Span)), (uint)i);
				fields[i] = field;
			}
			var arraySpan = storageStreamHeaders.Length == 0 ? new HexSpan(pos, 0) : HexSpan.FromBounds(storageStreamHeaders[0].Span.Start, storageStreamHeaders[storageStreamHeaders.Length - 1].Span.End);
			StreamHeaders = new StructField<VariableLengthArrayData<DotNetStorageStream>>("Pools", new VariableLengthArrayData<DotNetStorageStream>(string.Empty, new HexBufferSpan(buffer, arraySpan), fields));

			Fields = new BufferField[] {
				Signature,
				MajorVersion,
				MinorVersion,
				ExtraData,
				VersionStringCount,
				VersionString,
				Flags,
				Pad,
				StreamCount,
				StreamHeaders,
			};
		}

		public static DotNetMetadataHeaderData? TryCreate(HexBufferFile? file, HexSpan span, int stringLength, StorageStreamHeader[] storageStreamHeaders) {
			if (file is null)
				throw new ArgumentNullException(nameof(file));
			if (!file.Span.Contains(span))
				return null;
			return new DotNetMetadataHeaderDataImpl(new HexBufferSpan(file.Buffer, span), stringLength, storageStreamHeaders);
		}
	}
}
