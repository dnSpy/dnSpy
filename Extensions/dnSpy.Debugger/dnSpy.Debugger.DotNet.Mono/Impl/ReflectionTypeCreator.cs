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
		readonly SpecialTypes specialTypes;
		List<DmdType> typesList;
		int recursionCounter;

		sealed class SpecialTypes {
			readonly Dictionary<TypeMirror, DmdType> dict;

			public SpecialTypes(DmdAppDomain reflectionAppDomain) {
				var monoAppDomain = DbgMonoDebugInternalAppDomainImpl.GetAppDomainMirror(reflectionAppDomain);
				var asm = monoAppDomain.Corlib;
				const int LEN = 18;
				dict = new Dictionary<TypeMirror, DmdType>(LEN);
				Add(asm, "System.Void", reflectionAppDomain.System_Void);
				Add(asm, "System.Boolean", reflectionAppDomain.System_Boolean);
				Add(asm, "System.Char", reflectionAppDomain.System_Char);
				Add(asm, "System.SByte", reflectionAppDomain.System_SByte);
				Add(asm, "System.Byte", reflectionAppDomain.System_Byte);
				Add(asm, "System.Int16", reflectionAppDomain.System_Int16);
				Add(asm, "System.UInt16", reflectionAppDomain.System_UInt16);
				Add(asm, "System.Int32", reflectionAppDomain.System_Int32);
				Add(asm, "System.UInt32", reflectionAppDomain.System_UInt32);
				Add(asm, "System.Int64", reflectionAppDomain.System_Int64);
				Add(asm, "System.UInt64", reflectionAppDomain.System_UInt64);
				Add(asm, "System.Single", reflectionAppDomain.System_Single);
				Add(asm, "System.Double", reflectionAppDomain.System_Double);
				Add(asm, "System.String", reflectionAppDomain.System_String);
				Add(asm, "System.TypedReference", reflectionAppDomain.System_TypedReference);
				Add(asm, "System.IntPtr", reflectionAppDomain.System_IntPtr);
				Add(asm, "System.UIntPtr", reflectionAppDomain.System_UIntPtr);
				Add(asm, "System.Object", reflectionAppDomain.System_Object);
				SD.Debug.Assert(dict.Count == LEN);
			}

			void Add(AssemblyMirror asm, string fullname, DmdType reflectionType) {
				var monoType = asm.GetType(fullname, false, false);
				if (monoType == null)
					throw new InvalidOperationException();
				dict.Add(monoType, reflectionType);
			}

			public bool TryGetType(TypeMirror monoType, out DmdType type) => dict.TryGetValue(monoType, out type);
		}

		public ReflectionTypeCreator(DbgEngineImpl engine, DmdAppDomain reflectionAppDomain) {
			this.engine = engine;
			this.reflectionAppDomain = reflectionAppDomain;
			specialTypes = GetOrCreateSpecialTypes(reflectionAppDomain);
			typesList = null;
			recursionCounter = 0;
		}

		static SpecialTypes GetOrCreateSpecialTypes(DmdAppDomain reflectionAppDomain) {
			if (reflectionAppDomain.TryGetData(out SpecialTypes specialTypes))
				return specialTypes;
			return GetOrCreateSpecialTypesCore(reflectionAppDomain);

			SpecialTypes GetOrCreateSpecialTypesCore(DmdAppDomain reflectionAppDomain2) =>
				reflectionAppDomain2.GetOrCreateData(() => new SpecialTypes(reflectionAppDomain2));
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
			if (!specialTypes.TryGetType(type, out var result)) {
				if (type.IsByRef)
					result = Create(type.GetElementType()).MakeByRefType();
				else if (type.IsArray) {
					if (type.GetArrayRank() == 1) {
						//TODO: Verify that this works
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
			}

			recursionCounter--;
			return result;
		}
	}
}
