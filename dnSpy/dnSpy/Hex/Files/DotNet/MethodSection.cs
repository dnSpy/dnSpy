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

using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class SmallSectionImpl : SmallSection {
		public override StructField<ByteFlagsData> Kind { get; }
		public override StructField<ByteData> DataSize { get; }

		protected override BufferField[] Fields { get; }

		internal static readonly FlagInfo[] methodSectionKindFlagInfos = new FlagInfo[] {
			FlagInfo.CreateEnumName(0x3F, "Kind"),
			new FlagInfo(0x3F, 0x00, "Reserved"),
			new FlagInfo(0x3F, 0x01, "EHTable"),
			new FlagInfo(0x3F, 0x02, "OptILTable"),
			new FlagInfo(0x40, "FatFormat"),
			new FlagInfo(0x80, "MoreSects"),
		};

		public SmallSectionImpl(HexBuffer buffer, HexPosition pos)
			: base(new HexBufferSpan(buffer, new HexSpan(pos, 2))) {
			Kind = new StructField<ByteFlagsData>("Kind", new ByteFlagsData(buffer, pos, methodSectionKindFlagInfos));
			DataSize = new StructField<ByteData>("DataSize", new ByteData(buffer, pos + 1));
			Fields = new BufferField[] {
				Kind,
				DataSize,
			};
		}
	}

	sealed class FatSectionImpl : FatSection {
		public override StructField<ByteFlagsData> Kind { get; }
		public override StructField<UInt24Data> DataSize { get; }

		protected override BufferField[] Fields { get; }

		public FatSectionImpl(HexBuffer buffer, HexPosition pos)
			: base(new HexBufferSpan(buffer, new HexSpan(pos, 4))) {
			Kind = new StructField<ByteFlagsData>("Kind", new ByteFlagsData(buffer, pos, SmallSectionImpl.methodSectionKindFlagInfos));
			DataSize = new StructField<UInt24Data>("DataSize", new UInt24Data(buffer, pos + 1));
			Fields = new BufferField[] {
				Kind,
				DataSize,
			};
		}
	}
}
