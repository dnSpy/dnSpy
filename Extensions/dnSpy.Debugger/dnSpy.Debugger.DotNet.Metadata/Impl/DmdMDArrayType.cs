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
	sealed class DmdMDArrayType : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.MDArray;
		public override DmdTypeScope TypeScope => SkipElementTypes().TypeScope;
		public override DmdModule Module => SkipElementTypes().Module;
		public override string Namespace => SkipElementTypes().Namespace;
		public override DmdType BaseType => AppDomain.System_Array;
		public override StructLayoutAttribute StructLayoutAttribute => null;
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.Public | DmdTypeAttributes.AutoLayout | DmdTypeAttributes.Class | DmdTypeAttributes.Sealed | DmdTypeAttributes.AnsiClass | DmdTypeAttributes.Serializable;
		public override string Name => DmdMemberFormatter.FormatName(this);
		public override DmdType DeclaringType => null;
		public override int MetadataToken => 0x02000000;
		public override bool IsMetadataReference { get; }

		readonly DmdTypeBase elementType;
		readonly int rank;
		readonly ReadOnlyCollection<int> sizes;
		readonly ReadOnlyCollection<int> lowerBounds;

		public DmdMDArrayType(DmdTypeBase elementType, int rank, IList<int> sizes, IList<int> lowerBounds, IList<DmdCustomModifier> customModifiers) : base(customModifiers) {
			// Allow 0, it's allowed in the MD
			if (rank < 0)
				throw new ArgumentOutOfRangeException(nameof(rank));
			if (sizes == null)
				throw new ArgumentNullException(nameof(sizes));
			if (lowerBounds == null)
				throw new ArgumentNullException(nameof(lowerBounds));
			this.rank = rank;
			this.elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
			this.sizes = sizes.Count == 0 ? emptyInt32Collection : sizes as ReadOnlyCollection<int> ?? new ReadOnlyCollection<int>(sizes);
			this.lowerBounds = lowerBounds.Count == 0 ? emptyInt32Collection : lowerBounds as ReadOnlyCollection<int> ?? new ReadOnlyCollection<int>(lowerBounds);
			IsMetadataReference = elementType.IsMetadataReference;
			IsFullyResolved = elementType.IsFullyResolved;
		}
		static readonly ReadOnlyCollection<int> emptyInt32Collection = new ReadOnlyCollection<int>(Array.Empty<int>());

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => AppDomain.MakeArrayType(elementType, rank, sizes, lowerBounds, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeArrayType(elementType, rank, sizes, lowerBounds, null);
		public override DmdType GetElementType() => elementType;
		public override int GetArrayRank() => rank;
		public override ReadOnlyCollection<int> GetArraySizes() => sizes;
		public override ReadOnlyCollection<int> GetArrayLowerBounds() => lowerBounds;

		protected override DmdType ResolveNoThrowCore() {
			if (!IsMetadataReference)
				return this;
			var newElementType = elementType.ResolveNoThrow();
			if ((object)newElementType != null)
				return AppDomain.MakeArrayType(newElementType, rank, sizes, lowerBounds, GetCustomModifiers());
			return null;
		}

		public override bool IsFullyResolved { get; }
		public override DmdTypeBase FullResolve() {
			if (IsFullyResolved)
				return this;
			var et = elementType.FullResolve();
			if ((object)et != null)
				return (DmdTypeBase)AppDomain.MakeArrayType(et, rank, sizes, lowerBounds, GetCustomModifiers());
			return null;
		}

		protected override IList<DmdType> ReadDeclaredInterfaces() => null;
	}
}
