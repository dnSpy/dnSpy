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
	sealed class DmdPointerType : DmdTypeBase {
		public override DmdAppDomain AppDomain => SkipElementTypes().AppDomain;
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Pointer;
		public override DmdTypeScope TypeScope => SkipElementTypes().TypeScope;
		public override DmdModule Module => SkipElementTypes().Module;
		public override string? MetadataNamespace => null;
		public override string? MetadataName => null;
		public override DmdType? BaseType => null;
		public override StructLayoutAttribute? StructLayoutAttribute => null;
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.NotPublic | DmdTypeAttributes.AutoLayout | DmdTypeAttributes.Class | DmdTypeAttributes.AnsiClass;
		public override DmdType? DeclaringType => null;
		public override int MetadataToken => 0x02000000;
		public override bool IsMetadataReference { get; }
		internal override bool HasTypeEquivalence => elementType.HasTypeEquivalence;

		readonly DmdTypeBase elementType;

		public DmdPointerType(DmdTypeBase elementType, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			this.elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
			IsMetadataReference = elementType.IsMetadataReference;
			IsFullyResolved = elementType.IsFullyResolved;
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => AppDomain.MakePointerType(elementType, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakePointerType(elementType, null);
		public override DmdType? GetElementType() => elementType;

		protected override DmdType? ResolveNoThrowCore() {
			if (!IsMetadataReference)
				return this;
			var newElementType = elementType.ResolveNoThrow();
			if (newElementType is not null)
				return AppDomain.MakePointerType(newElementType, GetCustomModifiers());
			return null;
		}

		public override bool IsFullyResolved { get; }
		public override DmdTypeBase? FullResolve() {
			if (IsFullyResolved)
				return this;
			var et = elementType.FullResolve();
			if (et is not null)
				return (DmdTypeBase)AppDomain.MakePointerType(et, GetCustomModifiers());
			return null;
		}

		public override DmdType[]? ReadDeclaredInterfaces() => null;
		public override ReadOnlyCollection<DmdType> NestedTypes => ReadOnlyCollectionHelpers.Empty<DmdType>();
	}
}
