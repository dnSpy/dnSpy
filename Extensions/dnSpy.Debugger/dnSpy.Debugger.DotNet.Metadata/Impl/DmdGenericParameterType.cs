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
	abstract class DmdGenericParameterType : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => (object)declaringType != null ? DmdTypeSignatureKind.TypeGenericParameter : DmdTypeSignatureKind.MethodGenericParameter;
		public override DmdTypeScope TypeScope => new DmdTypeScope(Module);
		public override DmdMethodBase DeclaringMethod => declaringMethod;
		public override DmdType DeclaringType => declaringType;
		public override DmdModule Module => ((DmdMemberInfo)declaringType ?? declaringMethod).Module;
		public override string Namespace => declaringType?.Namespace;
		public override StructLayoutAttribute StructLayoutAttribute => null;
		public override DmdGenericParameterAttributes GenericParameterAttributes { get; }
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.Public;
		public override int GenericParameterPosition { get; }
		public override string Name { get; }
		public override int MetadataToken => (int)(0x2A000000 + rid);
		public override bool IsMetadataReference => false;

		public override DmdType BaseType {
			get {
				var baseType = AppDomain.System_Object;
				foreach (var gpcType in GetOrCreateGenericParameterConstraints()) {
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
		readonly DmdType declaringType;
		readonly DmdMethodBase declaringMethod;

		protected DmdGenericParameterType(uint rid, DmdType declaringType, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers)
			: this(rid, declaringType, null, name, position, attributes, customModifiers) {
			if ((object)declaringType == null)
				throw new ArgumentNullException(nameof(declaringType));
		}

		protected DmdGenericParameterType(uint rid, DmdMethodBase declaringMethod, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers)
			: this(rid, null, declaringMethod, name, position, attributes, customModifiers) {
			if ((object)declaringMethod == null)
				throw new ArgumentNullException(nameof(declaringMethod));
		}

		DmdGenericParameterType(uint rid, DmdType declaringType, DmdMethodBase declaringMethod, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers) : base(customModifiers) {
			this.rid = rid;
			this.declaringType = declaringType;
			this.declaringMethod = declaringMethod;
			Name = name ?? string.Empty;
			GenericParameterPosition = position;
			GenericParameterAttributes = attributes;
		}

		ReadOnlyCollection<DmdType> GetOrCreateGenericParameterConstraints() {
			if (__genericParameterConstraints_DONT_USE != null)
				return __genericParameterConstraints_DONT_USE;
			lock (LockObject) {
				if (__genericParameterConstraints_DONT_USE != null)
					return __genericParameterConstraints_DONT_USE;
				var res = CreateGenericParameterConstraints_NoLock();
				__genericParameterConstraints_DONT_USE = res == null || res.Length == 0 ? emptyTypeCollection : new ReadOnlyCollection<DmdType>(res);
				return __genericParameterConstraints_DONT_USE;
			}
		}
		ReadOnlyCollection<DmdType> __genericParameterConstraints_DONT_USE;
		protected abstract DmdType[] CreateGenericParameterConstraints_NoLock();
		public override ReadOnlyCollection<DmdType> GetGenericParameterConstraints() => GetOrCreateGenericParameterConstraints();

		protected override DmdType ResolveNoThrowCore() => this;

		public override bool IsFullyResolved => true;
		public override DmdTypeBase FullResolve() => this;

		protected override IList<DmdType> ReadDeclaredInterfaces() {
			var list = new List<DmdType>();
			foreach (var gpcType in GetOrCreateGenericParameterConstraints()) {
				if (gpcType.IsInterface)
					list.Add(gpcType);
				list.AddRange(gpcType.GetInterfaces());
			}
			return list.ToArray();
		}
	}
}
