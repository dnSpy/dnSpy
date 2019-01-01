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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using MD = dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	unsafe struct MetadataFixer {
		readonly MD.Metadata md;
		readonly void* peFileData;
		bool patchedMetadata;

		public MetadataFixer(MD.Metadata metadata, void* peFileData) {
			md = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.peFileData = peFileData;
			patchedMetadata = false;
		}

		public bool Fix() {
			FixAssemblyRow();
			FixAssemblyRefRows();

			// This can fail if it's an obfuscated assembly
			Debug.Assert(!patchedMetadata);
			return patchedMetadata;
		}

		void Write(MDTable tbl, uint rid, ColumnInfo column, uint value) {
			patchedMetadata = true;
			byte* p = (byte*)peFileData + (int)((uint)tbl.StartOffset + column.Offset + (rid - 1) * tbl.RowSize);
			switch (column.Size) {
			case 1:
				*p = (byte)value;
				break;

			case 2:
				*(ushort*)p = (ushort)value;
				break;

			case 4:
				*(uint*)p = value;
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		void FixAssemblyRow() {
			var tbl = md.TablesStream.AssemblyTable;

			const uint rid = 1;
			if (!md.TablesStream.TryReadAssemblyRow(rid, out var row))
				return;

			if (row.Locale != 0) {
				var locale = UTF8String.ToSystemString(md.StringsStream.Read(row.Locale));
				if (!IsValidName(locale))
					Write(tbl, rid, tbl.Columns[8], 0);
			}

			if (row.PublicKey != 0) {
				bool valid = md.BlobStream.TryCreateReader(row.PublicKey, out var pkReader) &&
					IsValidPublicKey(ref pkReader);
				if (!valid)
					Write(tbl, rid, tbl.Columns[6], 0);
			}
		}

		void FixAssemblyRefRows() {
			var tbl = md.TablesStream.AssemblyRefTable;

			uint asmName = md.TablesStream.TryReadAssemblyRow(1, out var asmRow) ? asmRow.Name : 1;

			for (uint rid = 1; ; rid++) {
				if (!md.TablesStream.TryReadAssemblyRefRow(rid, out var row))
					return;

				var name = UTF8String.ToSystemString(md.StringsStream.Read(row.Name));
				if (!IsValidName(name))
					Write(tbl, rid, tbl.Columns[6], asmName);

				if (row.Locale != 0) {
					var locale = UTF8String.ToSystemString(md.StringsStream.Read(row.Locale));
					if (!IsValidName(locale))
						Write(tbl, rid, tbl.Columns[7], 0);
				}

				bool validPublicKey;
				if ((row.Flags & (uint)AssemblyAttributes.PublicKey) != 0) {
					validPublicKey = md.BlobStream.TryCreateReader(row.PublicKeyOrToken, out var pkReader) &&
						IsValidPublicKey(ref pkReader);
				}
				else {
					if (row.PublicKeyOrToken == 0)
						validPublicKey = true;
					else
						validPublicKey = md.BlobStream.TryCreateReader(row.PublicKeyOrToken, out var pkReader) && (pkReader.Length == 0 || pkReader.Length == 8);
				}
				if (!validPublicKey) {
					Write(tbl, rid, tbl.Columns[4], row.Flags & ~(uint)AssemblyAttributes.PublicKey);
					Write(tbl, rid, tbl.Columns[5], 0);
				}
			}
		}

		bool IsValidName(string s) {
			if (string.IsNullOrEmpty(s))
				return false;
			for (int i = 0; i < s.Length; i++) {
				var c = s[i];
				if (char.IsLowSurrogate(c))
					return false;
				if (char.IsHighSurrogate(c)) {
					i++;
					if (i >= s.Length || !char.IsLowSurrogate(s[i]))
						return false;
				}
			}
			return true;
		}

		// See Microsoft.CodeAnalysis.CryptoBlobParser.IsValidPublicKey
		bool IsValidPublicKey(ref DataReader reader) {
			if (reader.Length < 13)
				return false;
			uint signatureAlgorithm = reader.ReadUInt32();
			uint hashAlgorithm = reader.ReadUInt32();
			uint size = reader.ReadUInt32();
			if (reader.Length != 12UL + size)
				return false;
			reader.Position = 0;
			if (SameBytes(ref reader, ecmaKey))
				return true;
			if (signatureAlgorithm != 0 && ((signatureAlgorithm >> 13) & 7) != 1)
				return false;
			if (hashAlgorithm != 0 && (((hashAlgorithm >> 13) & 7) != 4 || (hashAlgorithm & 0x1FF) < 4))
				return false;

			return true;
		}
		static readonly byte[] ecmaKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };

		static bool SameBytes(ref DataReader reader, byte[] data) {
			if (reader.BytesLeft < (uint)data.Length)
				return false;
			for (int i = 0; i < data.Length; i++) {
				if (reader.ReadByte() != data[i])
					return false;
			}
			return true;
		}
	}
}
