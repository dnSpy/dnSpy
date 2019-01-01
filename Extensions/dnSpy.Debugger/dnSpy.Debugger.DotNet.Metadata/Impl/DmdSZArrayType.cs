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
	sealed class DmdSZArrayType : DmdTypeBase {
		public override DmdAppDomain AppDomain => SkipElementTypes().AppDomain;
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.SZArray;
		public override DmdTypeScope TypeScope => SkipElementTypes().TypeScope;
		public override DmdModule Module => SkipElementTypes().Module;
		public override string MetadataNamespace => null;
		public override string MetadataName => null;
		public override DmdType BaseType => AppDomain.System_Array;
		public override StructLayoutAttribute StructLayoutAttribute => null;
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.Public | DmdTypeAttributes.AutoLayout | DmdTypeAttributes.Class | DmdTypeAttributes.Sealed | DmdTypeAttributes.AnsiClass | DmdTypeAttributes.Serializable;
		public override DmdType DeclaringType => null;
		public override int MetadataToken => 0x02000000;
		public override bool IsMetadataReference { get; }
		internal override bool HasTypeEquivalence => elementType.HasTypeEquivalence;

		readonly DmdTypeBase elementType;

		public DmdSZArrayType(DmdTypeBase elementType, IList<DmdCustomModifier> customModifiers) : base(customModifiers) {
			this.elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
			IsMetadataReference = elementType.IsMetadataReference;
			IsFullyResolved = elementType.IsFullyResolved;
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => AppDomain.MakeArrayType(elementType, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeArrayType(elementType, null);
		public override DmdType GetElementType() => elementType;
		public override int GetArrayRank() => 1;
		public override ReadOnlyCollection<int> GetArraySizes() => ReadOnlyCollectionHelpers.Empty<int>();
		public override ReadOnlyCollection<int> GetArrayLowerBounds() => ReadOnlyCollectionHelpers.Empty<int>();

		protected override DmdType ResolveNoThrowCore() {
			if (!IsMetadataReference)
				return this;
			var newElementType = elementType.ResolveNoThrow();
			if ((object)newElementType != null)
				return AppDomain.MakeArrayType(newElementType, GetCustomModifiers());
			return null;
		}

		public override bool IsFullyResolved { get; }
		public override DmdTypeBase FullResolve() {
			if (IsFullyResolved)
				return this;
			var et = elementType.FullResolve();
			if ((object)et != null)
				return (DmdTypeBase)AppDomain.MakeArrayType(et, GetCustomModifiers());
			return null;
		}

		public override DmdType[] ReadDeclaredInterfaces() => ((DmdAppDomainImpl)AppDomain).GetSZArrayInterfaces(elementType);

		public override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType) {
			var appDomain = AppDomain;
			return new DmdMethodBase[4] {
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Constructor1, DmdConstructorInfo.ConstructorName, appDomain.System_Void, appDomain.System_Int32),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Set, "Set", appDomain.System_Void, appDomain.System_Int32, elementType),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Address, "Address", appDomain.MakeByRefType(elementType, null), appDomain.System_Int32),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Get, "Get", elementType, appDomain.System_Int32),
			};
		}

		DmdMethodBase CreateMethod(DmdType reflectedType, DmdSpecialMethodKind specialMethodKind, string name, DmdType returnType, params DmdType[] parameterTypes) {
			var flags = DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis;
			var sig = new DmdMethodSignature(flags, 0, returnType, parameterTypes, null);
			if (name == DmdConstructorInfo.ConstructorName)
				return new DmdCreatedConstructorDef(specialMethodKind, name, sig, this, reflectedType);
			return new DmdCreatedMethodDef(specialMethodKind, name, sig, this, reflectedType);
		}

		public override ReadOnlyCollection<DmdType> NestedTypes => ReadOnlyCollectionHelpers.Empty<DmdType>();
	}
}
