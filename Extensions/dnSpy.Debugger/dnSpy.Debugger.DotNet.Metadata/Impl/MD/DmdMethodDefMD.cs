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
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdMethodDefMD : DmdMethodDef {
		public override DmdMethodImplAttributes MethodImplementationFlags { get; }
		public override DmdMethodAttributes Attributes { get; }
		public override string Name { get; }

		readonly DmdEcma335MetadataReader reader;
		readonly DmdMethodSignature methodSignature;

		public DmdMethodDefMD(DmdEcma335MetadataReader reader, RawMethodRow row, uint rid, string name, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			MethodImplementationFlags = (DmdMethodImplAttributes)row.ImplFlags;
			Attributes = (DmdMethodAttributes)row.Flags;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			methodSignature = reader.ReadMethodSignature(row.Signature, DeclaringType.GetGenericArguments(), GetGenericArguments(), isProperty: false);
		}

		protected override DmdType[] CreateGenericParameters() => reader.CreateGenericParameters(this);

		public override DmdMethodBody GetMethodBody() => reader.GetMethodBody(this, DeclaringType.GetGenericArguments(), GetGenericArguments());
		internal override DmdMethodBody GetMethodBody(IList<DmdType> genericMethodArguments) => reader.GetMethodBody(this, DeclaringType.GetGenericArguments(), genericMethodArguments);
		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		protected override (DmdParameterInfo returnParameter, DmdParameterInfo[] parameters) CreateParameters() => reader.CreateParameters(this, createReturnParameter: true);
		protected override (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas, DmdImplMap? implMap) CreateCustomAttributes() {
			var cas = reader.ReadCustomAttributes(MetadataToken);
			var sas = reader.ReadSecurityAttributes(MetadataToken);
			DmdImplMap? implMap;
			if (IsPinvokeImpl) {
				if (!reader.TablesStream.TryReadImplMapRow(reader.Metadata.GetImplMapRid(Table.Method, Rid), out var row))
					implMap = null;
				else {
					var name = reader.StringsStream.ReadNoNull(row.ImportName);
					reader.TablesStream.TryReadModuleRefRow(row.ImportScope, out var modRow);
					var module = reader.StringsStream.ReadNoNull(modRow.Name);
					implMap = new DmdImplMap((DmdPInvokeAttributes)row.MappingFlags, name, module);
				}
			}
			else
				implMap = null;
			return (cas, sas, implMap);
		}

		private protected override DmdMethodSignature GetMethodSignatureCore(IList<DmdType> genericMethodArguments) {
			bool b = reader.TablesStream.TryReadMethodRow(Rid, out var row);
			Debug.Assert(b);
			return reader.ReadMethodSignature(row.Signature, DeclaringType.GetGenericArguments(), genericMethodArguments, isProperty: false);
		}

		protected override uint GetRVA() => reader.GetRVA(this);
	}
}
