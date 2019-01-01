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
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdPropertyDefMD : DmdPropertyDef {
		public override string Name { get; }
		public override DmdPropertyAttributes Attributes { get; }

		readonly DmdEcma335MetadataReader reader;
		readonly DmdMethodSignature methodSignature;

		public DmdPropertyDefMD(DmdEcma335MetadataReader reader, uint rid, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			bool b = reader.TablesStream.TryReadPropertyRow(rid, out var row);
			Debug.Assert(b);
			Name = reader.StringsStream.ReadNoNull(row.Name);
			Attributes = (DmdPropertyAttributes)row.PropFlags;
			methodSignature = reader.ReadMethodSignature(row.Type, DeclaringType.GetGenericArguments(), null, isProperty: true);
		}

		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		protected override DmdCustomAttributeData[] CreateCustomAttributes() => reader.ReadCustomAttributes(MetadataToken);
		public override object GetRawConstantValue() => reader.ReadConstant(MetadataToken).value;

		protected override void GetMethods(out DmdMethodInfo getMethod, out DmdMethodInfo setMethod, out DmdMethodInfo[] otherMethods) {
			getMethod = null;
			setMethod = null;
			List<DmdMethodInfo> otherMethodsList = null;

			var ridList = reader.Metadata.GetMethodSemanticsRidList(Table.Property, Rid);
			for (int i = 0; i < ridList.Count; i++) {
				if (!reader.TablesStream.TryReadMethodSemanticsRow(ridList[i], out var row))
					continue;
				var method = ReflectedType.GetMethod(Module, 0x06000000 + (int)row.Method) as DmdMethodInfo;
				if ((object)method == null)
					continue;

				switch ((MethodSemanticsAttributes)row.Semantic) {
				case MethodSemanticsAttributes.Setter:
					if ((object)setMethod == null)
						setMethod = method;
					break;

				case MethodSemanticsAttributes.Getter:
					if ((object)getMethod == null)
						getMethod = method;
					break;

				case MethodSemanticsAttributes.Other:
					if (otherMethodsList == null)
						otherMethodsList = new List<DmdMethodInfo>();
					otherMethodsList.Add(method);
					break;
				}
			}

			otherMethods = otherMethodsList?.ToArray();
		}
	}
}
