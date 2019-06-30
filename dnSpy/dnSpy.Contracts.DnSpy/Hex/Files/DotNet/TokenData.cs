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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET metadata token
	/// </summary>
	public class TokenData : UInt32Data {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public TokenData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public TokenData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteToken(ReadValue());

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var mdHeaders = file.GetHeaders<DotNetMetadataHeaders>();
			if (mdHeaders is null)
				return null;
			var tablesStream = mdHeaders.TablesStream;
			if (tablesStream is null)
				return null;
			var token = new MDToken(ReadValue());
			if ((uint)token.Table >= (uint)tablesStream.MDTables.Count)
				return null;
			var mdTable = tablesStream.MDTables[(int)token.Table];
			if (!mdTable.IsValidRID(token.Rid))
				return null;
			return new HexSpan(mdTable.Span.Start + (token.Rid - 1) * mdTable.RowSize, mdTable.RowSize);
		}
	}

	/// <summary>
	/// Coded .NET metadata token
	/// </summary>
	public abstract class CodedTokenData : SimpleData {
		readonly CodedToken codedToken;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="codedToken">Coded token info</param>
		protected CodedTokenData(HexBufferSpan span, CodedToken codedToken)
			: base(span) => this.codedToken = codedToken ?? throw new ArgumentNullException(nameof(codedToken));

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected abstract uint ReadTokenValue();

		/// <summary>
		/// Writes an error
		/// </summary>
		/// <param name="formatter">Formatter</param>
		protected abstract void WriteValueError(HexFieldFormatter formatter);

		MDToken? ReadToken() {
			if (!codedToken.Decode(ReadTokenValue(), out MDToken token))
				return null;
			return token;
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			var token = ReadToken();
			if (!(token is null))
				formatter.WriteToken(token.Value.Raw);
			else
				WriteValueError(formatter);
		}

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var tablesStream = file.GetHeaders<DotNetMetadataHeaders>()?.TablesStream;
			if (tablesStream is null)
				return null;
			var token = ReadToken();
			if (token is null)
				return null;
			if ((uint)token.Value.Table >= (uint)tablesStream.MDTables.Count)
				return null;
			var mdTable = tablesStream.MDTables[(int)token.Value.Table];
			if (!mdTable.IsValidRID(token.Value.Rid))
				return null;
			return new HexSpan(mdTable.Span.Start + (token.Value.Rid - 1) * mdTable.RowSize, mdTable.RowSize);
		}
	}

	/// <summary>
	/// 16-bit coded .NET metadata token
	/// </summary>
	public class CodedToken16Data : CodedTokenData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="codedToken">Coded token info</param>
		public CodedToken16Data(HexBufferSpan span, CodedToken codedToken)
			: base(span, codedToken) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="codedToken">Coded token info</param>
		public CodedToken16Data(HexBuffer buffer, HexPosition position, CodedToken codedToken)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2)), codedToken) {
		}

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadTokenValue() => Span.Buffer.ReadUInt16(Span.Start);

		/// <summary>
		/// Writes an error
		/// </summary>
		/// <param name="formatter">Formatter</param>
		protected override void WriteValueError(HexFieldFormatter formatter) => formatter.WriteUInt16((ushort)ReadTokenValue());
	}

	/// <summary>
	/// 32-bit coded .NET metadata token
	/// </summary>
	public class CodedToken32Data : CodedTokenData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="codedToken">Coded token info</param>
		public CodedToken32Data(HexBufferSpan span, CodedToken codedToken)
			: base(span, codedToken) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="codedToken">Coded token info</param>
		public CodedToken32Data(HexBuffer buffer, HexPosition position, CodedToken codedToken)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4)), codedToken) {
		}

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadTokenValue() => Span.Buffer.ReadUInt32(Span.Start);

		/// <summary>
		/// Writes an error
		/// </summary>
		/// <param name="formatter">Formatter</param>
		protected override void WriteValueError(HexFieldFormatter formatter) => formatter.WriteUInt32(ReadTokenValue());
	}

	/// <summary>
	/// .NET table rid
	/// </summary>
	public abstract class RidData : SimpleData {
		readonly Table table;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="table">Table</param>
		protected RidData(HexBufferSpan span, Table table)
			: base(span) => this.table = table;

		/// <summary>
		/// Reads the rid
		/// </summary>
		/// <returns></returns>
		protected abstract uint ReadRidValue();

		MDToken ReadToken() => new MDToken(table, ReadRidValue());

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteToken(ReadToken().Raw);

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var tablesStream = file.GetHeaders<DotNetMetadataHeaders>()?.TablesStream;
			if (tablesStream is null)
				return null;
			var token = ReadToken();
			if ((uint)token.Table >= (uint)tablesStream.MDTables.Count)
				return null;
			var mdTable = tablesStream.MDTables[(int)token.Table];
			if (!mdTable.IsValidRID(token.Rid))
				return null;
			return new HexSpan(mdTable.Span.Start + (token.Rid - 1) * mdTable.RowSize, mdTable.RowSize);
		}
	}

	/// <summary>
	/// 16-bit .NET table rid
	/// </summary>
	public class Rid16Data : RidData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="table">Table</param>
		public Rid16Data(HexBufferSpan span, Table table)
			: base(span, table) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="table">Table</param>
		public Rid16Data(HexBuffer buffer, HexPosition position, Table table)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2)), table) {
		}

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadRidValue() => Span.Buffer.ReadUInt16(Span.Start);
	}

	/// <summary>
	/// 32-bit .NET table rid
	/// </summary>
	public class Rid32Data : RidData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="table">Table</param>
		public Rid32Data(HexBufferSpan span, Table table)
			: base(span, table) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="table">Table</param>
		public Rid32Data(HexBuffer buffer, HexPosition position, Table table)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4)), table) {
		}

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadRidValue() => Span.Buffer.ReadUInt32(Span.Start);
	}
}
