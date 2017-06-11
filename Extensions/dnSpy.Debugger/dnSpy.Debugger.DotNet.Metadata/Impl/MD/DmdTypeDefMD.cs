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
	sealed class DmdTypeDefMD : DmdTypeDef {
		public override DmdModule Module => reader.Module;
		public override string Namespace { get; }
		public override string Name { get; }
		public override DmdTypeAttributes Attributes { get; }

		readonly DmdEcma335MetadataReader reader;

		public DmdTypeDefMD(DmdEcma335MetadataReader reader, uint rid, IList<DmdCustomModifier> customModifiers) : base(rid, customModifiers) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			var row = reader.TablesStream.ReadTypeDefRow(rid);
			string ns = reader.StringsStream.Read(row.Namespace);
			Namespace = string.IsNullOrEmpty(ns) ? null : ns;
			Name = reader.StringsStream.ReadNoNull(row.Name);
			Attributes = (DmdTypeAttributes)row.Flags;
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => AppDomain.Intern(new DmdTypeDefMD(reader, Rid, customModifiers));
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.Intern(new DmdTypeDefMD(reader, Rid, null));

		protected override int GetDeclaringTypeToken() => 0x02000000 + (int)(reader.TablesStream.ReadNestedClassRow(reader.Metadata.GetNestedClassRid(Rid))?.EnclosingClass ?? 0);

		protected override int GetBaseTypeTokenCore() {
			uint extends = reader.TablesStream.ReadTypeDefRow(Rid).Extends;
			if (!CodedToken.TypeDefOrRef.Decode(extends, out uint token))
				return 0;
			return (int)token;
		}

		protected override DmdType[] CreateGenericParameters() {
			var ridList = reader.Metadata.GetGenericParamRidList(Table.TypeDef, Rid);
			if (ridList.Count == 0)
				return null;
			var genericParams = new DmdType[ridList.Count];
			for (int i = 0; i < genericParams.Length; i++) {
				uint rid = ridList[i];
				var row = reader.TablesStream.ReadGenericParamRow(rid) ?? new RawGenericParamRow();
				var gpName = reader.StringsStream.ReadNoNull(row.Name);
				var gpType = new DmdGenericParameterTypeMD(reader, rid, this, gpName, row.Number, (DmdGenericParameterAttributes)row.Flags, null);
				genericParams[i] = gpType;
			}
			return genericParams;
		}

		public override DmdFieldInfo[] ReadDeclaredFields(DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			var ridList = reader.Metadata.GetFieldRidList(Rid);
			if (ridList.Count == 0)
				return Array.Empty<DmdFieldInfo>();
			var fields = new DmdFieldInfo[ridList.Count];
			for (int i = 0; i < fields.Length; i++) {
				uint rid = ridList[i];
				fields[i] = reader.CreateFieldDef(rid, this, reflectedType, genericTypeArguments);
			}
			return fields;
		}

		public override DmdMethodBase[] ReadDeclaredMethods(DmdType reflectedType, IList<DmdType> genericTypeArguments, bool includeConstructors) {
			var ridList = reader.Metadata.GetMethodRidList(Rid);
			if (ridList.Count == 0)
				return Array.Empty<DmdMethodBase>();
			var fields = new DmdMethodBase[ridList.Count];
			for (int i = 0; i < fields.Length; i++) {
				uint rid = ridList[i];
				fields[i] = reader.CreateMethodDef(rid, this, reflectedType, genericTypeArguments);
			}
			return fields;
		}

		public override DmdPropertyInfo[] ReadDeclaredProperties(DmdType reflectedType, IList<DmdType> genericTypeArguments) => throw new NotImplementedException();//TODO:
		public override DmdEventInfo[] ReadDeclaredEvents(DmdType reflectedType, IList<DmdType> genericTypeArguments) => throw new NotImplementedException();//TODO:

		protected override DmdType[] ReadDeclaredInterfacesCore(IList<DmdType> genericTypeArguments) {
			var ridList = reader.Metadata.GetInterfaceImplRidList(Rid);
			if (ridList.Count == 0)
				return null;
			var res = new DmdType[ridList.Count];
			for (int i = 0; i < res.Length; i++) {
				uint rid = ridList[i];
				var row = reader.Metadata.TablesStream.ReadInterfaceImplRow(rid);
				if (row == null || !CodedToken.TypeDefOrRef.Decode(row.Interface, out uint token))
					return null;
				res[i] = Module.ResolveType((int)token, genericTypeArguments, null, throwOnError: true);
			}
			return res;
		}

		protected override DmdType[] CreateNestedTypes() {
			var ridList = reader.Metadata.GetNestedClassRidList(Rid);
			if (ridList.Count == 0)
				return null;
			var res = new DmdType[ridList.Count];
			for (int i = 0; i < res.Length; i++) {
				uint rid = ridList[i];
				var nestedType = Module.ResolveType(0x02000000 + (int)rid, null, null, throwOnError: false);
				if ((object)nestedType == null)
					return null;
				res[i] = nestedType;
			}
			return res;
		}

		protected override DmdCustomAttributeData[] CreateCustomAttributes() => reader.ReadCustomAttributes(MetadataToken);
	}
}
