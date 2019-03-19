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
	sealed class DmdTypeDefMD : DmdTypeDef {
		public override DmdAppDomain AppDomain => reader.Module.AppDomain;
		public override DmdModule Module => reader.Module;
		public override string MetadataNamespace { get; }
		public override string MetadataName { get; }
		public override DmdTypeAttributes Attributes { get; }

		readonly DmdEcma335MetadataReader reader;

		public DmdTypeDefMD(DmdEcma335MetadataReader reader, uint rid, IList<DmdCustomModifier> customModifiers) : base(rid, customModifiers) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			bool b = reader.TablesStream.TryReadTypeDefRow(rid, out var row);
			Debug.Assert(b);
			string ns = reader.StringsStream.Read(row.Namespace);
			MetadataNamespace = string.IsNullOrEmpty(ns) ? null : ns;
			MetadataName = reader.StringsStream.ReadNoNull(row.Name);
			Attributes = FixAttributes((DmdTypeAttributes)row.Flags);
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => AppDomain.Intern(new DmdTypeDefMD(reader, Rid, VerifyCustomModifiers(customModifiers)));
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.Intern(new DmdTypeDefMD(reader, Rid, null));

		protected override DmdType GetDeclaringType() {
			if (!reader.TablesStream.TryReadNestedClassRow(reader.Metadata.GetNestedClassRid(Rid), out var row))
				return null;
			return Module.ResolveType(0x02000000 + (int)row.EnclosingClass, DmdResolveOptions.None);
		}

		protected override DmdType GetBaseTypeCore(IList<DmdType> genericTypeArguments) {
			if (!reader.TablesStream.TryReadTypeDefRow(Rid, out var row))
				return null;
			if (!CodedToken.TypeDefOrRef.Decode(row.Extends, out uint token))
				return null;
			return reader.Module.ResolveType((int)token, genericTypeArguments, null, DmdResolveOptions.None);
		}

		protected override DmdType[] CreateGenericParameters() {
			var ridList = reader.Metadata.GetGenericParamRidList(Table.TypeDef, Rid);
			if (ridList.Count == 0)
				return null;
			var genericParams = new DmdType[ridList.Count];
			for (int i = 0; i < genericParams.Length; i++) {
				uint rid = ridList[i];
				reader.TablesStream.TryReadGenericParamRow(rid, out var row);
				var gpName = reader.StringsStream.ReadNoNull(row.Name);
				var gpType = new DmdGenericParameterTypeMD(reader, rid, this, gpName, row.Number, (DmdGenericParameterAttributes)row.Flags, null);
				genericParams[i] = gpType;
			}
			return genericParams;
		}

		public override DmdFieldInfo[] ReadDeclaredFields(DmdType declaringType, DmdType reflectedType) {
			var ridList = reader.Metadata.GetFieldRidList(Rid);
			if (ridList.Count == 0)
				return Array.Empty<DmdFieldInfo>();
			var fields = new DmdFieldInfo[ridList.Count];
			for (int i = 0; i < fields.Length; i++) {
				uint rid = ridList[i];
				fields[i] = reader.CreateFieldDef(rid, declaringType, reflectedType);
			}
			return fields;
		}

		public override DmdMethodBase[] ReadDeclaredMethods(DmdType declaringType, DmdType reflectedType) {
			var ridList = reader.Metadata.GetMethodRidList(Rid);
			if (ridList.Count == 0)
				return Array.Empty<DmdMethodBase>();
			var methods = new DmdMethodBase[ridList.Count];
			for (int i = 0; i < methods.Length; i++) {
				uint rid = ridList[i];
				methods[i] = reader.CreateMethodDef(rid, declaringType, reflectedType);
			}
			return methods;
		}

		public override DmdPropertyInfo[] ReadDeclaredProperties(DmdType declaringType, DmdType reflectedType) {
			var mapRid = reader.Metadata.GetPropertyMapRid(Rid);
			var ridList = reader.Metadata.GetPropertyRidList(mapRid);
			if (ridList.Count == 0)
				return Array.Empty<DmdPropertyInfo>();
			var properties = new DmdPropertyInfo[ridList.Count];
			for (int i = 0; i < properties.Length; i++) {
				uint rid = ridList[i];
				properties[i] = reader.CreatePropertyDef(rid, declaringType, reflectedType);
			}
			return properties;
		}

		public override DmdEventInfo[] ReadDeclaredEvents(DmdType declaringType, DmdType reflectedType) {
			var mapRid = reader.Metadata.GetEventMapRid(Rid);
			var ridList = reader.Metadata.GetEventRidList(mapRid);
			if (ridList.Count == 0)
				return Array.Empty<DmdEventInfo>();
			var events = new DmdEventInfo[ridList.Count];
			for (int i = 0; i < events.Length; i++) {
				uint rid = ridList[i];
				events[i] = reader.CreateEventDef(rid, declaringType, reflectedType);
			}
			return events;
		}

		protected override DmdType[] ReadDeclaredInterfacesCore(IList<DmdType> genericTypeArguments) {
			var ridList = reader.Metadata.GetInterfaceImplRidList(Rid);
			if (ridList.Count == 0)
				return null;
			var res = new DmdType[ridList.Count];
			for (int i = 0; i < res.Length; i++) {
				uint rid = ridList[i];
				if (!reader.Metadata.TablesStream.TryReadInterfaceImplRow(rid, out var row))
					return null;
				if (!CodedToken.TypeDefOrRef.Decode(row.Interface, out uint token))
					return null;
				res[i] = Module.ResolveType((int)token, genericTypeArguments, null, DmdResolveOptions.ThrowOnError);
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
				var nestedType = Module.ResolveType(0x02000000 + (int)rid, DmdResolveOptions.None);
				if ((object)nestedType == null)
					return null;
				res[i] = nestedType;
			}
			return res;
		}

		public override (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas) CreateCustomAttributes() {
			var cas = reader.ReadCustomAttributes(MetadataToken);
			var sas = reader.ReadSecurityAttributes(MetadataToken);
			return (cas, sas);
		}

		protected override (int packingSize, int classSize) GetClassLayout() {
			if (!reader.TablesStream.TryReadClassLayoutRow(reader.Metadata.GetClassLayoutRid(Rid), out var row))
				return (0, 0);
			return (row.PackingSize, (int)row.ClassSize);
		}
	}
}
