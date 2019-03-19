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
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdFieldDefMD : DmdFieldDef {
		public sealed override string Name { get; }
		public sealed override DmdType FieldType { get; }
		public sealed override DmdFieldAttributes Attributes { get; }
		public override uint FieldRVA { get; }

		readonly DmdEcma335MetadataReader reader;

		public DmdFieldDefMD(DmdEcma335MetadataReader reader, uint rid, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			bool b = reader.TablesStream.TryReadFieldRow(rid, out var row);
			Debug.Assert(b);
			Attributes = (DmdFieldAttributes)row.Flags;
			Name = reader.StringsStream.ReadNoNull(row.Name);
			FieldType = reader.ReadFieldType(row.Signature, DeclaringType.GetGenericArguments());
			if (HasFieldRVA) {
				reader.TablesStream.TryReadFieldRVARow(reader.Metadata.GetFieldRVARid(rid), out var rvaRow);
				FieldRVA = rvaRow.RVA;
			}
		}

		public sealed override object GetRawConstantValue() => reader.ReadConstant(MetadataToken).value;

		protected override (DmdCustomAttributeData[] cas, uint? fieldOffset, DmdMarshalType marshalType) CreateCustomAttributes() {
			var marshalType = reader.ReadMarshalType(MetadataToken, ReflectedType.Module, null);
			var cas = reader.ReadCustomAttributes(MetadataToken);
			uint? fieldOffset;
			if (reader.TablesStream.TryReadFieldLayoutRow(reader.Metadata.GetFieldLayoutRid(Rid), out var row))
				fieldOffset = row.OffSet;
			else
				fieldOffset = null;
			return (cas, fieldOffset, marshalType);
		}
	}
}
