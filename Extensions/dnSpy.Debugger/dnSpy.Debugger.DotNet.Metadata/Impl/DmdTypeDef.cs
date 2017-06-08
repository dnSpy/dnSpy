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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeDef : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public override DmdTypeScope TypeScope => new DmdTypeScope(Module);
		public override bool IsMetadataReference => false;
		public override bool IsGenericType => GetOrCreateGenericParameters().Count != 0;
		public override bool IsGenericTypeDefinition => GetOrCreateGenericParameters().Count != 0;
		public override int MetadataToken => (int)(0x02000000 + rid);
		public override StructLayoutAttribute StructLayoutAttribute => throw new NotImplementedException();//TODO:

		public override DmdType DeclaringType {
			get {
				if (!declaringTypeInitd) {
					lock (LockObject) {
						if (!declaringTypeInitd) {
							int declTypeToken = GetDeclaringTypeToken();
							if ((declTypeToken & 0x00FFFFFF) == 0)
								__declaringType_DONT_USE = null;
							else {
								if (((uint)declTypeToken >> 24) != 0x02)
									throw new InvalidOperationException();
								__declaringType_DONT_USE = Module.ResolveType(declTypeToken, null, null, throwOnError: false);
							}
							declaringTypeInitd = true;
						}
					}
				}
				return __declaringType_DONT_USE;
			}
		}
		DmdType __declaringType_DONT_USE;
		bool declaringTypeInitd;

		public override DmdType BaseType {
			get {
				if (!baseTypeInitd) {
					lock (LockObject) {
						if (!baseTypeInitd) {
							int baseTypeToken = GetBaseTypeToken();
							if ((baseTypeToken & 0x00FFFFFF) == 0)
								__baseType_DONT_USE = null;
							else
								__baseType_DONT_USE = Module.ResolveType(baseTypeToken, GetOrCreateGenericParameters(), null, throwOnError: false);
							baseTypeInitd = true;
						}
					}
				}
				return __baseType_DONT_USE;
			}
		}
		DmdType __baseType_DONT_USE;
		bool baseTypeInitd;

		protected abstract int GetDeclaringTypeToken();
		protected abstract int GetBaseTypeTokenCore();

		public int GetBaseTypeToken() {
			if (baseTypeToken == -1) {
				baseTypeToken = GetBaseTypeTokenCore();
				Debug.Assert(baseTypeToken != -1);
			}
			return baseTypeToken;
		}
		int baseTypeToken = -1;

		protected abstract DmdType[] CreateGenericParameters_NoLock();
		ReadOnlyCollection<DmdType> GetOrCreateGenericParameters() {
			if (__genericParameters_DONT_USE != null)
				return __genericParameters_DONT_USE;
			lock (LockObject) {
				if (__genericParameters_DONT_USE != null)
					return __genericParameters_DONT_USE;
				var res = CreateGenericParameters_NoLock();
				__genericParameters_DONT_USE = res == null || res.Length == 0 ? emptyTypeCollection : new ReadOnlyCollection<DmdType>(res);
				return __genericParameters_DONT_USE;
			}
		}
		ReadOnlyCollection<DmdType> __genericParameters_DONT_USE;

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdTypeDef(uint rid, IList<DmdCustomModifier> customModifiers) : base(customModifiers) => this.rid = rid;

		protected override DmdType ResolveNoThrowCore() => this;
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments() => GetOrCreateGenericParameters();
		public override DmdType GetGenericTypeDefinition() => IsGenericType ? this : throw new InvalidOperationException();

		public abstract DmdFieldInfo[] ReadDeclaredFields(DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdMethodBase[] ReadDeclaredMethods(DmdType reflectedType, IList<DmdType> genericTypeArguments, bool includeConstructors);
		public abstract DmdPropertyInfo[] ReadDeclaredProperties(DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdEventInfo[] ReadDeclaredEvents(DmdType reflectedType, IList<DmdType> genericTypeArguments);

		protected sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => ReadDeclaredFields(reflectedType, GetReadOnlyGenericArguments());
		protected sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType, bool includeConstructors) => ReadDeclaredMethods(reflectedType, GetReadOnlyGenericArguments(), includeConstructors);
		protected sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => ReadDeclaredProperties(reflectedType, GetReadOnlyGenericArguments());
		protected sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => ReadDeclaredEvents(reflectedType, GetReadOnlyGenericArguments());

		internal DmdFieldInfo[] CreateDeclaredFields2(DmdType reflectedType) => ReadDeclaredFields(reflectedType, GetReadOnlyGenericArguments());
		internal DmdMethodBase[] CreateDeclaredMethods2(DmdType reflectedType, bool includeConstructors) => ReadDeclaredMethods(reflectedType, GetReadOnlyGenericArguments(), includeConstructors);
		internal DmdPropertyInfo[] CreateDeclaredProperties2(DmdType reflectedType) => ReadDeclaredProperties(reflectedType, GetReadOnlyGenericArguments());
		internal DmdEventInfo[] CreateDeclaredEvents2(DmdType reflectedType) => ReadDeclaredEvents(reflectedType, GetReadOnlyGenericArguments());

		public override bool IsFullyResolved => true;
		public override DmdTypeBase FullResolve() => this;
	}
}
