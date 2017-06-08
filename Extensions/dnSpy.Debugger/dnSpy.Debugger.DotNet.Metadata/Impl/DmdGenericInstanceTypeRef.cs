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
	sealed class DmdGenericInstanceTypeRef : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.GenericInstance;
		public override DmdTypeScope TypeScope => ResolvedType.TypeScope;
		public override DmdModule Module => ResolvedType.Module;
		public override string Namespace => ResolvedType.Namespace;
		public override DmdType BaseType => ResolvedType.BaseType;
		public override StructLayoutAttribute StructLayoutAttribute => ResolvedType.StructLayoutAttribute;
		public override DmdTypeAttributes Attributes => ResolvedType.Attributes;
		public override string Name => DmdMemberFormatter.FormatName(this);
		public override DmdType DeclaringType => ResolvedType.DeclaringType;
		public override int MetadataToken => ResolvedType.MetadataToken;
		public override bool IsMetadataReference => true;

		DmdGenericInstanceType ResolvedType => GetResolvedType(throwOnError: true);
		DmdGenericInstanceType GetResolvedType(bool throwOnError) {
			if ((object)__resolvedType_DONT_USE != null)
				return __resolvedType_DONT_USE;
			lock (LockObject) {
				if ((object)__resolvedType_DONT_USE != null)
					return __resolvedType_DONT_USE;
				var typeDef = genericTypeRef.GetResolvedType(throwOnError);
				if ((object)typeDef == null)
					return null;
				__resolvedType_DONT_USE = (DmdGenericInstanceType)typeDef.AppDomain.MakeGenericType(typeDef, typeArguments);
				return __resolvedType_DONT_USE;
			}
		}
		DmdGenericInstanceType __resolvedType_DONT_USE;

		readonly DmdTypeRef genericTypeRef;
		readonly ReadOnlyCollection<DmdType> typeArguments;

		public DmdGenericInstanceTypeRef(DmdTypeRef genericTypeRef, IList<DmdType> typeArguments) {
			if (typeArguments == null)
				throw new ArgumentNullException(nameof(typeArguments));
			this.genericTypeRef = genericTypeRef ?? throw new ArgumentNullException(nameof(genericTypeRef));
			this.typeArguments = typeArguments.Count == 0 ? emptyTypeCollection : typeArguments as ReadOnlyCollection<DmdType> ?? new ReadOnlyCollection<DmdType>(typeArguments);
		}

		public override bool IsGenericType => true;
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments() => typeArguments;
		public override DmdType GetGenericTypeDefinition() => ResolvedType.GetGenericTypeDefinition();

		protected override DmdType ResolveNoThrowCore() => GetResolvedType(throwOnError: false);
		public override bool IsFullyResolved => false;
		public override DmdTypeBase FullResolve() => GetResolvedType(throwOnError: false)?.FullResolve();

		protected sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => ResolvedType.CreateDeclaredFields2(reflectedType);
		protected sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType, bool includeConstructors) => ResolvedType.CreateDeclaredMethods2(reflectedType, includeConstructors);
		protected sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => ResolvedType.CreateDeclaredProperties2(reflectedType);
		protected sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => ResolvedType.CreateDeclaredEvents2(reflectedType);
	}
}
