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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdTypeRef : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public override DmdTypeScope TypeScope { get; }
		public override DmdModule Module => ResolvedType.Module;
		public override string Namespace { get; }
		public override DmdType BaseType => ResolvedType.BaseType;
		public override StructLayoutAttribute StructLayoutAttribute => ResolvedType.StructLayoutAttribute;
		public override DmdTypeAttributes Attributes => ResolvedType.Attributes;
		public override string Name { get; }
		public override DmdType DeclaringType { get; }
		public override int MetadataToken => ResolvedType.MetadataToken;
		public override bool IsMetadataReference => true;

		internal DmdTypeDef ResolvedType => GetResolvedType(throwOnError: true);
		internal DmdTypeDef GetResolvedType(bool throwOnError) {
			if ((object)__resolvedType_DONT_USE != null)
				return __resolvedType_DONT_USE;
			lock (LockObject) {
				if ((object)__resolvedType_DONT_USE != null)
					return __resolvedType_DONT_USE;
				__resolvedType_DONT_USE = appDomain.Resolve(this, throwOnError, ignoreCase: false);
				return __resolvedType_DONT_USE;
			}
		}
		DmdTypeDef __resolvedType_DONT_USE;

		readonly DmdAppDomainImpl appDomain;

		public DmdTypeRef(DmdAppDomainImpl appDomain, DmdTypeScope typeScope, DmdTypeRef declaringType, string @namespace, string name) {
			if (typeScope.Kind == DmdTypeScopeKind.Invalid)
				throw new ArgumentException();
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			TypeScope = typeScope;
			Namespace = string.IsNullOrEmpty(@namespace) ? null : @namespace;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DeclaringType = declaringType;
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
	}
}
