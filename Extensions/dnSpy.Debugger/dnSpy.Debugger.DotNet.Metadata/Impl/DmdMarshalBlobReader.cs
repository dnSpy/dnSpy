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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	readonly struct DmdMarshalBlobReader : IDisposable {
		readonly DmdModule module;
		readonly DmdDataStream reader;
		readonly IList<DmdType> genericTypeArguments;

		public static DmdMarshalType? Read(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments) {
			using (var marshalReader = new DmdMarshalBlobReader(module, reader, genericTypeArguments))
				return marshalReader.Read();
		}

		DmdMarshalBlobReader(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments) {
			this.module = module;
			this.reader = reader;
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
		}

		DmdMarshalType? Read() {
			const int DEFAULT = 0;
			try {
				var nativeType = (UnmanagedType)reader.ReadByte();
				UnmanagedType nt;
				int size;
				switch (nativeType) {
				case UnmanagedType.ByValTStr:
					size = CanRead ? (int)reader.ReadCompressedUInt32() : DEFAULT;
					return DmdMarshalType.CreateFixedSysString(size);

				case UnmanagedType.SafeArray:
					var vt = CanRead ? (VarEnum)reader.ReadCompressedUInt32() : DEFAULT;
					var udtName = CanRead ? ReadUTF8String() : null;
					var udtRef = udtName is null ? null : DmdTypeNameParser.Parse(module, udtName, genericTypeArguments);
					return DmdMarshalType.CreateSafeArray(vt, udtRef);

				case UnmanagedType.ByValArray:
					size = CanRead ? (int)reader.ReadCompressedUInt32() : DEFAULT;
					nt = CanRead ? (UnmanagedType)reader.ReadCompressedUInt32() : DEFAULT;
					return DmdMarshalType.CreateFixedArray(size, nt);

				case UnmanagedType.LPArray:
					nt = CanRead ? (UnmanagedType)reader.ReadCompressedUInt32() : DEFAULT;
					int paramNum = CanRead ? (int)reader.ReadCompressedUInt32() : DEFAULT;
					size = CanRead ? (int)reader.ReadCompressedUInt32() : DEFAULT;
					bool hasFlags = CanRead;
					int flags = hasFlags ? (int)reader.ReadCompressedUInt32() : DEFAULT;
					const int ntaSizeParamIndexSpecified = 1;
					if (hasFlags && (flags & ntaSizeParamIndexSpecified) == 0)
						paramNum = 0;
					return DmdMarshalType.CreateArray(nt, (short)paramNum, size);

				case UnmanagedType.CustomMarshaler:
					var guid = ReadUTF8String();
					var nativeTypeName = ReadUTF8String();
					var custMarshalerName = ReadUTF8String();
					var cmRef = custMarshalerName.Length == 0 ? null : DmdTypeNameParser.Parse(module, custMarshalerName, genericTypeArguments);
					var cookie = ReadUTF8String();
					return DmdMarshalType.CreateCustomMarshaler(custMarshalerName, cmRef, cookie);

				case UnmanagedType.IUnknown:
				case UnmanagedType.IDispatch:
				case UnmanagedType.Interface:
					int iidParamIndex = CanRead ? (int)reader.ReadCompressedUInt32() : DEFAULT;
					return DmdMarshalType.CreateInterface(nativeType, iidParamIndex);

				default:
					return DmdMarshalType.Create(nativeType);
				}
			}
			catch (ArgumentException) {
			}
			catch (IOException) {
			}
			return null;
		}

		bool CanRead => reader.Position < reader.Length;

		string ReadUTF8String() {
			uint len = reader.ReadCompressedUInt32();
			return len == 0 ? string.Empty : Encoding.UTF8.GetString(reader.ReadBytes((int)len));
		}

		public void Dispose() => reader?.Dispose();
	}
}
