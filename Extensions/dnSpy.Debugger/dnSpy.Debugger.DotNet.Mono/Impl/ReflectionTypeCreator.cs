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
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	struct ReflectionTypeCreator {
		readonly DbgEngineImpl engine;
		readonly DmdAppDomain reflectionAppDomain;
		readonly TypeCache typeCache;
		List<DmdType>? typesList;
		int recursionCounter;

		public ReflectionTypeCreator(DbgEngineImpl engine, DmdAppDomain reflectionAppDomain) {
			Debug.Assert(engine.CheckMonoDebugThread());
			this.engine = engine;
			this.reflectionAppDomain = reflectionAppDomain;
			typeCache = TypeCache.GetOrCreate(reflectionAppDomain);
			typesList = null;
			recursionCounter = 0;
		}

		List<DmdType> GetTypesList() {
			if (typesList is null)
				return new List<DmdType>();
			var list = typesList;
			typesList = null;
			list.Clear();
			return list;
		}

		void FreeTypesList(ref List<DmdType>? list) {
			if (list is null)
				return;
			typesList = list;
			list = null;
		}

		public DmdType Create(TypeMirror type) {
			var result = CreateCore(type);
			if (result is null)
				throw new InvalidOperationException();
			return result;
		}

		DmdType? CreateCore(TypeMirror type) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			if (recursionCounter++ > 100)
				throw new InvalidOperationException();

			List<DmdType>? types;
			DmdType? result;
			if (!typeCache.TryGetType(type, out result)) {
				bool canAddType = true;
				if (type.IsByRef)
					result = CreateCore(type.GetElementType())?.MakeByRefType();
				else if (type.IsArray) {
					if (type.GetArrayRank() == 1) {
						if (type.FullName.EndsWith("[*]", StringComparison.Ordinal))
							result = CreateCore(type.GetElementType())?.MakeArrayType(1);
						else
							result = CreateCore(type.GetElementType())?.MakeArrayType();
					}
					else
						result = CreateCore(type.GetElementType())?.MakeArrayType(type.GetArrayRank());
				}
				else if (type.IsPointer)
					result = CreateCore(type.GetElementType())?.MakePointerType();
				else {
					var module = engine.TryGetModule(type.Module)?.GetReflectionModule() ?? throw new InvalidOperationException();
					var reflectionType = module.ResolveType(type.MetadataToken, DmdResolveOptions.None);
					if (reflectionType is not null && reflectionType.GetGenericArguments().Count != 0) {
						DmdType? parsedType = null;
						TypeMirror[] genericArgs;
						if (type.VirtualMachine.Version.AtLeast(2, 15))
							genericArgs = type.GetGenericArguments();
						else {
							parsedType = reflectionType.Assembly.GetType(type.FullName);
							if (parsedType is not null && parsedType.MetadataToken != type.MetadataToken)
								parsedType = null;
							genericArgs = Array.Empty<TypeMirror>();
							canAddType = parsedType is not null;
						}

						if (parsedType is not null)
							reflectionType = parsedType;
						else {
							types = GetTypesList();
							foreach (var t in genericArgs) {
								var newType = CreateCore(t);
								if (newType is null) {
									reflectionType = null;
									break;
								}
								types.Add(newType);
							}
							if (reflectionType is not null) {
								Debug.Assert(types.Count == 0 || reflectionType.GetGenericArguments().Count == types.Count);
								if (types.Count != 0)
									reflectionType = reflectionType.MakeGenericType(types.ToArray());
							}
							FreeTypesList(ref types);
						}
					}
					result = reflectionType;
				}
				if (canAddType && result is not null)
					typeCache.Add(type, result);
			}

			recursionCounter--;
			return result;
		}
	}
}
