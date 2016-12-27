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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class SmallExceptionHandlerTableImpl : SmallExceptionHandlerTable {
		public override StructField<SmallSection> SectSmall { get; }
		public override StructField<UInt16Data> Reserved { get; }
		public override StructField<ArrayData<SmallExceptionClause>> Clauses { get; }

		protected override BufferField[] Fields { get; }

		public SmallExceptionHandlerTableImpl(HexBufferSpan span)
			: base(span) {
			if (span.Length < 4)
				throw new ArgumentOutOfRangeException(nameof(span));
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			SectSmall = new StructField<SmallSection>("SectSmall", new SmallSectionImpl(buffer, pos));
			Reserved = new StructField<UInt16Data>("Reserved", new UInt16Data(buffer, pos + 2));

			var startPos = pos + 4;
			int elements = (int)((span.End.Position - startPos).ToUInt64() / 12);
			var fields = new ArrayField<SmallExceptionClause>[elements];
			var currPos = startPos;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<SmallExceptionClause>(new SmallExceptionClauseImpl(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			Clauses = new StructField<ArrayData<SmallExceptionClause>>("Clauses", new ArrayData<SmallExceptionClause>("Clauses", new HexBufferSpan(buffer, HexSpan.FromBounds(startPos, currPos)), fields));

			Fields = new BufferField[] {
				SectSmall,
				Reserved,
				Clauses,
			};
		}
	}

	sealed class FatExceptionHandlerTableImpl : FatExceptionHandlerTable {
		public override StructField<FatSection> SectFat { get; }
		public override StructField<ArrayData<FatExceptionClause>> Clauses { get; }

		protected override BufferField[] Fields { get; }

		public FatExceptionHandlerTableImpl(HexBufferSpan span)
			: base(span) {
			if (span.Length < 4)
				throw new ArgumentOutOfRangeException(nameof(span));
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			SectFat = new StructField<FatSection>("SectFat", new FatSectionImpl(buffer, pos));

			var startPos = pos + 4;
			int elements = (int)((span.End.Position - startPos).ToUInt64() / 24);
			var fields = new ArrayField<FatExceptionClause>[elements];
			var currPos = startPos;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<FatExceptionClause>(new FatExceptionClauseImpl(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			Clauses = new StructField<ArrayData<FatExceptionClause>>("Clauses", new ArrayData<FatExceptionClause>("Clauses", new HexBufferSpan(buffer, HexSpan.FromBounds(startPos, currPos)), fields));

			Fields = new BufferField[] {
				SectFat,
				Clauses,
			};
		}
	}
}
