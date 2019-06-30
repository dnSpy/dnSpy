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
	sealed class DmdGenericInstanceTypeRef : DmdTypeBase {
		public override DmdAppDomain AppDomain => genericTypeRef.AppDomain;
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.GenericInstance;
		public override DmdTypeScope TypeScope => ResolvedType.TypeScope;
		public override DmdModule Module => ResolvedType.Module;
		public override string? MetadataNamespace => ResolvedType.MetadataNamespace;
		public override DmdType? BaseType => ResolvedType.BaseType;
		public override StructLayoutAttribute? StructLayoutAttribute => ResolvedType.StructLayoutAttribute;
		public override DmdTypeAttributes Attributes => ResolvedType.Attributes;
		public override string? MetadataName => genericTypeRef.MetadataName;
		public override DmdType? DeclaringType => ResolvedType.DeclaringType;
		public override int MetadataToken => ResolvedType.MetadataToken;
		public override bool IsMetadataReference => true;
		internal override bool HasTypeEquivalence => ResolvedType.HasTypeEquivalence;

		DmdGenericInstanceType ResolvedType => GetResolvedType(throwOnError: true)!;
		DmdGenericInstanceType? GetResolvedType(bool throwOnError) {
			if (!(__resolvedType_DONT_USE is null))
				return __resolvedType_DONT_USE;
			var typeDef = genericTypeRef.GetResolvedType(throwOnError);
			var newRT = (DmdGenericInstanceType?)typeDef?.AppDomain.MakeGenericType(typeDef, typeArguments, GetCustomModifiers());
			Interlocked.CompareExchange(ref __resolvedType_DONT_USE, newRT, null);
			return __resolvedType_DONT_USE!;
		}
		volatile DmdGenericInstanceType? __resolvedType_DONT_USE;

		readonly DmdTypeRef genericTypeRef;
		readonly ReadOnlyCollection<DmdType> typeArguments;

		public DmdGenericInstanceTypeRef(DmdTypeRef genericTypeRef, IList<DmdType> typeArguments, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			if (typeArguments is null)
				throw new ArgumentNullException(nameof(typeArguments));
			this.genericTypeRef = genericTypeRef ?? throw new ArgumentNullException(nameof(genericTypeRef));
			this.typeArguments = ReadOnlyCollectionHelpers.Create(typeArguments);
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => AppDomain.MakeGenericType(genericTypeRef, typeArguments, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeGenericType(genericTypeRef, typeArguments, null);

		public override bool IsGenericType => true;
		protected override ReadOnlyCollection<DmdType> GetGenericArgumentsCore() => typeArguments;

		public override DmdType GetGenericTypeDefinition() {
			var resolvedType = GetResolvedType(throwOnError: false);
			if (!(resolvedType is null))
				return resolvedType.GetGenericTypeDefinition();
			return genericTypeRef;
		}

		protected override DmdType? ResolveNoThrowCore() => GetResolvedType(throwOnError: false);
		public override bool IsFullyResolved => false;
		public override DmdTypeBase? FullResolve() => GetResolvedType(throwOnError: false)?.FullResolve();

		public sealed override DmdFieldInfo[]? CreateDeclaredFields(DmdType reflectedType) => ResolvedType.CreateDeclaredFields(reflectedType);
		public sealed override DmdMethodBase[]? CreateDeclaredMethods(DmdType reflectedType) => ResolvedType.CreateDeclaredMethods(reflectedType);
		public sealed override DmdPropertyInfo[]? CreateDeclaredProperties(DmdType reflectedType) => ResolvedType.CreateDeclaredProperties(reflectedType);
		public sealed override DmdEventInfo[]? CreateDeclaredEvents(DmdType reflectedType) => ResolvedType.CreateDeclaredEvents(reflectedType);

		public override DmdType[]? ReadDeclaredInterfaces() => ResolvedType.ReadDeclaredInterfaces();
		public override ReadOnlyCollection<DmdType> NestedTypes => ResolvedType.NestedTypes;
		public override (DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas) CreateCustomAttributes() => ResolvedType.CreateCustomAttributes();
	}
}
