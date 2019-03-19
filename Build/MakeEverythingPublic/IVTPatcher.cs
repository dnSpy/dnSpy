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

// This code patches an assembly's System.Runtime.CompilerServices.InternalsVisibleToAttribute
// so the string passed to the attribute constructor is the name of another assembly.
// If there's no such attribute or if the new string doesn't fit in the old string, this code fails.
// If it happens, use a smaller public key and/or a shorter assembly name and use no spaces
// or don't use PublicKey=xxxx... (since Roslyn C# compiler seems to ignore the public key).
//
// A more generic patcher would rewrite some of the metadata tables but this isn't needed since
// we only use it to patch Roslyn assemblies which contain a ton of IVT attributes.
//
// PERF is the same as copying a file since it just patches the data in memory, nothing is rewritten.

using System;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace MakeEverythingPublic {
	enum IVTPatcherResult {
		OK,
		NoCustomAttributes,
		NoIVTs,
		IVTBlobTooSmall,
	}

	struct IVTPatcher {
		// Prefer overwriting IVTs with this public key since they're just test assemblies
		const string ROSLYN_OPEN_SOURCE_PUBLIC_KEY = "002400000480000094000000060200000024000052534131000400000100010055e0217eb635f69281051f9a823e0c7edd90f28063eb6c7a742a19b4f6139778ee0af438f47aed3b6e9f99838aa8dba689c7a71ddb860c96d923830b57bbd5cd6119406ddb9b002cf1c723bf272d6acbb7129e9d6dd5a5309c94e0ff4b2c884d45a55f475cd7dba59198086f61f5a8c8b5e601c0edbf269733f6f578fc8579c2";

		readonly byte[] data;
		readonly Metadata md;
		readonly byte[] ivtBlob;

		public IVTPatcher(byte[] data, Metadata md, byte[] ivtBlob) {
			this.data = data;
			this.md = md;
			this.ivtBlob = ivtBlob;
		}

		public IVTPatcherResult Patch() {
			var rids = md.GetCustomAttributeRidList(Table.Assembly, 1);
			if (rids.Count == 0)
				return IVTPatcherResult.NoCustomAttributes;

			if (FindIVT(rids, out var foundIVT, out uint ivtBlobOffset)) {
				Array.Copy(ivtBlob, 0, data, ivtBlobOffset, ivtBlob.Length);
				return IVTPatcherResult.OK;
			}

			if (!foundIVT)
				return IVTPatcherResult.NoIVTs;
			return IVTPatcherResult.IVTBlobTooSmall;
		}

		bool FindIVT(RidList rids, out bool foundIVT, out uint ivtBlobDataOffset) {
			ivtBlobDataOffset = 0;
			foundIVT = false;
			uint otherIVTBlobOffset = uint.MaxValue;
			var blobStream = md.BlobStream.CreateReader();
			var tbl = md.TablesStream.CustomAttributeTable;
			uint baseOffset = (uint)tbl.StartOffset;
			var columnType = tbl.Columns[1];
			var columnValue = tbl.Columns[2];
			for (int i = 0; i < rids.Count; i++) {
				uint rid = rids[i];
				uint offset = baseOffset + (rid - 1) * tbl.RowSize;
				uint type = ReadColumn(columnType, offset);
				if (!IsIVTCtor(type))
					continue;
				foundIVT = true;
				uint blobOffset = ReadColumn(columnValue, offset);
				if (blobOffset + ivtBlob.Length > blobStream.Length)
					continue;
				blobStream.Position = blobOffset;
				if (!blobStream.TryReadCompressedUInt32(out uint len))
					continue;
				var compressedSize = blobStream.Position - blobOffset;
				if (compressedSize + len < ivtBlob.Length)
					continue;
				if (!ParseIVTBlob(ref blobStream, blobStream.Position + len, out var publicKeyString))
					continue;
				if (StringComparer.OrdinalIgnoreCase.Equals(publicKeyString, ROSLYN_OPEN_SOURCE_PUBLIC_KEY)) {
					ivtBlobDataOffset = (uint)md.BlobStream.StartOffset + blobOffset;
					return true;
				}
				else
					otherIVTBlobOffset = (uint)md.BlobStream.StartOffset + blobOffset;
			}
			if (otherIVTBlobOffset != uint.MaxValue) {
				ivtBlobDataOffset = otherIVTBlobOffset;
				return true;
			}

			return false;
		}

		static bool ParseIVTBlob(ref DataReader reader, uint end, out string publicKeyString) {
			publicKeyString = null;
			if ((ulong)reader.Position + 2 > end)
				return false;
			if (reader.ReadUInt16() != 1)
				return false;
			if (!reader.TryReadCompressedUInt32(out uint len) || (ulong)reader.Position + len >= end)
				return false;
			var s = reader.ReadUtf8String((int)len);
			const string PublicKeyPattern = "PublicKey=";
			int index = s.IndexOf(PublicKeyPattern, StringComparison.OrdinalIgnoreCase);
			if (index >= 0)
				publicKeyString = s.Substring(index + PublicKeyPattern.Length).Trim();
			return true;
		}

		bool IsIVTCtor(uint codedType) {
			if (!CodedToken.CustomAttributeType.Decode(codedType, out MDToken ctor))
				return false;

			switch (ctor.Table) {
			case Table.Method:
				uint declTypeDefToken = md.GetOwnerTypeOfMethod(ctor.Rid);
				return IsIVT_TypeDef(declTypeDefToken);

			case Table.MemberRef:
				if (!md.TablesStream.TryReadMemberRefRow(ctor.Rid, out var memberRefRow))
					return false;
				if (!CodedToken.MemberRefParent.Decode(memberRefRow.Class, out MDToken parentToken))
					return false;
				switch (parentToken.Table) {
				case Table.TypeDef:
					return IsIVT_TypeDef(parentToken.Rid);

				case Table.TypeRef:
					return IsIVT_TypeRef(parentToken.Rid);

				case Table.TypeSpec:
				default:
					return false;
				}

			default:
				return false;
			}
		}

		bool IsIVT_TypeRef(uint typeRefRid) {
			if (!md.TablesStream.TryReadTypeRefRow(typeRefRid, out var typeRefRow))
				return false;
			if (!CodedToken.ResolutionScope.Decode(typeRefRow.ResolutionScope, out MDToken scope) || scope.Table == Table.TypeRef)
				return false;
			return IsIVTCtor(typeRefRow.Namespace, typeRefRow.Name);
		}

		bool IsIVT_TypeDef(uint typeDefRid) {
			if (!md.TablesStream.TryReadTypeDefRow(typeDefRid, out var typeDefRow))
				return false;
			if ((typeDefRow.Flags & (uint)TypeAttributes.VisibilityMask) >= (uint)TypeAttributes.NestedPublic)
				return false;
			return IsIVTCtor(typeDefRow.Namespace, typeDefRow.Name);
		}

		bool IsIVTCtor(uint @namespace, uint name) =>
			md.StringsStream.ReadNoNull(name) == InternalsVisibleToAttribute &&
			md.StringsStream.ReadNoNull(@namespace) == System_Runtime_CompilerServices;
		static readonly UTF8String System_Runtime_CompilerServices = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String InternalsVisibleToAttribute = new UTF8String("InternalsVisibleToAttribute");

		uint ReadColumn(ColumnInfo column, uint columnOffset) {
			columnOffset += (uint)column.Offset;
			switch (column.Size) {
			case 1: return data[columnOffset];
			case 2: return data[columnOffset++] | ((uint)data[columnOffset] << 8);
			case 4: return data[columnOffset++] | ((uint)data[columnOffset++] << 8) | ((uint)data[columnOffset++] << 16) | ((uint)data[columnOffset] << 24);
			default: throw new InvalidOperationException();
			}
		}
	}
}
