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
using System.Diagnostics;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using SSP = System.Security.Permissions;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdComMetadataReader : DmdMetadataReaderBase {
		public override Guid ModuleVersionId {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __moduleVersionId_DONT_USE;
			}
		}

		public override int MDStreamVersion {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __mdStreamVersion_DONT_USE;
			}
		}

		public override string ModuleScopeName {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __moduleScopeName_DONT_USE;
			}
		}

		public override string ImageRuntimeVersion {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __imageRuntimeVersion_DONT_USE;
			}
		}

		void InitializeModuleProperties() {
			if (modulePropsInitd)
				return;
			if (IsCOMThread)
				InitializeModuleProperties_COMThread();
			else
				COMThread(InitializeModuleProperties_COMThread);
		}

		void InitializeModuleProperties_COMThread() {
			dispatcher.VerifyAccess();
			if (modulePropsInitd)
				return;

			__imageRuntimeVersion_DONT_USE = MDAPI.GetModuleVersionString(MetaDataImport) ?? string.Empty;
			__moduleVersionId_DONT_USE = MDAPI.GetModuleMvid(MetaDataImport) ?? Guid.Empty;
			__moduleScopeName_DONT_USE = MDAPI.GetModuleName(MetaDataImport) ?? string.Empty;
			__machine_DONT_USE = MDAPI.GetModuleMachineAndPEKind(MetaDataImport, out __peKind_DONT_USE) ?? DmdImageFileMachine.I386;

			bool isV1x =
				__imageRuntimeVersion_DONT_USE.StartsWith("v1.", StringComparison.OrdinalIgnoreCase) ||
				StringComparer.OrdinalIgnoreCase.Equals(__imageRuntimeVersion_DONT_USE, MDHeaderRuntimeVersion.MS_CLR_10_RETAIL) ||
				StringComparer.OrdinalIgnoreCase.Equals(__imageRuntimeVersion_DONT_USE, MDHeaderRuntimeVersion.MS_CLR_10_COMPLUS);
			if (isV1x)
				__mdStreamVersion_DONT_USE = 0x00010000;
			else
				__mdStreamVersion_DONT_USE = 0x00020000;

			modulePropsInitd = true;
		}
		bool modulePropsInitd;
		Guid __moduleVersionId_DONT_USE;
		int __mdStreamVersion_DONT_USE;
		string __moduleScopeName_DONT_USE;
		string __imageRuntimeVersion_DONT_USE;
		DmdPortableExecutableKinds __peKind_DONT_USE;
		DmdImageFileMachine __machine_DONT_USE;

		// It doesn't seem to be possible to get the entry point from the COM MetaData API
		public override DmdMethodInfo EntryPoint => null;

		internal IMetaDataImport2 MetaDataImport {
			get {
				Debug.Assert(IsCOMThread);
				return __metaDataImport_DONT_USE;
			}
		}

		internal IMetaDataAssemblyImport MetaDataAssemblyImport {
			get {
				Debug.Assert(IsCOMThread);
				return __metaDataAssemblyImport_DONT_USE;
			}
		}

		internal DmdModule Module => module;
		internal DmdDispatcher Dispatcher => dispatcher;

		readonly DmdModuleImpl module;
		readonly IMetaDataImport2 __metaDataImport_DONT_USE;
		readonly IMetaDataAssemblyImport __metaDataAssemblyImport_DONT_USE;
		readonly DmdDispatcher dispatcher;
		Dictionary<uint, List<uint>> ridToNested;
		Dictionary<uint, uint> ridToEnclosing;
		readonly LazyList<DmdTypeRef> typeRefList;
		readonly LazyList<DmdFieldDef, DmdTypeDef> fieldList;
		readonly LazyList<DmdTypeDef> typeDefList;
		readonly LazyList<DmdMethodBase, DmdTypeDef> methodList;
		readonly LazyList2<DmdMemberInfo, IList<DmdType>, IList<DmdType>> memberRefList;
		readonly LazyList<DmdEventDef, DmdTypeDef> eventList;
		readonly LazyList<DmdPropertyDef, DmdTypeDef> propertyList;
		readonly LazyList2<DmdType, IList<DmdType>> typeSpecList;
		readonly LazyList<DmdTypeRef> exportedTypeList;
		readonly DmdNullGlobalType globalTypeIfThereAreNoTypes;
		readonly Dictionary<IntPtr, DmdType> fieldTypeCache;
		readonly Dictionary<IntPtr, DmdMethodSignature> methodSignatureCache;

		public DmdComMetadataReader(DmdModuleImpl module, IMetaDataImport2 metaDataImport, DmdDispatcher dispatcher) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			__metaDataImport_DONT_USE = metaDataImport ?? throw new ArgumentNullException(nameof(metaDataImport));
			__metaDataAssemblyImport_DONT_USE = (IMetaDataAssemblyImport)metaDataImport;
			this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

			fieldTypeCache = new Dictionary<IntPtr, DmdType>();
			methodSignatureCache = new Dictionary<IntPtr, DmdMethodSignature>();

			typeRefList = new LazyList<DmdTypeRef>(TryCreateTypeRefCOMD_COMThread);
			fieldList = new LazyList<DmdFieldDef, DmdTypeDef>(CreateResolvedField_COMThread);
			typeDefList = new LazyList<DmdTypeDef>(TryCreateTypeDefCOMD_COMThread);
			methodList = null;//TODO: new LazyList<DmdMethodBase, DmdTypeDef>(CreateResolvedMethod);
			memberRefList = null;//TODO: new LazyList2<DmdMemberInfo, IList<DmdType>, IList<DmdType>>(CreateResolvedMemberRef);
			eventList = null;//TODO: new LazyList<DmdEventDef, DmdTypeDef>(CreateResolvedEvent);
			propertyList = null;//TODO: new LazyList<DmdPropertyDef, DmdTypeDef>(CreateResolvedProperty);
			typeSpecList = new LazyList2<DmdType, IList<DmdType>>(TryCreateTypeSpecCOMD_COMThread);
			exportedTypeList = new LazyList<DmdTypeRef>(TryCreateExportedTypeCOMD_COMThread);

			globalTypeIfThereAreNoTypes = new DmdNullGlobalType(module, null);
		}

		bool IsCOMThread => dispatcher.CheckAccess();
		void COMThread(Action action) {
			Debug.Assert(!IsCOMThread);
			dispatcher.Invoke(action);
		}
		T COMThread<T>(Func<T> action) {
			Debug.Assert(!IsCOMThread);
			return dispatcher.Invoke(action);
		}

		DmdTypeRefCOMD TryCreateTypeRefCOMD_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (!MDAPI.IsValidToken(MetaDataImport, 0x01000000 + rid))
				return null;
			return new DmdTypeRefCOMD(this, rid, null);
		}

		DmdTypeDefCOMD TryCreateTypeDefCOMD_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (!MDAPI.IsValidToken(MetaDataImport, 0x02000000 + rid))
				return null;
			return new DmdTypeDefCOMD(this, rid, null);
		}

		DmdExportedTypeCOMD TryCreateExportedTypeCOMD_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (!MDAPI.IsValidToken(MetaDataImport, 0x27000000 + rid))
				return null;
			return new DmdExportedTypeCOMD(this, rid, null);
		}

		internal uint GetEnclosingTypeDefRid_COMThread(uint typeDefRid) {
			dispatcher.VerifyAccess();
			InitializeTypeTables_COMThread();
			bool b = ridToEnclosing.TryGetValue(typeDefRid, out uint enclTypeRid);
			Debug.Assert(b);
			return enclTypeRid;
		}

		void InitializeTypeTables_COMThread() {
			dispatcher.VerifyAccess();
			if (ridToNested != null)
				return;

			var allTypes = MDAPI.GetTypeDefTokens(MetaDataImport);
			int capacity = allTypes.Length;
			ridToNested = new Dictionary<uint, List<uint>>(capacity);
			ridToEnclosing = new Dictionary<uint, uint>(capacity);
			UpdateTypeTables_COMThread(allTypes);
		}

		void UpdateTypeTables_COMThread(uint[] tokens) {
			dispatcher.VerifyAccess();
			Array.Sort(tokens);
			foreach (uint token in tokens) {
				uint rid = token & 0x00FFFFFF;
				Debug.Assert(rid != 0);
				Debug.Assert(!ridToNested.ContainsKey(rid));

				var enclTypeToken = new MDToken(MDAPI.GetTypeDefEnclosingType(MetaDataImport, token));
				if (enclTypeToken.Rid != 0 && !MDAPI.IsValidToken(MetaDataImport, enclTypeToken.Raw)) {
					// Here if it's an obfuscated assembly with invalid MD
					enclTypeToken = new MDToken(Table.TypeDef, 0);
				}
				var enclTypeRid = enclTypeToken.Rid;
				if (enclTypeRid == 0) {
				} // All nested types must be after their enclosing type
				else if (!ridToNested.TryGetValue(enclTypeRid, out var enclTypeList)) {
					// Here if it's an obfuscated assembly with invalid MD
					enclTypeRid = 0;
				}
				else {
					if (enclTypeList == null)
						ridToNested[enclTypeRid] = enclTypeList = new List<uint>();
					enclTypeList.Add(rid);
				}

				ridToNested[rid] = null;
				ridToEnclosing[rid] = enclTypeRid;
			}
		}

		(DmdType type, bool containedGenericParams) TryCreateTypeSpecCOMD_COMThread(uint rid, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			uint token = 0x1B000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return (null, containedGenericParams: true);
			var blob = MDAPI.GetTypeSpecSignatureBlob(MetaDataImport, token);
			if (blob.addr == IntPtr.Zero)
				return (null, containedGenericParams: true);
			return DmdSignatureReader.ReadTypeSignature(module, new DmdPointerDataStream(blob), genericTypeArguments, resolveTypes);
		}

		DmdFieldDefCOMD CreateResolvedField_COMThread(uint rid, DmdTypeDef declaringType) {
			dispatcher.VerifyAccess();
			if ((object)declaringType == null)
				declaringType = ResolveTypeDef_COMThread(MDAPI.GetFieldOwnerRid(MetaDataImport, 0x04000000 + rid)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef_COMThread(MDAPI.GetFieldOwnerRid(MetaDataImport, 0x04000000 + rid)));
			return CreateFieldDefCore_COMThread(rid, declaringType, declaringType, declaringType.GetGenericArguments());
		}

		internal DmdFieldDef CreateFieldDef_COMThread(uint rid, DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			if ((object)declaringType == reflectedType && declaringType is DmdTypeDef declaringTypeDef) {
				Debug.Assert(declaringTypeDef.GetGenericArguments() == genericTypeArguments);
				return ResolveFieldDef_COMThread(rid, declaringTypeDef);
			}
			return CreateFieldDefCore_COMThread(rid, declaringType, reflectedType, genericTypeArguments);
		}

		DmdFieldDefCOMD CreateFieldDefCore_COMThread(uint rid, DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			return new DmdFieldDefCOMD(this, rid, declaringType, reflectedType, genericTypeArguments);
		}

		internal DmdType ReadFieldType_COMThread((IntPtr addr, uint size) signature, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			if (fieldTypeCache.TryGetValue(signature.addr, out var fieldType)) {
				if ((object)fieldType != null)
					return fieldType;
				var info = ReadFieldTypeCore_COMThread(signature, genericTypeArguments);
				Debug.Assert(info.containedGenericParams);
				return info.fieldType;
			}
			else {
				var info = ReadFieldTypeCore_COMThread(signature, genericTypeArguments);
				if (info.containedGenericParams)
					fieldTypeCache.Add(signature.addr, null);
				else
					fieldTypeCache.Add(signature.addr, info.fieldType);
				return info.fieldType;
			}
		}

		(DmdType fieldType, bool containedGenericParams) ReadFieldTypeCore_COMThread((IntPtr addr, uint size) signature, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			if (signature.addr == IntPtr.Zero)
				return (module.AppDomain.System_Void, false);
			return DmdSignatureReader.ReadFieldSignature(module, new DmdPointerDataStream(signature), genericTypeArguments, resolveTypes);
		}

		public override DmdTypeDef[] GetTypes() {
			if (IsCOMThread)
				return GetTypes_COMThread();
			else
				return COMThread(GetTypes_COMThread);
		}

		DmdTypeDef[] GetTypes_COMThread() {
			dispatcher.VerifyAccess();
			var result = new List<DmdTypeDef>();
			for (uint rid = 1; rid <= 0x00FFFFFF; rid++) {
				var type = ResolveTypeDef_COMThread(rid);
				if ((object)type == null)
					break;
				result.Add(type);
			}
			// This should never happen but we must return at least one type
			if (result.Count == 0)
				return new DmdTypeDef[] { globalTypeIfThereAreNoTypes };
			return result.ToArray();
		}

		public override DmdTypeRef[] GetExportedTypes() {
			if (IsCOMThread)
				return GetExportedTypes_COMThread();
			else
				return COMThread(GetExportedTypes_COMThread);
		}

		DmdTypeRef[] GetExportedTypes_COMThread() {
			dispatcher.VerifyAccess();
			List<DmdTypeRef> result = null;
			for (uint rid = 1; rid <= 0x00FFFFFF; rid++) {
				var type = ResolveExportedType_COMThread(rid);
				if ((object)type == null)
					break;
				if (result == null)
					result = new List<DmdTypeRef>();
				result.Add(type);
			}
			return result?.ToArray() ?? Array.Empty<DmdTypeRef>();
		}

		protected override DmdTypeRef ResolveTypeRef(uint rid) {
			if (IsCOMThread)
				return ResolveTypeRef_COMThread(rid);
			else
				return COMThread(() => ResolveTypeRef_COMThread(rid));
		}

		DmdTypeRef ResolveTypeRef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return typeRefList[rid - 1];
		}

		protected override DmdTypeDef ResolveTypeDef(uint rid) {
			if (IsCOMThread)
				return ResolveTypeDef_COMThread(rid);
			else
				return COMThread(() => ResolveTypeDef_COMThread(rid));
		}

		DmdTypeDef ResolveTypeDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			var type = typeDefList[rid - 1];
			if ((object)type == null && rid == 1)
				return globalTypeIfThereAreNoTypes;
			return type;
		}

		protected override DmdFieldDef ResolveFieldDef(uint rid) {
			if (IsCOMThread)
				return ResolveFieldDef_COMThread(rid);
			else
				return COMThread(() => ResolveFieldDef_COMThread(rid));
		}

		DmdFieldDef ResolveFieldDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return fieldList[rid - 1, null];
		}

		DmdFieldDef ResolveFieldDef_COMThread(uint rid, DmdTypeDef declaringType) {
			dispatcher.VerifyAccess();
			return fieldList[rid - 1, declaringType];
		}

		protected override DmdMethodBase ResolveMethodDef(uint rid) {
			if (IsCOMThread)
				return ResolveMethodDef_COMThread(rid);
			else
				return COMThread(() => ResolveMethodDef_COMThread(rid));
		}

		DmdMethodBase ResolveMethodDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return methodList[rid - 1, null];
		}

		protected override DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			if (IsCOMThread)
				return ResolveMemberRef_COMThread(rid, genericTypeArguments, genericMethodArguments);
			else
				return COMThread(() => ResolveMemberRef_COMThread(rid, genericTypeArguments, genericMethodArguments));
		}

		DmdMemberInfo ResolveMemberRef_COMThread(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			dispatcher.VerifyAccess();
			return memberRefList[rid - 1, genericTypeArguments, genericMethodArguments]; ;
		}

		protected override DmdEventDef ResolveEventDef(uint rid) {
			if (IsCOMThread)
				return ResolveEventDef_COMThread(rid);
			else
				return COMThread(() => ResolveEventDef_COMThread(rid));
		}

		DmdEventDef ResolveEventDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return eventList[rid - 1, null];
		}

		protected override DmdPropertyDef ResolvePropertyDef(uint rid) {
			if (IsCOMThread)
				return ResolvePropertyDef_COMThread(rid);
			else
				return COMThread(() => ResolvePropertyDef_COMThread(rid));
		}

		DmdPropertyDef ResolvePropertyDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return propertyList[rid - 1, null];
		}

		protected override DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments) {
			if (IsCOMThread)
				return ResolveTypeSpec_COMThread(rid, genericTypeArguments);
			else
				return COMThread(() => ResolveTypeSpec_COMThread(rid, genericTypeArguments));
		}

		DmdType ResolveTypeSpec_COMThread(uint rid, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			return typeSpecList[rid - 1, genericTypeArguments];
		}

		protected override DmdTypeRef ResolveExportedType(uint rid) {
			if (IsCOMThread)
				return ResolveExportedType_COMThread(rid);
			else
				return COMThread(() => ResolveExportedType_COMThread(rid));
		}

		DmdTypeRef ResolveExportedType_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return exportedTypeList[rid - 1];
		}

		protected override DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			if (IsCOMThread)
				return ResolveMethodSpec_COMThread(rid, genericTypeArguments, genericMethodArguments);
			else
				return COMThread(() => ResolveMethodSpec_COMThread(rid, genericTypeArguments, genericMethodArguments));
		}

		DmdMethodBase ResolveMethodSpec_COMThread(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			dispatcher.VerifyAccess();
			throw new NotImplementedException();//TODO:
		}

		protected override byte[] ResolveFieldSignature(uint rid) {
			if (IsCOMThread)
				return ResolveFieldSignature_COMThread(rid);
			else
				return COMThread(() => ResolveFieldSignature_COMThread(rid));
		}

		protected override byte[] ResolveMethodSignature(uint rid) {
			if (IsCOMThread)
				return ResolveMethodSignature_COMThread(rid);
			else
				return COMThread(() => ResolveMethodSignature_COMThread(rid));
		}

		protected override byte[] ResolveMemberRefSignature(uint rid) {
			if (IsCOMThread)
				return ResolveMemberRefSignature_COMThread(rid);
			else
				return COMThread(() => ResolveMemberRefSignature_COMThread(rid));
		}

		protected override byte[] ResolveStandAloneSigSignature(uint rid) {
			if (IsCOMThread)
				return ResolveStandAloneSigSignature_COMThread(rid);
			else
				return COMThread(() => ResolveStandAloneSigSignature_COMThread(rid));
		}

		protected override byte[] ResolveTypeSpecSignature(uint rid) {
			if (IsCOMThread)
				return ResolveTypeSpecSignature_COMThread(rid);
			else
				return COMThread(() => ResolveTypeSpecSignature_COMThread(rid));
		}

		protected override byte[] ResolveMethodSpecSignature(uint rid) {
			if (IsCOMThread)
				return ResolveMethodSpecSignature_COMThread(rid);
			else
				return COMThread(() => ResolveMethodSpecSignature_COMThread(rid));
		}

		static byte[] GetData((IntPtr addr, uint size) info) {
			if (info.addr == IntPtr.Zero)
				return Array.Empty<byte>();
			var sig = new byte[info.size];
			Marshal.Copy(info.addr, sig, 0, sig.Length);
			return sig;
		}

		byte[] ResolveFieldSignature_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return GetData(MDAPI.GetFieldSignatureBlob(MetaDataImport, 0x04000000 + rid));
		}

		byte[] ResolveMethodSignature_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return GetData(MDAPI.GetMethodSignatureBlob(MetaDataImport, 0x06000000 + rid));
		}

		byte[] ResolveMemberRefSignature_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return GetData(MDAPI.GetMemberRefSignatureBlob(MetaDataImport, 0x0A000000 + rid));
		}

		byte[] ResolveStandAloneSigSignature_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return GetData(MDAPI.GetStandAloneSigBlob(MetaDataImport, 0x11000000 + rid));
		}

		byte[] ResolveTypeSpecSignature_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return GetData(MDAPI.GetTypeSpecSignatureBlob(MetaDataImport, 0x1B000000 + rid));
		}

		byte[] ResolveMethodSpecSignature_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return GetData(MDAPI.GetMethodSpecProps(MetaDataImport, 0x2B000000 + rid, out _));
		}

		protected override string ResolveStringCore(uint offset) {
			if (IsCOMThread)
				return ResolveStringCore_COMThread(offset);
			else
				return COMThread(() => ResolveStringCore_COMThread(offset));
		}

		string ResolveStringCore_COMThread(uint offset) {
			dispatcher.VerifyAccess();
			return MDAPI.GetUserString(MetaDataImport, 0x70000000 + offset);
		}

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			if (!modulePropsInitd)
				InitializeModuleProperties();
			peKind = __peKind_DONT_USE;
			machine = __machine_DONT_USE;
		}

		public override DmdAssemblyName GetName() {
			if (assemblyName == null)
				InitializeAssemblyName();
			return assemblyName.Clone();
		}
		DmdAssemblyName assemblyName;

		void InitializeAssemblyName() {
			if (assemblyName != null)
				return;
			if (IsCOMThread)
				InitializeAssemblyName_COMThread();
			else
				COMThread(InitializeAssemblyName_COMThread);
		}

		void InitializeAssemblyName_COMThread() {
			dispatcher.VerifyAccess();
			if (assemblyName != null)
				return;
			var name = new DmdAssemblyName();

			const uint token = 0x20000001;
			name.Version = MDAPI.GetAssemblyVersionAndLocale(MetaDataAssemblyImport, token, out string locale) ?? new Version(0, 0, 0, 0);
			name.Name = MDAPI.GetAssemblySimpleName(MetaDataAssemblyImport, token) ?? string.Empty;
			name.CultureName = locale ?? string.Empty;
			name.HashAlgorithm = MDAPI.GetAssemblyHashAlgorithm(MetaDataAssemblyImport, token) ?? DmdAssemblyHashAlgorithm.SHA1;
			name.SetPublicKey(MDAPI.GetAssemblyPublicKey(MetaDataAssemblyImport, token) ?? Array.Empty<byte>());
			name.RawFlags = MDAPI.GetAssemblyAttributes(MetaDataAssemblyImport, token) ?? DmdAssemblyNameFlags.None;

			// PERF: Make sure the public key token is created once so it doesn't have to be recreated
			// for each caller.
			name.GetPublicKeyToken();

			assemblyName = name;
		}

		public override DmdAssemblyName[] GetReferencedAssemblies() {
			if (IsCOMThread)
				return GetReferencedAssemblies_COMThread();
			else
				return COMThread(GetReferencedAssemblies_COMThread);
		}

		DmdAssemblyName[] GetReferencedAssemblies_COMThread() {
			var list = new List<DmdAssemblyName>();
			for (uint token = 0x23000001; ; token++) {
				if (!MDAPI.IsValidToken(MetaDataImport, token))
					break;
				list.Add(ReadAssemblyName_COMThread(token & 0x00FFFFFF));
			}
			return list.ToArray();
		}

		internal DmdAssemblyName ReadAssemblyName_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			var asmName = new DmdAssemblyName();

			uint token = 0x23000000 + rid;
			asmName.Name = MDAPI.GetAssemblyRefSimpleName(MetaDataAssemblyImport, token) ?? string.Empty;
			asmName.Version = MDAPI.GetAssemblyRefVersionAndLocale(MetaDataAssemblyImport, token, out string locale) ?? new Version(0, 0, 0, 0);
			asmName.CultureName = locale ?? string.Empty;
			var publicKeyOrToken = MDAPI.GetAssemblyRefPublicKeyOrToken(MetaDataAssemblyImport, token, out var attrs);
			if (publicKeyOrToken != null) {
				if ((attrs & DmdAssemblyNameFlags.PublicKey) != 0)
					asmName.SetPublicKey(publicKeyOrToken);
				else
					asmName.SetPublicKeyToken(publicKeyOrToken);
			}
			asmName.RawFlags = attrs;
			return asmName;
		}

		protected override DmdCustomAttributeData[] ReadAssemblyCustomAttributes(uint rid) => ReadCustomAttributesCore(0x20000000 + rid);
		protected override DmdCustomAttributeData[] ReadModuleCustomAttributes(uint rid) => ReadCustomAttributesCore(0x00000000 + rid);
		protected override DmdCustomAttributeData[] ReadTypeDefCustomAttributes(uint rid) => ReadCustomAttributesCore(0x02000000 + rid);
		protected override DmdCustomAttributeData[] ReadFieldCustomAttributes(uint rid) => ReadCustomAttributesCore(0x04000000 + rid);
		protected override DmdCustomAttributeData[] ReadMethodCustomAttributes(uint rid) => ReadCustomAttributesCore(0x06000000 + rid);
		protected override DmdCustomAttributeData[] ReadParamCustomAttributes(uint rid) => ReadCustomAttributesCore(0x08000000 + rid);
		protected override DmdCustomAttributeData[] ReadEventCustomAttributes(uint rid) => ReadCustomAttributesCore(0x14000000 + rid);
		protected override DmdCustomAttributeData[] ReadPropertyCustomAttributes(uint rid) => ReadCustomAttributesCore(0x17000000 + rid);

		DmdCustomAttributeData[] ReadCustomAttributesCore(uint token) {
			if (IsCOMThread)
				return ReadCustomAttributesCore_COMThread(token);
			else
				return COMThread(() => ReadCustomAttributesCore_COMThread(token));
		}

		internal DmdCustomAttributeData[] ReadCustomAttributesCore_COMThread(uint token) {
			dispatcher.VerifyAccess();
			var tokens = MDAPI.GetCustomAttributeTokens(MetaDataImport, token);
			if (tokens.Length == 0)
				return Array.Empty<DmdCustomAttributeData>();

			var res = new DmdCustomAttributeData[tokens.Length];
			int w = 0;
			for (int i = 0; i < tokens.Length; i++) {
				var info = MDAPI.GetCustomAttributeBlob(MetaDataImport, tokens[i]);
				if (info.addr == IntPtr.Zero)
					continue;

				var ctor = ResolveMethod(info.typeToken, null, null, DmdResolveOptions.None) as DmdConstructorInfo;
				if ((object)ctor == null)
					continue;

				var ca = DmdCustomAttributeReader.Read(module, new DmdPointerDataStream(info.addr, info.size), ctor);
				if (ca == null)
					continue;

				res[w++] = ca;
			}
			if (res.Length != w)
				Array.Resize(ref res, w);
			return res;
		}

		protected override DmdCustomAttributeData[] ReadAssemblySecurityAttributes(uint rid) => ReadSecurityAttributesCore(0x20000000 + rid);
		protected override DmdCustomAttributeData[] ReadTypeDefSecurityAttributes(uint rid) => ReadSecurityAttributesCore(0x02000000 + rid);
		protected override DmdCustomAttributeData[] ReadMethodSecurityAttributes(uint rid) => ReadSecurityAttributesCore(0x06000000 + rid);

		DmdCustomAttributeData[] ReadSecurityAttributesCore(uint token) {
			if (IsCOMThread)
				return ReadSecurityAttributesCore_COMThread(token);
			else
				return COMThread(() => ReadSecurityAttributesCore_COMThread(token));
		}

		DmdCustomAttributeData[] ReadSecurityAttributesCore_COMThread(uint token) {
			dispatcher.VerifyAccess();
			var tokens = MDAPI.GetPermissionSetTokens(MetaDataImport, token);
			if (tokens.Length == 0)
				return Array.Empty<DmdCustomAttributeData>();
			IList<DmdType> genericTypeArguments = null;
			DmdCustomAttributeData[] firstCas = null;
			SSP.SecurityAction firstAction = 0;
			List<(DmdCustomAttributeData[] cas, SSP.SecurityAction action)> res = null;
			for (int i = 0; i < tokens.Length; i++) {
				if (!MDAPI.IsValidToken(MetaDataImport, tokens[i]))
					continue;
				var info = MDAPI.GetPermissionSetBlob(MetaDataImport, token);
				if (info.addr == IntPtr.Zero)
					continue;
				var action = (SSP.SecurityAction)(info.action & 0x1F);
				var cas = DmdDeclSecurityReader.Read(module, new DmdPointerDataStream(info.addr, (int)info.size), action, genericTypeArguments);
				if (cas.Length == 0)
					continue;
				if (res == null && firstCas == null) {
					firstAction = action;
					firstCas = cas;
				}
				else {
					if (res == null) {
						res = new List<(DmdCustomAttributeData[], SSP.SecurityAction)>(firstCas.Length + cas.Length);
						res.Add((firstCas, firstAction));
						firstCas = null;
					}
					res.Add((cas, action));
				}
			}
			if (firstCas != null)
				return firstCas;
			if (res == null)
				return Array.Empty<DmdCustomAttributeData>();
			// Reflection sorts it by action
			res.Sort((a, b) => (int)a.action - (int)b.action);
			int count = 0;
			for (int i = 0; i < res.Count; i++)
				count += res[i].cas.Length;
			var sas = new DmdCustomAttributeData[count];
			int w = 0;
			for (int i = 0; i < res.Count; i++) {
				foreach (var ca in res[i].cas)
					sas[w++] = ca;
			}
			if (sas.Length != w)
				throw new InvalidOperationException();
			return sas;
		}

		internal DmdMarshalType ReadFieldMarshalType_COMThread(int metadataToken, DmdModule module, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			var signature = MDAPI.GetFieldMarshalBlob(MetaDataImport, (uint)metadataToken);
			if (signature.addr == IntPtr.Zero)
				return null;
			return DmdMarshalBlobReader.Read(module, new DmdPointerDataStream(signature), genericTypeArguments);
		}

		internal (object value, bool hasValue) ReadFieldConstant_COMThread(int metadataToken) {
			dispatcher.VerifyAccess();
			var c = MDAPI.GetFieldConstant(MetaDataImport, (uint)metadataToken, out var etype);
			if (etype == ElementType.End)
				return (null, false);
			return (c, true);
		}
	}
}
