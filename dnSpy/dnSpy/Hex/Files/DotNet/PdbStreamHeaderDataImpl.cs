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

using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class PdbStreamHeaderDataImpl : PdbStreamHeaderData {
		public override StructField<PortablePdbIdData> PdbId { get; }
		public override StructField<TokenData> EntryPoint { get; }
		public override StructField<UInt64FlagsData> ReferencedTypeSystemTables { get; }
		public override StructField<ArrayData<UInt32Data>> TypeSystemTableRows { get; }

		protected override BufferField[] Fields { get; }

		public PdbStreamHeaderDataImpl(HexBufferSpan span, int rowsFieldCount)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			PdbId = new StructField<PortablePdbIdData>("PdbId", new PortablePdbIdData(buffer, pos));
			EntryPoint = new StructField<TokenData>("EntryPoint", new TokenData(buffer, pos + 20));
			ReferencedTypeSystemTables = new StructField<UInt64FlagsData>("ReferencedTypeSystemTables", new UInt64FlagsData(buffer, pos + 24, TablesHeaderDataImpl.tableFlagInfos));
			TypeSystemTableRows = new StructField<ArrayData<UInt32Data>>("TypeSystemTableRows", ArrayData.CreateUInt32Array(buffer, pos + 32, rowsFieldCount));
			Fields = new BufferField[] {
				PdbId,
				EntryPoint,
				ReferencedTypeSystemTables,
				TypeSystemTableRows,
			};
		}
	}
}
