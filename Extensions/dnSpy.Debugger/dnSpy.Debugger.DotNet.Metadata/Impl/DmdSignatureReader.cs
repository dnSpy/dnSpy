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
using System.Collections.Generic;
using System.IO;
using DMD = dnlib.DotNet;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdSignatureStream : IDisposable {
		public abstract byte ReadByte();
		public abstract uint ReadUInt32();
		public abstract void Dispose();

		public uint ReadCompressedUInt32() {
			byte b = ReadByte();
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80)
				return (uint)(((b & 0x3F) << 8) | ReadByte());

			return (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
		}

		public int ReadCompressedInt32() {
			byte b = ReadByte();
			if ((b & 0x80) == 0) {
				if ((b & 1) != 0)
					return -0x40 | (b >> 1);
				return b >> 1;
			}

			if ((b & 0xC0) == 0x80) {
				uint tmp = (uint)(((b & 0x3F) << 8) | ReadByte());
				if ((tmp & 1) != 0)
					return -0x2000 | (int)(tmp >> 1);
				return (int)(tmp >> 1);
			}

			if ((b & 0xE0) == 0xC0) {
				uint tmp = (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) |
						(ReadByte() << 8) | ReadByte());
				if ((tmp & 1) != 0)
					return -0x10000000 | (int)(tmp >> 1);
				return (int)(tmp >> 1);
			}

			throw new IOException();
		}
	}

	struct DmdSignatureReader : IDisposable {
		public static (DmdType type, bool containedGenericParams) ReadTypeSignature(DmdModule module, DmdSignatureStream reader, IList<DmdType> genericTypeArguments) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, null)) {
					var type = sigReader.ReadType().type;
					return (type, sigReader.containedGenericParams);
				}
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			return (module.AppDomain.System_Void, false);
		}

		const int MAX_RECURSION_COUNT = 100;
		int recursionCounter;
		readonly DmdModule module;
		readonly DmdSignatureStream reader;
		readonly IList<DmdType> genericTypeArguments;
		readonly IList<DmdType> genericMethodArguments;
		List<DmdCustomModifier> customModifiers;
		bool containedGenericParams;

		DmdSignatureReader(DmdModule module, DmdSignatureStream reader, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			this.module = module;
			this.reader = reader;
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
			this.genericMethodArguments = genericMethodArguments ?? Array.Empty<DmdType>();
			customModifiers = null;
			containedGenericParams = false;
			recursionCounter = 0;
		}

		bool IncrementRecursionCounter() {
			if (recursionCounter >= MAX_RECURSION_COUNT)
				return false;
			recursionCounter++;
			return true;
		}
		void DecrementRecursionCounter() => recursionCounter--;

		void AddCustomModifier(DmdType type, bool isRequired) {
			if (customModifiers == null)
				customModifiers = new List<DmdCustomModifier>();
			customModifiers.Add(new DmdCustomModifier(type, isRequired));
		}

		DmdCustomModifier[] GetCustomModifiers() {
			var customModifiers = this.customModifiers;
			if (customModifiers == null || customModifiers.Count == 0)
				return Array.Empty<DmdCustomModifier>();
			var res = customModifiers.ToArray();
			customModifiers.Clear();
			return res;
		}

		[Flags]
		enum TypeFlags {
			None				= 0,
			IsSentinel			= 0x00000001,
			IsPinned			= 0x00000002,
		}

		(DmdType type, TypeFlags flags) ReadType() {
			if (!IncrementRecursionCounter())
				return (module.AppDomain.System_Void, TypeFlags.None);

			uint num;
			DmdType nextType, result;
			TypeFlags resultFlags = TypeFlags.None;
			var et = (DMD.ElementType)reader.ReadByte();
			switch (et) {
			case DMD.ElementType.Void:		result = module.AppDomain.System_Void; break;
			case DMD.ElementType.Boolean:	result = module.AppDomain.System_Boolean; break;
			case DMD.ElementType.Char:		result = module.AppDomain.System_Char; break;
			case DMD.ElementType.I1:		result = module.AppDomain.System_SByte; break;
			case DMD.ElementType.U1:		result = module.AppDomain.System_Byte; break;
			case DMD.ElementType.I2:		result = module.AppDomain.System_Int16; break;
			case DMD.ElementType.U2:		result = module.AppDomain.System_UInt16; break;
			case DMD.ElementType.I4:		result = module.AppDomain.System_Int32; break;
			case DMD.ElementType.U4:		result = module.AppDomain.System_UInt32; break;
			case DMD.ElementType.I8:		result = module.AppDomain.System_Int64; break;
			case DMD.ElementType.U8:		result = module.AppDomain.System_UInt64; break;
			case DMD.ElementType.R4:		result = module.AppDomain.System_Single; break;
			case DMD.ElementType.R8:		result = module.AppDomain.System_Double; break;
			case DMD.ElementType.String:	result = module.AppDomain.System_String; break;
			case DMD.ElementType.TypedByRef:result = module.AppDomain.System_TypedReference; break;
			case DMD.ElementType.I:			result = module.AppDomain.System_IntPtr; break;
			case DMD.ElementType.U:			result = module.AppDomain.System_UIntPtr; break;
			case DMD.ElementType.Object:	result = module.AppDomain.System_Object; break;

			case DMD.ElementType.Ptr:		result = module.AppDomain.MakePointerType(ReadType().type, null); break;
			case DMD.ElementType.ByRef:		result = module.AppDomain.MakeByRefType(ReadType().type, null); break;
			case DMD.ElementType.ValueType:	result = ReadTypeDefOrRef(); break;
			case DMD.ElementType.Class:		result = ReadTypeDefOrRef(); break;
			case DMD.ElementType.FnPtr:		result = ReadMethodSignatureType(); break;
			case DMD.ElementType.SZArray:	result = module.AppDomain.MakeArrayType(ReadType().type, null); break;

			case DMD.ElementType.Pinned:
				resultFlags |= TypeFlags.IsPinned;
				result = ReadType().type;
				break;

			case DMD.ElementType.Sentinel:
				resultFlags |= TypeFlags.IsSentinel;
				result = module.AppDomain.System_Void;
				break;

			case DMD.ElementType.CModReqd:
			case DMD.ElementType.CModOpt:
				AddCustomModifier(ReadTypeDefOrRef(), isRequired: et == DMD.ElementType.CModReqd);
				result = ReadType().type;
				var customModifiers = GetCustomModifiers();
				if (customModifiers.Length != 0)
					result = result.WithCustomModifiers(customModifiers);
				break;

			case DMD.ElementType.Var:
				containedGenericParams = true;
				num = reader.ReadCompressedUInt32();
				result = num >= (uint)genericTypeArguments.Count ? module.AppDomain.System_Void : genericTypeArguments[(int)num];
				break;

			case DMD.ElementType.MVar:
				containedGenericParams = true;
				num = reader.ReadCompressedUInt32();
				result = num >= (uint)genericMethodArguments.Count ? module.AppDomain.System_Void : genericMethodArguments[(int)num];
				break;

			case DMD.ElementType.GenericInst:
				nextType = ReadType().type;
				if (!(nextType is DmdTypeDef || nextType is DmdTypeRef))
					throw new IOException();
				var args = new DmdType[reader.ReadCompressedUInt32()];
				for (int i = 0; i < args.Length; i++)
					args[i] = ReadType().type;
				result = module.AppDomain.MakeGenericType(nextType, args, null);
				break;

			case DMD.ElementType.Array:
				nextType = ReadType().type;
				uint rank = reader.ReadCompressedUInt32();
				if (rank == 0) {
					result = module.AppDomain.MakeArrayType(nextType, (int)rank, Array.Empty<int>(), Array.Empty<int>(), null);
					break;
				}
				var sizes = new int[(int)reader.ReadCompressedUInt32()];
				for (int i = 0; i < sizes.Length; i++)
					sizes[i] = (int)reader.ReadCompressedUInt32();
				var lowerBounds = new int[(int)reader.ReadCompressedUInt32()];
				for (int i = 0; i < lowerBounds.Length; i++)
					lowerBounds[i] = reader.ReadCompressedInt32();
				result = module.AppDomain.MakeArrayType(nextType, (int)rank, sizes, lowerBounds, null);
				break;

			case DMD.ElementType.ValueArray:
			case DMD.ElementType.Module:
			case DMD.ElementType.Internal:
			case DMD.ElementType.End:
			case DMD.ElementType.R:
			default:
				result = module.AppDomain.System_Void;
				break;
			}

			DecrementRecursionCounter();
			return (result, resultFlags);
		}

		DmdType ReadTypeDefOrRef() {
			uint codedToken;
			codedToken = reader.ReadCompressedUInt32();
			if (!DMD.MD.CodedToken.TypeDefOrRef.Decode(codedToken, out uint token))
				return module.AppDomain.System_Void;
			// Assume it contains generic parameters if it's a TypeSpec
			if ((token >> 24) == 0x1B)
				containedGenericParams = true;
			return module.ResolveType((int)token, genericTypeArguments, null, throwOnError: false) ?? module.AppDomain.System_Void;
		}

		DmdType ReadMethodSignatureType() {
			var origCustomModifiers = customModifiers;
			customModifiers = null;

			ReadMethodSignature(out var flags, out var genericParameterCount, out var returnType, out var parameterTypes, out var varArgsParameterTypes);
			var fnPtrType = module.AppDomain.MakeFunctionPointerType(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes, null);

			customModifiers = origCustomModifiers;
			return fnPtrType;
		}

		void ReadMethodSignature(out DmdSignatureCallingConvention flags, out int genericParameterCount, out DmdType returnType, out IList<DmdType> parameterTypes, out IList<DmdType> varArgsParameterTypes) {
			flags = (DmdSignatureCallingConvention)reader.ReadByte();
			switch (flags & DmdSignatureCallingConvention.Mask) {
			case DmdSignatureCallingConvention.Default:
			case DmdSignatureCallingConvention.C:
			case DmdSignatureCallingConvention.StdCall:
			case DmdSignatureCallingConvention.ThisCall:
			case DmdSignatureCallingConvention.FastCall:
			case DmdSignatureCallingConvention.VarArg:
			case DmdSignatureCallingConvention.NativeVarArg:
				break;

			default:
				throw new IOException();
			}

			if ((flags & DmdSignatureCallingConvention.Generic) != 0)
				genericParameterCount = (int)reader.ReadCompressedUInt32();
			else
				genericParameterCount = 0;

			int numParams = (int)reader.ReadCompressedUInt32();
			returnType = ReadType().type;

			var allParameters = new DmdType[numParams];
			int sentinelIndex = int.MaxValue;
			for (int i = 0; i < allParameters.Length; i++) {
				var (type, typeFlags) = ReadType();
				if ((typeFlags & TypeFlags.IsSentinel) != 0) {
					sentinelIndex = i;
					i--;
				}
				else
					allParameters[i] = type;
			}

			if (sentinelIndex >= allParameters.Length) {
				parameterTypes = allParameters;
				varArgsParameterTypes = Array.Empty<DmdType>();
			}
			else {
				var parameterTypesArray = sentinelIndex == 0 ? Array.Empty<DmdType>() : new DmdType[sentinelIndex];
				var varArgsParameterTypesArray = new DmdType[allParameters.Length - sentinelIndex];
				Array.Copy(allParameters, 0, parameterTypesArray, 0, parameterTypesArray.Length);
				Array.Copy(allParameters, sentinelIndex, varArgsParameterTypesArray, 0, varArgsParameterTypesArray.Length);
				parameterTypes = parameterTypesArray;
				varArgsParameterTypes = varArgsParameterTypesArray;
			}
		}

		public void Dispose() => reader.Dispose();
	}
}
