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

using System;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.AsmEditor.Compiler {
	/// <summary>
	/// Overwrites the metadata to make everything public
	/// </summary>
	unsafe struct MetadataFixer {
		readonly byte* data;
		readonly int dataSize;
		readonly bool isFileLayout;
		IMetaData md;

		public MetadataFixer(RawModuleBytes rawData, bool isFileLayout) {
			data = (byte*)rawData.Pointer;
			dataSize = rawData.Size;
			this.isFileLayout = isFileLayout;
			md = null;
		}

		public bool MakePublic() {
			try {
				try {
					md = MetaDataCreator.CreateMetaData(new PEImage((IntPtr)data, dataSize, isFileLayout ? ImageLayout.File : ImageLayout.Memory, verify: true));
				}
				catch (IOException) {
					return false;
				}
				catch (BadImageFormatException) {
					return false;
				}

				UpdateTypeDefTable();
				UpdateFieldTable();
				UpdateMethodTable();
				UpdateExportedTypeTable();

				return true;
			}
			finally {
				md?.Dispose();
			}
		}

		void UpdateTypeDefTable() {
			var table = md.TablesStream.TypeDefTable;
			int rowSize = (int)table.RowSize;
			// Don't make the global type public so start from 2nd row
			int offset = (int)table.StartOffset + rowSize;
			for (uint row = 1; row < table.Rows; row++, offset += rowSize) {
				var b = data[offset];
				if ((b & 7) <= 1)
					data[offset] = (byte)((b & ~7) | 1);	// Public
				else
					data[offset] = (byte)((b & ~7) | 2);	// NestedPublic
			}
		}

		void UpdateFieldTable() {
			var table = md.TablesStream.FieldTable;
			int offset = (int)table.StartOffset;
			int rowSize = (int)table.RowSize;
			for (uint row = 0; row < table.Rows; row++, offset += rowSize)
				data[offset] = (byte)((data[offset] & ~7) | 6);	// Public
		}

		void UpdateMethodTable() {
			var table = md.TablesStream.MethodTable;
			int offset = (int)table.StartOffset;
			int rowSize = (int)table.RowSize;
			offset += table.Columns[2].Offset;
			for (uint row = 0; row < table.Rows; row++, offset += rowSize)
				data[offset] = (byte)((data[offset] & ~7) | 6);	// Public
		}

		void UpdateExportedTypeTable() {
			var table = md.TablesStream.ExportedTypeTable;
			int offset = (int)table.StartOffset;
			int rowSize = (int)table.RowSize;
			for (uint row = 0; row < table.Rows; row++, offset += rowSize) {
				var b = data[offset];
				if ((b & 7) <= 1)
					data[offset] = (byte)((b & ~7) | 1);	// Public
				else
					data[offset] = (byte)((b & ~7) | 2);	// NestedPublic
			}
		}
	}
}
