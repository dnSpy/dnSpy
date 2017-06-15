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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdEcma335MetadataReader : DmdMetadataReaderBase, IMethodBodyResolver {
		public override Guid ModuleVersionId { get; }
		public override int MDStreamVersion => ((TablesStream.Version & 0xFF00) << 8) | (TablesStream.Version & 0xFF);
		public override string ModuleScopeName { get; }
		public override string ImageRuntimeVersion => Metadata.VersionString;
		public override DmdMethodInfo EntryPoint {
			get {
				if ((Metadata.ImageCor20Header.Flags & ComImageFlags.NativeEntryPoint) != 0)
					return null;
				uint token = Metadata.ImageCor20Header.EntryPointToken_or_RVA;
				if ((token >> 24) != (uint)Table.File)
					return ResolveMethod((int)token, null, null, DmdResolveOptions.None) as DmdMethodInfo;
				return null;
			}
		}

		public static DmdEcma335MetadataReader Create(DmdModuleImpl module, IntPtr address, uint size, bool isFileLayout) {
			var peImage = new PEImage(address, size, isFileLayout ? ImageLayout.File : ImageLayout.Memory, true);
			return Create(module, peImage);
		}

		public static DmdEcma335MetadataReader Create(DmdModuleImpl module, byte[] bytes, bool isFileLayout) {
			var peImage = new PEImage(bytes, isFileLayout ? ImageLayout.File : ImageLayout.Memory, true);
			return Create(module, peImage);
		}

		public static DmdEcma335MetadataReader Create(DmdModuleImpl module, string filename, bool isFileLayout) =>
			Create(module, File.ReadAllBytes(filename), isFileLayout);

		static DmdEcma335MetadataReader Create(DmdModuleImpl module, IPEImage peImage) {
			var metadata = MetaDataCreator.CreateMetaData(peImage);
			return new DmdEcma335MetadataReader(module, metadata);
		}

		internal DmdModule Module => module;
		internal IMetaData Metadata { get; }
		internal TablesStream TablesStream => Metadata.TablesStream;
		internal StringsStream StringsStream => Metadata.StringsStream;
		internal GuidStream GuidStream => Metadata.GuidStream;
		internal BlobStream BlobStream => Metadata.BlobStream;

		readonly object signatureLock;
		readonly DmdModuleImpl module;
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
		readonly Dictionary<uint, DmdType> fieldTypeCache;
		readonly Dictionary<uint, DmdMethodSignature> methodSignatureCache;
		const bool resolveTypes = true;

		DmdEcma335MetadataReader(DmdModuleImpl module, IMetaData metadata) {
			signatureLock = new object();
			this.module = module;
			Metadata = metadata;
			fieldTypeCache = new Dictionary<uint, DmdType>();
			methodSignatureCache = new Dictionary<uint, DmdMethodSignature>();

			var row = TablesStream.ReadModuleRow(1);
			ModuleScopeName = metadata.StringsStream.ReadNoNull(row?.Name ?? 0);
			ModuleVersionId = metadata.GuidStream.Read(row?.Mvid ?? 0) ?? Guid.Empty;

			var ts = TablesStream;
			typeRefList = new LazyList<DmdTypeRef>(ts.TypeRefTable.Rows, rid => new DmdTypeRefMD(this, rid, null));
			fieldList = new LazyList<DmdFieldDef, DmdTypeDef>(ts.FieldTable.Rows, CreateResolvedField);
			typeDefList = new LazyList<DmdTypeDef>(ts.TypeDefTable.Rows, rid => new DmdTypeDefMD(this, rid, null));
			methodList = new LazyList<DmdMethodBase, DmdTypeDef>(ts.MethodTable.Rows, CreateResolvedMethod);
			memberRefList = new LazyList2<DmdMemberInfo, IList<DmdType>, IList<DmdType>>(ts.MemberRefTable.Rows, CreateResolvedMemberRef);
			eventList = new LazyList<DmdEventDef, DmdTypeDef>(ts.EventTable.Rows, CreateResolvedEvent);
			propertyList = new LazyList<DmdPropertyDef, DmdTypeDef>(ts.PropertyTable.Rows, CreateResolvedProperty);
			typeSpecList = new LazyList2<DmdType, IList<DmdType>>(ts.TypeSpecTable.Rows, ReadTypeSpec);
			exportedTypeList = new LazyList<DmdTypeRef>(ts.ExportedTypeTable.Rows, rid => new DmdExportedTypeMD(this, rid, null));

			globalTypeIfThereAreNoTypes = new DmdNullGlobalType(module, null);
		}

		(DmdType type, bool containedGenericParams) ReadTypeSpec(uint rid, IList<DmdType> genericTypeArguments) {
			var row = Metadata.TablesStream.ReadTypeSpecRow(rid);
			using (var stream = BlobStream.CreateStream(row.Signature))
				return DmdSignatureReader.ReadTypeSignature(module, new DmdDataStreamImpl(stream), genericTypeArguments, resolveTypes);
		}

		DmdFieldDefMD CreateResolvedField(uint rid, DmdTypeDef declaringType) {
			if ((object)declaringType == null)
				declaringType = ResolveTypeDef(Metadata.GetOwnerTypeOfField(rid)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef(Metadata.GetOwnerTypeOfField(rid)));
			return CreateFieldDefCore(rid, declaringType, declaringType, declaringType.GetGenericArguments());
		}

		internal DmdFieldDef CreateFieldDef(uint rid, DmdTypeDefMD declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			if ((object)declaringType == reflectedType) {
				Debug.Assert(declaringType.GetGenericArguments() == genericTypeArguments);
				return ResolveFieldDef(rid, declaringType);
			}
			return CreateFieldDefCore(rid, declaringType, reflectedType, genericTypeArguments);
		}

		DmdFieldDefMD CreateFieldDefCore(uint rid, DmdTypeDef declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			new DmdFieldDefMD(this, rid, declaringType, reflectedType, genericTypeArguments);

		internal DmdType ReadFieldType(uint signature, IList<DmdType> genericTypeArguments) {
			lock (signatureLock) {
				if (fieldTypeCache.TryGetValue(signature, out var fieldType)) {
					if ((object)fieldType != null)
						return fieldType;
					var info = ReadFieldTypeCore(signature, genericTypeArguments);
					Debug.Assert(info.containedGenericParams);
					return info.fieldType;
				}
				else {
					var info = ReadFieldTypeCore(signature, genericTypeArguments);
					if (info.containedGenericParams)
						fieldTypeCache.Add(signature, null);
					else
						fieldTypeCache.Add(signature, info.fieldType);
					return info.fieldType;
				}
			}
		}

		(DmdType fieldType, bool containedGenericParams) ReadFieldTypeCore(uint signature, IList<DmdType> genericTypeArguments) {
			using (var stream = BlobStream.CreateStream(signature))
				return DmdSignatureReader.ReadFieldSignature(module, new DmdDataStreamImpl(stream), genericTypeArguments, resolveTypes);
		}

		DmdMethodBase CreateResolvedMethod(uint rid, DmdTypeDef declaringType) {
			if ((object)declaringType == null)
				declaringType = ResolveTypeDef(Metadata.GetOwnerTypeOfMethod(rid)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef(Metadata.GetOwnerTypeOfMethod(rid)));
			return CreateMethodDefCore(rid, declaringType, declaringType, declaringType.GetGenericArguments());
		}

		internal DmdMethodBase CreateMethodDef(uint rid, DmdTypeDefMD declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			if ((object)declaringType == reflectedType) {
				Debug.Assert(declaringType.GetGenericArguments() == genericTypeArguments);
				return ResolveMethodDef(rid, declaringType);
			}
			return CreateMethodDefCore(rid, declaringType, reflectedType, genericTypeArguments);
		}

		DmdMethodBase CreateMethodDefCore(uint rid, DmdTypeDef declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			var row = TablesStream.ReadMethodRow(rid);
			string name = StringsStream.ReadNoNull(row.Name);
			if ((row.Flags & (int)DmdMethodAttributes.RTSpecialName) != 0 && name.Length > 0 && name[0] == '.') {
				if (name == DmdConstructorInfo.ConstructorName || name == DmdConstructorInfo.TypeConstructorName)
					return new DmdConstructorDefMD(this, row, rid, name, declaringType, reflectedType, genericTypeArguments);
			}
			return new DmdMethodDefMD(this, row, rid, name, declaringType, reflectedType, genericTypeArguments);
		}

		internal DmdMethodSignature ReadMethodSignature(uint signature, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool isProperty) {
			lock (signatureLock) {
				if (methodSignatureCache.TryGetValue(signature, out var methodSignature)) {
					if ((object)methodSignature != null)
						return methodSignature;
					var info = ReadMethodSignatureCore(signature, genericTypeArguments, genericMethodArguments, isProperty);
					Debug.Assert(info.containedGenericParams);
					return info.methodSignature;
				}
				else {
					var info = ReadMethodSignatureCore(signature, genericTypeArguments, genericMethodArguments, isProperty);
					if (info.containedGenericParams)
						methodSignatureCache.Add(signature, null);
					else
						methodSignatureCache.Add(signature, info.methodSignature);
					return info.methodSignature;
				}
			}
		}

		(DmdMethodSignature methodSignature, bool containedGenericParams) ReadMethodSignatureCore(uint signature, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool isProperty) {
			using (var stream = BlobStream.CreateStream(signature))
				return DmdSignatureReader.ReadMethodSignature(module, new DmdDataStreamImpl(stream), genericTypeArguments, genericMethodArguments, isProperty, resolveTypes);
		}

		internal (DmdParameterInfo returnParameter, DmdParameterInfo[] parameters) CreateParameters(DmdMethodBase method, bool createReturnParameter) {
			var ridList = Metadata.GetParamRidList((uint)method.MetadataToken & 0x00FFFFFF);
			var methodSignature = method.GetMethodSignature();
			var sigParamTypes = methodSignature.GetParameterTypes();
			DmdParameterInfo returnParameter = null;
			var parameters = sigParamTypes.Count == 0 ? Array.Empty<DmdParameterInfo>() : new DmdParameterInfo[sigParamTypes.Count];
			for (int i = 0; i < ridList.Count; i++) {
				uint rid = ridList[i];
				var row = TablesStream.ReadParamRow(rid);
				var name = StringsStream.Read(row.Name);
				if (row.Sequence == 0) {
					if (createReturnParameter && (object)returnParameter == null)
						returnParameter = new DmdParameterDefMD(this, rid, name, (DmdParameterAttributes)row.Flags, method, -1, methodSignature.ReturnType);
				}
				else {
					int paramIndex = row.Sequence - 1;
					if ((uint)paramIndex < (uint)parameters.Length) {
						if ((object)parameters[paramIndex] == null)
							parameters[paramIndex] = new DmdParameterDefMD(this, rid, name, (DmdParameterAttributes)row.Flags, method, paramIndex, sigParamTypes[paramIndex]);
					}
				}
			}
			for (int i = 0; i < parameters.Length; i++) {
				if ((object)parameters[i] == null)
					parameters[i] = new DmdCreatedParameterDef(method, i, sigParamTypes[i]);
			}
			if (createReturnParameter && (object)returnParameter == null)
				returnParameter = new DmdCreatedParameterDef(method, -1, methodSignature.ReturnType);

			return (returnParameter, parameters);
		}

		DmdEventDef CreateResolvedEvent(uint rid, DmdTypeDef declaringType) {
			if ((object)declaringType == null)
				declaringType = ResolveTypeDef(Metadata.GetOwnerTypeOfEvent(rid)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef(Metadata.GetOwnerTypeOfEvent(rid)));
			return CreateEventDefCore(rid, declaringType, declaringType, declaringType.GetGenericArguments());
		}

		internal DmdEventDef CreateEventDef(uint rid, DmdTypeDefMD declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			if ((object)declaringType == reflectedType) {
				Debug.Assert(declaringType.GetGenericArguments() == genericTypeArguments);
				return ResolveEventDef(rid, declaringType);
			}
			return CreateEventDefCore(rid, declaringType, reflectedType, genericTypeArguments);
		}

		DmdEventDef CreateEventDefCore(uint rid, DmdTypeDef declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			new DmdEventDefMD(this, rid, declaringType, reflectedType, genericTypeArguments);

		DmdPropertyDef CreateResolvedProperty(uint rid, DmdTypeDef declaringType) {
			if ((object)declaringType == null)
				declaringType = ResolveTypeDef(Metadata.GetOwnerTypeOfProperty(rid)) ?? globalTypeIfThereAreNoTypes;
			else
				Debug.Assert((object)declaringType == ResolveTypeDef(Metadata.GetOwnerTypeOfProperty(rid)));
			return CreatePropertyDefCore(rid, declaringType, declaringType, declaringType.GetGenericArguments());
		}

		internal DmdPropertyDef CreatePropertyDef(uint rid, DmdTypeDefMD declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) {
			if ((object)declaringType == reflectedType) {
				Debug.Assert(declaringType.GetGenericArguments() == genericTypeArguments);
				return ResolvePropertyDef(rid, declaringType);
			}
			return CreatePropertyDefCore(rid, declaringType, reflectedType, genericTypeArguments);
		}

		DmdPropertyDef CreatePropertyDefCore(uint rid, DmdTypeDef declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments) =>
			new DmdPropertyDefMD(this, rid, declaringType, reflectedType, genericTypeArguments);

		internal DmdType[] CreateGenericParameters(DmdMethodBase method) {
			var ridList = Metadata.GetGenericParamRidList(Table.Method, (uint)method.MetadataToken & 0x00FFFFFF);
			if (ridList.Count == 0)
				return null;
			var genericParams = new DmdType[ridList.Count];
			for (int i = 0; i < genericParams.Length; i++) {
				uint rid = ridList[i];
				var row = TablesStream.ReadGenericParamRow(rid) ?? new RawGenericParamRow();
				var gpName = StringsStream.ReadNoNull(row.Name);
				var gpType = new DmdGenericParameterTypeMD(this, rid, method, gpName, row.Number, (DmdGenericParameterAttributes)row.Flags, null);
				genericParams[i] = gpType;
			}
			return genericParams;
		}

		(DmdMemberInfo member, bool containedGenericParams) CreateResolvedMemberRef(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			var row = TablesStream.ReadMemberRefRow(rid);
			var name = StringsStream.ReadNoNull(row.Name);
			var info = ReadMethodSignatureOrFieldType(row.Signature, genericTypeArguments, genericMethodArguments);
			var rawInfo = info.containedGenericParams ? ReadMethodSignatureOrFieldType(row.Signature, null, null) : info;

			if (!CodedToken.MemberRefParent.Decode(row.Class, out uint classToken))
				classToken = uint.MaxValue;
			var reflectedTypeRef = GetMemberRefParent(classToken, genericTypeArguments, genericMethodArguments);

			if ((object)info.fieldType != null) {
				var fieldRef = new DmdFieldRef(reflectedTypeRef, name, rawInfo.fieldType, info.fieldType);
				return (fieldRef, info.containedGenericParams);
			}
			else {
				Debug.Assert((object)info.methodSignature != null);
				if (name == DmdConstructorInfo.ConstructorName || name == DmdConstructorInfo.TypeConstructorName) {
					var ctorRef = new DmdConstructorRefMD(this, reflectedTypeRef, name, rawInfo.methodSignature, info.methodSignature);
					return (ctorRef, info.containedGenericParams);
				}
				else {
					var methodRef = new DmdMethodRefMD(this, row.Signature, genericTypeArguments, reflectedTypeRef, name, rawInfo.methodSignature, info.methodSignature);
					return (methodRef, info.containedGenericParams);
				}
			}
		}

		DmdType GetMemberRefParent(uint classToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			switch ((Table)(classToken >> 24)) {
			case Table.TypeDef:
			case Table.TypeRef:
				return ResolveType((int)classToken, null, null, DmdResolveOptions.None) ?? Module.AppDomain.System_Void;

			case Table.ModuleRef:
				var moduleRefRow = TablesStream.ReadModuleRefRow(classToken & 0x00FFFFFF);
				var moduleName = StringsStream.ReadNoNull(moduleRefRow?.Name ?? 0);
				if (StringComparer.OrdinalIgnoreCase.Equals(moduleName, Module.ScopeName))
					return Module.GlobalType;
				var referencedModule = Module.Assembly.GetModule(moduleName);
				return referencedModule?.GlobalType ?? Module.AppDomain.System_Void;

			case Table.Method:
				return ResolveMethod((int)classToken, null, null, DmdResolveOptions.None)?.DeclaringType ?? Module.AppDomain.System_Void;

			case Table.TypeSpec:
				return ResolveType((int)classToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.None) ?? Module.AppDomain.System_Void;

			default:
				return Module.AppDomain.System_Void;
			}
		}

		(DmdType fieldType, DmdMethodSignature methodSignature, bool containedGenericParams) ReadMethodSignatureOrFieldType(uint signature, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			lock (signatureLock) {
				if (methodSignatureCache.TryGetValue(signature, out var methodSignature)) {
					if ((object)methodSignature != null)
						return (null, methodSignature, false);
					var info = ReadMethodSignatureCore(signature, genericTypeArguments, genericMethodArguments, isProperty: false);
					if ((object)info.methodSignature == null)
						throw new InvalidOperationException();
					Debug.Assert(info.containedGenericParams);
					return (null, info.methodSignature, info.containedGenericParams);
				}
				else if (fieldTypeCache.TryGetValue(signature, out var fieldType)) {
					if ((object)fieldType != null)
						return (fieldType, null, false);
					var info = ReadFieldTypeCore(signature, genericTypeArguments);
					if ((object)info.fieldType == null)
						throw new InvalidOperationException();
					Debug.Assert(info.containedGenericParams);
					return (info.fieldType, null, info.containedGenericParams);
				}
				else {
					using (var stream = BlobStream.CreateStream(signature)) {
						var info = DmdSignatureReader.ReadMethodSignatureOrFieldType(module, new DmdDataStreamImpl(stream), genericTypeArguments, genericMethodArguments, resolveTypes);
						if ((object)info.fieldType != null) {
							if (info.containedGenericParams)
								fieldTypeCache.Add(signature, null);
							else
								fieldTypeCache.Add(signature, info.fieldType);
							return (info.fieldType, null, info.containedGenericParams);
						}
						else {
							Debug.Assert((object)info.methodSignature != null);
							if (info.containedGenericParams)
								methodSignatureCache.Add(signature, null);
							else
								methodSignatureCache.Add(signature, info.methodSignature);
							return (null, info.methodSignature, info.containedGenericParams);
						}
					}
				}
			}
		}

		internal DmdMethodBody GetMethodBody(DmdMethodBase method, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			if ((method.MethodImplementationFlags & DmdMethodImplAttributes.CodeTypeMask) != DmdMethodImplAttributes.IL)
				return null;
			var row = TablesStream.ReadMethodRow((uint)method.MetadataToken & 0x00FFFFFF);
			if (row == null)
				return null;
			if (row.RVA == 0)
				return null;

			using (var stream = Metadata.PEImage.CreateFullStream()) {
				stream.Position = (long)Metadata.PEImage.ToFileOffset((RVA)row.RVA);
				var body = DmdMethodBodyReader.Create(this, new DmdDataStreamImpl(stream), genericTypeArguments, genericMethodArguments);
				Debug.Assert(body != null);
				return body;
			}
		}

		(DmdType type, bool isPinned)[] IMethodBodyResolver.ReadLocals(int localSignatureMetadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			if ((localSignatureMetadataToken & 0x00FFFFFF) == 0 || (byte)(localSignatureMetadataToken >> 24) != 0x11)
				return Array.Empty<(DmdType, bool)>();
			uint rid = (uint)localSignatureMetadataToken & 0x00FFFFFF;
			var row = TablesStream.ReadStandAloneSigRow(rid);
			if (row == null)
				return Array.Empty<(DmdType, bool)>();
			using (var stream = BlobStream.CreateStream(row.Signature))
				return DmdSignatureReader.ReadLocalsSignature(module, new DmdDataStreamImpl(stream), genericTypeArguments, genericMethodArguments, resolveTypes);
		}

		internal DmdMethodSignature GetOriginalMethodDefSignature(DmdMethodBase method) {
			if ((method.MetadataToken >> 24) != 0x06)
				throw new InvalidOperationException();
			var row = TablesStream.ReadMethodRow((uint)method.MetadataToken & 0x00FFFFFF);
			return ReadMethodSignature(row?.Signature ?? uint.MaxValue, null, null, isProperty: false);
		}

		internal DmdMethodSignature GetOriginalMethodRefSignature(DmdMethodBase method) {
			if ((method.MetadataToken >> 24) != 0x0A)
				throw new InvalidOperationException();
			var row = TablesStream.ReadMemberRefRow((uint)method.MetadataToken & 0x00FFFFFF);
			return ReadMethodSignature(row?.Signature ?? uint.MaxValue, null, null, isProperty: false);
		}

		public override DmdType GetNonNestedType(string @namespace, string name, bool ignoreCase) {
			uint typeDefRows = TablesStream.TypeDefTable.Rows;
			if (typeDefRows == 0)
				typeDefRows = 1;

			if (ignoreCase) {
				for (uint rid = 1; rid <= typeDefRows; rid++) {
					var type = ResolveTypeDef(rid);
					if (type.IsNested)
						continue;
					if (StringComparer.OrdinalIgnoreCase.Equals(name, type.Name) && StringComparer.OrdinalIgnoreCase.Equals(@namespace, type.Namespace))
						return type;
				}
			}
			else {
				for (uint rid = 1; rid <= typeDefRows; rid++) {
					var type = ResolveTypeDef(rid);
					if (type.IsNested)
						continue;
					if (StringComparer.Ordinal.Equals(name, type.Name) && StringComparer.Ordinal.Equals(@namespace, type.Namespace))
						return type;
				}
			}
			return null;
		}

		public override DmdType[] GetTypes() {
			uint typeDefRows = TablesStream.TypeDefTable.Rows;
			// This should never happen but we must return at least one type
			if (typeDefRows == 0)
				return new DmdType[] { globalTypeIfThereAreNoTypes };
			var result = new DmdType[typeDefRows];
			for (int i = 0; i < result.Length; i++) {
				var type = ResolveTypeDef((uint)i + 1);
				result[i] = type ?? throw new InvalidOperationException();
			}
			return result;
		}

		public override DmdType[] GetExportedTypes() {
			if (TablesStream.ExportedTypeTable.Rows == 0)
				return Array.Empty<DmdType>();
			var result = new DmdType[TablesStream.ExportedTypeTable.Rows];
			for (int i = 0; i < result.Length; i++) {
				var type = ResolveExportedType((uint)i + 1);
				result[i] = type ?? throw new InvalidOperationException();
			}
			return result;
		}

		protected override DmdTypeRef ResolveTypeRef(uint rid) => typeRefList[rid - 1];
		protected override DmdTypeDef ResolveTypeDef(uint rid) {
			var type = typeDefList[rid - 1];
			if ((object)type == null && rid == 1)
				return globalTypeIfThereAreNoTypes;
			return type;
		}
		protected override DmdFieldDef ResolveFieldDef(uint rid) => fieldList[rid - 1, null];
		DmdFieldDef ResolveFieldDef(uint rid, DmdTypeDef declaringType) => fieldList[rid - 1, declaringType];
		protected override DmdMethodBase ResolveMethodDef(uint rid) => methodList[rid - 1, null];
		DmdMethodBase ResolveMethodDef(uint rid, DmdTypeDef declaringType) => methodList[rid - 1, declaringType];
		protected override DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => memberRefList[rid - 1, genericTypeArguments, genericMethodArguments];
		protected override DmdEventDef ResolveEventDef(uint rid) => eventList[rid - 1, null];
		DmdEventDef ResolveEventDef(uint rid, DmdTypeDef declaringType) => eventList[rid - 1, declaringType];
		protected override DmdPropertyDef ResolvePropertyDef(uint rid) => propertyList[rid - 1, null];
		DmdPropertyDef ResolvePropertyDef(uint rid, DmdTypeDef declaringType) => propertyList[rid - 1, declaringType];
		protected override DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments) => typeSpecList[rid - 1, genericTypeArguments];
		protected override DmdTypeRef ResolveExportedType(uint rid) => exportedTypeList[rid - 1];
		protected override DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			var row = TablesStream.ReadMethodSpecRow(rid);
			if (row == null)
				return null;
			DmdType[] instantiation;
			using (var stream = BlobStream.CreateStream(row.Instantiation))
				instantiation = DmdSignatureReader.ReadMethodSpecSignature(module, new DmdDataStreamImpl(stream), genericTypeArguments, genericMethodArguments, resolveTypes).types;
			if (!CodedToken.MethodDefOrRef.Decode(row.Method, out uint token))
				return null;
			var genericMethod = ResolveMethod((int)token, genericTypeArguments, genericMethodArguments, DmdResolveOptions.None) as DmdMethodInfo;
			if (genericMethod?.IsGenericMethodDefinition != true)
				return null;
			return genericMethod.MakeGenericMethod(instantiation);
		}

		protected override byte[] ResolveFieldSignature(uint rid) {
			var row = TablesStream.ReadFieldRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveMethodSignature(uint rid) {
			var row = TablesStream.ReadMethodRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveMemberRefSignature(uint rid) {
			var row = TablesStream.ReadMemberRefRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveStandAloneSigSignature(uint rid) {
			var row = TablesStream.ReadStandAloneSigRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveTypeSpecSignature(uint rid) {
			var row = TablesStream.ReadTypeSpecRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveMethodSpecSignature(uint rid) {
			var row = TablesStream.ReadMethodSpecRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Instantiation);
		}

		protected override string ResolveStringCore(uint offset) => Metadata.USStream.Read(offset);

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			machine = (DmdImageFileMachine)Metadata.PEImage.ImageNTHeaders.FileHeader.Machine;
			peKind = 0;
			if ((Metadata.ImageCor20Header.Flags & ComImageFlags.ILOnly) != 0)
				peKind |= DmdPortableExecutableKinds.ILOnly;
			if (Metadata.PEImage.ImageNTHeaders.OptionalHeader.Magic != 0x010B)
				peKind |= DmdPortableExecutableKinds.PE32Plus;
			if ((Metadata.ImageCor20Header.Flags & ComImageFlags._32BitRequired) != 0)
				peKind |= DmdPortableExecutableKinds.Required32Bit;
			if ((Metadata.ImageCor20Header.Flags & ComImageFlags._32BitPreferred) != 0)
				peKind |= DmdPortableExecutableKinds.Preferred32Bit;
		}

		public override DmdAssemblyName GetName() {
			var name = new DmdAssemblyName();
			var row = TablesStream.ReadAssemblyRow(1);
			if (row == null) {
				name.Name = "no-asm-" + Guid.NewGuid().ToString();
				return name;
			}

			name.Version = new Version(row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber);
			name.Name = StringsStream.ReadNoNull(row.Name);
			name.CultureName = StringsStream.ReadNoNull(row.Locale);
			name.HashAlgorithm = (DmdAssemblyHashAlgorithm)row.HashAlgId;
			name.SetPublicKey(BlobStream.ReadNoNull(row.PublicKey));
			name.Flags = (DmdAssemblyNameFlags)row.Flags;
			return name;
		}

		public override DmdAssemblyName[] GetReferencedAssemblies() {
			var tbl = TablesStream.AssemblyRefTable;
			if (tbl.Rows == 0)
				return Array.Empty<DmdAssemblyName>();
			var res = new DmdAssemblyName[tbl.Rows];
			for (int i = 0; i < res.Length; i++)
				res[i] = ReadAssemblyName((uint)i + 1);
			return res;
		}

		internal DmdAssemblyName ReadAssemblyName(uint rid) {
			var asmName = new DmdAssemblyName();
			var row = TablesStream.ReadAssemblyRefRow(rid) ?? new RawAssemblyRefRow();
			asmName.Name = Metadata.StringsStream.ReadNoNull(row.Name);
			asmName.CultureName = Metadata.StringsStream.ReadNoNull(row.Locale);
			asmName.Version = new Version(row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber);
			if (row.PublicKeyOrToken != 0) {
				var bytes = Metadata.BlobStream.ReadNoNull(row.PublicKeyOrToken);
				if ((row.Flags & (int)DmdAssemblyNameFlags.PublicKey) != 0)
					asmName.SetPublicKey(bytes);
				else
					asmName.SetPublicKeyToken(bytes);
			}
			asmName.Flags = (DmdAssemblyNameFlags)row.Flags;
			return asmName;
		}

		protected override DmdCustomAttributeData[] ReadAssemblyCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Assembly, rid);
		protected override DmdCustomAttributeData[] ReadModuleCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Module, rid);
		protected override DmdCustomAttributeData[] ReadTypeDefCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.TypeDef, rid);
		protected override DmdCustomAttributeData[] ReadFieldCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Field, rid);
		protected override DmdCustomAttributeData[] ReadMethodCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Method, rid);
		protected override DmdCustomAttributeData[] ReadParamCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Param, rid);
		protected override DmdCustomAttributeData[] ReadEventCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Event, rid);
		protected override DmdCustomAttributeData[] ReadPropertyCustomAttributes(uint rid) => ReadCustomAttributesCore(Table.Property, rid);

		DmdCustomAttributeData[] ReadCustomAttributesCore(Table table, uint rid) {
			var ridList = Metadata.GetCustomAttributeRidList(table, rid);
			if (ridList.Count == 0)
				return Array.Empty<DmdCustomAttributeData>();

			var res = new DmdCustomAttributeData[ridList.Count];
			int w = 0;
			for (int i = 0; i < ridList.Count; i++) {
				var row = TablesStream.ReadCustomAttributeRow(ridList[i]);
				if (row == null)
					continue;

				var ctor = ResolveCustomAttributeType(row.Type, null);
				if ((object)ctor == null)
					continue;

				DmdCustomAttributeData ca;
				using (var stream = BlobStream.CreateStream(row.Value))
					ca = DmdCustomAttributeReader.Read(module, new DmdDataStreamImpl(stream), ctor);
				if (ca == null)
					continue;

				res[w++] = ca;
			}
			if (res.Length != w)
				Array.Resize(ref res, w);
			return res;
		}

		DmdConstructorInfo ResolveCustomAttributeType(uint caType, IList<DmdType> genericTypeArguments) {
			if (!CodedToken.CustomAttributeType.Decode(caType, out uint ctorToken))
				return null;
			var method = ResolveMethod((int)ctorToken, genericTypeArguments, null, DmdResolveOptions.None);
			return (method?.ResolveMemberNoThrow() ?? method) as DmdConstructorInfo;
		}

		internal (object value, bool hasValue) ReadConstant(int metadataToken) {
			var constantRid = Metadata.GetConstantRid((Table)((uint)metadataToken >> 24), (uint)(metadataToken & 0x00FFFFFF));
			if (constantRid == 0)
				return (null, false);
			var row = TablesStream.ReadConstantRow(constantRid);
			if (row == null)
				return (null, false);
			return (MetadataConstantUtilities.GetValue((ElementType)row.Type, BlobStream.ReadNoNull(row.Value)), true);
		}
	}
}
