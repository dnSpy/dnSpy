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
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using SSP = System.Security.Permissions;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdComMetadataReader : DmdMetadataReaderBase, IMethodBodyResolver {
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
				return __moduleScopeName_DONT_USE!;
			}
		}

		public override string ImageRuntimeVersion {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __imageRuntimeVersion_DONT_USE!;
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
		string? __moduleScopeName_DONT_USE;
		string? __imageRuntimeVersion_DONT_USE;
		DmdPortableExecutableKinds __peKind_DONT_USE;
		DmdImageFileMachine __machine_DONT_USE;

		// It doesn't seem to be possible to get the entry point from the COM MetaData API
		public override DmdMethodInfo? EntryPoint => null;

		internal IMetaDataImport2 MetaDataImport {
			get {
				Debug.Assert(IsCOMThread);
				return __metaDataImport_DONT_USE;
			}
		}

		internal IMetaDataAssemblyImport MetaDataAssemblyImport {
			get {
				Debug.Assert(IsCOMThread);
				// It's initialized lazily and not in the ctor because the ctor is not necessarily running
				// on the COM thread.
				if (__metaDataAssemblyImport_DONT_USE is null)
					__metaDataAssemblyImport_DONT_USE = (IMetaDataAssemblyImport)__metaDataImport_DONT_USE;
				return __metaDataAssemblyImport_DONT_USE;
			}
		}

		internal DmdModule Module => module;
		internal DmdDispatcher Dispatcher => dispatcher;

		readonly DmdModuleImpl module;
		readonly IMetaDataImport2 __metaDataImport_DONT_USE;
		IMetaDataAssemblyImport? __metaDataAssemblyImport_DONT_USE;
		readonly DmdDynamicModuleHelper dynamicModuleHelper;
		readonly DmdDispatcher dispatcher;
		Dictionary<uint, List<uint>?>? ridToNested;
		Dictionary<uint, uint>? ridToEnclosing;
		readonly LazyList<DmdTypeRef> typeRefList;
		readonly LazyList<DmdFieldDef, DmdTypeDef?> fieldList;
		readonly LazyList<DmdTypeDef> typeDefList;
		readonly LazyList<DmdMethodBase, DmdTypeDef?> methodList;
		readonly LazyList2<DmdMemberInfo, IList<DmdType>?, IList<DmdType>?> memberRefList;
		readonly LazyList<DmdEventDef, DmdTypeDef?> eventList;
		readonly LazyList<DmdPropertyDef, DmdTypeDef?> propertyList;
		readonly LazyList2<DmdType, IList<DmdType>?, IList<DmdType>?> typeSpecList;
		readonly LazyList<DmdTypeRef> exportedTypeList;
		readonly DmdNullGlobalType globalTypeIfThereAreNoTypes;
		readonly Dictionary<IntPtr, DmdType?> fieldTypeCache;
		readonly Dictionary<IntPtr, DmdMethodSignature?> methodSignatureCache;

		public DmdComMetadataReader(DmdModuleImpl module, IMetaDataImport2 metaDataImport, DmdDynamicModuleHelper dynamicModuleHelper, DmdDispatcher dispatcher) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			__metaDataImport_DONT_USE = metaDataImport ?? throw new ArgumentNullException(nameof(metaDataImport));
			this.dynamicModuleHelper = dynamicModuleHelper ?? throw new ArgumentNullException(nameof(dynamicModuleHelper));
			this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

			fieldTypeCache = new Dictionary<IntPtr, DmdType?>();
			methodSignatureCache = new Dictionary<IntPtr, DmdMethodSignature?>();

			typeRefList = new LazyList<DmdTypeRef>(TryCreateTypeRefCOMD_COMThread);
			fieldList = new LazyList<DmdFieldDef, DmdTypeDef?>(CreateResolvedField_COMThread);
			typeDefList = new LazyList<DmdTypeDef>(TryCreateTypeDefCOMD_COMThread);
			methodList = new LazyList<DmdMethodBase, DmdTypeDef?>(CreateResolvedMethod_COMThread);
			memberRefList = new LazyList2<DmdMemberInfo, IList<DmdType>?, IList<DmdType>?>(CreateResolvedMemberRef_COMThread);
			eventList = new LazyList<DmdEventDef, DmdTypeDef?>(CreateResolvedEvent_COMThread);
			propertyList = new LazyList<DmdPropertyDef, DmdTypeDef?>(CreateResolvedProperty_COMThread);
			typeSpecList = new LazyList2<DmdType, IList<DmdType>?, IList<DmdType>?>(TryCreateTypeSpecCOMD_COMThread);
			exportedTypeList = new LazyList<DmdTypeRef>(TryCreateExportedTypeCOMD_COMThread);

			globalTypeIfThereAreNoTypes = new DmdNullGlobalType(module, null);
			dynamicModuleHelper.TypeLoaded += DmdDynamicModuleHelper_TypeLoaded_COMThread;
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

		DmdTypeRefCOMD? TryCreateTypeRefCOMD_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (!MDAPI.IsValidToken(MetaDataImport, 0x01000000 + rid))
				return null;
			return new DmdTypeRefCOMD(this, rid, null);
		}

		DmdTypeDefCOMD? TryCreateTypeDefCOMD_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (!MDAPI.IsValidToken(MetaDataImport, 0x02000000 + rid))
				return null;
			return new DmdTypeDefCOMD(this, rid, null);
		}

		DmdExportedTypeCOMD? TryCreateExportedTypeCOMD_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (!MDAPI.IsValidToken(MetaDataImport, 0x27000000 + rid))
				return null;
			return new DmdExportedTypeCOMD(this, rid, null);
		}

		internal uint GetEnclosingTypeDefRid_COMThread(uint typeDefRid) {
			dispatcher.VerifyAccess();
			InitializeTypeTables_COMThread();
			Debug2.Assert(ridToEnclosing is not null);
			bool b = ridToEnclosing.TryGetValue(typeDefRid, out uint enclTypeRid);
			Debug.Assert(b);
			return enclTypeRid;
		}

		internal uint[] GetTypeDefNestedClassRids_COMThread(uint typeDefRid) {
			dispatcher.VerifyAccess();
			InitializeTypeTables_COMThread();
			Debug2.Assert(ridToNested is not null);
			bool b = ridToNested.TryGetValue(typeDefRid, out var list);
			Debug.Assert(b);
			return list is null || list.Count == 0 ? Array.Empty<uint>() : list.ToArray();
		}

		void InitializeTypeTables_COMThread() {
			dispatcher.VerifyAccess();
			if (ridToNested is not null)
				return;

			var allTypes = MDAPI.GetTypeDefTokens(MetaDataImport);
			int capacity = allTypes.Length;
			ridToNested = new Dictionary<uint, List<uint>?>(capacity);
			ridToEnclosing = new Dictionary<uint, uint>(capacity);
			UpdateTypeTables_COMThread(allTypes);
		}

		void UpdateTypeTables_COMThread(uint[] tokens) {
			dispatcher.VerifyAccess();
			Debug2.Assert(ridToNested is not null);
			Debug2.Assert(ridToEnclosing is not null);
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
					if (enclTypeList is null)
						ridToNested[enclTypeRid] = enclTypeList = new List<uint>();
					enclTypeList.Add(rid);
				}

				ridToNested[rid] = null;
				ridToEnclosing[rid] = enclTypeRid;
			}
		}

		void DmdDynamicModuleHelper_TypeLoaded_COMThread(object? sender, DmdTypeLoadedEventArgs e) {
			dispatcher.VerifyAccess();
			bool b = (e.MetadataToken >> 24) == 0x02 && (e.MetadataToken & 0x00FFFFFF) != 0 && MDAPI.IsValidToken(MetaDataImport, (uint)e.MetadataToken);
			Debug.Assert(b);
			if (!b)
				return;
			if (!module.IsDynamic)
				return;

			uint typeToken = (uint)e.MetadataToken;
			uint[] newTokens;
			if (ridToNested is not null)
				newTokens = UpdateTypeTables_COMThread(typeToken);
			else
				newTokens = GetNewTokens_COMThread(typeToken);
			typeDefList.TryGet(typeToken)?.DynamicType_InvalidateCachedMembers();

			TypesUpdated?.Invoke(this, new DmdTypesUpdatedEventArgs(newTokens));
		}

		public override event EventHandler<DmdTypesUpdatedEventArgs>? TypesUpdated;

		uint[] UpdateTypeTables_COMThread(uint typeToken) {
			dispatcher.VerifyAccess();
			uint typeRid = typeToken & 0x00FFFFFF;
			bool b = ridToEnclosing is not null && !ridToEnclosing.ContainsKey(typeRid);
			Debug.Assert(b);
			if (!b)
				return new[] { typeToken };
			Debug2.Assert(ridToEnclosing is not null);

			var tokens = GetNewTokens_COMThread(typeRid);
			UpdateTypeTables_COMThread(tokens);

			foreach (var token in tokens) {
				uint rid = token & 0x00FFFFFF;
				if (token != typeToken) {
					b = typeDefList.TryGet(rid) is not null;
					Debug.Assert(!b);
					if (b)
						continue;
					_ = typeDefList[rid - 1];
				}

				b = ridToEnclosing.TryGetValue(rid, out uint enclTypeRid);
				Debug.Assert(b);
				if (enclTypeRid != 0) {
					var enclType = typeDefList.TryGet(enclTypeRid);
					enclType?.DynamicType_InvalidateCachedNestedTypes();
				}
			}

			return tokens;
		}

		uint[] GetNewTokens_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			if (ridToEnclosing is null)
				return new[] { rid };
			var hash = tmpHash;
			hash.Clear();
			for (;;) {
				if (ridToEnclosing.ContainsKey(rid))
					break;
				if (rid == 0 || !hash.Add(rid))
					break;
				rid = MDAPI.GetTypeDefEnclosingType(MetaDataImport, 0x02000000 + rid) & 0x00FFFFFF;
			}
			var tokens = new uint[hash.Count];
			int i = 0;
			foreach (uint rid2 in hash)
				tokens[i++] = 0x02000000 + rid2;
			return tokens;
		}
		readonly HashSet<uint> tmpHash = new HashSet<uint>();

		(DmdType? type, bool containedGenericParams) TryCreateTypeSpecCOMD_COMThread(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			uint token = 0x1B000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return (null, containedGenericParams: true);
			var blob = MDAPI.GetTypeSpecSignatureBlob(MetaDataImport, token);
			return DmdSignatureReader.ReadTypeSignature(module, new DmdPointerDataStream(blob), genericTypeArguments, genericMethodArguments, resolveTypes);
		}

		DmdFieldDefCOMD? CreateResolvedField_COMThread(uint rid, DmdTypeDef? declaringType) {
			dispatcher.VerifyAccess();
			uint token = 0x04000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return null;
			if (declaringType is null)
				declaringType = ResolveTypeDef_COMThread(MDAPI.GetFieldOwnerRid(MetaDataImport, token)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef_COMThread(MDAPI.GetFieldOwnerRid(MetaDataImport, token)));
			return CreateFieldDefCore_COMThread(rid, declaringType, declaringType);
		}

		internal DmdFieldDef CreateFieldDef_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			if ((object)declaringType == reflectedType && declaringType is DmdTypeDef declaringTypeDef)
				return ResolveFieldDef_COMThread(rid, declaringTypeDef)!;
			return CreateFieldDefCore_COMThread(rid, declaringType, reflectedType);
		}

		DmdFieldDefCOMD CreateFieldDefCore_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			return new DmdFieldDefCOMD(this, rid, declaringType, reflectedType);
		}

		internal DmdType ReadFieldType_COMThread((IntPtr addr, uint size) signature, IList<DmdType> genericTypeArguments) {
			dispatcher.VerifyAccess();
			if (fieldTypeCache.TryGetValue(signature.addr, out var fieldType)) {
				if (fieldType is not null)
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

		(DmdType fieldType, bool containedGenericParams) ReadFieldTypeCore_COMThread((IntPtr addr, uint size) signature, IList<DmdType>? genericTypeArguments) {
			dispatcher.VerifyAccess();
			if (signature.addr == IntPtr.Zero)
				return (module.AppDomain.System_Void, false);
			return DmdSignatureReader.ReadFieldSignature(module, new DmdPointerDataStream(signature), genericTypeArguments, resolveTypes);
		}

		DmdMethodBase? CreateResolvedMethod_COMThread(uint rid, DmdTypeDef? declaringType) {
			dispatcher.VerifyAccess();
			uint token = 0x06000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return null;
			if (declaringType is null)
				declaringType = ResolveTypeDef_COMThread(MDAPI.GetMethodOwnerRid(MetaDataImport, token)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef_COMThread(MDAPI.GetMethodOwnerRid(MetaDataImport, token)));
			return CreateMethodDefCore_COMThread(rid, declaringType, declaringType);
		}

		internal DmdMethodBase CreateMethodDef_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			if ((object)declaringType == reflectedType && declaringType is DmdTypeDef declaringTypeDef)
				return ResolveMethodDef_COMThread(rid, declaringTypeDef)!;
			return CreateMethodDefCore_COMThread(rid, declaringType, reflectedType);
		}

		DmdMethodBase CreateMethodDefCore_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			uint token = 0x06000000 + rid;
			var name = MDAPI.GetMethodName(MetaDataImport, token) ?? string.Empty;
			MDAPI.GetMethodAttributes(MetaDataImport, token, out var attrs, out var implAttrs);
			if ((attrs & DmdMethodAttributes.RTSpecialName) != 0 && name.Length > 0 && name[0] == '.') {
				if (name == DmdConstructorInfo.ConstructorName || name == DmdConstructorInfo.TypeConstructorName)
					return new DmdConstructorDefCOMD(this, attrs, implAttrs, rid, name, declaringType, reflectedType);
			}
			return new DmdMethodDefCOMD(this, attrs, implAttrs, rid, name, declaringType, reflectedType);
		}

		internal DmdMethodSignature ReadMethodSignature_COMThread((IntPtr addr, uint size) signature, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool isProperty) {
			dispatcher.VerifyAccess();
			if (methodSignatureCache.TryGetValue(signature.addr, out var methodSignature)) {
				if (methodSignature is not null)
					return methodSignature;
				var info = ReadMethodSignatureCore_COMThread(signature, genericTypeArguments, genericMethodArguments, isProperty);
				Debug.Assert(info.containedGenericParams);
				return info.methodSignature;
			}
			else {
				var info = ReadMethodSignatureCore_COMThread(signature, genericTypeArguments, genericMethodArguments, isProperty);
				if (info.containedGenericParams)
					methodSignatureCache.Add(signature.addr, null);
				else
					methodSignatureCache.Add(signature.addr, info.methodSignature);
				return info.methodSignature;
			}
		}

		(DmdMethodSignature methodSignature, bool containedGenericParams) ReadMethodSignatureCore_COMThread((IntPtr addr, uint size) signature, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments, bool isProperty) {
			dispatcher.VerifyAccess();
			return DmdSignatureReader.ReadMethodSignature(module, new DmdPointerDataStream(signature), genericTypeArguments, genericMethodArguments, isProperty, resolveTypes);
		}

		internal (DmdParameterInfo? returnParameter, DmdParameterInfo[] parameters) CreateParameters_COMThread(DmdMethodBase method, bool createReturnParameter) {
			dispatcher.VerifyAccess();
			var tokens = MDAPI.GetParamTokens(MetaDataImport, (uint)method.MetadataToken);
			var methodSignature = method.GetMethodSignature();
			var sigParamTypes = methodSignature.GetParameterTypes();
			DmdParameterInfo? returnParameter = null;
			var parameters = sigParamTypes.Count == 0 ? Array.Empty<DmdParameterInfo>() : new DmdParameterInfo[sigParamTypes.Count];
			for (int i = 0; i < tokens.Length; i++) {
				uint token = tokens[i];
				uint rid = token & 0x00FFFFFF;
				var name = MDAPI.GetParamName(MetaDataImport, token);
				if (!MDAPI.GetParamSeqAndAttrs(MetaDataImport, token, out uint seq, out var attrs))
					continue;
				if (seq == 0) {
					if (createReturnParameter && returnParameter is null)
						returnParameter = new DmdParameterDefCOMD(this, rid, name, attrs, method, -1, methodSignature.ReturnType);
				}
				else {
					int paramIndex = (int)seq - 1;
					if ((uint)paramIndex < (uint)parameters.Length) {
						if (parameters[paramIndex] is null)
							parameters[paramIndex] = new DmdParameterDefCOMD(this, rid, name, attrs, method, paramIndex, sigParamTypes[paramIndex]);
					}
				}
			}
			for (int i = 0; i < parameters.Length; i++) {
				if (parameters[i] is null)
					parameters[i] = new DmdCreatedParameterDef(method, i, sigParamTypes[i]);
			}
			if (createReturnParameter && returnParameter is null)
				returnParameter = new DmdCreatedParameterDef(method, -1, methodSignature.ReturnType);

			return (returnParameter, parameters);
		}

		DmdEventDef? CreateResolvedEvent_COMThread(uint rid, DmdTypeDef? declaringType) {
			dispatcher.VerifyAccess();
			uint token = 0x14000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return null;
			if (declaringType is null)
				declaringType = ResolveTypeDef_COMThread(MDAPI.GetEventOwnerRid(MetaDataImport, token)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef_COMThread(MDAPI.GetEventOwnerRid(MetaDataImport, token)));
			return CreateEventDefCore_COMThread(rid, declaringType, declaringType);
		}

		internal DmdEventDef CreateEventDef_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			if ((object)declaringType == reflectedType && declaringType is DmdTypeDef declaringTypeDef)
				return ResolveEventDef_COMThread(rid, declaringTypeDef)!;
			return CreateEventDefCore_COMThread(rid, declaringType, reflectedType);
		}

		DmdEventDef CreateEventDefCore_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			return new DmdEventDefCOMD(this, rid, declaringType, reflectedType);
		}

		DmdPropertyDef? CreateResolvedProperty_COMThread(uint rid, DmdTypeDef? declaringType) {
			dispatcher.VerifyAccess();
			uint token = 0x17000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return null;
			if (declaringType is null)
				declaringType = ResolveTypeDef_COMThread(MDAPI.GetPropertyOwnerRid(MetaDataImport, token)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef_COMThread(MDAPI.GetPropertyOwnerRid(MetaDataImport, token)));
			return CreatePropertyDefCore_COMThread(rid, declaringType, declaringType);
		}

		internal DmdPropertyDef CreatePropertyDef_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			if ((object)declaringType == reflectedType && declaringType is DmdTypeDef declaringTypeDef)
				return ResolvePropertyDef_COMThread(rid, declaringTypeDef)!;
			return CreatePropertyDefCore_COMThread(rid, declaringType, reflectedType);
		}

		DmdPropertyDef CreatePropertyDefCore_COMThread(uint rid, DmdType declaringType, DmdType reflectedType) {
			dispatcher.VerifyAccess();
			return new DmdPropertyDefCOMD(this, rid, declaringType, reflectedType);
		}

		internal DmdType[]? CreateGenericParameters_COMThread(DmdMethodBase method) {
			dispatcher.VerifyAccess();
			var tokens = MDAPI.GetGenericParamTokens(MetaDataImport, (uint)method.MetadataToken);
			if (tokens.Length == 0)
				return null;
			var genericParams = new DmdType[tokens.Length];
			for (int i = 0; i < genericParams.Length; i++) {
				uint token = tokens[i];
				uint rid = token & 0x00FFFFFF;
				var gpName = MDAPI.GetGenericParamName(MetaDataImport, token) ?? string.Empty;
				if (!MDAPI.GetGenericParamNumAndAttrs(MetaDataImport, token, out var gpNumber, out var gpAttrs))
					return null;
				var gpType = new DmdGenericParameterTypeCOMD(this, rid, method, gpName, gpNumber, gpAttrs, null);
				genericParams[i] = gpType;
			}
			return genericParams;
		}

		(DmdMemberInfo member, bool containedGenericParams) CreateResolvedMemberRef_COMThread(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			uint token = 0x0A000000 + rid;
			var signature = MDAPI.GetMemberRefSignatureBlob(MetaDataImport, token);
			var name = MDAPI.GetMemberRefName(MetaDataImport, token) ?? string.Empty;

			uint classToken = MDAPI.GetMemberRefClassToken(MetaDataImport, token);
			var reflectedTypeRef = GetMemberRefParent_COMThread(classToken, genericTypeArguments, genericMethodArguments);
			if (reflectedTypeRef is DmdGenericInstanceType || reflectedTypeRef is DmdGenericInstanceTypeRef)
				genericTypeArguments = reflectedTypeRef.GetGenericArguments();

			var info = ReadMethodSignatureOrFieldType_COMThread(signature, genericTypeArguments, genericMethodArguments);
			var rawInfo = info.containedGenericParams ? ReadMethodSignatureOrFieldType_COMThread(signature, null, null) : info;

			bool containedGenericParams = info.containedGenericParams;
			if ((classToken >> 24) == 0x1B)
				containedGenericParams = true;

			if (info.fieldType is not null) {
				var fieldRef = new DmdFieldRef(reflectedTypeRef, name, rawInfo.fieldType!, info.fieldType);
				return (fieldRef, containedGenericParams);
			}
			else {
				Debug2.Assert(info.methodSignature is not null);
				if (name == DmdConstructorInfo.ConstructorName || name == DmdConstructorInfo.TypeConstructorName) {
					var ctorRef = new DmdConstructorRef(reflectedTypeRef, name, rawInfo.methodSignature!, info.methodSignature);
					return (ctorRef, containedGenericParams);
				}
				else {
					var methodRef = new DmdMethodRefCOMD(this, signature, genericTypeArguments, reflectedTypeRef, name, rawInfo.methodSignature!, info.methodSignature);
					return (methodRef, containedGenericParams);
				}
			}
		}

		DmdType GetMemberRefParent_COMThread(uint classToken, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			uint rid = classToken & 0x00FFFFFF;
			switch ((Table)(classToken >> 24)) {
			case Table.TypeRef:
			case Table.TypeDef:
			case Table.TypeSpec:
				return ResolveType((int)classToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.None) ?? Module.AppDomain.System_Void;

			case Table.ModuleRef:
				var moduleName = MDAPI.GetModuleRefName(MetaDataImport, classToken) ?? string.Empty;
				if (StringComparer.OrdinalIgnoreCase.Equals(moduleName, Module.ScopeName))
					return Module.GlobalType;
				var referencedModule = Module.Assembly.GetModule(moduleName);
				return referencedModule?.GlobalType ?? Module.AppDomain.System_Void;

			case Table.Method:
				return ResolveMethodDef_COMThread(rid)?.DeclaringType ?? Module.AppDomain.System_Void;

			default:
				return Module.AppDomain.System_Void;
			}
		}

		(DmdType? fieldType, DmdMethodSignature? methodSignature, bool containedGenericParams) ReadMethodSignatureOrFieldType_COMThread((IntPtr addr, uint size) signature, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			if (methodSignatureCache.TryGetValue(signature.addr, out var methodSignature)) {
				if (methodSignature is not null)
					return (null, methodSignature, false);
				var info = ReadMethodSignatureCore_COMThread(signature, genericTypeArguments, genericMethodArguments, isProperty: false);
				if (info.methodSignature is null)
					throw new InvalidOperationException();
				Debug.Assert(info.containedGenericParams);
				return (null, info.methodSignature, info.containedGenericParams);
			}
			else if (fieldTypeCache.TryGetValue(signature.addr, out var fieldType)) {
				if (fieldType is not null)
					return (fieldType, null, false);
				var info = ReadFieldTypeCore_COMThread(signature, genericTypeArguments);
				if (info.fieldType is null)
					throw new InvalidOperationException();
				Debug.Assert(info.containedGenericParams);
				return (info.fieldType, null, info.containedGenericParams);
			}
			else {
				var info = DmdSignatureReader.ReadMethodSignatureOrFieldType(module, new DmdPointerDataStream(signature), genericTypeArguments, genericMethodArguments, resolveTypes);
				if (info.fieldType is not null) {
					if (info.containedGenericParams)
						fieldTypeCache.Add(signature.addr, null);
					else
						fieldTypeCache.Add(signature.addr, info.fieldType);
					return (info.fieldType, null, info.containedGenericParams);
				}
				else {
					Debug2.Assert(info.methodSignature is not null);
					if (info.containedGenericParams)
						methodSignatureCache.Add(signature.addr, null);
					else
						methodSignatureCache.Add(signature.addr, info.methodSignature);
					return (null, info.methodSignature, info.containedGenericParams);
				}
			}
		}

		internal DmdMethodBody? GetMethodBody_COMThread(DmdMethodBase method, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			if ((method.MethodImplementationFlags & DmdMethodImplAttributes.CodeTypeMask) != DmdMethodImplAttributes.IL)
				return null;

			var rvaTmp = MDAPI.GetRVA(MetaDataImport, (uint)method.MetadataToken);
			if (rvaTmp is null)
				return null;
			uint rva = rvaTmp.Value;

			// dynamic modules can have methods with RVA == 0 because it's relative to the .text section
			// and not really an RVA.
			if (!module.IsDynamic) {
				if (rva == 0)
					return null;
			}
			else {
				if ((method.Attributes & DmdMethodAttributes.Abstract) != 0 && rva == 0)
					return null;
			}

			var bodyStream = dynamicModuleHelper.TryGetMethodBody(method.Module, method.MetadataToken, rva);
			if (bodyStream is null)
				return null;
			using (bodyStream) {
				var body = DmdMethodBodyReader.Create(this, bodyStream, genericTypeArguments, genericMethodArguments);
				Debug2.Assert(body is not null);
				return body;
			}
		}

		(DmdType type, bool isPinned)[] IMethodBodyResolver.ReadLocals(int localSignatureMetadataToken, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			if ((localSignatureMetadataToken & 0x00FFFFFF) == 0 || (localSignatureMetadataToken >> 24) != 0x11)
				return Array.Empty<(DmdType, bool)>();
			var signature = MDAPI.GetStandAloneSigBlob(MetaDataImport, (uint)localSignatureMetadataToken);
			if (signature.addr == IntPtr.Zero)
				return Array.Empty<(DmdType, bool)>();
			return DmdSignatureReader.ReadLocalsSignature(module, new DmdPointerDataStream(signature), genericTypeArguments, genericMethodArguments, resolveTypes);
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
				if (type is null)
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
			List<DmdTypeRef>? result = null;
			for (uint rid = 1; rid <= 0x00FFFFFF; rid++) {
				var type = ResolveExportedType_COMThread(rid);
				if (type is null)
					break;
				if (result is null)
					result = new List<DmdTypeRef>();
				result.Add(type);
			}
			return result?.ToArray() ?? Array.Empty<DmdTypeRef>();
		}

		protected override DmdTypeRef? ResolveTypeRef(uint rid) {
			if (IsCOMThread)
				return ResolveTypeRef_COMThread(rid);
			else
				return COMThread(() => ResolveTypeRef_COMThread(rid));
		}

		DmdTypeRef? ResolveTypeRef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return typeRefList[rid - 1];
		}

		protected override DmdTypeDef? ResolveTypeDef(uint rid) {
			if (IsCOMThread)
				return ResolveTypeDef_COMThread(rid);
			else
				return COMThread(() => ResolveTypeDef_COMThread(rid));
		}

		DmdTypeDef? ResolveTypeDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			var type = typeDefList[rid - 1];
			if (type is null && rid == 1)
				return globalTypeIfThereAreNoTypes;
			return type;
		}

		protected override DmdFieldDef? ResolveFieldDef(uint rid) {
			if (IsCOMThread)
				return ResolveFieldDef_COMThread(rid);
			else
				return COMThread(() => ResolveFieldDef_COMThread(rid));
		}

		DmdFieldDef? ResolveFieldDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return fieldList[rid - 1, null];
		}

		DmdFieldDef? ResolveFieldDef_COMThread(uint rid, DmdTypeDef declaringType) {
			dispatcher.VerifyAccess();
			return fieldList[rid - 1, declaringType];
		}

		protected override DmdMethodBase? ResolveMethodDef(uint rid) {
			if (IsCOMThread)
				return ResolveMethodDef_COMThread(rid);
			else
				return COMThread(() => ResolveMethodDef_COMThread(rid));
		}

		DmdMethodBase? ResolveMethodDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return methodList[rid - 1, null];
		}

		DmdMethodBase? ResolveMethodDef_COMThread(uint rid, DmdTypeDef declaringType) {
			dispatcher.VerifyAccess();
			return methodList[rid - 1, declaringType];
		}

		protected override DmdMemberInfo? ResolveMemberRef(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			if (IsCOMThread)
				return ResolveMemberRef_COMThread(rid, genericTypeArguments, genericMethodArguments);
			else
				return COMThread(() => ResolveMemberRef_COMThread(rid, genericTypeArguments, genericMethodArguments));
		}

		DmdMemberInfo? ResolveMemberRef_COMThread(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			return memberRefList[rid - 1, genericTypeArguments, genericMethodArguments]; ;
		}

		protected override DmdEventDef? ResolveEventDef(uint rid) {
			if (IsCOMThread)
				return ResolveEventDef_COMThread(rid);
			else
				return COMThread(() => ResolveEventDef_COMThread(rid));
		}

		DmdEventDef? ResolveEventDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return eventList[rid - 1, null];
		}

		DmdEventDef? ResolveEventDef_COMThread(uint rid, DmdTypeDef declaringType) {
			dispatcher.VerifyAccess();
			return eventList[rid - 1, declaringType];
		}

		protected override DmdPropertyDef? ResolvePropertyDef(uint rid) {
			if (IsCOMThread)
				return ResolvePropertyDef_COMThread(rid);
			else
				return COMThread(() => ResolvePropertyDef_COMThread(rid));
		}

		DmdPropertyDef? ResolvePropertyDef_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return propertyList[rid - 1, null];
		}

		DmdPropertyDef? ResolvePropertyDef_COMThread(uint rid, DmdTypeDef declaringType) {
			dispatcher.VerifyAccess();
			return propertyList[rid - 1, declaringType];
		}

		protected override DmdType? ResolveTypeSpec(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			if (IsCOMThread)
				return ResolveTypeSpec_COMThread(rid, genericTypeArguments, genericMethodArguments);
			else
				return COMThread(() => ResolveTypeSpec_COMThread(rid, genericTypeArguments, genericMethodArguments));
		}

		DmdType? ResolveTypeSpec_COMThread(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			return typeSpecList[rid - 1, genericTypeArguments, genericMethodArguments];
		}

		protected override DmdTypeRef? ResolveExportedType(uint rid) {
			if (IsCOMThread)
				return ResolveExportedType_COMThread(rid);
			else
				return COMThread(() => ResolveExportedType_COMThread(rid));
		}

		DmdTypeRef? ResolveExportedType_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			return exportedTypeList[rid - 1];
		}

		protected override DmdMethodBase? ResolveMethodSpec(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			if (IsCOMThread)
				return ResolveMethodSpec_COMThread(rid, genericTypeArguments, genericMethodArguments);
			else
				return COMThread(() => ResolveMethodSpec_COMThread(rid, genericTypeArguments, genericMethodArguments));
		}

		DmdMethodBase? ResolveMethodSpec_COMThread(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			uint token = 0x2B000000 + rid;
			if (!MDAPI.IsValidToken(MetaDataImport, token))
				return null;
			var signature = MDAPI.GetMethodSpecProps(MetaDataImport, token, out var methodToken);
			var instantiation = DmdSignatureReader.ReadMethodSpecSignature(module, new DmdPointerDataStream(signature), genericTypeArguments, genericMethodArguments, resolveTypes).types;
			var genericMethod = ResolveMethod((int)methodToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.None) as DmdMethodInfo;
			if (genericMethod?.IsGenericMethodDefinition != true)
				return null;
			return genericMethod.MakeGenericMethod(instantiation);
		}

		protected override DmdMethodSignature? ResolveMethodSignature(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			if (IsCOMThread)
				return ResolveMethodSignature_COMThread(rid, genericTypeArguments, genericMethodArguments);
			else
				return COMThread(() => ResolveMethodSignature_COMThread(rid, genericTypeArguments, genericMethodArguments));
		}

		DmdMethodSignature? ResolveMethodSignature_COMThread(uint rid, IList<DmdType>? genericTypeArguments, IList<DmdType>? genericMethodArguments) {
			dispatcher.VerifyAccess();
			var signature = MDAPI.GetStandAloneSigBlob(MetaDataImport, 0x11000000 + rid);
			if (signature.addr == IntPtr.Zero)
				return null;
			return ReadMethodSignature_COMThread(signature, genericTypeArguments, genericMethodArguments, isProperty: false);
		}

		protected override byte[]? ResolveFieldSignature(uint rid) {
			if (IsCOMThread)
				return ResolveFieldSignature_COMThread(rid);
			else
				return COMThread(() => ResolveFieldSignature_COMThread(rid));
		}

		protected override byte[]? ResolveMethodSignature(uint rid) {
			if (IsCOMThread)
				return ResolveMethodSignature_COMThread(rid);
			else
				return COMThread(() => ResolveMethodSignature_COMThread(rid));
		}

		protected override byte[]? ResolveMemberRefSignature(uint rid) {
			if (IsCOMThread)
				return ResolveMemberRefSignature_COMThread(rid);
			else
				return COMThread(() => ResolveMemberRefSignature_COMThread(rid));
		}

		protected override byte[]? ResolveStandAloneSigSignature(uint rid) {
			if (IsCOMThread)
				return ResolveStandAloneSigSignature_COMThread(rid);
			else
				return COMThread(() => ResolveStandAloneSigSignature_COMThread(rid));
		}

		protected override byte[]? ResolveTypeSpecSignature(uint rid) {
			if (IsCOMThread)
				return ResolveTypeSpecSignature_COMThread(rid);
			else
				return COMThread(() => ResolveTypeSpecSignature_COMThread(rid));
		}

		protected override byte[]? ResolveMethodSpecSignature(uint rid) {
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
			return MDAPI.GetUserString(MetaDataImport, 0x70000000 + offset) ?? string.Empty;
		}

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			if (!modulePropsInitd)
				InitializeModuleProperties();
			peKind = __peKind_DONT_USE;
			machine = __machine_DONT_USE;
		}

		public override DmdReadOnlyAssemblyName GetName() {
			if (assemblyName is null)
				InitializeAssemblyName();
			Debug2.Assert(assemblyName is not null);
			return assemblyName;
		}
		DmdReadOnlyAssemblyName? assemblyName;

		void InitializeAssemblyName() {
			if (assemblyName is not null)
				return;
			if (IsCOMThread)
				InitializeAssemblyName_COMThread();
			else
				COMThread(InitializeAssemblyName_COMThread);
		}

		void InitializeAssemblyName_COMThread() {
			dispatcher.VerifyAccess();
			if (assemblyName is not null)
				return;

			const uint token = 0x20000001;
			var version = MDAPI.GetAssemblyVersionAndLocale(MetaDataAssemblyImport, token, out var locale) ?? new Version(0, 0, 0, 0);
			var name = MDAPI.GetAssemblySimpleName(MetaDataAssemblyImport, token) ?? string.Empty;
			var cultureName = locale ?? string.Empty;
			var hashAlgorithm = MDAPI.GetAssemblyHashAlgorithm(MetaDataAssemblyImport, token) ?? DmdAssemblyHashAlgorithm.SHA1;
			var publicKey = MDAPI.GetAssemblyPublicKey(MetaDataAssemblyImport, token) ?? Array.Empty<byte>();
			var flags = MDAPI.GetAssemblyAttributes(MetaDataAssemblyImport, token) ?? DmdAssemblyNameFlags.None;

			assemblyName = new DmdReadOnlyAssemblyName(name, version, cultureName, flags, publicKey, null, hashAlgorithm);
		}

		public override DmdReadOnlyAssemblyName[] GetReferencedAssemblies() {
			if (IsCOMThread)
				return GetReferencedAssemblies_COMThread();
			else
				return COMThread(GetReferencedAssemblies_COMThread);
		}

		DmdReadOnlyAssemblyName[] GetReferencedAssemblies_COMThread() {
			dispatcher.VerifyAccess();
			var list = new List<DmdReadOnlyAssemblyName>();
			for (uint token = 0x23000001; ; token++) {
				if (!MDAPI.IsValidToken(MetaDataImport, token))
					break;
				list.Add(ReadAssemblyName_COMThread(token & 0x00FFFFFF));
			}
			return list.ToArray();
		}

		internal DmdReadOnlyAssemblyName ReadAssemblyName_COMThread(uint rid) {
			dispatcher.VerifyAccess();
			uint token = 0x23000000 + rid;
			var name = MDAPI.GetAssemblyRefSimpleName(MetaDataAssemblyImport, token) ?? string.Empty;
			var version = MDAPI.GetAssemblyRefVersionAndLocale(MetaDataAssemblyImport, token, out var locale) ?? new Version(0, 0, 0, 0);
			var cultureName = locale ?? string.Empty;
			var publicKeyOrToken = MDAPI.GetAssemblyRefPublicKeyOrToken(MetaDataAssemblyImport, token, out var flags);
			return new DmdReadOnlyAssemblyName(name, version, cultureName, flags, publicKeyOrToken, DmdAssemblyHashAlgorithm.None);
		}

		public override unsafe bool ReadMemory(uint rva, void* destination, int size) => false;

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
				if (ctor is null)
					continue;

				var ca = DmdCustomAttributeReader.Read(module, new DmdPointerDataStream(info.addr, info.size), ctor);
				if (ca is null)
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

