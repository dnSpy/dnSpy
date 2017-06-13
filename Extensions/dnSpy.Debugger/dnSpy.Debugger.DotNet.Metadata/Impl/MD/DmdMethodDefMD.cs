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
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdMethodDefMD : DmdMethodDef {
		public override DmdMethodImplAttributes MethodImplementationFlags { get; }
		public override DmdMethodAttributes Attributes { get; }
		public override string Name { get; }

		readonly DmdEcma335MetadataReader reader;
		readonly DmdMethodSignature methodSignature;
		readonly IList<DmdType> genericTypeArguments;

		public DmdMethodDefMD(DmdEcma335MetadataReader reader, RawMethodRow row, uint rid, string name, DmdTypeDef declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			MethodImplementationFlags = (DmdMethodImplAttributes)row.ImplFlags;
			Attributes = (DmdMethodAttributes)row.Flags;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			methodSignature = reader.ReadMethodSignature(row.Signature, genericTypeArguments, GetGenericArguments(), isProperty: false);

			// Needed by GetMethodSignatureCore() and GetMethodBody(). It's not always identical to ReflectedType.GetGenericArguments()
			this.genericTypeArguments = genericTypeArguments;
		}

		protected override DmdType[] CreateGenericParameters() => reader.CreateGenericParameters(this);

		public override DmdMethodBody GetMethodBody() => reader.GetMethodBody(this, genericTypeArguments, GetGenericArguments());
		internal override DmdMethodBody GetMethodBody(IList<DmdType> genericMethodArguments) => reader.GetMethodBody(this, genericTypeArguments, genericMethodArguments);
		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		protected override (DmdParameterInfo returnParameter, DmdParameterInfo[] parameters) CreateParameters() => reader.CreateParameters(this, createReturnParameter: true);
		protected override (DmdCustomAttributeData[] cas, DmdImplMap? implMap) CreateCustomAttributes() {
			var cas = reader.ReadCustomAttributes(MetadataToken);
			DmdImplMap? implMap;
			if (IsPinvokeImpl) {
				var row = reader.TablesStream.ReadImplMapRow(reader.Metadata.GetImplMapRid(Table.Method, Rid));
				if (row == null)
					implMap = null;
				else {
					var name = reader.StringsStream.ReadNoNull(row.ImportName);
					var modRow = reader.TablesStream.ReadModuleRefRow(row.ImportScope);
					var module = reader.StringsStream.ReadNoNull(modRow?.Name ?? 0);
					implMap = new DmdImplMap((DmdPInvokeAttributes)row.MappingFlags, name, module);
				}
			}
			else
				implMap = null;
			return (cas, implMap);
		}

		internal override DmdMethodSignature GetMethodSignatureCore(IList<DmdType> genericMethodArguments) {
			var row = reader.TablesStream.ReadMethodRow(Rid);
			return reader.ReadMethodSignature(row.Signature, genericTypeArguments, genericMethodArguments, isProperty: false);
		}
	}
}
