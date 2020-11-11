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
	abstract class DmdGenericParameterType : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => declaringType is not null ? DmdTypeSignatureKind.TypeGenericParameter : DmdTypeSignatureKind.MethodGenericParameter;
		public sealed override DmdTypeScope TypeScope => new DmdTypeScope(Module);
		public sealed override DmdMethodBase? DeclaringMethod => declaringMethod;
		public sealed override DmdType? DeclaringType => declaringType;
		public override DmdModule Module => ((DmdMemberInfo?)declaringType ?? declaringMethod)!.Module;
		public sealed override string? MetadataNamespace => declaringType?.MetadataNamespace;
		public sealed override StructLayoutAttribute? StructLayoutAttribute => null;
		public sealed override DmdGenericParameterAttributes GenericParameterAttributes { get; }
		public sealed override DmdTypeAttributes Attributes => DmdTypeAttributes.Public;
		public sealed override int GenericParameterPosition { get; }
		public sealed override string? MetadataName { get; }
		public sealed override int MetadataToken => (int)(0x2A000000 + rid);
		public sealed override bool IsMetadataReference => false;
		internal override bool HasTypeEquivalence => false;

		public sealed override DmdType? BaseType {
			get {
				var baseType = AppDomain.System_Object;
				foreach (var gpcType in GetGenericParameterConstraints()) {
					if (gpcType.IsInterface)
						continue;
					if (gpcType.IsGenericParameter && (gpcType.GenericParameterAttributes & (DmdGenericParameterAttributes.ReferenceTypeConstraint | DmdGenericParameterAttributes.NotNullableValueTypeConstraint)) == 0)
						continue;
					baseType = gpcType;
				}
				if (baseType == AppDomain.System_Object && (GenericParameterAttributes & DmdGenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
					baseType = AppDomain.System_ValueType;
				return baseType;
			}
		}

		protected uint Rid => rid;
		readonly uint rid;
		readonly DmdTypeBase? declaringType;
		readonly DmdMethodBase? declaringMethod;

		protected DmdGenericParameterType(uint rid, DmdTypeBase declaringType, string? name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers)
			: this(rid, declaringType, null, name, position, attributes, customModifiers) {
			if (declaringType is null)
				throw new ArgumentNullException(nameof(declaringType));
		}

		protected DmdGenericParameterType(uint rid, DmdMethodBase declaringMethod, string? name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers)
			: this(rid, null, declaringMethod, name, position, attributes, customModifiers) {
			if (declaringMethod is null)
				throw new ArgumentNullException(nameof(declaringMethod));
		}

		DmdGenericParameterType(uint rid, DmdTypeBase? declaringType, DmdMethodBase? declaringMethod, string? name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			this.rid = rid;
			this.declaringType = declaringType;
			this.declaringMethod = declaringMethod;
			MetadataName = name ?? string.Empty;
			GenericParameterPosition = position;
			GenericParameterAttributes = attributes;
		}

		protected DmdGenericParameterType(int position, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			rid = 0;
			declaringType = null;
			declaringMethod = null;
			MetadataName = string.Empty;
			GenericParameterPosition = position;
			GenericParameterAttributes = 0;
		}

		public sealed override ReadOnlyCollection<DmdType> GetGenericParameterConstraints() {
			if (__genericParameterConstraints_DONT_USE is not null)
				return __genericParameterConstraints_DONT_USE;
			var res = CreateGenericParameterConstraints();
			Interlocked.CompareExchange(ref __genericParameterConstraints_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
			return __genericParameterConstraints_DONT_USE!;
		}
		volatile ReadOnlyCollection<DmdType>? __genericParameterConstraints_DONT_USE;
		protected abstract DmdType[]? CreateGenericParameterConstraints();

		protected override DmdType? ResolveNoThrowCore() => this;

		public sealed override bool IsFullyResolved => true;
		public sealed override DmdTypeBase? FullResolve() => this;

		public override DmdType[]? ReadDeclaredInterfaces() {
			List<DmdType>? list = ObjectPools.AllocListOfType();
			foreach (var gpcType in GetGenericParameterConstraints()) {
				if (gpcType.IsInterface)
					list.Add(gpcType);
				list.AddRange(gpcType.GetInterfaces());
			}
			return ObjectPools.FreeAndToArray(ref list);
		}

		public sealed override ReadOnlyCollection<DmdType> NestedTypes => ReadOnlyCollectionHelpers.Empty<DmdType>();
	}
}
