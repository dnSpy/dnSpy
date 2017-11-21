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
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	struct MonoDebugTypeCreator {
		readonly DbgEngineImpl engine;
		readonly TypeCache typeCache;
		int recursionCounter;

		public static TypeMirror GetType(DbgEngineImpl engine, DmdType type) {
			var typeCache = TypeCache.GetOrCreate(type.AppDomain);
			if (typeCache.TryGetType(type, out var monoType))
				return monoType;

			monoType = new MonoDebugTypeCreator(engine, typeCache).Create(type);
			typeCache.Add(monoType, type);
			return monoType;
		}

		MonoDebugTypeCreator(DbgEngineImpl engine, TypeCache typeCache) {
			this.engine = engine;
			this.typeCache = typeCache;
			recursionCounter = 0;
		}

		public TypeMirror Create(DmdType type) {
			if ((object)type == null)
				throw new ArgumentNullException(nameof(type));

			if (typeCache.TryGetType(type, out var cachedType))
				return cachedType;

			if (recursionCounter++ > 100)
				throw new InvalidOperationException();

			TypeMirror result;
			bool addType = true;
			switch (type.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				if (!engine.TryGetMonoModule(type.Module.GetDebuggerModule() ?? throw new InvalidOperationException(), out var monoModule))
					throw new InvalidOperationException();
				Debug.Assert((type.MetadataToken >> 24) == 0x02);
				//TODO: It's possible to resolve types, but it's an internal method and it requires a method in the module
				result = monoModule.Assembly.GetType(type.FullName, false, false);
				if (result == null)
					throw new InvalidOperationException();
				if (result.MetadataToken != type.MetadataToken)
					throw new InvalidOperationException();
				break;

			case DmdTypeSignatureKind.Pointer:
				result = Create(type.GetElementType());
				result = result.Module.Assembly.GetType(type.FullName, false, false);
				if (result == null)
					throw new InvalidOperationException();
				if (!result.IsPointer)
					throw new InvalidOperationException();
				break;

			case DmdTypeSignatureKind.ByRef:
				result = Create(type.GetElementType());
				result = result.Module.Assembly.GetType(type.FullName, false, false);
				if (result == null)
					throw new InvalidOperationException();
				// This currently always fails
				//TODO: We could func-eval MakeByRefType()
				if (!result.IsByRef)
					throw new InvalidOperationException();
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
				throw new InvalidOperationException();

			case DmdTypeSignatureKind.SZArray:
				result = Create(type.GetElementType());
				result = result.Module.Assembly.GetType(type.FullName, false, false);
				if (result == null)
					throw new InvalidOperationException();
				if (!result.IsArray || result.GetArrayRank() != 1 || !result.FullName.EndsWith("[]", StringComparison.Ordinal))
					throw new InvalidOperationException();
				break;

			case DmdTypeSignatureKind.MDArray:
				result = Create(type.GetElementType());
				result = result.Module.Assembly.GetType(type.FullName, false, false);
				if (result == null)
					throw new InvalidOperationException();
				if (!result.IsArray || (result.GetArrayRank() == 1 && result.FullName.EndsWith("[]", StringComparison.Ordinal)))
					throw new InvalidOperationException();
				break;

			case DmdTypeSignatureKind.GenericInstance:
				result = Create(type.GetGenericTypeDefinition());
				result = result.Module.Assembly.GetType(type.FullName, false, false);
				if (result == null)
					throw new InvalidOperationException();
				if (!result.IsGenericType)
					throw new InvalidOperationException();
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				// It's not possible to create function pointers, so use a pointer type instead
				result = Create(type.AppDomain.System_Void.MakePointerType());
				addType = false;
				break;

			default:
				throw new InvalidOperationException();
			}

			if (result == null)
				throw new InvalidOperationException();
			if (addType)
				typeCache.Add(result, type);

			recursionCounter--;
			return result;
		}
	}
}
