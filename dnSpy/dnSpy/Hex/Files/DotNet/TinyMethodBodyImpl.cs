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

using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class TinyMethodBodyImpl : TinyMethodBody {
		internal const string InstructionsFieldName = "Instructions";
		public override StructField<ByteFlagsData> Flags_CodeSize { get; }
		public override StructField<VirtualArrayData<ByteData>> Instructions { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> flagsCodeSizeFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x03, "Format"),
			new FlagInfo(0x03, 0x02, "TinyFormat"),
		});

		public TinyMethodBodyImpl(DotNetMethodProvider methodProvider, HexBufferSpan span, ReadOnlyCollection<uint> tokens)
			: base(methodProvider, span, tokens) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Flags_CodeSize = new StructField<ByteFlagsData>("Flags_CodeSize", new ByteFlagsData(buffer, pos, flagsCodeSizeFlagInfos));
			Instructions = new StructField<VirtualArrayData<ByteData>>(InstructionsFieldName, ArrayData.CreateVirtualByteArray(HexBufferSpan.FromBounds(span.Start + 1, span.End), InstructionsFieldName));
			Fields = new BufferField[] {
				Flags_CodeSize,
				Instructions,
			};
		}
	}
}
