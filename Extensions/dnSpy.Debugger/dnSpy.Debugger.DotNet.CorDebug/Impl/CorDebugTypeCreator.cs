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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	struct CorDebugTypeCreator {
		readonly DbgEngineImpl engine;
		readonly CorAppDomain appDomain;
		int recursionCounter;

		public static CorType GetType(DbgEngineImpl engine, CorAppDomain appDomain, DmdType type) {
			if (type.TryGetData(out CorType corType))
				return corType;
			return GetTypeCore(engine, appDomain, type);

			CorType GetTypeCore(DbgEngineImpl engine2, CorAppDomain appDomain2, DmdType type2) => type2.GetOrCreateData(() => new CorDebugTypeCreator(engine2, appDomain2).Create(type2));
		}

		CorDebugTypeCreator(DbgEngineImpl engine, CorAppDomain appDomain) {
			this.engine = engine;
			this.appDomain = appDomain;
			recursionCounter = 0;
		}

		public CorType Create(DmdType type) {
			if ((object)type == null)
				throw new ArgumentNullException(nameof(type));
			if (recursionCounter++ > 100)
				throw new InvalidOperationException();

			CorType result;
			int i;
			ReadOnlyCollection<DmdType> types;
			CorType[] corTypes;
			DnModule dnModule;
			switch (type.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				if (!engine.TryGetDnModule(type.Module.GetDebuggerModule() ?? throw new InvalidOperationException(), out dnModule))
					throw new InvalidOperationException();
				Debug.Assert((type.MetadataToken >> 24) == 0x02);
				result = dnModule.CorModule.GetClassFromToken((uint)type.MetadataToken).GetParameterizedType(type.IsValueType ? CorElementType.ValueType : CorElementType.Class);
				break;

			case DmdTypeSignatureKind.Pointer:
				result = Create(type.GetElementType());
				result = appDomain.GetPtr(result);
				break;

			case DmdTypeSignatureKind.ByRef:
				result = Create(type.GetElementType());
				result = appDomain.GetByRef(result);
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
				throw new InvalidOperationException();

			case DmdTypeSignatureKind.SZArray:
				result = Create(type.GetElementType());
				result = appDomain.GetSZArray(result);
				break;

			case DmdTypeSignatureKind.MDArray:
				result = Create(type.GetElementType());
				result = appDomain.GetArray(result, (uint)type.GetArrayRank());
				break;

			case DmdTypeSignatureKind.GenericInstance:
				result = Create(type.GetGenericTypeDefinition());
				types = type.GetGenericArguments();
				corTypes = new CorType[types.Count];
				for (i = 0; i < corTypes.Length; i++)
					corTypes[i] = Create(types[i]);
				result = result.Class.GetParameterizedType(type.IsValueType ? CorElementType.ValueType : CorElementType.Class, corTypes);
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				var methodSig = type.GetFunctionPointerMethodSignature();
				types = methodSig.GetParameterTypes();
				corTypes = new CorType[1 + types.Count + methodSig.GetVarArgsParameterTypes().Count];
				corTypes[0] = Create(methodSig.ReturnType);
				for (i = 0; i < types.Count; i++)
					corTypes[i + 1] = Create(types[i]);
				types = methodSig.GetVarArgsParameterTypes();
				for (i = 0; i < types.Count; i++)
					corTypes[i + 1 + methodSig.GetParameterTypes().Count] = Create(types[i]);
				result = appDomain.GetFnPtr(corTypes);
				break;

			default:
				throw new InvalidOperationException();
			}

			if (result == null)
				throw new InvalidOperationException();

			recursionCounter--;
			return result;
		}
	}
}
