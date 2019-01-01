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
	abstract class DmdParameterDef : DmdParameterInfoBase {
		public sealed override DmdMemberInfo Member { get; }
		public sealed override int Position { get; }
		public sealed override DmdType ParameterType { get; }
		public sealed override int MetadataToken => (int)(0x08000000 + rid);

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdParameterDef(uint rid, DmdMemberInfo member, int position, DmdType parameterType) {
			this.rid = rid;
			Member = member ?? throw new ArgumentNullException(nameof(member));
			Position = position;
			ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
		}

		public sealed override bool HasDefaultValue {
			get {
				if (!hasInitializedDefaultValue)
					InitializeDefaultValue();
				return __hasDefaultValue_DONT_USE;
			}
		}

		public sealed override object RawDefaultValue {
			get {
				if (!hasInitializedDefaultValue)
					InitializeDefaultValue();
				return __rawDefaultValue_DONT_USE;
			}
		}

		void InitializeDefaultValue() {
			if (hasInitializedDefaultValue)
				return;
			var info = CreateDefaultValue();
			lock (LockObject) {
				if (!hasInitializedDefaultValue) {
					__rawDefaultValue_DONT_USE = info.rawDefaultValue;
					__hasDefaultValue_DONT_USE = info.hasDefaultValue;
					hasInitializedDefaultValue = true;
				}
			}
		}
		volatile object __rawDefaultValue_DONT_USE;
		volatile bool __hasDefaultValue_DONT_USE;
		volatile bool hasInitializedDefaultValue;
		protected abstract (object rawDefaultValue, bool hasDefaultValue) CreateDefaultValue();

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			if (__customAttributes_DONT_USE != null)
				return __customAttributes_DONT_USE;
			var info = CreateCustomAttributes();
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, info.cas, info.marshalType);
			Interlocked.CompareExchange(ref __customAttributes_DONT_USE, newCAs, null);
			return __customAttributes_DONT_USE;
		}
		volatile ReadOnlyCollection<DmdCustomAttributeData> __customAttributes_DONT_USE;

		protected abstract (DmdCustomAttributeData[] cas, DmdMarshalType marshalType) CreateCustomAttributes();
	}
}
