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
using System.Collections.ObjectModel;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdFieldDef : DmdFieldInfoBase {
		public sealed override DmdType? DeclaringType { get; }
		public sealed override DmdType? ReflectedType { get; }
		public sealed override int MetadataToken => (int)(0x04000000 + rid);
		public sealed override bool IsMetadataReference => false;

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdFieldDef(uint rid, DmdType declaringType, DmdType reflectedType) {
			this.rid = rid;
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ReflectedType = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
		}

		public sealed override DmdFieldInfo? Resolve(bool throwOnError) => this;

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			if (!(__customAttributes_DONT_USE is null))
				return __customAttributes_DONT_USE;
			var info = CreateCustomAttributes();
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, info.cas, info.fieldOffset, info.marshalType);
			Interlocked.CompareExchange(ref __customAttributes_DONT_USE, newCAs, null);
			return __customAttributes_DONT_USE!;
		}
		volatile ReadOnlyCollection<DmdCustomAttributeData>? __customAttributes_DONT_USE;

		protected abstract (DmdCustomAttributeData[] cas, uint? fieldOffset, DmdMarshalType? marshalType) CreateCustomAttributes();
	}
}
