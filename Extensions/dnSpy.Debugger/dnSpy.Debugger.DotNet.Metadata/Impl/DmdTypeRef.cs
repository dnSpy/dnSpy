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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeRef : DmdTypeBase {
		public sealed override DmdAppDomain AppDomain => ownerModule.AppDomain;
		public sealed override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public abstract override DmdTypeScope TypeScope { get; }
		public abstract override string? MetadataNamespace { get; }
		public abstract override string? MetadataName { get; }
		public sealed override DmdModule Module => ResolvedType.Module;
		public sealed override DmdType? BaseType => ResolvedType.BaseType;
		public sealed override StructLayoutAttribute? StructLayoutAttribute => ResolvedType.StructLayoutAttribute;
		public sealed override DmdTypeAttributes Attributes => ResolvedType.Attributes;
		// Always return the TypeRef and never the resolved type's DeclaringType so callers can get the reference
		// in case it's an exported type. Also, the member formatter shouldn't throw if the type can't be resolved.
		public sealed override DmdType? DeclaringType => DeclaringTypeRef;
		public sealed override int MetadataToken => ResolvedType.MetadataToken;
		public sealed override bool IsMetadataReference => true;
		internal override bool HasTypeEquivalence => ResolvedType.HasTypeEquivalence;

		public virtual DmdTypeRef? DeclaringTypeRef {
			get {
				if (!declaringTypeRefInitd) {
					int declTypeToken = GetDeclaringTypeRefToken();
					DmdTypeRef? newDT;
					if ((declTypeToken & 0x00FFFFFF) == 0)
						newDT = null;
					else
						newDT = (DmdTypeRef?)ownerModule.ResolveType(declTypeToken, DmdResolveOptions.NoTryResolveRefs);

					lock (LockObject) {
						if (!declaringTypeRefInitd) {
							__declaringTypeRef_DONT_USE = newDT;
							declaringTypeRefInitd = true;
						}
					}
				}
				return __declaringTypeRef_DONT_USE;
			}
		}
		volatile DmdTypeRef? __declaringTypeRef_DONT_USE;
		volatile bool declaringTypeRefInitd;
		protected abstract int GetDeclaringTypeRefToken();

		internal DmdTypeDef ResolvedType => GetResolvedType(throwOnError: true)!;
		internal DmdTypeDef? GetResolvedType(bool throwOnError) {
			if (!(__resolvedType_DONT_USE is null))
				return __resolvedType_DONT_USE;
			var appDomain = (DmdAppDomainImpl)ownerModule.AppDomain;
			var type = appDomain.Resolve(this, throwOnError, ignoreCase: false);
			if (!(type is null) && GetCustomModifiers().Count != 0)
				type = (DmdTypeDef)appDomain.Intern(type.WithCustomModifiers(GetCustomModifiers()), DmdMakeTypeOptions.None);
			Interlocked.CompareExchange(ref __resolvedType_DONT_USE, type, null);
			return __resolvedType_DONT_USE;
		}
		volatile DmdTypeDef? __resolvedType_DONT_USE;

		protected uint Rid => rid;
		protected DmdModule OwnerModule => ownerModule;
		readonly uint rid;
		readonly DmdModule ownerModule;

		public DmdTypeRef(DmdModule ownerModule, uint rid, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			this.ownerModule = ownerModule ?? throw new ArgumentNullException(nameof(ownerModule));
			this.rid = rid;
		}

		public sealed override bool IsFullyResolved => false;
		protected override DmdType? ResolveNoThrowCore() => GetResolvedType(throwOnError: false);
		public sealed override DmdTypeBase? FullResolve() => GetResolvedType(throwOnError: false);

		public sealed override bool IsGenericType => ResolvedType.IsGenericType;
		public sealed override bool IsGenericTypeDefinition => ResolvedType.IsGenericTypeDefinition;
		protected sealed override ReadOnlyCollection<DmdType> GetGenericArgumentsCore() => ResolvedType.GetGenericArguments();
		public sealed override DmdType GetGenericTypeDefinition() => ResolvedType.GetGenericTypeDefinition();

		public sealed override DmdFieldInfo[]? CreateDeclaredFields(DmdType reflectedType) => ResolvedType.CreateDeclaredFields(reflectedType);
		public sealed override DmdMethodBase[]? CreateDeclaredMethods(DmdType reflectedType) => ResolvedType.CreateDeclaredMethods(reflectedType);
		public sealed override DmdPropertyInfo[]? CreateDeclaredProperties(DmdType reflectedType) => ResolvedType.CreateDeclaredProperties(reflectedType);
		public sealed override DmdEventInfo[]? CreateDeclaredEvents(DmdType reflectedType) => ResolvedType.CreateDeclaredEvents(reflectedType);

		public override DmdType[]? ReadDeclaredInterfaces() => ResolvedType.ReadDeclaredInterfaces();
		public sealed override ReadOnlyCollection<DmdType> NestedTypes => ResolvedType.NestedTypes;
		public override (DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas) CreateCustomAttributes() => ResolvedType.CreateCustomAttributes();
	}
}
