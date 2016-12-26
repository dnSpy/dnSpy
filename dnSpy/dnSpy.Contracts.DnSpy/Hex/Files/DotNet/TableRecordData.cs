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
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET table record
	/// </summary>
	public sealed class TableRecordData : StructureData {
		/// <summary>
		/// Gets the token
		/// </summary>
		public MDToken Token { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Gets the owner heap
		/// </summary>
		public TablesHeap TablesHeap { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tableName">Name of table</param>
		/// <param name="token">Token</param>
		/// <param name="span">Span</param>
		/// <param name="fields">Fields</param>
		/// <param name="tablesHeap">Owner heap</param>
		public TableRecordData(string tableName, MDToken token, HexBufferSpan span, BufferField[] fields, TablesHeap tablesHeap)
			: base(tableName, span) {
			if (fields == null)
				throw new ArgumentNullException(nameof(fields));
			if (fields.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(fields));
			if (tablesHeap == null)
				throw new ArgumentNullException(nameof(tablesHeap));
			Token = token;
			Fields = fields;
			TablesHeap = tablesHeap;
		}

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteName(HexFieldFormatter formatter) {
			formatter.Write(Token.Table.ToString(), PredefinedClassifiedTextTags.ValueType);
			formatter.WriteArrayField(Token.Rid);
		}
	}
}
