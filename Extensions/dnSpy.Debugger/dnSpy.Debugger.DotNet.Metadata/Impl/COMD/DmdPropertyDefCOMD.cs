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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdPropertyDefCOMD : DmdPropertyDef {
		public override string Name { get; }
		public override DmdPropertyAttributes Attributes { get; }

		readonly DmdComMetadataReader reader;
		readonly DmdMethodSignature methodSignature;

		public DmdPropertyDefCOMD(DmdComMetadataReader reader, uint rid, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Dispatcher.VerifyAccess();
			uint token = 0x17000000 + rid;
			Name = MDAPI.GetPropertyName(reader.MetaDataImport, token) ?? string.Empty;
			Attributes = MDAPI.GetPropertyAttributes(reader.MetaDataImport, token);
			methodSignature = reader.ReadMethodSignature_COMThread(MDAPI.GetPropertySignatureBlob(reader.MetaDataImport, token), DeclaringType.GetGenericArguments(), null, isProperty: true);
		}

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		protected override DmdCustomAttributeData[] CreateCustomAttributes() => COMThread(() => reader.ReadCustomAttributesCore_COMThread((uint)MetadataToken));
		public override object GetRawConstantValue() => COMThread(() => reader.ReadPropertyConstant_COMThread(MetadataToken).value);

		protected override void GetMethods(out DmdMethodInfo getMethod, out DmdMethodInfo setMethod, out DmdMethodInfo[] otherMethods) {
			var info = COMThread(GetMethods_COMThread);
			getMethod = info.getMethod;
			setMethod = info.setMethod;
			otherMethods = info.otherMethods;
		}

		(DmdMethodInfo getMethod, DmdMethodInfo setMethod, DmdMethodInfo[] otherMethods) GetMethods_COMThread() {
			reader.Dispatcher.VerifyAccess();
			uint token = 0x17000000 + Rid;
			MDAPI.GetPropertyGetterSetter(reader.MetaDataImport, token, out uint getToken, out uint setToken);
			var otherMethodTokens = MDAPI.GetPropertyOtherMethodTokens(reader.MetaDataImport, token);
			var getMethod = Lookup_COMThread(getToken);
			var setMethod = Lookup_COMThread(setToken);
			var otherMethods = otherMethodTokens.Length == 0 ? Array.Empty<DmdMethodInfo>() : new DmdMethodInfo[otherMethodTokens.Length];
			for (int i = 0; i < otherMethods.Length; i++) {
				var otherMethod = Lookup_COMThread(otherMethodTokens[i]);
				if ((object)otherMethod == null) {
					otherMethods = Array.Empty<DmdMethodInfo>();
					break;
				}
				otherMethods[i] = otherMethod;
			}
			return (getMethod, setMethod, otherMethods);
		}

		DmdMethodInfo Lookup_COMThread(uint token) {
			if ((token >> 24) != 0x06 || (token & 0x00FFFFFF) == 0)
				return null;
			var method = ReflectedType.GetMethod(Module, (int)token) as DmdMethodInfo;
			Debug.Assert((object)method != null);
			return method;
		}
	}
}
