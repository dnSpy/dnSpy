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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetMultiFileResourceHeaderDataImpl : DotNetMultiFileResourceHeaderData {
		public override StructField<UInt32Data> MagicNum { get; }
		public override StructField<UInt32Data> ResMgrHeaderVersion { get; }
		public override StructField<UInt32Data> HeaderSize { get; }
		public override StructField<VirtualArrayData<ByteData>> UnknownHeader { get; }
		public override StructField<Bit7EncodedStringData> ReaderType { get; }
		public override StructField<Bit7EncodedStringData> ResourceSetType { get; }
		public override StructField<UInt32Data> Version { get; }
		public override StructField<UInt32Data> NumResources { get; }
		public override StructField<UInt32Data> NumTypes { get; }
		public override StructField<VariableLengthArrayData<Bit7EncodedStringData>> TypeNames { get; }
		public override StructField<ArrayData<ByteData>> Alignment8 { get; }
		public override StructField<VirtualArrayData<UInt32Data>> NameHashes { get; }
		public override StructField<VirtualArrayData<UInt32Data>> NamePositions { get; }
		public override StructField<UInt32Data> DataSectionOffset { get; }

		protected override BufferField[] Fields { get; }

		public DotNetMultiFileResourceHeaderDataImpl(HexBufferSpan span, Bit7String? resourceTypeSpan, Bit7String? resourceSetTypeSpan, HexPosition versionPosition, HexSpan paddingSpan, Bit7String[] typeNames, int numResources)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;

			MagicNum = new StructField<UInt32Data>("MagicNum", new UInt32Data(buffer, pos));
			ResMgrHeaderVersion = new StructField<UInt32Data>("ResMgrHeaderVersion", new UInt32Data(buffer, pos + 4));
			HeaderSize = new StructField<UInt32Data>("HeaderSize", new UInt32Data(buffer, pos + 8));

			if (resourceTypeSpan == null) {
				if (resourceSetTypeSpan != null)
					throw new ArgumentException();
				UnknownHeader = new StructField<VirtualArrayData<ByteData>>("Header", ArrayData.CreateVirtualByteArray(new HexBufferSpan(buffer, HexSpan.FromBounds(pos + 0x0C, versionPosition))));
			}
			else {
				if (resourceSetTypeSpan == null)
					throw new ArgumentNullException(nameof(resourceSetTypeSpan));
				ReaderType = new StructField<Bit7EncodedStringData>("ReaderType", new Bit7EncodedStringData(buffer, resourceTypeSpan.Value.LengthSpan, resourceTypeSpan.Value.StringSpan, Encoding.UTF8));
				ResourceSetType = new StructField<Bit7EncodedStringData>("ResourceSetType", new Bit7EncodedStringData(buffer, resourceSetTypeSpan.Value.LengthSpan, resourceSetTypeSpan.Value.StringSpan, Encoding.UTF8));
			}

			pos = versionPosition;
			Version = new StructField<UInt32Data>("Version", new UInt32Data(buffer, pos));
			NumResources = new StructField<UInt32Data>("NumResources", new UInt32Data(buffer, pos + 4));
			NumTypes = new StructField<UInt32Data>("NumTypes", new UInt32Data(buffer, pos + 8));
			pos += 0x0C;

			var fields = new ArrayField<Bit7EncodedStringData>[typeNames.Length];
			var currPos = pos;
			for (int i = 0; i < fields.Length; i++) {
				var info = typeNames[i];
				var field = new ArrayField<Bit7EncodedStringData>(new Bit7EncodedStringData(buffer, info.LengthSpan, info.StringSpan, Encoding.UTF8), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			TypeNames = new StructField<VariableLengthArrayData<Bit7EncodedStringData>>("TypeNames", new VariableLengthArrayData<Bit7EncodedStringData>(string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(pos, currPos)), fields));

			Alignment8 = new StructField<ArrayData<ByteData>>("Padding", ArrayData.CreateByteArray(buffer, paddingSpan.Start, (int)paddingSpan.Length.ToUInt64()));
			pos = paddingSpan.End;

			NameHashes = new StructField<VirtualArrayData<UInt32Data>>("NameHashes", ArrayData.CreateVirtualUInt32Array(new HexBufferSpan(buffer, new HexSpan(pos, (ulong)numResources * 4))));
			pos += (ulong)numResources * 4;
			NamePositions = new StructField<VirtualArrayData<UInt32Data>>("NamePositions", ArrayData.CreateVirtualUInt32Array(new HexBufferSpan(buffer, new HexSpan(pos, (ulong)numResources * 4))));
			pos += (ulong)numResources * 4;
			DataSectionOffset = new StructField<UInt32Data>("DataSectionOffset", new UInt32Data(buffer, pos));
			pos += 4;
			if (pos != span.Span.End)
				throw new ArgumentOutOfRangeException(nameof(span));

			var list = new List<BufferField>(13);
			list.Add(MagicNum);
			list.Add(ResMgrHeaderVersion);
			list.Add(HeaderSize);
			if (UnknownHeader != null)
				list.Add(UnknownHeader);
			if (ReaderType != null)
				list.Add(ReaderType);
			if (ResourceSetType != null)
				list.Add(ResourceSetType);
			list.Add(Version);
			list.Add(NumResources);
			list.Add(NumTypes);
			list.Add(TypeNames);
			list.Add(Alignment8);
			list.Add(NameHashes);
			list.Add(NamePositions);
			list.Add(DataSectionOffset);
			Fields = list.ToArray();
		}
	}
}
