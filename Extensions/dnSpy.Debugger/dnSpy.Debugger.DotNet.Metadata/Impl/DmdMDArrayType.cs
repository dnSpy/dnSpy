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
	sealed class DmdMDArrayType : DmdTypeBase {
		public override DmdAppDomain AppDomain => SkipElementTypes().AppDomain;
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.MDArray;
		public override DmdTypeScope TypeScope => SkipElementTypes().TypeScope;
		public override DmdModule Module => SkipElementTypes().Module;
		public override string? MetadataNamespace => null;
		public override string? MetadataName => null;
		public override DmdType? BaseType => AppDomain.System_Array;
		public override StructLayoutAttribute? StructLayoutAttribute => null;
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.Public | DmdTypeAttributes.AutoLayout | DmdTypeAttributes.Class | DmdTypeAttributes.Sealed | DmdTypeAttributes.AnsiClass | DmdTypeAttributes.Serializable;
		public override DmdType? DeclaringType => null;
		public override int MetadataToken => 0x02000000;
		public override bool IsMetadataReference { get; }
		internal override bool HasTypeEquivalence => elementType.HasTypeEquivalence;

		readonly DmdTypeBase elementType;
		readonly int rank;
		readonly ReadOnlyCollection<int> sizes;
		readonly ReadOnlyCollection<int> lowerBounds;

		public DmdMDArrayType(DmdTypeBase elementType, int rank, IList<int> sizes, IList<int> lowerBounds, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			// Allow 0, it's allowed in the MD
			if (rank < 0)
				throw new ArgumentOutOfRangeException(nameof(rank));
			if (sizes is null)
				throw new ArgumentNullException(nameof(sizes));
			if (lowerBounds is null)
				throw new ArgumentNullException(nameof(lowerBounds));
			this.rank = rank;
			this.elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
			this.sizes = ReadOnlyCollectionHelpers.Create(sizes);
			this.lowerBounds = ReadOnlyCollectionHelpers.Create(lowerBounds);
			IsMetadataReference = elementType.IsMetadataReference;
			IsFullyResolved = elementType.IsFullyResolved;
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => AppDomain.MakeArrayType(elementType, rank, sizes, lowerBounds, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeArrayType(elementType, rank, sizes, lowerBounds, null);
		public override DmdType? GetElementType() => elementType;
		public override int GetArrayRank() => rank;
		public override ReadOnlyCollection<int> GetArraySizes() => sizes;
		public override ReadOnlyCollection<int> GetArrayLowerBounds() => lowerBounds;

		protected override DmdType? ResolveNoThrowCore() {
			if (!IsMetadataReference)
				return this;
			var newElementType = elementType.ResolveNoThrow();
			if (newElementType is not null)
				return AppDomain.MakeArrayType(newElementType, rank, sizes, lowerBounds, GetCustomModifiers());
			return null;
		}

		public override bool IsFullyResolved { get; }
		public override DmdTypeBase? FullResolve() {
			if (IsFullyResolved)
				return this;
			var et = elementType.FullResolve();
			if (et is not null)
				return (DmdTypeBase)AppDomain.MakeArrayType(et, rank, sizes, lowerBounds, GetCustomModifiers());
			return null;
		}

		public override DmdType[]? ReadDeclaredInterfaces() => null;

		public override DmdMethodBase[]? CreateDeclaredMethods(DmdType reflectedType) {
			var appDomain = AppDomain;
			return new DmdMethodBase[5] {
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Constructor1, DmdConstructorInfo.ConstructorName, appDomain.System_Void, CreateParameterTypes(null)),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Constructor2, DmdConstructorInfo.ConstructorName, appDomain.System_Void, CreateParameterTypesPair()),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Set, "Set", appDomain.System_Void, CreateParameterTypes(elementType)),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Address, "Address", appDomain.MakeByRefType(elementType, null), CreateParameterTypes(null)),
				CreateMethod(reflectedType, DmdSpecialMethodKind.Array_Get, "Get", elementType, CreateParameterTypes(null)),
			};
		}

		int SafeRank => (uint)rank <= 100 ? rank : 100;

		DmdType[] CreateParameterTypes(DmdType? lastType) {
			var rank = SafeRank;
			var types = new DmdType[rank + (lastType is null ? 0 : 1)];
			var int32Type = AppDomain.System_Int32;
			for (int i = 0; i < types.Length; i++)
				types[i] = int32Type;
			if (lastType is not null)
				types[types.Length - 1] = lastType;
			return types;
		}

		DmdType[] CreateParameterTypesPair() {
			var rank = SafeRank;
			var types = new DmdType[rank * 2];
			var int32Type = AppDomain.System_Int32;
			for (int i = 0; i < rank; i++) {
				types[i * 2 + 0] = int32Type;
				types[i * 2 + 1] = int32Type;
			}
			return types;
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