#pragma warning disable SYSLIB0003 // SecurityAction
		internal DmdCustomAttributeData[] ReadSecurityAttributesCore_COMThread(uint token) {
			dispatcher.VerifyAccess();
			var tokens = MDAPI.GetPermissionSetTokens(MetaDataImport, token);
			if (tokens.Length == 0)
				return Array.Empty<DmdCustomAttributeData>();
			IList<DmdType>? genericTypeArguments = null;
			DmdCustomAttributeData[]? firstCas = null;
			SSP.SecurityAction firstAction = 0;
			List<(DmdCustomAttributeData[] cas, SSP.SecurityAction action)>? res = null;
			for (int i = 0; i < tokens.Length; i++) {
				if (!MDAPI.IsValidToken(MetaDataImport, tokens[i]))
					continue;
				var info = MDAPI.GetPermissionSetBlob(MetaDataImport, tokens[i]);
				if (info.addr == IntPtr.Zero)
					continue;
				var action = (SSP.SecurityAction)(info.action & 0x1F);
				var cas = DmdDeclSecurityReader.Read(module, new DmdPointerDataStream(info.addr, (int)info.size), action, genericTypeArguments);
				if (cas.Length == 0)
					continue;
				if (res is null && firstCas is null) {
					firstAction = action;
					firstCas = cas;
				}
				else {
					if (res is null) {
						res = new List<(DmdCustomAttributeData[], SSP.SecurityAction)>(firstCas!.Length + cas.Length);
						res.Add((firstCas, firstAction));
						firstCas = null;
					}
					res.Add((cas, action));
				}
			}
			if (firstCas is not null)
				return firstCas;
			if (res is null)
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
#pragma warning restore SYSLIB0003 // SecurityAction

		internal DmdMarshalType? ReadFieldMarshalType_COMThread(int metadataToken, DmdModule module, IList<DmdType>? genericTypeArguments) {
			dispatcher.VerifyAccess();
			var signature = MDAPI.GetFieldMarshalBlob(MetaDataImport, (uint)metadataToken);
			if (signature.addr == IntPtr.Zero)
				return null;
			return DmdMarshalBlobReader.Read(module, new DmdPointerDataStream(signature), genericTypeArguments);
		}

		internal (object? value, bool hasValue) ReadFieldConstant_COMThread(int metadataToken) {
			dispatcher.VerifyAccess();
			var c = MDAPI.GetFieldConstant(MetaDataImport, (uint)metadataToken, out var etype);
			if (etype == ElementType.End)
				return (null, false);
			return (c, true);
		}

		internal (object? value, bool hasValue) ReadParamConstant_COMThread(int metadataToken) {
			dispatcher.VerifyAccess();
			var c = MDAPI.GetParamConstant(MetaDataImport, (uint)metadataToken, out var etype);
			if (etype == ElementType.End)
				return (null, false);
			return (c, true);
		}

		internal (object? value, bool hasValue) ReadPropertyConstant_COMThread(int metadataToken) {
			dispatcher.VerifyAccess();
			var c = MDAPI.GetPropertyConstant(MetaDataImport, (uint)metadataToken, out var etype);
			if (etype == ElementType.End)
				return (null, false);
			return (c, true);
		}
	}
}
