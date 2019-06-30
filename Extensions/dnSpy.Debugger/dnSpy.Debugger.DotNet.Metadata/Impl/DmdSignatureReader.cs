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
using DMD = dnlib.DotNet;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	struct DmdSignatureReader : IDisposable {
		public static (DmdType type, bool containedGenericParams) ReadTypeSignature(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool resolve) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, genericMethodArguments, resolve)) {
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

		public static (DmdType fieldType, bool containedGenericParams) ReadFieldSignature(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, bool resolve) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, null, resolve)) {
					var fieldType = sigReader.ReadFieldSignature();
					return (fieldType, sigReader.containedGenericParams);
				}
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			return (module.AppDomain.System_Void, false);
		}

		public static (DmdMethodSignature methodSignature, bool containedGenericParams) ReadMethodSignature(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool isProperty, bool resolve) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, genericMethodArguments, resolve)) {
					sigReader.ReadMethodSignature(out var flags, out var genericParameterCount, out var returnType, out var parameterTypes, out var varArgsParameterTypes);
					if (((flags & DmdSignatureCallingConvention.Mask) == DmdSignatureCallingConvention.Property) == isProperty) {
						var methodSignature = new DmdMethodSignature(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes);
						return (methodSignature, sigReader.containedGenericParams);
					}
				}
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			var dummySig = new DmdMethodSignature(isProperty ? DmdSignatureCallingConvention.Property : DmdSignatureCallingConvention.Default, 0, module.AppDomain.System_Void, null, null);
			return (dummySig, false);
		}

		public static (DmdType? fieldType, DmdMethodSignature? methodSignature, bool containedGenericParams) ReadMethodSignatureOrFieldType(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool resolve) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, genericMethodArguments, resolve)) {
					var flags = (DmdSignatureCallingConvention)reader.ReadByte();
					if ((flags & DmdSignatureCallingConvention.Mask) == DmdSignatureCallingConvention.Field) {
						var fieldType = sigReader.ReadType().type;
						return (fieldType, null, sigReader.containedGenericParams);
					}
					else {
						sigReader.ReadMethodSignature(flags, out var genericParameterCount, out var returnType, out var parameterTypes, out var varArgsParameterTypes);
						if ((flags & DmdSignatureCallingConvention.Mask) != DmdSignatureCallingConvention.Property) {
							var methodSignature = new DmdMethodSignature(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes);
							return (null, methodSignature, sigReader.containedGenericParams);
						}
					}
				}
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			return (module.AppDomain.System_Void, null, false);
		}

		public static (DmdType[] types, bool containedGenericParams) ReadMethodSpecSignature(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool resolve) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, genericMethodArguments, resolve)) {
					var types = sigReader.ReadInstantiation();
					return (types, sigReader.containedGenericParams);
				}
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			return (Array.Empty<DmdType>(), false);
		}

		public static (DmdType type, bool isPinned)[] ReadLocalsSignature(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool resolve) {
			try {
				using (var sigReader = new DmdSignatureReader(module, reader, genericTypeArguments, genericMethodArguments, resolve))
					return sigReader.ReadLocalsSignature();
			}
			catch (IOException) {
			}
			catch (OutOfMemoryException) {
			}
			return Array.Empty<(DmdType, bool)>();
		}

		const int MAX_RECURSION_COUNT = 100;
		int recursionCounter;
		readonly DmdModule module;
		readonly DmdDataStream reader;
		readonly IList<DmdType> genericTypeArguments;
		readonly IList<DmdType> genericMethodArguments;
		readonly bool resolve;
		List<DmdCustomModifier>? customModifiers;
		bool containedGenericParams;

		DmdSignatureReader(DmdModule module, DmdDataStream reader, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool resolve) {
			this.module = module;
			this.reader = reader;
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
			this.genericMethodArguments = genericMethodArguments ?? Array.Empty<DmdType>();
			this.resolve = resolve;
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
			if (customModifiers is null)
				customModifiers = new List<DmdCustomModifier>();
			customModifiers.Add(new DmdCustomModifier(type, isRequired));
		}

		DmdCustomModifier[] GetCustomModifiers() {
			var customModifiers = this.customModifiers;
			if (customModifiers is null || customModifiers.Count == 0)
				return Array.Empty<DmdCustomModifier>();
			// Reflection reverses the custom modifiers
			customModifiers.Reverse();
			var res = customModifiers.ToArray();
			customModifiers.Clear();
			return res;
		}

		DmdType AddCustomModifiers(DmdCustomModifier[] customModifiers, DmdType type) {
			if (customModifiers.Length == 0)
				return type;
			return type.WithCustomModifiers(customModifiers);
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
			case DMD.ElementType.Void:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Void); break;
			case DMD.ElementType.Boolean:	result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Boolean); break;
			case DMD.ElementType.Char:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Char); break;
			case DMD.ElementType.I1:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_SByte); break;
			case DMD.ElementType.U1:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Byte); break;
			case DMD.ElementType.I2:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Int16); break;
			case DMD.ElementType.U2:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_UInt16); break;
			case DMD.ElementType.I4:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Int32); break;
			case DMD.ElementType.U4:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_UInt32); break;
			case DMD.ElementType.I8:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Int64); break;
			case DMD.ElementType.U8:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_UInt64); break;
			case DMD.ElementType.R4:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Single); break;
			case DMD.ElementType.R8:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Double); break;
			case DMD.ElementType.String:	result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_String); break;
			case DMD.ElementType.TypedByRef:result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_TypedReference); break;
			case DMD.ElementType.I:			result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_IntPtr); break;
			case DMD.ElementType.U:			result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_UIntPtr); break;
			case DMD.ElementType.Object:	result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Object); break;

			case DMD.ElementType.Ptr:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.MakePointerType(ReadType().type, null, DmdMakeTypeOptions.NoResolve)); break;
			case DMD.ElementType.ByRef:		result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.MakeByRefType(ReadType().type, null, DmdMakeTypeOptions.NoResolve)); break;
			case DMD.ElementType.ValueType:	result = AddCustomModifiers(GetCustomModifiers(), ReadTypeDefOrRef()); break;
			case DMD.ElementType.Class:		result = AddCustomModifiers(GetCustomModifiers(), ReadTypeDefOrRef()); break;
			case DMD.ElementType.FnPtr:		result = AddCustomModifiers(GetCustomModifiers(), ReadMethodSignatureType()); break;
			case DMD.ElementType.SZArray:	result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.MakeArrayType(ReadType().type, null, DmdMakeTypeOptions.NoResolve)); break;

			case DMD.ElementType.Pinned:
				resultFlags |= TypeFlags.IsPinned;
				result = AddCustomModifiers(GetCustomModifiers(), ReadType().type);
				break;

			case DMD.ElementType.Sentinel:
				resultFlags |= TypeFlags.IsSentinel;
				result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Void);
				break;

			case DMD.ElementType.CModReqd:
			case DMD.ElementType.CModOpt:
				AddCustomModifier(ReadTypeDefOrRef(), isRequired: et == DMD.ElementType.CModReqd);
				(result, resultFlags) = ReadType();
				break;

			case DMD.ElementType.Var:
				containedGenericParams = true;
				num = reader.ReadCompressedUInt32();
				result = num >= (uint)genericTypeArguments.Count ? new DmdCreatedGenericParameterType(module, true, (int)num, null) : genericTypeArguments[(int)num];
				result = AddCustomModifiers(GetCustomModifiers(), result);
				break;

			case DMD.ElementType.MVar:
				containedGenericParams = true;
				num = reader.ReadCompressedUInt32();
				result = num >= (uint)genericMethodArguments.Count ? new DmdCreatedGenericParameterType(module, false, (int)num, null) : genericMethodArguments[(int)num];
				result = AddCustomModifiers(GetCustomModifiers(), result);
				break;

			case DMD.ElementType.GenericInst:
				var customModifiers = GetCustomModifiers();
				nextType = ReadType().type;
				if (!(nextType is DmdTypeDef || nextType is DmdTypeRef))
					throw new IOException();
				var args = new DmdType[reader.ReadCompressedUInt32()];
				for (int i = 0; i < args.Length; i++)
					args[i] = ReadType().type;
				result = AddCustomModifiers(customModifiers, module.AppDomain.MakeGenericType(nextType, args, null, DmdMakeTypeOptions.NoResolve));
				break;

			case DMD.ElementType.Array:
				nextType = ReadType().type;
				uint rank = reader.ReadCompressedUInt32();
				if (rank == 0) {
					result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.MakeArrayType(nextType, (int)rank, Array.Empty<int>(), Array.Empty<int>(), null, DmdMakeTypeOptions.NoResolve));
					break;
				}
				var sizes = new int[(int)reader.ReadCompressedUInt32()];
				for (int i = 0; i < sizes.Length; i++)
					sizes[i] = (int)reader.ReadCompressedUInt32();
				var lowerBounds = new int[(int)reader.ReadCompressedUInt32()];
				for (int i = 0; i < lowerBounds.Length; i++)
					lowerBounds[i] = reader.ReadCompressedInt32();
				result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.MakeArrayType(nextType, (int)rank, sizes, lowerBounds, null, DmdMakeTypeOptions.NoResolve));
				break;

			case DMD.ElementType.ValueArray:
			case DMD.ElementType.Module:
			case DMD.ElementType.Internal:
			case DMD.ElementType.End:
			case DMD.ElementType.R:
			default:
				result = AddCustomModifiers(GetCustomModifiers(), module.AppDomain.System_Void);
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
			if ((token >> 24) == 0x1B)
				containedGenericParams = true;
			var type = module.ResolveType((int)token, genericTypeArguments, null, DmdResolveOptions.None) ?? module.AppDomain.System_Void;
			if (resolve)
				return type.ResolveNoThrow() ?? type;
			return type;
		}

		DmdType ReadMethodSignatureType() {
			ReadMethodSignature(out var flags, out var genericParameterCount, out var returnType, out var parameterTypes, out var varArgsParameterTypes);
			if ((flags & DmdSignatureCallingConvention.Mask) == DmdSignatureCallingConvention.Property)
				throw new IOException();
			return module.AppDomain.MakeFunctionPointerType(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes, null, resolve ? DmdMakeTypeOptions.None : DmdMakeTypeOptions.NoResolve);
		}

		void ReadMethodSignature(out DmdSignatureCallingConvention flags, out int genericParameterCount, out DmdType returnType, out IList<DmdType> parameterTypes, out IList<DmdType> varArgsParameterTypes) {
			flags = (DmdSignatureCallingConvention)reader.ReadByte();
			ReadMethodSignature(flags, out genericParameterCount, out returnType, out parameterTypes, out varArgsParameterTypes);
		}

		void ReadMethodSignature(DmdSignatureCallingConvention flags, out int genericParameterCount, out DmdType returnType, out IList<DmdType> parameterTypes, out IList<DmdType> varArgsParameterTypes) {
			switch (flags & DmdSignatureCallingConvention.Mask) {
			case DmdSignatureCallingConvention.Default:
			case DmdSignatureCallingConvention.C:
			case DmdSignatureCallingConvention.StdCall:
			case DmdSignatureCallingConvention.ThisCall:
			case DmdSignatureCallingConvention.FastCall:
			case DmdSignatureCallingConvention.VarArg:
			case DmdSignatureCallingConvention.NativeVarArg:
			case DmdSignatureCallingConvention.Property:
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

		DmdType ReadFieldSignature() {
			var flags = (DmdSignatureCallingConvention)reader.ReadByte();
			if ((flags & DmdSignatureCallingConvention.Mask) != DmdSignatureCallingConvention.Field)
				return module.AppDomain.System_Void;
			return ReadType().type;
		}

		DmdType[] ReadInstantiation() {
			var flags = (DmdSignatureCallingConvention)reader.ReadByte();
			if ((flags & DmdSignatureCallingConvention.Mask) != DmdSignatureCallingConvention.GenericInst)
				return Array.Empty<DmdType>();
			int args = (int)reader.ReadCompressedUInt32();
			var res = new DmdType[args];
			for (int i = 0; i < res.Length; i++)
				res[i] = ReadType().type;
			return res;
		}

		(DmdType type, bool isPinned)[] ReadLocalsSignature() {
			var flags = (DmdSignatureCallingConvention)reader.ReadByte();
			if ((flags & DmdSignatureCallingConvention.Mask) != DmdSignatureCallingConvention.LocalSig)
				return Array.Empty<(DmdType, bool)>();
			int count = (int)reader.ReadCompressedUInt32();
			var res = new(DmdType, bool)[count];
			for (int i = 0; i < res.Length; i++) {
				var info = ReadType();
				res[i] = (info.type, (info.flags & TypeFlags.IsPinned) != 0);
			}
			return res;
		}

		public void Dispose() => reader.Dispose();
	}
}
