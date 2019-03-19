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
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	struct ReflectionTypeCreator {
		readonly DbgEngineImpl engine;
		readonly DmdAppDomain reflectionAppDomain;
		List<DmdType> typesList;
		int recursionCounter;

		public ReflectionTypeCreator(DbgEngineImpl engine, DmdAppDomain reflectionAppDomain) {
			this.engine = engine;
			this.reflectionAppDomain = reflectionAppDomain;
			typesList = null;
			recursionCounter = 0;
		}

		List<DmdType> GetTypesList() {
			if (typesList == null)
				return new List<DmdType>();
			var list = typesList;
			typesList = null;
			list.Clear();
			return list;
		}

		void FreeTypesList(ref List<DmdType> list) {
			if (list == null)
				return;
			typesList = list;
			list = null;
		}

		public DmdType Create(CorType type) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (recursionCounter++ > 100)
				throw new InvalidOperationException();

			DmdType result;
			List<DmdType> types;
			switch (type.ElementType) {
			case CorElementType.Void:		result = reflectionAppDomain.System_Void; break;
			case CorElementType.Boolean:	result = reflectionAppDomain.System_Boolean; break;
			case CorElementType.Char:		result = reflectionAppDomain.System_Char; break;
			case CorElementType.I1:			result = reflectionAppDomain.System_SByte; break;
			case CorElementType.U1:			result = reflectionAppDomain.System_Byte; break;
			case CorElementType.I2:			result = reflectionAppDomain.System_Int16; break;
			case CorElementType.U2:			result = reflectionAppDomain.System_UInt16; break;
			case CorElementType.I4:			result = reflectionAppDomain.System_Int32; break;
			case CorElementType.U4:			result = reflectionAppDomain.System_UInt32; break;
			case CorElementType.I8:			result = reflectionAppDomain.System_Int64; break;
			case CorElementType.U8:			result = reflectionAppDomain.System_UInt64; break;
			case CorElementType.R4:			result = reflectionAppDomain.System_Single; break;
			case CorElementType.R8:			result = reflectionAppDomain.System_Double; break;
			case CorElementType.String:		result = reflectionAppDomain.System_String; break;
			case CorElementType.TypedByRef:	result = reflectionAppDomain.System_TypedReference; break;
			case CorElementType.I:			result = reflectionAppDomain.System_IntPtr; break;
			case CorElementType.U:			result = reflectionAppDomain.System_UIntPtr; break;
			case CorElementType.Object:		result = reflectionAppDomain.System_Object; break;

			case CorElementType.Ptr:
				result = Create(type.FirstTypeParameter).MakePointerType();
				break;

			case CorElementType.ByRef:
				result = Create(type.FirstTypeParameter).MakeByRefType();
				break;

			case CorElementType.SZArray:
				result = Create(type.FirstTypeParameter).MakeArrayType();
				break;

			case CorElementType.Array:
				result = Create(type.FirstTypeParameter).MakeArrayType((int)type.Rank);
				break;

			case CorElementType.ValueType:
			case CorElementType.Class:
				var cl = type.Class ?? throw new InvalidOperationException();
				var module = engine.TryGetModule(cl.Module)?.GetReflectionModule() ?? throw new InvalidOperationException();
				var reflectionType = module.ResolveType((int)cl.Token, DmdResolveOptions.ThrowOnError);
				if (reflectionType.GetGenericArguments().Count != 0) {
					types = GetTypesList();
					foreach (var t in type.TypeParameters)
						types.Add(Create(t));
					Debug.Assert(types.Count == 0 || reflectionType.GetGenericArguments().Count == types.Count);
					if (types.Count != 0)
						reflectionType = reflectionType.MakeGenericType(types.ToArray());
					FreeTypesList(ref types);
				}
				result = reflectionType;
				break;

			case CorElementType.FnPtr:
				DmdType returnType = null;
				types = null;
				foreach (var t in type.TypeParameters) {
					if ((object)returnType == null)
						returnType = Create(t);
					else {
						if (types == null)
							types = GetTypesList();
						types.Add(Create(t));
					}
				}
				if ((object)returnType == null)
					throw new InvalidOperationException();
				//TODO: Guessing FnPtr calling convention
				const DmdSignatureCallingConvention fnPtrCallingConvention = DmdSignatureCallingConvention.C;
				//TODO: We're assuming varArgsParameterTypes is empty
				result = reflectionAppDomain.MakeFunctionPointerType(fnPtrCallingConvention, 0, returnType, types?.ToArray() ?? Array.Empty<DmdType>(), Array.Empty<DmdType>(), null);
				FreeTypesList(ref types);
				break;

			case CorElementType.GenericInst:
			case CorElementType.Var:
			case CorElementType.MVar:
			case CorElementType.End:
			case CorElementType.ValueArray:
			case CorElementType.R:
			case CorElementType.CModReqd:
			case CorElementType.CModOpt:
			case CorElementType.Internal:
			case CorElementType.Module:
			case CorElementType.Sentinel:
			case CorElementType.Pinned:
			default:
				Debug.Fail($"Unsupported element type: {type.ElementType}");
				throw new InvalidOperationException();
			}

			recursionCounter--;
			return result;
		}
	}
}
