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
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;
using SD = System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	struct ReflectionTypeCreator {
		readonly DbgEngineImpl engine;
		readonly DmdAppDomain reflectionAppDomain;
		readonly TypeCache typeCache;
		List<DmdType> typesList;
		int recursionCounter;

		sealed class TypeCache {
			readonly Dictionary<TypeMirror, DmdType> dict = new Dictionary<TypeMirror, DmdType>();
			public bool TryGetType(TypeMirror monoType, out DmdType type) => dict.TryGetValue(monoType, out type);
			public void Add(TypeMirror monoType, DmdType reflectionType) => dict.Add(monoType, reflectionType);
		}

		public ReflectionTypeCreator(DbgEngineImpl engine, DmdAppDomain reflectionAppDomain) {
			SD.Debug.Assert(engine.CheckMonoDebugThread());
			this.engine = engine;
			this.reflectionAppDomain = reflectionAppDomain;
			typeCache = GetOrCreateTypeCache(reflectionAppDomain);
			typesList = null;
			recursionCounter = 0;
		}

		static TypeCache GetOrCreateTypeCache(DmdAppDomain reflectionAppDomain) {
			if (reflectionAppDomain.TryGetData(out TypeCache typeCache))
				return typeCache;
			return GetOrCreateTypeCacheCore(reflectionAppDomain);

			TypeCache GetOrCreateTypeCacheCore(DmdAppDomain reflectionAppDomain2) =>
				reflectionAppDomain2.GetOrCreateData(() => new TypeCache());
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

		public DmdType Create(TypeMirror type) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (recursionCounter++ > 100)
				throw new InvalidOperationException();

			List<DmdType> types;
			if (!typeCache.TryGetType(type, out var result)) {
				if (type.IsByRef)
					result = Create(type.GetElementType()).MakeByRefType();
				else if (type.IsArray) {
					if (type.GetArrayRank() == 1) {
						if (type.FullName.EndsWith("[*]", StringComparison.Ordinal))
							result = Create(type.GetElementType()).MakeArrayType(1);
						else
							result = Create(type.GetElementType()).MakeArrayType();
					}
					else
						result = Create(type.GetElementType()).MakeArrayType(type.GetArrayRank());
				}
				else if (type.IsPointer)
					result = Create(type.GetElementType()).MakePointerType();
				else {
					var module = engine.TryGetModule(type.Module)?.GetReflectionModule() ?? throw new InvalidOperationException();
					var reflectionType = module.ResolveType(type.MetadataToken, DmdResolveOptions.ThrowOnError);
					if (reflectionType.GetGenericArguments().Count != 0) {
						TypeMirror[] genericArgs;
						if (type.VirtualMachine.Version.AtLeast(2, 15))
							genericArgs = type.GetGenericArguments();
						else {
							SD.Debug.Fail("Old version doesn't support generics");
							genericArgs = Array.Empty<TypeMirror>();
						}

						types = GetTypesList();
						foreach (var t in genericArgs)
							types.Add(Create(t));
						SD.Debug.Assert(types.Count == 0 || reflectionType.GetGenericArguments().Count == types.Count);
						if (types.Count != 0)
							reflectionType = reflectionType.MakeGenericType(types.ToArray());
						FreeTypesList(ref types);
					}
					result = reflectionType;
				}
				typeCache.Add(type, result);
			}

			recursionCounter--;
			return result;
		}
	}
}
