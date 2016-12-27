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
	sealed class SmallExceptionClauseImpl : SmallExceptionClause {
		public override StructField<UInt16FlagsData> Flags { get; }
		public override StructField<UInt16Data> TryOffset { get; }
		public override StructField<ByteData> TryLength { get; }
		public override StructField<UInt16Data> HandlerOffset { get; }
		public override StructField<ByteData> HandlerLength { get; }
		public override StructField<UInt32Data> ClassTokenOrFilterOffset { get; }

		protected override BufferField[] Fields { get; }

		internal static readonly FlagInfo[] exceptionClauseFlagsFlagInfo = new FlagInfo[] {
			new FlagInfo(uint.MaxValue, 0, "EXCEPTION"),
			new FlagInfo(uint.MaxValue, 1, "FILTER"),
			new FlagInfo(uint.MaxValue, 2, "FINALLY"),
			new FlagInfo(uint.MaxValue, 4, "FAULT"),
			new FlagInfo(uint.MaxValue, 8, "DUPLICATED"),
		};

		public SmallExceptionClauseImpl(HexBuffer buffer, HexPosition pos)
			: base(new HexBufferSpan(buffer, pos, 12)) {
			Flags = new StructField<UInt16FlagsData>("Flags", new UInt16FlagsData(buffer, pos, exceptionClauseFlagsFlagInfo));
			TryOffset = new StructField<UInt16Data>("TryOffset", new UInt16Data(buffer, pos + 2));
			TryLength = new StructField<ByteData>("TryLength", new ByteData(buffer, pos + 4));
			HandlerOffset = new StructField<UInt16Data>("HandlerOffset", new UInt16Data(buffer, pos + 5));
			HandlerLength = new StructField<ByteData>("HandlerLength", new ByteData(buffer, pos + 7));
			ClassTokenOrFilterOffset = CreateClassTokenOrFilterOffsetField(buffer, pos + 8, Flags.Data.ReadValue());
			Fields = new BufferField[] {
				Flags,
				TryOffset,
				TryLength,
				HandlerOffset,
				HandlerLength,
				ClassTokenOrFilterOffset,
			};
		}

		internal static StructField<UInt32Data> CreateClassTokenOrFilterOffsetField(HexBuffer buffer, HexPosition position, uint value) {
			switch (value) {
			case 0:
				return new StructField<UInt32Data>("ClassToken", new TokenData(buffer, position));
			case 1:
				return new StructField<UInt32Data>("FilterOffset", new UInt32Data(buffer, position));
			case 2:
			case 4:
			case 8:
			default:
				return new StructField<UInt32Data>("Reserved", new UInt32Data(buffer, position));
			}
		}
	}

	sealed class FatExceptionClauseImpl : FatExceptionClause {
		public override StructField<UInt32FlagsData> Flags { get; }
		public override StructField<UInt32Data> TryOffset { get; }
		public override StructField<UInt32Data> TryLength { get; }
		public override StructField<UInt32Data> HandlerOffset { get; }
		public override StructField<UInt32Data> HandlerLength { get; }
		public override StructField<UInt32Data> ClassTokenOrFilterOffset { get; }

		protected override BufferField[] Fields { get; }

		public FatExceptionClauseImpl(HexBuffer buffer, HexPosition pos)
			: base(new HexBufferSpan(buffer, pos, 24)) {
			Flags = new StructField<UInt32FlagsData>("Flags", new UInt32FlagsData(buffer, pos, SmallExceptionClauseImpl.exceptionClauseFlagsFlagInfo));
			TryOffset = new StructField<UInt32Data>("TryOffset", new UInt32Data(buffer, pos + 4));
			TryLength = new StructField<UInt32Data>("TryLength", new UInt32Data(buffer, pos + 8));
			HandlerOffset = new StructField<UInt32Data>("HandlerOffset", new UInt32Data(buffer, pos + 0x0C));
			HandlerLength = new StructField<UInt32Data>("HandlerLength", new UInt32Data(buffer, pos + 0x10));
			ClassTokenOrFilterOffset = SmallExceptionClauseImpl.CreateClassTokenOrFilterOffsetField(buffer, pos + 0x14, Flags.Data.ReadValue());
			Fields = new BufferField[] {
				Flags,
				TryOffset,
				TryLength,
				HandlerOffset,
				HandlerLength,
				ClassTokenOrFilterOffset,
			};
		}
	}
}
