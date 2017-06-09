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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeRef : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public abstract override DmdTypeScope TypeScope { get; }
		public abstract override string Namespace { get; }
		public abstract override string Name { get; }
		public override DmdModule Module => ResolvedType.Module;
		public override DmdType BaseType => ResolvedType.BaseType;
		public override StructLayoutAttribute StructLayoutAttribute => ResolvedType.StructLayoutAttribute;
		public override DmdTypeAttributes Attributes => ResolvedType.Attributes;
		// Always return the TypeRef and never the resolved type's DeclaringType so callers can get the reference
		// in case it's an exported type. Also, the member formatter shouldn't throw if the type can't be resolved.
		public override DmdType DeclaringType => DeclaringTypeRef;
		public override int MetadataToken => ResolvedType.MetadataToken;
		public override bool IsMetadataReference => true;

		public virtual DmdTypeRef DeclaringTypeRef {
			get {
				if (!declaringTypeRefInitd) {
					lock (LockObject) {
						if (!declaringTypeRefInitd) {
							int declTypeToken = GetDeclaringTypeRefToken();
							if ((declTypeToken & 0x00FFFFFF) == 0)
								__declaringTypeRef_DONT_USE = null;
							else
								__declaringTypeRef_DONT_USE = (DmdTypeRef)ownerModule.ResolveType(declTypeToken, null, null, throwOnError: false);
							declaringTypeRefInitd = true;
						}
					}
				}
				return __declaringTypeRef_DONT_USE;
			}
		}
		DmdTypeRef __declaringTypeRef_DONT_USE;
		bool declaringTypeRefInitd;
		protected abstract int GetDeclaringTypeRefToken();

		internal DmdTypeDef ResolvedType => GetResolvedType(throwOnError: true);
		internal DmdTypeDef GetResolvedType(bool throwOnError) {
			if ((object)__resolvedType_DONT_USE != null)
				return __resolvedType_DONT_USE;
			lock (LockObject) {
				if ((object)__resolvedType_DONT_USE != null)
					return __resolvedType_DONT_USE;
				var appDomain = (DmdAppDomainImpl)ownerModule.AppDomain;
				var type = appDomain.Resolve(this, throwOnError, ignoreCase: false);
				if (GetCustomModifiers().Count != 0)
					type = (DmdTypeDef)appDomain.Intern(type.WithCustomModifiers(GetCustomModifiers()));
				__resolvedType_DONT_USE = type;
				return __resolvedType_DONT_USE;
			}
		}
		DmdTypeDef __resolvedType_DONT_USE;

		protected uint Rid => rid;
		protected DmdModule OwnerModule => ownerModule;
		readonly uint rid;
		readonly DmdModule ownerModule;

		public DmdTypeRef(DmdModule ownerModule, uint rid, IList<DmdCustomModifier> customModifiers) : base(customModifiers) {
			this.ownerModule = ownerModule ?? throw new ArgumentNullException(nameof(ownerModule));
			this.rid = rid;
		}

		public override bool IsFullyResolved => false;
		protected override DmdType ResolveNoThrowCore() => GetResolvedType(throwOnError: false);
		public override DmdTypeBase FullResolve() => GetResolvedType(throwOnError: false);

		public override bool IsGenericType => ResolvedType.IsGenericType;
		public override bool IsGenericTypeDefinition => ResolvedType.IsGenericTypeDefinition;
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments() => ResolvedType.GetReadOnlyGenericArguments();
		public override DmdType GetGenericTypeDefinition() => ResolvedType.GetGenericTypeDefinition();

		protected sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => ResolvedType.CreateDeclaredFields2(reflectedType);
		protected sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType, bool includeConstructors) => ResolvedType.CreateDeclaredMethods2(reflectedType, includeConstructors);
		protected sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => ResolvedType.CreateDeclaredProperties2(reflectedType);
		protected sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => ResolvedType.CreateDeclaredEvents2(reflectedType);

		protected override IList<DmdType> ReadDeclaredInterfaces() => ResolvedType.ReadDeclaredInterfaces2();
		protected override DmdType[] CreateNestedTypes() => ResolvedType.CreateNestedTypes2();
	}
}
