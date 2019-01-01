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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdGenericInstanceType : DmdTypeBase {
		public override DmdAppDomain AppDomain => genericTypeDefinition.AppDomain;
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.GenericInstance;
		public override DmdTypeScope TypeScope => genericTypeDefinition.TypeScope;
		public override DmdModule Module => genericTypeDefinition.Module;
		public override string MetadataNamespace => genericTypeDefinition.MetadataNamespace;
		public override StructLayoutAttribute StructLayoutAttribute => genericTypeDefinition.StructLayoutAttribute;
		public override DmdTypeAttributes Attributes => genericTypeDefinition.Attributes;
		public override string MetadataName => genericTypeDefinition.MetadataName;
		public override DmdType DeclaringType => genericTypeDefinition.DeclaringType;
		public override int MetadataToken => genericTypeDefinition.MetadataToken;
		public override bool IsMetadataReference => false;

		internal override bool HasTypeEquivalence {
			get {
				const byte BoolBit = 1;
				const byte InitializedBit = 2;
				const byte CalculatingBit = 4;
				if ((hasTypeEquivalenceFlags & (InitializedBit | CalculatingBit)) == 0) {
					// In case we get called recursively
					hasTypeEquivalenceFlags |= CalculatingBit;

					byte result = InitializedBit;
					if (genericTypeDefinition.HasTypeEquivalence)
						result |= BoolBit;
					else {
						foreach (var gaType in typeArguments) {
							if (gaType.HasTypeEquivalence) {
								result |= BoolBit;
								break;
							}
						}
					}

					hasTypeEquivalenceFlags = result;
				}
				return (hasTypeEquivalenceFlags & BoolBit) != 0;
			}
		}
		volatile byte hasTypeEquivalenceFlags;

		public override DmdType BaseType {
			get {
				if (!baseTypeInitd) {
					var newBT = genericTypeDefinition.GetBaseType(typeArguments);
					lock (LockObject) {
						if (!baseTypeInitd) {
							__baseType_DONT_USE = newBT;
							baseTypeInitd = true;
						}
					}
				}
				return __baseType_DONT_USE;
			}
		}
		volatile DmdType __baseType_DONT_USE;
		volatile bool baseTypeInitd;

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
			this.typeArguments = ReadOnlyCollectionHelpers.Create(typeArguments);
			IsFullyResolved = DmdTypeUtilities.IsFullyResolved(typeArguments);
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => AppDomain.MakeGenericType(genericTypeDefinition, typeArguments, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeGenericType(genericTypeDefinition, typeArguments, null);

		public override bool IsGenericType => true;
		protected override ReadOnlyCollection<DmdType> GetGenericArgumentsCore() => typeArguments;
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

		public sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredFields(this, reflectedType);
		public sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredMethods(this, reflectedType);
		public sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredProperties(this, reflectedType);
		public sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => genericTypeDefinition.ReadDeclaredEvents(this, reflectedType);

		public override DmdType[] ReadDeclaredInterfaces() => genericTypeDefinition.ReadDeclaredInterfaces(typeArguments);
		public override ReadOnlyCollection<DmdType> NestedTypes => genericTypeDefinition.NestedTypes;

		public override (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas) CreateCustomAttributes() => genericTypeDefinition.CreateCustomAttributes();
	}
}
