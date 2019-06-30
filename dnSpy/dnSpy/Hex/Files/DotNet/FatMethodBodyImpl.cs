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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class FatMethodBodyImpl : FatMethodBody {
		public override StructField<UInt16FlagsData> Flags_Size { get; }
		public override StructField<UInt16Data> MaxStack { get; }
		public override StructField<UInt32Data> CodeSize { get; }
		public override StructField<TokenData> LocalVarSigTok { get; }
		public override StructField<VirtualArrayData<ByteData>> Instructions { get; }
		public override StructField<VirtualArrayData<ByteData>>? Padding { get; }
		public override StructField<ExceptionHandlerTable>? EHTable { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> fatHeaderFlagsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x07, "Format"),
			new FlagInfo(0x07, 0x00, "SmallFormat"),
			new FlagInfo(0x07, 0x02, "TinyFormat"),
			new FlagInfo(0x07, 0x03, "FatFormat"),
			new FlagInfo(0x07, 0x06, "TinyFormat1"),
			new FlagInfo(0x08, "MoreSects"),
			new FlagInfo(0x10, "InitLocals"),
			new FlagInfo(0x40, "CompressedIL"),
			FlagInfo.CreateEnumName(0xF000, "Size"),
			new FlagInfo(0xF000, 0x3000, "Size0C"),
		});

		public FatMethodBodyImpl(DotNetMethodProvider methodProvider, HexBufferSpan span, ReadOnlyCollection<uint> tokens, HexSpan instructionsSpan, HexSpan ehSpan, bool fatEH)
			: base(methodProvider, span, tokens) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Flags_Size = new StructField<UInt16FlagsData>("Flags", new UInt16FlagsData(buffer, pos, fatHeaderFlagsFlagInfos));
			MaxStack = new StructField<UInt16Data>("MaxStack", new UInt16Data(buffer, pos + 2));
			CodeSize = new StructField<UInt32Data>("CodeSize", new UInt32Data(buffer, pos + 4));
			LocalVarSigTok = new StructField<TokenData>("LocalVarSigTok", new TokenData(buffer, pos + 8));
			Instructions = new StructField<VirtualArrayData<ByteData>>(TinyMethodBodyImpl.InstructionsFieldName, ArrayData.CreateVirtualByteArray(new HexBufferSpan(span.Buffer, instructionsSpan), TinyMethodBodyImpl.InstructionsFieldName));
			if (!ehSpan.IsEmpty) {
				ExceptionHandlerTable ehTable;
				if (fatEH)
					ehTable = new FatExceptionHandlerTableImpl(new HexBufferSpan(buffer, ehSpan));
				else
					ehTable = new SmallExceptionHandlerTableImpl(new HexBufferSpan(buffer, ehSpan));
				EHTable = new StructField<ExceptionHandlerTable>("EHTable", ehTable);

				var paddingSpan = HexBufferSpan.FromBounds(Instructions.Data.Span.End, EHTable.Data.Span.Start);
				Padding = new StructField<VirtualArrayData<ByteData>>("Padding", ArrayData.CreateVirtualByteArray(paddingSpan));
			}
			var fields = new List<BufferField>(7) {
				Flags_Size,
				MaxStack,
				CodeSize,
				LocalVarSigTok,
				Instructions,
			};
			if (!(Padding is null))
				fields.Add(Padding);
			if (!(EHTable is null))
				fields.Add(EHTable);
			Fields = fields.ToArray();
		}
	}
}
