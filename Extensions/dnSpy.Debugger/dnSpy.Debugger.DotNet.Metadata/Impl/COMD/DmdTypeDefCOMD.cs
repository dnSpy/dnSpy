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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdTypeDefCOMD : DmdTypeDef {
		public override DmdAppDomain AppDomain => reader.Module.AppDomain;
		public override DmdModule Module => reader.Module;
		public override string MetadataNamespace { get; }
		public override string MetadataName { get; }
		public override DmdTypeAttributes Attributes { get; }

		readonly DmdComMetadataReader reader;

		public DmdTypeDefCOMD(DmdComMetadataReader reader, uint rid, IList<DmdCustomModifier> customModifiers) : base(rid, customModifiers) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Dispatcher.VerifyAccess();
			uint token = 0x02000000 + rid;
			DmdTypeUtilities.SplitFullName(MDAPI.GetTypeDefName(reader.MetaDataImport, token) ?? string.Empty, out var @namespace, out var name);
			MetadataNamespace = @namespace;
			MetadataName = name;
			Attributes = FixAttributes(MDAPI.GetTypeDefAttributes(reader.MetaDataImport, token) ?? 0);
		}

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) {
			VerifyCustomModifiers(customModifiers);
			return AppDomain.Intern(COMThread(() => new DmdTypeDefCOMD(reader, Rid, customModifiers)));
		}

		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.Intern(COMThread(() => new DmdTypeDefCOMD(reader, Rid, null)));

		protected override DmdType GetDeclaringType() {
			if ((Attributes & DmdTypeAttributes.VisibilityMask) <= DmdTypeAttributes.Public)
				return null;
			return COMThread(GetDeclaringType_COMThread);
		}

		DmdType GetDeclaringType_COMThread() {
			reader.Dispatcher.VerifyAccess();
			return reader.Module.ResolveType((int)(0x02000000 + reader.GetEnclosingTypeDefRid_COMThread(Rid)), (IList<DmdType>)null, null, DmdResolveOptions.None);
		}

		protected override DmdType GetBaseTypeCore(IList<DmdType> genericTypeArguments) => COMThread(() => GetBaseTypeCore_COMThread(genericTypeArguments));
		DmdType GetBaseTypeCore_COMThread(IList<DmdType> genericTypeArguments) {
			reader.Dispatcher.VerifyAccess();
			uint extends = MDAPI.GetTypeDefExtends(reader.MetaDataImport, 0x02000000 + Rid);
			return reader.Module.ResolveType((int)extends, genericTypeArguments, null, DmdResolveOptions.None);
		}

		protected override DmdType[] CreateGenericParameters() => COMThread(CreateGenericParameters_COMThread);
		DmdType[] CreateGenericParameters_COMThread() {
			reader.Dispatcher.VerifyAccess();
			throw new NotImplementedException();//TODO:
		}

		public override DmdFieldInfo[] ReadDeclaredFields(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			COMThread(() => ReadDeclaredFields_COMThread(declaringType, reflectedType, genericTypeArguments));
		DmdFieldInfo[] ReadDeclaredFields_COMThread(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			reader.Dispatcher.VerifyAccess();
			var tokens = MDAPI.GetFieldTokens(reader.MetaDataImport, (uint)MetadataToken);
			if (tokens.Length == 0)
				return Array.Empty<DmdFieldInfo>();
			var fields = new DmdFieldInfo[tokens.Length];
			for (int i = 0; i < fields.Length; i++) {
				uint rid = tokens[i] & 0x00FFFFFF;
				fields[i] = reader.CreateFieldDef_COMThread(rid, declaringType, reflectedType, genericTypeArguments);
			}
			return fields;
		}

		public override DmdMethodBase[] ReadDeclaredMethods(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			COMThread(() => ReadDeclaredMethods_COMThread(declaringType, reflectedType, genericTypeArguments));
		DmdMethodBase[] ReadDeclaredMethods_COMThread(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			reader.Dispatcher.VerifyAccess();
			var tokens = MDAPI.GetMethodTokens(reader.MetaDataImport, (uint)MetadataToken);
			if (tokens.Length == 0)
				return Array.Empty<DmdMethodBase>();
			var methods = new DmdMethodBase[tokens.Length];
			for (int i = 0; i < methods.Length; i++) {
				uint rid = tokens[i] & 0x00FFFFFF;
				methods[i] = reader.CreateMethodDef_COMThread(rid, declaringType, reflectedType, genericTypeArguments);
			}
			return methods;
		}

		public override DmdPropertyInfo[] ReadDeclaredProperties(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			COMThread(() => ReadDeclaredProperties_COMThread(declaringType, reflectedType, genericTypeArguments));
		DmdPropertyInfo[] ReadDeclaredProperties_COMThread(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			reader.Dispatcher.VerifyAccess();
			throw new NotImplementedException();//TODO:
		}

		public override DmdEventInfo[] ReadDeclaredEvents(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			COMThread(() => ReadDeclaredEvents_COMThread(declaringType, reflectedType, genericTypeArguments));
		DmdEventInfo[] ReadDeclaredEvents_COMThread(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			reader.Dispatcher.VerifyAccess();
			throw new NotImplementedException();//TODO:
		}

		protected override DmdType[] ReadDeclaredInterfacesCore(IList<DmdType> genericTypeArguments) =>
			COMThread(() => ReadDeclaredInterfacesCore_COMThread(genericTypeArguments));
		DmdType[] ReadDeclaredInterfacesCore_COMThread(IList<DmdType> genericTypeArguments) {
			reader.Dispatcher.VerifyAccess();
			throw new NotImplementedException();//TODO:
		}

		protected override DmdType[] CreateNestedTypes() => COMThread(CreateNestedTypes_COMThread);
		DmdType[] CreateNestedTypes_COMThread() {
			reader.Dispatcher.VerifyAccess();
			throw new NotImplementedException();//TODO:
		}

		public override (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas) CreateCustomAttributes() => COMThread(CreateCustomAttributes_COMThread);
		(DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas) CreateCustomAttributes_COMThread() {
			reader.Dispatcher.VerifyAccess();
			var cas = reader.ReadCustomAttributes(MetadataToken);
			var sas = reader.ReadSecurityAttributes(MetadataToken);
			return (cas, sas);
		}

		protected override (int packingSize, int classSize) GetClassLayout() => COMThread(GetClassLayout_COMThread);
		(int packingSize, int classSize) GetClassLayout_COMThread() {
			reader.Dispatcher.VerifyAccess();
			MDAPI.GetClassLayout(reader.MetaDataImport, 0x02000000 + Rid, out ushort packingSize, out uint classSize);
			return (packingSize, (int)classSize);
		}
	}
}
