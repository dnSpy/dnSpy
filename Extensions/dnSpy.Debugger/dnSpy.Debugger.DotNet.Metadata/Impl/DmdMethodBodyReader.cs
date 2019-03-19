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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	interface IMethodBodyResolver {
		DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);
		(DmdType type, bool isPinned)[] ReadLocals(int localSignatureMetadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments);
	}

	readonly struct DmdMethodBodyReader {
		public static DmdMethodBody Create(IMethodBodyResolver methodBodyResolver, DmdDataStream reader, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			try {
				return new DmdMethodBodyReader(methodBodyResolver, reader, genericTypeArguments, genericMethodArguments).Read();
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			return null;
		}

		readonly IMethodBodyResolver methodBodyResolver;
		readonly DmdDataStream reader;
		readonly IList<DmdType> genericTypeArguments;
		readonly IList<DmdType> genericMethodArguments;

		DmdMethodBodyReader(IMethodBodyResolver methodBodyResolver, DmdDataStream reader, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			this.methodBodyResolver = methodBodyResolver ?? throw new ArgumentNullException(nameof(methodBodyResolver));
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
			this.genericMethodArguments = genericMethodArguments ?? Array.Empty<DmdType>();
		}

		DmdMethodBody Read() {
			if (!ReadHeader(out var localSignatureMetadataToken, out var maxStackSize, out var initLocals, out var codeSize, out var hasExceptionHandlers))
				return null;

			DmdLocalVariableInfo[] localVariables;
			if ((localSignatureMetadataToken & 0x00FFFFFF) != 0 && (byte)(localSignatureMetadataToken >> 24) == 0x11) {
				var localTypes = methodBodyResolver.ReadLocals(localSignatureMetadataToken, genericTypeArguments, genericMethodArguments);
				localVariables = new DmdLocalVariableInfo[localTypes.Length];
				for (int i = 0; i < localVariables.Length; i++) {
					var info = localTypes[i];
					localVariables[i] = new DmdLocalVariableInfo(info.type, i, info.isPinned);
				}
			}
			else
				localVariables = Array.Empty<DmdLocalVariableInfo>();

			var ilBytes = reader.ReadBytes(codeSize);

			DmdExceptionHandlingClause[] exceptionHandlingClauses;
			if (hasExceptionHandlers)
				exceptionHandlingClauses = ReadExceptionHandlingClauses();
			else
				exceptionHandlingClauses = Array.Empty<DmdExceptionHandlingClause>();

			return new DmdMethodBodyImpl(localSignatureMetadataToken, maxStackSize, initLocals, localVariables, exceptionHandlingClauses, genericTypeArguments, genericMethodArguments, ilBytes);
		}

		bool ReadHeader(out int localSignatureMetadataToken, out int maxStackSize, out bool initLocals, out int codeSize, out bool hasExceptionHandlers) {
			byte b = reader.ReadByte();
			switch (b & 7) {
			case 2:
			case 6:
				localSignatureMetadataToken = 0;
				maxStackSize = 8;
				initLocals = false;
				codeSize = b >> 2;
				hasExceptionHandlers = false;
				return true;

			case 3:
				uint flags = (ushort)((reader.ReadByte() << 8) | b);
				int headerSize = (int)(flags >> 12);
				initLocals = (flags & 0x10) != 0;
				maxStackSize = reader.ReadUInt16();
				codeSize = reader.ReadInt32();
				localSignatureMetadataToken = reader.ReadInt32();
				hasExceptionHandlers = (flags & 8) != 0;

				reader.Position += -12 + headerSize * 4;
				if (headerSize < 3)
					hasExceptionHandlers = false;
				return true;

			default:
				localSignatureMetadataToken = 0;
				maxStackSize = 0;
				initLocals = false;
				codeSize = 0;
				hasExceptionHandlers = false;
				return false;
			}
		}

		DmdExceptionHandlingClause[] ReadExceptionHandlingClauses() {
			reader.Position = (reader.Position + 3) & ~3;
			byte b = reader.ReadByte();
			if ((b & 0x3F) != 1)
				return Array.Empty<DmdExceptionHandlingClause>();
			if ((b & 0x40) != 0)
				return ReadFatExceptionHandlers();
			return ReadSmallExceptionHandlers();
		}

		// The CLR truncates the count so num handlers is always <= FFFFh.
		static ushort GetNumberOfExceptionHandlers(uint count) => (ushort)count;

		DmdExceptionHandlingClause[] ReadFatExceptionHandlers() {
			reader.Position--;
			int count = GetNumberOfExceptionHandlers((reader.ReadUInt32() >> 8) / 24);
			var res = new DmdExceptionHandlingClause[count];
			for (int i = 0; i < res.Length; i++) {
				var flags = (DmdExceptionHandlingClauseOptions)reader.ReadInt32();
				int tryOffset = reader.ReadInt32();
				int tryLength = reader.ReadInt32();
				int handlerOffset = reader.ReadInt32();
				int handlerLength = reader.ReadInt32();
				DmdType catchType = null;
				int filterOffset = 0;
				if (flags == DmdExceptionHandlingClauseOptions.Clause)
					catchType = methodBodyResolver.ResolveType(reader.ReadInt32(), genericTypeArguments, genericMethodArguments, DmdResolveOptions.None);
				else if (flags == DmdExceptionHandlingClauseOptions.Filter)
					filterOffset = reader.ReadInt32();
				else
					reader.Position += 4;
				res[i] = new DmdExceptionHandlingClause(flags, tryOffset, tryLength, handlerOffset, handlerLength, filterOffset, catchType);
			}
			return res;
		}

		DmdExceptionHandlingClause[] ReadSmallExceptionHandlers() {
			int count = GetNumberOfExceptionHandlers((uint)reader.ReadByte() / 12);
			var res = new DmdExceptionHandlingClause[count];
			reader.Position += 2;
			for (int i = 0; i < res.Length; i++) {
				var flags = (DmdExceptionHandlingClauseOptions)reader.ReadUInt16();
				int tryOffset = reader.ReadUInt16();
				int tryLength = reader.ReadByte();
				int handlerOffset = reader.ReadUInt16();
				int handlerLength = reader.ReadByte();
				DmdType catchType = null;
				int filterOffset = 0;
				if (flags == DmdExceptionHandlingClauseOptions.Clause)
					catchType = methodBodyResolver.ResolveType(reader.ReadInt32(), genericTypeArguments, genericMethodArguments, DmdResolveOptions.None);
				else if (flags == DmdExceptionHandlingClauseOptions.Filter)
					filterOffset = reader.ReadInt32();
				else
					reader.Position += 4;
				res[i] = new DmdExceptionHandlingClause(flags, tryOffset, tryLength, handlerOffset, handlerLength, filterOffset, catchType);
			}
			return res;
		}
	}
}
