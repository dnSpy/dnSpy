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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdMethodDefCOMD : DmdMethodDef {
		public override DmdMethodImplAttributes MethodImplementationFlags { get; }
		public override DmdMethodAttributes Attributes { get; }
		public override string Name { get; }

		readonly DmdComMetadataReader reader;
		readonly DmdMethodSignature methodSignature;

		public DmdMethodDefCOMD(DmdComMetadataReader reader, DmdMethodAttributes attributes, DmdMethodImplAttributes implementationFlags, uint rid, string name, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Dispatcher.VerifyAccess();
			MethodImplementationFlags = implementationFlags;
			Attributes = attributes;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			methodSignature = reader.ReadMethodSignature_COMThread(MDAPI.GetMethodSignatureBlob(reader.MetaDataImport, 0x06000000 + rid), DeclaringType!.GetGenericArguments(), GetGenericArguments(), isProperty: false);
		}

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		protected override DmdType[]? CreateGenericParameters() => COMThread(() => reader.CreateGenericParameters_COMThread(this));

		public override DmdMethodBody? GetMethodBody() => COMThread(() => reader.GetMethodBody_COMThread(this, DeclaringType!.GetGenericArguments(), GetGenericArguments()));
		internal override DmdMethodBody? GetMethodBody(IList<DmdType> genericMethodArguments) => COMThread(() => reader.GetMethodBody_COMThread(this, DeclaringType!.GetGenericArguments(), genericMethodArguments));
		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		protected override (DmdParameterInfo? returnParameter, DmdParameterInfo[] parameters) CreateParameters() => COMThread(() => reader.CreateParameters_COMThread(this, createReturnParameter: true))!;

		protected override (DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas, DmdImplMap? implMap) CreateCustomAttributes() => COMThread(CreateCustomAttributes_COMThread);
		(DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas, DmdImplMap? implMap) CreateCustomAttributes_COMThread() {
			reader.Dispatcher.VerifyAccess();
			var cas = reader.ReadCustomAttributes(MetadataToken);
			var sas = reader.ReadSecurityAttributes(MetadataToken);
			DmdImplMap? implMap;
			if (IsPinvokeImpl) {
				var name = MDAPI.GetPinvokeMapName(reader.MetaDataImport, (uint)MetadataToken);
				if (name is null || !MDAPI.GetPinvokeMapProps(reader.MetaDataImport, (uint)MetadataToken, out var attrs, out uint moduleToken))
					implMap = null;
				else {
					var module = MDAPI.GetModuleRefName(reader.MetaDataImport, moduleToken) ?? string.Empty;
					implMap = new DmdImplMap(attrs, name, module);
				}
			}
			else
				implMap = null;
			return (cas, sas, implMap);
		}

		private protected override DmdMethodSignature GetMethodSignatureCore(IList<DmdType> genericMethodArguments) => COMThread(() => reader.ReadMethodSignature_COMThread(MDAPI.GetMethodSignatureBlob(reader.MetaDataImport, 0x06000000 + Rid), DeclaringType!.GetGenericArguments(), genericMethodArguments, isProperty: false));
		protected override uint GetRVA() => 0;
	}
}
