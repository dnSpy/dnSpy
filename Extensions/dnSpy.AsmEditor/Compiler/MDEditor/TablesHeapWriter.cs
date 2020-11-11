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
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class TablesHeapWriter : MDHeapWriter {
		public override string Name => tablesHeap.Name;

		readonly TablesMDHeap tablesHeap;
		readonly StringsMDHeap stringsHeap;
		readonly GuidMDHeap guidHeap;
		readonly BlobMDHeap blobHeap;
		readonly MDStreamFlags mdStreamFlags;

		public TablesHeapWriter(TablesMDHeap tablesHeap, StringsMDHeap stringsHeap, GuidMDHeap guidHeap, BlobMDHeap blobHeap) {
			this.tablesHeap = tablesHeap;
			this.stringsHeap = stringsHeap;
			this.guidHeap = guidHeap;
			this.blobHeap = blobHeap;
			if (stringsHeap.IsBig)
				mdStreamFlags |= MDStreamFlags.BigStrings;
			if (guidHeap.IsBig)
				mdStreamFlags |= MDStreamFlags.BigGUID;
			if (blobHeap.IsBig)
				mdStreamFlags |= MDStreamFlags.BigBlob;
		}

		static TablesHeapWriter() {
			tablesToIgnore = new bool[0x40];

			// Don't include tables the compiler won't need

			// Not likely to be used by the compiler
			tablesToIgnore[(int)Table.FieldMarshal] = true;
			// Used only by method bodies, and we don't write any
			tablesToIgnore[(int)Table.StandAloneSig] = true;
			// Not likely to be used by the compiler, and we don't write the field data to the new file
			tablesToIgnore[(int)Table.FieldRVA] = true;
			// Not used by the compiler
			tablesToIgnore[(int)Table.ENCLog] = true;
			// Not used by the compiler
			tablesToIgnore[(int)Table.ENCMap] = true;
			// Not used by the compiler
			tablesToIgnore[(int)Table.AssemblyProcessor] = true;
			// Not used by the compiler
			tablesToIgnore[(int)Table.AssemblyOS] = true;
			// Not used by the compiler
			tablesToIgnore[(int)Table.AssemblyRefProcessor] = true;
			// Not used by the compiler
			tablesToIgnore[(int)Table.AssemblyRefOS] = true;
			// Not likely to be used by the compiler
			tablesToIgnore[(int)Table.ManifestResource] = true;

			// All portable PDB tables (everything >= 0x30)
			tablesToIgnore[(int)Table.Document] = true;
			tablesToIgnore[(int)Table.MethodDebugInformation] = true;
			tablesToIgnore[(int)Table.LocalScope] = true;
			tablesToIgnore[(int)Table.LocalVariable] = true;
			tablesToIgnore[(int)Table.LocalConstant] = true;
			tablesToIgnore[(int)Table.ImportScope] = true;
			tablesToIgnore[(int)Table.StateMachineMethod] = true;
			tablesToIgnore[(int)Table.CustomDebugInformation] = true;
			tablesToIgnore[0x38] = true;
			tablesToIgnore[0x39] = true;
			tablesToIgnore[0x3A] = true;
			tablesToIgnore[0x3B] = true;
			tablesToIgnore[0x3C] = true;
			tablesToIgnore[0x3D] = true;
			tablesToIgnore[0x3E] = true;
			tablesToIgnore[0x3F] = true;
		}
		static readonly bool[] tablesToIgnore;

		public unsafe override void Write(MDWriter mdWriter, MDWriterStream stream, byte[] tempBuffer) {
			var tblStream = mdWriter.MetadataEditor.RealMetadata.TablesStream;
			stream.Write(tblStream.Reserved1);
			stream.Write((byte)(tblStream.Version >> 8));
			stream.Write((byte)tblStream.Version);
			stream.Write((byte)mdStreamFlags);
			stream.Write((byte)1);
			stream.Write(GetValidMask(tablesHeap));
			stream.Write(GetSortedMask(tablesHeap, tblStream.SortedMask));
			var rowCounts = new uint[tablesHeap.TableInfos.Length];
			var infos = tablesHeap.TableInfos;
			for (int i = 0; i < infos.Length; i++) {
				if (tablesToIgnore[i])
					continue;
				var info = infos[i];
				if (info is not null && !info.IsEmpty) {
					rowCounts[i] = info.Rows;
					stream.Write(info.Rows);
				}
			}

			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables((byte)(tblStream.Version >> 8), (byte)tblStream.Version);
			dnTableSizes.InitializeSizes((mdStreamFlags & MDStreamFlags.BigStrings) != 0,
				(mdStreamFlags & MDStreamFlags.BigGUID) != 0,
				(mdStreamFlags & MDStreamFlags.BigBlob) != 0,
				rowCounts, rowCounts);

			long totalSize = 0;
			for (int i = 0; i < infos.Length; i++) {
				if (tablesToIgnore[i])
					continue;
				var info = infos[i];
				if (info is not null && !info.IsEmpty)
					totalSize += (long)info.Rows * tableInfos[i].RowSize;
			}

			// NOTE: We don't write method bodies or field data, the compiler shouldn't
			// read Method.RVA. We also don't write the FieldRVA table.

			// PERF: Write to a temp buffer followed by a call to stream.Write(byte[]). It's faster
			// than calling stream.Write() for every row + column.

			var tablesPos = stream.Position;
			int tempBufferIndex = 0;
			for (int i = 0; i < infos.Length; i++) {
				if (tablesToIgnore[i])
					continue;
				var info = infos[i];
				if (info is null || info.IsEmpty)
					continue;

				var tableWriter = TableWriter.Create(info);
				var mdTable = info.MDTable;

				var tbl = tableInfos[i];
				var columns = tbl.Columns;
				var rows = tableWriter.Rows;
				uint currentRowIndex = 0;
				var rowSize = (uint)tbl.RowSize;
				Debug.Assert(tempBuffer.Length >= rowSize, "Temp buffer is too small");

				// If there are no changes in the original metadata or layout, just copy everything
				uint unmodifiedRows = tableWriter.FirstModifiedRowId - 1;
				if (unmodifiedRows > 0 && Equals(mdTable.TableInfo, tbl)) {
					if (tempBufferIndex > 0) {
						stream.Write(tempBuffer, 0, tempBufferIndex);
						tempBufferIndex = 0;
					}

					stream.Write((byte*)mdWriter.ModuleData.Pointer + (int)mdTable.StartOffset, (int)(unmodifiedRows * mdTable.RowSize));

					Debug.Assert(unmodifiedRows <= rows);
					rows -= unmodifiedRows;
					currentRowIndex += unmodifiedRows;
				}

				while (rows > 0) {
					int bytesLeft = tempBuffer.Length - tempBufferIndex;
					uint maxRows = Math.Min((uint)bytesLeft / rowSize, rows);
					if (maxRows == 0) {
						stream.Write(tempBuffer, 0, tempBufferIndex);
						tempBufferIndex = 0;

						bytesLeft = tempBuffer.Length;
						maxRows = Math.Min((uint)bytesLeft / rowSize, rows);
					}
					Debug.Assert(maxRows > 0);

					for (uint endRowIndex = currentRowIndex + maxRows; currentRowIndex < endRowIndex; currentRowIndex++, tempBufferIndex += (int)rowSize)
						tableWriter.WriteRow(currentRowIndex, columns, tempBuffer, tempBufferIndex);
					rows -= maxRows;
				}
			}
			if (tempBufferIndex > 0)
				stream.Write(tempBuffer, 0, tempBufferIndex);
			if (tablesPos + totalSize != stream.Position)
				throw new InvalidOperationException();
		}

		static bool Equals(TableInfo a, TableInfo b) {
			Debug.Assert(a.Name == b.Name);
			if (a.RowSize != b.RowSize)
				return false;
			var ac = a.Columns;
			var bc = b.Columns;
			Debug.Assert(ac.Length == bc.Length);
			for (int i = 0; i < ac.Length; i++) {
				if (ac[i].Offset != bc[i].Offset)
					return false;
			}
			return true;
		}

		static ulong GetValidMask(TablesMDHeap tablesHeap) {
			ulong mask = 0;
			var infos = tablesHeap.TableInfos;
			for (int i = 0; i < infos.Length; i++) {
				if (tablesToIgnore[i])
					continue;
				var info = infos[i];
				if (info is not null && !info.IsEmpty)
					mask |= 1UL << i;
			}
			return mask;
		}

		static ulong GetSortedMask(TablesMDHeap tablesHeap, ulong mask) {
			var ignore = tablesToIgnore;
			for (int i = 0; i < ignore.Length; i++) {
				if (ignore[i])
					mask &= ~(1UL << i);
			}
			return mask;
		}
	}
}
