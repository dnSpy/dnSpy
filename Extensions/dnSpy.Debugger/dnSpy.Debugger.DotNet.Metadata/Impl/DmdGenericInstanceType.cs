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
	sealed class DmdGenericInstanceType : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.GenericInstance;
		public override DmdTypeScope TypeScope => genericTypeDefinition.TypeScope;
		public override DmdModule Module => genericTypeDefinition.Module;
		public override string Namespace => genericTypeDefinition.Namespace;
		public override StructLayoutAttribute StructLayoutAttribute => genericTypeDefinition.StructLayoutAttribute;
		public override DmdTypeAttributes Attributes => genericTypeDefinition.Attributes;
		public override string Name => DmdMemberFormatter.FormatName(this);
		public override DmdType DeclaringType => genericTypeDefinition.DeclaringType;
		public override int MetadataToken => genericTypeDefinition.MetadataToken;
		public override bool IsMetadataReference => false;

		public override DmdType BaseType {
			get {
				if (!baseTypeInitd) {
					lock (LockObject) {
						if (!baseTypeInitd) {
							int baseTypeToken = genericTypeDefinition.GetBaseTypeToken();
							if ((baseTypeToken & 0x00FFFFFF) == 0)
								__baseType_DONT_USE = null;
							else
								__baseType_DONT_USE = Module.ResolveType(baseTypeToken, typeArguments, null, throwOnError: false);
							baseTypeInitd = true;
						}
					}
				}
				return __baseType_DONT_USE;
			}
		}
		DmdType __baseType_DONT_USE;
		bool baseTypeInitd;

		readonly DmdTypeDef genericTypeDefinition;
		readonly ReadOnlyCollection<DmdType> typeArguments;

		public DmdGenericInstanceType(DmdTypeDef genericTypeDefinition, IList<DmdType> typeArguments, IList<DmdCustomModifier> customModifiers) : base(customModifiers) {
			if ((object)genericTypeDefinition == null)
				throw new ArgumentNullException(nameof(genericTypeDefinition));
			if (typeArguments == null)
				throw new ArgumentNullException(nameof(typeArguments));
			if (genericTypeDefinition.GetGenericArguments().Count != typeArguments.Count)
				throw new ArgumentException();
			this.genericTypeDefinition = genericTypeDefinition;
			this.typeArguments = typeArguments.Count == 0 ? emptyTypeCollection : typeArguments as ReadOnlyCollection<DmdType> ?? new ReadOnlyCollection<DmdType>(typeArguments);
			IsFullyResolved = DmdTypeUtilities.IsFullyResolved(typeArguments);
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => AppDomain.MakeGenericType(genericTypeDefinition, typeArguments, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeGenericType(genericTypeDefinition, typeArguments, null);

		public override bool IsGenericType => true;
		public override ReadOnlyCollection<DmdType> GetGenericArguments() => typeArguments;
		public override DmdType GetGenericTypeDefinition() => genericTypeDefinition;

		protected override DmdType ResolveNoThrowCore() => this;
		public override bool IsFullyResolved { get; }
		public override DmdTypeBase FullResolve() {
			if (IsFullyResolved)
				return this;
			var newTypeArguments = DmdTypeUtilities.FullResolve(typeArguments);
			if (newTypeArguments != null)
				return (DmdTypeBase)AppDomain.MakeGenericType(genericTypeDefinition, newTypeArguments, GetCustomModifiers());
			return null;
		}

		protected sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredFields(reflectedType, typeArguments);
		protected sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType, bool includeConstructors) => genericTypeDefinition.ReadDeclaredMethods(reflectedType, typeArguments, includeConstructors);
		protected sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredProperties(reflectedType, typeArguments);
		protected sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredEvents(reflectedType, typeArguments);

		internal DmdFieldInfo[] CreateDeclaredFields2(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredFields(reflectedType, typeArguments);
		internal DmdMethodBase[] CreateDeclaredMethods2(DmdType reflectedType, bool includeConstructors) => genericTypeDefinition.ReadDeclaredMethods(reflectedType, typeArguments, includeConstructors);
		internal DmdPropertyInfo[] CreateDeclaredProperties2(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredProperties(reflectedType, typeArguments);
		internal DmdEventInfo[] CreateDeclaredEvents2(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredEvents(reflectedType, typeArguments);

		protected override IList<DmdType> ReadDeclaredInterfaces() => ReadDeclaredInterfaces2();
		internal IList<DmdType> ReadDeclaredInterfaces2() => genericTypeDefinition.ReadDeclaredInterfaces(typeArguments);
		protected override DmdType[] CreateNestedTypes() => CreateNestedTypes2();
		internal DmdType[] CreateNestedTypes2() => genericTypeDefinition.CreateNestedTypes2();

		protected override DmdCustomAttributeData[] CreateCustomAttributes() => CreateCustomAttributes2();
		internal DmdCustomAttributeData[] CreateCustomAttributes2() => genericTypeDefinition.CreateCustomAttributes2();
	}
}
