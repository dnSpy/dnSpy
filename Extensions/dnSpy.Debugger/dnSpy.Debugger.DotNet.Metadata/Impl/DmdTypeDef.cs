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
using System.Linq;
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
		public override DmdType DeclaringType => throw new NotImplementedException();//TODO:

		public override DmdType BaseType {
			get {
				if (!baseTypeInitd) {
					lock (LockObject) {
						if (!baseTypeInitd) {
							baseType = Module.ResolveType(GetBaseTypeToken(), GetOrCreateGenericParameters().ToArray(), null, throwOnError: false);
							baseTypeInitd = true;
						}
					}
				}
				return baseType;
			}
		}
		DmdType baseType;
		bool baseTypeInitd;

		protected abstract int GetBaseTypeToken();

		protected abstract DmdType[] CreateGenericParameters_NoLock();
		ReadOnlyCollection<DmdType> GetOrCreateGenericParameters() {
			if (__genericParameters_DONT_USE != null)
				return __genericParameters_DONT_USE;
			lock (LockObject) {
				if (__genericParameters_DONT_USE != null)
					return __genericParameters_DONT_USE;
				var res = CreateGenericParameters_NoLock();
				__genericParameters_DONT_USE = res == null || res.Length == 0 ? emptyReadOnlyCollection : new ReadOnlyCollection<DmdType>(res);
				return __genericParameters_DONT_USE;
			}
		}
		ReadOnlyCollection<DmdType> __genericParameters_DONT_USE;

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdTypeDef(uint rid) => this.rid = rid;

		public override DmdType Resolve(bool throwOnError) => this;
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments() => GetOrCreateGenericParameters();
		public override DmdType GetGenericTypeDefinition() => IsGenericType ? this : throw new InvalidOperationException();

		public abstract DmdFieldInfo[] ReadDeclaredFields(DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdMethodBase[] ReadDeclaredMethods(DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdPropertyInfo[] ReadDeclaredProperties(DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdEventInfo[] ReadDeclaredEvents(DmdType reflectedType, IList<DmdType> genericTypeArguments);

		protected sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => ReadDeclaredFields(reflectedType, GetReadOnlyGenericArguments());
		protected sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType) => ReadDeclaredMethods(reflectedType, GetReadOnlyGenericArguments());
		protected sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => ReadDeclaredProperties(reflectedType, GetReadOnlyGenericArguments());
		protected sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => ReadDeclaredEvents(reflectedType, GetReadOnlyGenericArguments());
	}
}
