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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdFieldDefCOMD : DmdFieldDef {
		public override string Name { get; }
		public override DmdType FieldType { get; }
		public override DmdFieldAttributes Attributes { get; }
		public override uint FieldRVA => 0;

		readonly DmdComMetadataReader reader;

		public DmdFieldDefCOMD(DmdComMetadataReader reader, uint rid, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Dispatcher.VerifyAccess();

			uint token = 0x04000000 + rid;
			Attributes = MDAPI.GetFieldAttributes(reader.MetaDataImport, token);
			Name = MDAPI.GetFieldName(reader.MetaDataImport, token) ?? string.Empty;
			FieldType = reader.ReadFieldType_COMThread(MDAPI.GetFieldSignatureBlob(reader.MetaDataImport, token), DeclaringType.GetGenericArguments());
		}

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		public override object GetRawConstantValue() => COMThread(() => reader.ReadFieldConstant_COMThread(MetadataToken).value);

		protected override (DmdCustomAttributeData[] cas, uint? fieldOffset, DmdMarshalType marshalType) CreateCustomAttributes() => COMThread(CreateCustomAttributes_COMThread);

		(DmdCustomAttributeData[] cas, uint? fieldOffset, DmdMarshalType marshalType) CreateCustomAttributes_COMThread() {
			reader.Dispatcher.VerifyAccess();
			var marshalType = reader.ReadFieldMarshalType_COMThread(MetadataToken, ReflectedType.Module, null);
			var cas = reader.ReadCustomAttributesCore_COMThread((uint)MetadataToken);
			var fieldOffset = MDAPI.GetFieldOffset(reader.MetaDataImport, (uint)ReflectedType.MetadataToken, 0x04000000 + Rid);
			return (cas, fieldOffset, marshalType);
		}
	}
}
