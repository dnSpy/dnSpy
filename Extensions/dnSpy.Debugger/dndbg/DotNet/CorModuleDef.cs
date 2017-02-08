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

// The class isn't thread safe but can be accessed from multiple threads if every type and their
// properties have been read at least once from the same thread that IMetaDataImport was created on.
// LoadEverything() can be called to read everything. DisableMDAPICalls should also be set to true
// or ResolveToken() calls could throw an error due to IsValidToken() being called which calls the
// MD API.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using dndbg.COM.MetaData;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;
using dnlib.Threading;
using dnlib.Utils;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dndbg.DotNet {
	sealed class MemberInfo<TItem> {
		readonly TItem item;
		readonly Action<TItem> initItem;

		public TItem Item {
			get {
				initItem(item);
				return item;
			}
		}

		internal TItem _ItemNoInit => item;

		public MemberInfo(TItem item, Action<TItem> initItem) {
			this.item = item;
			this.initItem = initItem;
		}
	}

	/// <summary>
	/// A <see cref="ModuleDef"/> created from an <c>IMetaDataImport</c>
	/// </summary>
	public sealed class CorModuleDef : ModuleDef, ISignatureReaderHelper, IInstructionOperandResolver, ICorHasCustomAttribute {
		readonly IMetaDataImport mdi;
		readonly IMetaDataImport2 mdi2;
		readonly uint origRid;
		readonly ICorModuleDefHelper corModuleDefHelper;
		readonly bool isManifestModule;
		readonly bool isExeFile;

		readonly Dictionary<uint, MemberInfo<CorFieldDef>> ridToField = new Dictionary<uint, MemberInfo<CorFieldDef>>();
		readonly Dictionary<uint, MemberInfo<CorMethodDef>> ridToMethod = new Dictionary<uint, MemberInfo<CorMethodDef>>();
		readonly Dictionary<uint, MemberInfo<CorParamDef>> ridToParam = new Dictionary<uint, MemberInfo<CorParamDef>>();
		readonly Dictionary<uint, MemberInfo<CorGenericParam>> ridToGenericParam = new Dictionary<uint, MemberInfo<CorGenericParam>>();
		readonly Dictionary<uint, MemberInfo<CorPropertyDef>> ridToPropertyDef = new Dictionary<uint, MemberInfo<CorPropertyDef>>();
		readonly Dictionary<uint, MemberInfo<CorEventDef>> ridToEventDef = new Dictionary<uint, MemberInfo<CorEventDef>>();

		readonly Dictionary<uint, CorInterfaceImpl> ridToInterfaceImpl = new Dictionary<uint, CorInterfaceImpl>();
		readonly Dictionary<uint, CorMemberRef> ridToMemberRef = new Dictionary<uint, CorMemberRef>();
		readonly Dictionary<uint, CorStandAloneSig> ridToStandAloneSig = new Dictionary<uint, CorStandAloneSig>();
		readonly Dictionary<uint, CorTypeSpec> ridToTypeSpec = new Dictionary<uint, CorTypeSpec>();

		readonly Dictionary<uint, CorInterfaceImpl> ridToInterfaceImplNoCtx = new Dictionary<uint, CorInterfaceImpl>();
		readonly Dictionary<uint, CorMemberRef> ridToMemberRefNoCtx = new Dictionary<uint, CorMemberRef>();
		readonly Dictionary<uint, CorStandAloneSig> ridToStandAloneSigNoCtx = new Dictionary<uint, CorStandAloneSig>();
		readonly Dictionary<uint, CorTypeSpec> ridToTypeSpecNoCtx = new Dictionary<uint, CorTypeSpec>();
		readonly Dictionary<uint, CorMethodSpec> ridToMethodSpecNoCtx = new Dictionary<uint, CorMethodSpec>();
		readonly Dictionary<uint, CorGenericParamConstraint> ridToGenericParamConstraintNoCtx = new Dictionary<uint, CorGenericParamConstraint>();

		readonly Dictionary<uint, CorTypeDef> ridToType = new Dictionary<uint, CorTypeDef>();
		readonly Dictionary<uint, CorTypeRef> ridToTypeRef = new Dictionary<uint, CorTypeRef>();
		readonly Dictionary<uint, CorAssemblyRef> ridToAssemblyRef = new Dictionary<uint, CorAssemblyRef>();
		readonly Dictionary<uint, CorModuleRef> ridToModuleRef = new Dictionary<uint, CorModuleRef>();
		readonly Dictionary<uint, CorDeclSecurity> ridToDeclSecurity = new Dictionary<uint, CorDeclSecurity>();
		readonly Dictionary<uint, CorFileDef> ridToFileDef = new Dictionary<uint, CorFileDef>();
		readonly Dictionary<uint, CorExportedType> ridToExportedType = new Dictionary<uint, CorExportedType>();
		readonly Dictionary<uint, CorManifestResource> ridToManifestResource = new Dictionary<uint, CorManifestResource>();

		Dictionary<uint, List<uint>> ridToNested;
		Dictionary<uint, uint> ridToEnclosing;
		HashSet<uint> nestedListInitd;

		uint lastExportedTypeRidInList;
		uint lastManifestResourceRidInList;
		uint lastTriedTypeRid;

		/// <summary>
		/// Needs to be set to true after <see cref="LoadEverything()"/> has been called if this
		/// instance can be accessed from other threads other than the main thread.
		/// </summary>
		public bool DisableMDAPICalls {
			get { return disableMDAPICalls; }
			set { disableMDAPICalls = value; }
		}
		bool disableMDAPICalls;

		/// <summary>
		/// Gets notified when a new type is added/updated. If this isn't a dynamic module, this
		/// event is never raised.
		/// </summary>
		public event EventHandler<TypeUpdatedEventArgs> TypeUpdated;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		/// <summary>
		/// Gets the <see cref="IMetaDataImport"/>. This is never null
		/// </summary>
		public IMetaDataImport MetaDataImport => mdi;

		/// <summary>
		/// Gets the <see cref="IMetaDataImport2"/>. This is null if it's not available, eg.
		/// a .NET 1.x process is being debugged using the CLR 1.x runtime.
		/// </summary>
		public IMetaDataImport2 MetaDataImport2 => mdi2;

		public IMetaDataAssemblyImport MetaDataAssemblyImport => mdi as IMetaDataAssemblyImport;

		public CorModuleDef(IMetaDataImport mdi, ICorModuleDefHelper corModuleDefHelper) {
			if (mdi == null)
				throw new ArgumentNullException(nameof(mdi));
			rid = 1;
			origRid = 1;
			this.mdi = mdi;
			mdi2 = mdi as IMetaDataImport2;
			this.corModuleDefHelper = corModuleDefHelper;
			var fname = corModuleDefHelper.Filename;
			Debug.Assert(fname == null || File.Exists(fname));
			location = fname ?? string.Empty;
			isManifestModule = corModuleDefHelper.IsManifestModule;
			isExeFile = IsExeFile(isManifestModule, location);
			InitializeLastUsedRids();
		}

		static bool IsExeFile(bool isManifestModule, string location) {
			if (!isManifestModule)
				return false;
			if (string.IsNullOrEmpty(location))
				return false;
			return location.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
		}

		void InitializeLastUsedRids() {
			// Could be a dynamic assembly where types, methods etc could be created. We don't
			// want any TypeDefUser + CorTypeDef to have the same rid.
			for (int i = 0; i < lastUsedRids.Length; i++)
				lastUsedRids[i] = 0x00800000;
			lastUsedRids[(int)Table.Module] = 1;
			lastUsedRids[(int)Table.Assembly] = 1;
		}

		void CreateCorLibTypes() {
			var corLibAsm = GetCorAssemblyRef() ?? AssemblyRefUser.CreateMscorlibReferenceCLR20();
			var corLibAsmRef = corLibAsm as AssemblyRef;
			if (corLibAsmRef == null)
				corLibAsmRef = new AssemblyRefUser(corLibAsm);
			corLibTypes = new CorLibTypes(this, UpdateRowId(corLibAsmRef));
		}

		IAssembly GetCorAssemblyRef() {
			if (corModuleDefHelper.IsCorLib == true)
				return Assembly;
			if (corModuleDefHelper.IsDynamic)
				return corModuleDefHelper.CorLib;

			var asmRef = TryGetCorLibAssemblyRef();
			if (asmRef != null)
				return asmRef;

			return corModuleDefHelper.CorLib;
		}

		AssemblyRef TryGetCorLibAssemblyRef() {
			AssemblyRef bestMscorlib = null;
			AssemblyRef bestNonMscorlib = null;
			for (uint rid = 1; ; rid++) {
				if (!IsValidToken(new MDToken(Table.AssemblyRef, rid).Raw))
					break;
				var asmRef = ResolveAssemblyRef(rid);
				if (asmRef == null)
					break;
				if (!UTF8String.IsNullOrEmpty(asmRef.Culture))
					continue;

				if ("mscorlib".Equals(asmRef.Name, StringComparison.OrdinalIgnoreCase)) {
					if ((pktMscorlibDesktop.Equals(asmRef.PublicKeyOrToken) || pktMscorlibDotNetCore.Equals(asmRef.PublicKeyOrToken)) &&
						asmRef.Version != mscorlibWinMDVersion) {
						if (bestMscorlib == null || asmRef.Version > bestMscorlib.Version)
							bestMscorlib = asmRef;
					}
				}
				else if (bestNonMscorlib == null && asmRef.IsCorLib())
					bestNonMscorlib = asmRef;
			}

			if (bestMscorlib != null)
				return bestMscorlib;
			return bestNonMscorlib;
		}
		static readonly Version mscorlibWinMDVersion = new Version(0xFF, 0xFF, 0xFF, 0xFF);
		static readonly PublicKeyToken pktMscorlibDesktop = new PublicKeyToken("b77a5c561934e089");
		static readonly PublicKeyToken pktMscorlibDotNetCore = new PublicKeyToken("7cec85d7bea7798e");

		/// <summary>
		/// Should be called after caller has added it to its assembly
		/// </summary>
		public void Initialize() {
			CreateCorLibTypes();

			InitModuleProperties_NoLock();

			//TODO: protected virtual RVA GetNativeEntryPoint_NoLock() {
			//TODO: protected virtual IManagedEntryPoint GetManagedEntryPoint_NoLock() {
			//TODO: protected virtual VTableFixups GetVTableFixups_NoLock() {
			//TODO: protected virtual Win32Resources GetWin32Resources_NoLock() {
		}

		public bool IsValidToken(uint token) {
			if ((token & 0x00FFFFFF) == 0)
				return false;
			return MDAPI.IsValidToken(mdi, token);
		}

		bool IsValidToken(Table table, uint rid) => IsValidToken(new MDToken(table, rid).Raw);

		/// <summary>
		/// Should be called when a <c>UnloadClass</c> debug event has been received
		/// </summary>
		/// <param name="token">Token of unloaded class</param>
		public void UnloadClass(uint token) {
			// Nothing to do
		}

		/// <summary>
		/// Should be called when a <c>LoadClass</c> debug event has been received
		/// </summary>
		/// <param name="token">Token of loaded class</param>
		public TypeDef LoadClass(uint token) {
			if (!IsValidToken(token))
				return null;
			var mdToken = new MDToken(token);
			bool b = mdToken.Table == Table.TypeDef && mdToken.Rid != 0;
			Debug.Assert(b);
			if (!b)
				return null;
			if (!corModuleDefHelper.IsDynamic)
				return ResolveTypeDef(mdToken.Rid);
			bool created;
			return InitializeTypeDef(mdToken.Rid, true, out created);
		}

		CorTypeDef InitializeTypeDef(uint rid, bool calledFromLoadClass, out bool created) {
			created = false;
			if (!DisableMDAPICalls && !IsValidToken(Table.TypeDef, rid))
				return null;
			CorTypeDef td;
			bool b = ridToType.TryGetValue(rid, out td);
			if (b && !calledFromLoadClass)
				return td;
			if (DisableMDAPICalls)
				return null;

			if (td == null) {
				created = true;
				td = new CorTypeDef(this, rid);
				ridToType.Add(rid, td);
				UpdateTypeTables(td);
			}
			return Initialize(td, created, calledFromLoadClass);
		}

		CorTypeDef Initialize(CorTypeDef td, bool created, bool calledFromLoadClass) {
			td.Initialize(corModuleDefHelper.IsDynamic ? calledFromLoadClass : true);
			if (corModuleDefHelper.IsDynamic) {
				TypeUpdated?.Invoke(this, new TypeUpdatedEventArgs(td, created, calledFromLoadClass));
			}
			return td;
		}

		internal CallingConventionSig ReadSignature(byte[] data, GenericParamContext gpContext) {
			if (data == null)
				return null;
			return SignatureReader.ReadSig(this, CorLibTypes, data, gpContext);
		}

		internal TypeSig ReadTypeSignature(byte[] data, GenericParamContext gpContext, out byte[] extraData) {
			if (data == null) {
				extraData = null;
				return null;
			}

			return SignatureReader.ReadTypeSig(this, CorLibTypes, data, gpContext, out extraData);
		}

		protected override void InitializeCustomAttributes() {
			InitCustomAttributes(this, ref customAttributes, new GenericParamContext());
		}

		internal void InitCustomAttributes(ICorHasCustomAttribute hca, ref CustomAttributeCollection customAttributes, GenericParamContext gpContext) {
			var tokens = MDAPI.GetCustomAttributeTokens(mdi, hca.OriginalToken.Raw);
			var tmp = new CustomAttributeCollection(tokens.Length, tokens, (tokens2, index) => ReadCustomAttribute(tokens[index], gpContext));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		CustomAttribute ReadCustomAttribute(uint caToken, GenericParamContext gpContext) {
			uint typeToken;
			var caBlob = MDAPI.GetCustomAttributeBlob(mdi, caToken, out typeToken) ?? Array.Empty<byte>();
			var cat = ResolveToken(typeToken, gpContext) as ICustomAttributeType;
			var ca = CustomAttributeReader.Read(this, caBlob, cat, gpContext);
			Debug.Assert(ca != null);
			return ca;
		}

		internal void InitDeclSecurities(ICorHasDeclSecurity hds, ref ThreadSafe.IList<DeclSecurity> declSecurities) {
			var tokens = MDAPI.GetPermissionSetTokens(mdi, hds.OriginalToken.Raw);
			var tmp = new LazyList<DeclSecurity>(tokens.Length, tokens, (tokens2, index) => ResolveDeclSecurity(tokens[index]));
			Interlocked.CompareExchange(ref declSecurities, tmp, null);
		}

		internal MarshalType ReadMarshalType(ICorHasFieldMarshal hfm, GenericParamContext gpContext) {
			var mdi = MetaDataImport;
			uint token = hfm.OriginalToken.Raw;

			var data = MDAPI.GetFieldMarshalBlob(mdi, token);
			if (data == null)
				return null;
			return MarshalBlobReader.Read(this, data, gpContext);
		}

		void InitModuleProperties_NoLock() {
			CorPEKind peKind;
			var mach = MDAPI.GetModuleMachineAndPEKind(mdi2, out peKind);
			Machine = mach ?? Machine.I386;
			RuntimeVersion = CalculateRuntimeVersion();

			Kind = CalculateModuleKind();

			// This property checks RuntimeVersion which we've already initialized above
			if (IsClr1x) {
				// .NET 1.x
				Cor20HeaderRuntimeVersion = 0x00020000;
				TablesHeaderVersion = 0x0100;
			}
			else {
				// .NET 2.0 or later
				Cor20HeaderRuntimeVersion = 0x00020005;
				TablesHeaderVersion = 0x0200;
			}

			Characteristics = Characteristics.ExecutableImage;
			DllCharacteristics = DefaultDllCharacteristics;
			if (!isExeFile)
				Characteristics |= Characteristics.Dll;
			if (mach == null) {
				Cor20HeaderFlags = ComImageFlags.ILOnly;
				Characteristics |= Characteristics.LargeAddressAware;
			}
			else {
				Cor20HeaderFlags = 0;
				if ((peKind & CorPEKind.peILonly) != 0)
					Cor20HeaderFlags |= ComImageFlags.ILOnly;

				// Only one of these two bits can be set
				if ((peKind & CorPEKind.pe32BitRequired) != 0)
					Cor20HeaderFlags |= ComImageFlags._32BitRequired;
				else if ((peKind & CorPEKind.pe32BitPreferred) != 0)
					Cor20HeaderFlags |= ComImageFlags._32BitRequired | ComImageFlags._32BitPreferred;

				if (mach == Machine.AMD64 || mach == Machine.ARM64 || mach == Machine.IA64 || (peKind & CorPEKind.pe32BitRequired) == 0)
					Characteristics |= Characteristics.LargeAddressAware;
				else
					Characteristics |= Characteristics._32BitMachine;
			}

			if (Kind != ModuleKind.NetModule) {
				var pk = MDAPI.GetAssemblyPublicKey(MetaDataAssemblyImport, new MDToken(Table.Assembly, 1).Raw);
				if (!PublicKeyBase.IsNullOrEmpty2(pk))
					Cor20HeaderFlags |= ComImageFlags.StrongNameSigned;
			}

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetModuleName(mdi) ?? string.Empty);
			Mvid = MDAPI.GetModuleMvid(mdi);
			EncId = null;
			EncBaseId = null;
		}

		string CalculateRuntimeVersion() {
			if (mdi2 == null)
				return MDHeaderRuntimeVersion.MS_CLR_10;    // Could be .NET 1.0 or 1.1 but choose 1.0
			var s = MDAPI.GetModuleVersionString(mdi2);
			if (s != null)
				return s;
			return MDHeaderRuntimeVersion.MS_CLR_20;
		}

		ModuleKind CalculateModuleKind() {
			if (!isManifestModule)
				return ModuleKind.NetModule;
			if (!isExeFile)
				return ModuleKind.Dll;
			// We don't have enough info, so play it safe and assume it's a console exe
			return ModuleKind.Console;
		}

		internal byte[] ReadFieldInitialValue(CorFieldDef cfd, uint rva, int size) =>
			corModuleDefHelper.ReadFieldInitialValue(rva, cfd.OriginalToken.Raw, size);
		public TypeSig ConvertRTInternalAddress(IntPtr address) => null;

		internal MemberInfo<CorFieldDef> Register(CorFieldDef item, Action<CorFieldDef> initItem) {
			MemberInfo<CorFieldDef> info;
			if (!ridToField.TryGetValue(item.OriginalToken.Rid, out info))
				ridToField.Add(item.OriginalToken.Rid, info = new MemberInfo<CorFieldDef>(item, initItem));
			info._ItemNoInit.MustInitialize = true;
			memberNeedsReInitialization = true;
			return info;
		}

		internal MemberInfo<CorMethodDef> Register(CorMethodDef item, Action<CorMethodDef> initItem) {
			MemberInfo<CorMethodDef> info;
			if (!ridToMethod.TryGetValue(item.OriginalToken.Rid, out info))
				ridToMethod.Add(item.OriginalToken.Rid, info = new MemberInfo<CorMethodDef>(item, initItem));
			info._ItemNoInit.MustInitialize = true;
			memberNeedsReInitialization = true;
			return info;
		}

		internal MemberInfo<CorParamDef> Register(CorParamDef item, Action<CorParamDef> initItem) {
			MemberInfo<CorParamDef> info;
			if (!ridToParam.TryGetValue(item.OriginalToken.Rid, out info))
				ridToParam.Add(item.OriginalToken.Rid, info = new MemberInfo<CorParamDef>(item, initItem));
			info._ItemNoInit.MustInitialize = true;
			memberNeedsReInitialization = true;
			return info;
		}

		internal MemberInfo<CorGenericParam> Register(CorGenericParam item, Action<CorGenericParam> initItem) {
			MemberInfo<CorGenericParam> info;
			if (!ridToGenericParam.TryGetValue(item.OriginalToken.Rid, out info))
				ridToGenericParam.Add(item.OriginalToken.Rid, info = new MemberInfo<CorGenericParam>(item, initItem));
			info._ItemNoInit.MustInitialize = true;
			memberNeedsReInitialization = true;
			return info;
		}

		internal MemberInfo<CorPropertyDef> Register(CorPropertyDef item, Action<CorPropertyDef> initItem) {
			MemberInfo<CorPropertyDef> info;
			if (!ridToPropertyDef.TryGetValue(item.OriginalToken.Rid, out info))
				ridToPropertyDef.Add(item.OriginalToken.Rid, info = new MemberInfo<CorPropertyDef>(item, initItem));
			info._ItemNoInit.MustInitialize = true;
			memberNeedsReInitialization = true;
			return info;
		}

		internal MemberInfo<CorEventDef> Register(CorEventDef item, Action<CorEventDef> initItem) {
			MemberInfo<CorEventDef> info;
			if (!ridToEventDef.TryGetValue(item.OriginalToken.Rid, out info))
				ridToEventDef.Add(item.OriginalToken.Rid, info = new MemberInfo<CorEventDef>(item, initItem));
			info._ItemNoInit.MustInitialize = true;
			memberNeedsReInitialization = true;
			return info;
		}

		internal MethodBody ReadMethodBody(CorMethodDef method, uint rva, MethodAttributes attrs, MethodImplAttributes implAttrs, GenericParamContext gpContext) {
			// dynamic modules can have methods with RVA == 0 because it's relative to the .text section
			// and not really an RVA.
			if (!corModuleDefHelper.IsDynamic) {
				if (rva == 0)
					return null;
			}
			else {
				if ((attrs & MethodAttributes.Abstract) != 0 && rva == 0)
					return null;
			}
			var codeType = implAttrs & MethodImplAttributes.CodeTypeMask;
			if (codeType == MethodImplAttributes.IL)
				return ReadCilBody(method.Parameters, rva, method.OriginalToken.Raw, gpContext);
			if (codeType == MethodImplAttributes.Native)
				return new NativeMethodBody((RVA)rva);
			return null;
		}

		CilBody ReadCilBody(IList<Parameter> parameters, uint rva, uint mdToken, GenericParamContext gpContext) {
			// rva could be 0 if it's a dynamic module so we can't exit early
			var reader = corModuleDefHelper.CreateBodyReader(rva, mdToken);
			if (reader == null)
				return new CilBody();
			using (reader)
				return MethodBodyReader.CreateCilBody(this, reader, parameters, gpContext);
		}

		public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken) => ResolveTypeDefOrRef(codedToken, new GenericParamContext());

		public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
				return null;
			return ResolveTypeDefOrRefInternal(token, gpContext);
		}

		internal ITypeDefOrRef ResolveTypeDefOrRefInternal(uint token, GenericParamContext gpContext) {
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef:		return ResolveTypeDef(rid);
			case Table.TypeRef:		return ResolveTypeRef(rid);
			case Table.TypeSpec:	return ResolveTypeSpec(rid, gpContext);
			}
			return null;
		}

		internal IResolutionScope ResolveResolutionScope(uint token) {
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Module:		return ResolveModule(rid);
			case Table.ModuleRef:	return ResolveModuleRef(rid);
			case Table.AssemblyRef:	return ResolveAssemblyRef(rid);
			case Table.TypeRef:		return ResolveTypeRef(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public override IMDTokenProvider ResolveToken(uint token, GenericParamContext gpContext) {
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Module:			return ResolveModule(rid);
			case Table.TypeRef:			return ResolveTypeRef(rid);
			case Table.TypeDef:			return ResolveTypeDef(rid);
			case Table.Field:			return ResolveField(rid);
			case Table.Method:			return ResolveMethod(rid);
			case Table.Param:			return ResolveParam(rid);
			case Table.InterfaceImpl:	return ResolveInterfaceImpl(rid, gpContext);
			case Table.MemberRef:		return ResolveMemberRef(rid, gpContext);
			case Table.DeclSecurity:	return ResolveDeclSecurity(rid);
			case Table.StandAloneSig:	return ResolveStandAloneSig(rid, gpContext);
			case Table.Event:			return ResolveEvent(rid);
			case Table.Property:		return ResolveProperty(rid);
			case Table.ModuleRef:		return ResolveModuleRef(rid);
			case Table.TypeSpec:		return ResolveTypeSpec(rid, gpContext);
			case Table.Assembly:		return ResolveAssembly(rid);
			case Table.AssemblyRef:		return ResolveAssemblyRef(rid);
			case Table.File:			return ResolveFile(rid);
			case Table.ExportedType:	return ResolveExportedType(rid);
			case Table.ManifestResource:return ResolveManifestResource(rid);
			case Table.GenericParam:	return ResolveGenericParam(rid);
			case Table.MethodSpec:		return ResolveMethodSpec(rid, gpContext);
			case Table.GenericParamConstraint: return ResolveGenericParamConstraint(rid, gpContext);
			}
			return null;
		}

		ModuleDef ResolveModule(uint rid) {
			if (rid == 1)
				return this;
			return null;
		}

		TypeRef ResolveTypeRef(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.TypeRef, rid))
				return null;
			CorTypeRef ctr;
			if (ridToTypeRef.TryGetValue(rid, out ctr))
				return ctr;
			if (DisableMDAPICalls)
				return null;
			ctr = new CorTypeRef(this, rid);
			ridToTypeRef.Add(rid, ctr);
			return ctr;
		}

		internal TypeDef ResolveTypeDef(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.TypeDef, rid))
				return null;
			bool created;
			return InitializeTypeDef(rid, false, out created);
		}

		FieldDef ResolveField(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.Field, rid))
				return null;
			MemberInfo<CorFieldDef> info;
			if (ridToField.TryGetValue(rid, out info))
				return info.Item;
			if (DisableMDAPICalls)
				return null;
			uint ownerRid = MDAPI.GetFieldOwnerRid(mdi, new MDToken(Table.Field, rid).Raw);
			((CorTypeDef)ResolveTypeDef(ownerRid))?.UpdateFields();
			ridToField.TryGetValue(rid, out info);
			return info?.Item;
		}

		MethodDef ResolveMethod(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.Method, rid))
				return null;
			MemberInfo<CorMethodDef> info;
			if (ridToMethod.TryGetValue(rid, out info))
				return info.Item;
			if (DisableMDAPICalls)
				return null;
			uint ownerRid = MDAPI.GetMethodOwnerRid(mdi, new MDToken(Table.Method, rid).Raw);
			((CorTypeDef)ResolveTypeDef(ownerRid))?.UpdateMethods();
			ridToMethod.TryGetValue(rid, out info);
			return info?.Item;
		}

		ParamDef ResolveParam(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.Param, rid))
				return null;
			MemberInfo<CorParamDef> info;
			if (ridToParam.TryGetValue(rid, out info))
				return info.Item;
			if (DisableMDAPICalls)
				return null;
			uint ownerRid = MDAPI.GetParamOwnerRid(mdi, new MDToken(Table.Param, rid).Raw);
			((CorMethodDef)ResolveMethod(ownerRid))?.UpdateParams();
			ridToParam.TryGetValue(rid, out info);
			return info?.Item;
		}

		InterfaceImpl ResolveInterfaceImpl(uint rid) => ResolveInterfaceImpl(rid, new GenericParamContext());

		internal InterfaceImpl ResolveInterfaceImpl(uint rid, GenericParamContext gpContext) {
			if (!DisableMDAPICalls && !IsValidToken(Table.InterfaceImpl, rid))
				return null;

			CorInterfaceImpl cii;

			if (gpContext.IsEmpty) {
				if (ridToInterfaceImplNoCtx.TryGetValue(rid, out cii))
					return cii; // could be null, but don't try to create a new instance. Must return null if ctx is empty
				if (DisableMDAPICalls)
					return null;
				ridToInterfaceImplNoCtx.Add(rid, null);
				cii = new CorInterfaceImpl(this, rid, gpContext);
				ridToInterfaceImplNoCtx[rid] = cii;
				return cii;
			}

			if (ridToInterfaceImpl.TryGetValue(rid, out cii)) {
				if (cii != null)
					return cii;
				if (DisableMDAPICalls)
					return null;
				// If it's null, it contains generic parameters so need to be re-init'd with
				// a new gpContext OR this method got called recursively (ContainsGenericParameter
				// property call below) and the same rid was referenced again.
				return new CorInterfaceImpl(this, rid, gpContext);
			}
			if (DisableMDAPICalls)
				return null;
			ridToInterfaceImpl.Add(rid, null);
			cii = new CorInterfaceImpl(this, rid, gpContext);
			if (!((IContainsGenericParameter)cii).ContainsGenericParameter)
				ridToInterfaceImpl[rid] = cii;
			return cii;
		}

		MemberRef ResolveMemberRef(uint rid) => ResolveMemberRef(rid, new GenericParamContext());

		MemberRef ResolveMemberRef(uint rid, GenericParamContext gpContext) {
			if (!DisableMDAPICalls && !IsValidToken(Table.MemberRef, rid))
				return null;
			CorMemberRef cmr;

			if (gpContext.IsEmpty) {
				if (ridToMemberRefNoCtx.TryGetValue(rid, out cmr))
					return cmr; // could be null, but don't try to create a new instance. Must return null if ctx is empty
				if (DisableMDAPICalls)
					return null;
				ridToMemberRefNoCtx.Add(rid, null);
				cmr = new CorMemberRef(this, rid, gpContext);
				ridToMemberRefNoCtx[rid] = cmr;
				return cmr;
			}

			if (ridToMemberRef.TryGetValue(rid, out cmr)) {
				if (cmr != null)
					return cmr;
				if (DisableMDAPICalls)
					return null;
				// If it's null, it contains generic parameters so need to be re-init'd with
				// a new gpContext OR this method got called recursively (ContainsGenericParameter
				// property call below) and the same rid was referenced again.
				return new CorMemberRef(this, rid, gpContext);
			}
			if (DisableMDAPICalls)
				return null;
			ridToMemberRef.Add(rid, null);
			cmr = new CorMemberRef(this, rid, gpContext);
			if (!((IContainsGenericParameter)cmr).ContainsGenericParameter)
				ridToMemberRef[rid] = cmr;
			return cmr;
		}

		DeclSecurity ResolveDeclSecurity(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.DeclSecurity, rid))
				return null;
			CorDeclSecurity cds;
			if (ridToDeclSecurity.TryGetValue(rid, out cds))
				return cds;
			if (DisableMDAPICalls)
				return null;
			cds = new CorDeclSecurity(this, rid);
			ridToDeclSecurity.Add(rid, cds);
			return cds;
		}

		StandAloneSig ResolveStandAloneSig(uint rid) => ResolveStandAloneSig(rid, new GenericParamContext());

		StandAloneSig ResolveStandAloneSig(uint rid, GenericParamContext gpContext) {
			if (!DisableMDAPICalls && !IsValidToken(Table.StandAloneSig, rid))
				return null;
			CorStandAloneSig cts;

			if (gpContext.IsEmpty) {
				if (ridToStandAloneSigNoCtx.TryGetValue(rid, out cts))
					return cts; // could be null, but don't try to create a new instance. Must return null if ctx is empty
				if (DisableMDAPICalls)
					return null;
				ridToStandAloneSigNoCtx.Add(rid, null);
				cts = new CorStandAloneSig(this, rid, gpContext);
				ridToStandAloneSigNoCtx[rid] = cts;
				return cts;
			}

			if (ridToStandAloneSig.TryGetValue(rid, out cts)) {
				if (cts != null)
					return cts;
				if (DisableMDAPICalls)
					return null;
				// If it's null, it contains generic parameters so need to be re-init'd with
				// a new gpContext OR this method got called recursively (ContainsGenericParameter
				// property call below) and the same rid was referenced again.
				return new CorStandAloneSig(this, rid, gpContext);
			}
			if (DisableMDAPICalls)
				return null;
			ridToStandAloneSig.Add(rid, null);
			cts = new CorStandAloneSig(this, rid, gpContext);
			if (!cts.ContainsGenericParameter)
				ridToStandAloneSig[rid] = cts;
			return cts;
		}

		EventDef ResolveEvent(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.Event, rid))
				return null;
			MemberInfo<CorEventDef> info;
			if (ridToEventDef.TryGetValue(rid, out info))
				return info.Item;
			if (DisableMDAPICalls)
				return null;
			uint ownerRid = MDAPI.GetEventOwnerRid(mdi, new MDToken(Table.Event, rid).Raw);
			((CorTypeDef)ResolveTypeDef(ownerRid))?.UpdateProperties();
			ridToEventDef.TryGetValue(rid, out info);
			return info?.Item;
		}

		PropertyDef ResolveProperty(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.Property, rid))
				return null;
			MemberInfo<CorPropertyDef> info;
			if (ridToPropertyDef.TryGetValue(rid, out info))
				return info.Item;
			if (DisableMDAPICalls)
				return null;
			uint ownerRid = MDAPI.GetPropertyOwnerRid(mdi, new MDToken(Table.Property, rid).Raw);
			((CorTypeDef)ResolveTypeDef(ownerRid))?.UpdateProperties();
			ridToPropertyDef.TryGetValue(rid, out info);
			return info?.Item;
		}

		ModuleRef ResolveModuleRef(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.ModuleRef, rid))
				return null;
			CorModuleRef cmr;
			if (ridToModuleRef.TryGetValue(rid, out cmr))
				return cmr;
			if (DisableMDAPICalls)
				return null;
			cmr = new CorModuleRef(this, rid);
			ridToModuleRef.Add(rid, cmr);
			return cmr;
		}

		TypeSpec ResolveTypeSpec(uint rid) => ResolveTypeSpec(rid, new GenericParamContext());

		TypeSpec ResolveTypeSpec(uint rid, GenericParamContext gpContext) {
			if (!DisableMDAPICalls && !IsValidToken(Table.TypeSpec, rid))
				return null;
			CorTypeSpec cts;

			if (gpContext.IsEmpty) {
				if (ridToTypeSpecNoCtx.TryGetValue(rid, out cts))
					return cts; // could be null, but don't try to create a new instance. Must return null if ctx is empty
				if (DisableMDAPICalls)
					return null;
				ridToTypeSpecNoCtx.Add(rid, null);
				cts = new CorTypeSpec(this, rid, gpContext);
				ridToTypeSpecNoCtx[rid] = cts;
				return cts;
			}

			if (ridToTypeSpec.TryGetValue(rid, out cts)) {
				if (cts != null)
					return cts;
				if (DisableMDAPICalls)
					return null;
				// If it's null, it contains generic parameters so need to be re-init'd with
				// a new gpContext OR this method got called recursively (ContainsGenericParameter
				// property call below) and the same rid was referenced again.
				return new CorTypeSpec(this, rid, gpContext);
			}
			if (DisableMDAPICalls)
				return null;
			ridToTypeSpec.Add(rid, null);
			cts = new CorTypeSpec(this, rid, gpContext);
			if (!cts.ContainsGenericParameter)
				ridToTypeSpec[rid] = cts;
			return cts;
		}

		AssemblyDef ResolveAssembly(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.Assembly, rid))
				return null;
			if (rid == 1 && corModuleDefHelper.IsManifestModule)
				return Assembly;
			return null;
		}

		AssemblyRef ResolveAssemblyRef(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.AssemblyRef, rid))
				return null;
			CorAssemblyRef car;
			if (ridToAssemblyRef.TryGetValue(rid, out car))
				return car;
			if (DisableMDAPICalls)
				return null;
			car = new CorAssemblyRef(this, rid);
			ridToAssemblyRef.Add(rid, car);
			return car;
		}

		FileDef ResolveFile(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.File, rid))
				return null;
			CorFileDef cfd;
			if (ridToFileDef.TryGetValue(rid, out cfd))
				return cfd;
			if (DisableMDAPICalls)
				return null;
			cfd = new CorFileDef(this, rid);
			ridToFileDef.Add(rid, cfd);
			return cfd;
		}

		ExportedType ResolveExportedType(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.ExportedType, rid))
				return null;
			CorExportedType cet;
			if (ridToExportedType.TryGetValue(rid, out cet))
				return cet;
			if (DisableMDAPICalls)
				return null;
			cet = new CorExportedType(this, rid);
			ridToExportedType.Add(rid, cet);
			return cet;
		}

		ManifestResource ResolveManifestResource(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.ManifestResource, rid))
				return null;
			CorManifestResource cmr;
			if (ridToManifestResource.TryGetValue(rid, out cmr))
				return cmr;
			if (DisableMDAPICalls)
				return null;
			cmr = new CorManifestResource(this, rid);
			ridToManifestResource.Add(rid, cmr);
			return cmr;
		}

		internal GenericParam ResolveGenericParam(uint rid) {
			if (!DisableMDAPICalls && !IsValidToken(Table.GenericParam, rid))
				return null;
			MemberInfo<CorGenericParam> info;
			if (ridToGenericParam.TryGetValue(rid, out info))
				return info.Item;
			if (DisableMDAPICalls)
				return null;
			uint ownerToken = MDAPI.GetGenericParamOwner(mdi2, new MDToken(Table.Param, rid).Raw);
			((ICorTypeOrMethodDef)ResolveToken(ownerToken))?.UpdateGenericParams();
			ridToGenericParam.TryGetValue(rid, out info);
			return info?.Item;
		}

		MethodSpec ResolveMethodSpec(uint rid) => ResolveMethodSpec(rid, new GenericParamContext());

		MethodSpec ResolveMethodSpec(uint rid, GenericParamContext gpContext) {
			if (!DisableMDAPICalls && !IsValidToken(Table.MethodSpec, rid))
				return null;

			if (gpContext.IsEmpty) {
				CorMethodSpec cms;
				if (ridToMethodSpecNoCtx.TryGetValue(rid, out cms))
					return cms; // could be null, but don't try to create a new instance. Must return null if ctx is empty
				if (DisableMDAPICalls)
					return null;
				ridToMethodSpecNoCtx.Add(rid, null);
				cms = new CorMethodSpec(this, rid, gpContext);
				ridToMethodSpecNoCtx[rid] = cms;
				return cms;
			}
			if (DisableMDAPICalls)
				return null;

			// No need to cache it since it contains generic parameters
			return new CorMethodSpec(this, rid, gpContext);
		}

		GenericParamConstraint ResolveGenericParamConstraint(uint rid) => ResolveGenericParamConstraint(rid, new GenericParamContext());

		GenericParamConstraint ResolveGenericParamConstraint(uint rid, GenericParamContext gpContext) {
			if (!DisableMDAPICalls && !IsValidToken(Table.GenericParamConstraint, rid))
				return null;

			if (gpContext.IsEmpty) {
				CorGenericParamConstraint cgpc;
				if (ridToGenericParamConstraintNoCtx.TryGetValue(rid, out cgpc))
					return cgpc; // could be null, but don't try to create a new instance. Must return null if ctx is empty
				if (DisableMDAPICalls)
					return null;
				ridToGenericParamConstraintNoCtx.Add(rid, null);
				cgpc = new CorGenericParamConstraint(this, rid, gpContext);
				ridToGenericParamConstraintNoCtx[rid] = cgpc;
				return cgpc;
			}
			if (DisableMDAPICalls)
				return null;

			return new CorGenericParamConstraint(this, rid, gpContext);
		}

		internal GenericParamConstraint ResolveGenericParamConstraintDontCache(uint rid, GenericParamContext gpContext) {
			if (!IsValidToken(Table.GenericParamConstraint, rid))
				return null;

			// Don't cache it. Will cause recursive init of the CorGenericParam owner when it
			// Clear()'s its list.
			return new CorGenericParamConstraint(this, rid, gpContext);
		}

		public string ReadUserString(uint token) {
			if (!IsValidToken(token))
				return null;
			if (DisableMDAPICalls)
				return null;
			return MDAPI.GetUserString(mdi, token);
		}

		protected override void InitializeTypes() {
			var list = GetNonNestedClassRids();
			var tmp = new LazyList<TypeDef>(list.Length, this, list, (list2, index) => ResolveTypeDef(((uint[])list2)[index]));
			Interlocked.CompareExchange(ref types, tmp, null);
		}

		uint[] GetNonNestedClassRids() {
			InitializeTypeTables();
			var list = new List<uint>(ridToEnclosing.Count);
			foreach (var kv in ridToEnclosing) {
				if (kv.Value == 0)
					list.Add(kv.Key);
			}
			return list.ToArray();
		}

		internal uint[] GetTypeDefNestedClassRids(CorTypeDef ctd) {
			Debug.Assert(!nestedListInitd.Contains(ctd.OriginalToken.Rid));
			nestedListInitd.Add(ctd.OriginalToken.Rid);
			List<uint> list;
			bool b = ridToNested.TryGetValue(ctd.OriginalToken.Rid, out list);
			Debug.Assert(b);
			return list == null ? Array.Empty<uint>() : list.ToArray();
		}

		void InitializeTypeTables() {
			if (ridToNested != null)
				return;

			var allTypes = MDAPI.GetTypeDefTokens(mdi);
			int capacity = allTypes.Length < 100 ? 100 : allTypes.Length;
			ridToNested = new Dictionary<uint, List<uint>>(capacity);
			ridToEnclosing = new Dictionary<uint, uint>(capacity);
			nestedListInitd = new HashSet<uint>();
			UpdateTypeTables(allTypes);
		}

		void UpdateTypeTables(uint[] tokens) {
			Array.Sort(tokens);
			foreach (uint token in tokens) {
				uint rid = token & 0x00FFFFFF;
				Debug.Assert(rid != 0);
				Debug.Assert(!ridToNested.ContainsKey(rid));

				var enclTypeToken = new MDToken(MDAPI.GetTypeDefEnclosingType(mdi, token));
				if (enclTypeToken.Rid != 0 && !IsValidToken(enclTypeToken.Raw)) {
					// Here if it's an obfuscated assembly with invalid MD
					enclTypeToken = new MDToken(Table.TypeDef, 0);
				}
				var enclTypeRid = enclTypeToken.Rid;
				List<uint> enclTypeList;
				if (enclTypeRid == 0) {
				} // All nested types must be after their enclosing type
				else if (!ridToNested.TryGetValue(enclTypeRid, out enclTypeList)) {
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

		void UpdateTypeTables(CorTypeDef type) {
			if (ridToEnclosing == null || ridToEnclosing.ContainsKey(type.OriginalToken.Rid))
				return;

			var tokens = GetNewTokens(type.OriginalToken.Rid);
			UpdateTypeTables(tokens);

			foreach (var token in tokens) {
				bool b;
				CorTypeDef td;
				uint rid = token & 0x00FFFFFF;
				if (token == type.OriginalToken.Raw) {
					td = type;
				}
				else {
					b = ridToType.ContainsKey(rid);
					Debug.Assert(!b);
					if (b)
						continue;
					const bool created = true;
					const bool calledFromLoadClass = false;
					td = new CorTypeDef(this, rid);
					ridToType.Add(rid, td);
					Initialize(td, created, calledFromLoadClass);
				}

				uint enclTypeRid;
				b = ridToEnclosing.TryGetValue(rid, out enclTypeRid);
				Debug.Assert(b);
				if (enclTypeRid != 0) {
					if (nestedListInitd.Contains(enclTypeRid)) {
						td.PrepareAutoInsert();
						var enclType = ridToType[enclTypeRid];
						enclType.NestedTypes.Add(td);
						if (corModuleDefHelper.IsDynamic) {
							TypeUpdated?.Invoke(this, new TypeUpdatedEventArgs(enclType, false, false));
						}
					}
				}
				else {
					td.PrepareAutoInsert();
					Types.Add(td);
				}
			}
		}

		uint[] GetNewTokens(uint rid) {
			var hash = new HashSet<uint>();
			for (;;) {
				if (ridToEnclosing.ContainsKey(rid))
					break;
				if (rid == 0 || hash.Contains(rid))
					break;
				hash.Add(rid);
				rid = MDAPI.GetTypeDefEnclosingType(mdi, new MDToken(Table.TypeDef, rid).Raw) & 0x00FFFFFF;
			}
			var tokens = new uint[hash.Count];
			int i = 0;
			foreach (uint rid2 in hash)
				tokens[i++] = new MDToken(Table.TypeDef, rid2).Raw;
			return tokens;
		}

		internal TypeDef GetEnclosingTypeDef(CorTypeDef ctd) {
			InitializeTypeTables();
			uint enclTypeRid;
			bool b = ridToEnclosing.TryGetValue(ctd.OriginalToken.Rid, out enclTypeRid);
			Debug.Assert(b);
			return enclTypeRid == 0 ? null : ResolveTypeDef(enclTypeRid);
		}

		protected override void InitializeExportedTypes() {
			var list = MDAPI.GetExportedTypeRids(MetaDataAssemblyImport);
			var tmp = new LazyList<ExportedType>(list.Length, list, (list2, i) => ResolveExportedType(((uint[])list2)[i]));
			Interlocked.CompareExchange(ref exportedTypes, tmp, null);
			lastExportedTypeRidInList = list.Length == 0 ? 0 : list.Max();
		}

		/// <summary>
		/// Update <see cref="ExportedTypes"/> with new <see cref="ExportedType"/>s that have been
		/// added to the dynamic assembly. Returns true if at least one new <see cref="ExportedType"/>
		/// was added to the list.
		/// </summary>
		/// <returns></returns>
		public bool UpdateExportedTypes() {
			if (!corModuleDefHelper.IsDynamic)
				return false;
			if (exportedTypes == null)
				return false;

			bool addedToList = false;
			for (uint rid = lastExportedTypeRidInList + 1; ; rid++) {
				if (!IsValidToken(new MDToken(Table.ExportedType, rid).Raw))
					break;
				var et = ResolveExportedType(rid);
				Debug.Assert(et != null);
				if (et == null)
					break;
				ExportedTypes.Add(et);
				lastExportedTypeRidInList = rid;
				addedToList = true;
			}
			return addedToList;
		}

		protected override void InitializeResources() {
			var list = MDAPI.GetManifestResourceRids(MetaDataAssemblyImport);
			var tmp = new ResourceCollection(list.Length, null, (ctx, i) => CreateResource(i + 1));
			Interlocked.CompareExchange(ref resources, tmp, null);
			lastManifestResourceRidInList = list.Length == 0 ? 0 : list.Max();
		}

		/// <summary>
		/// Update <see cref="Resources"/> with new <see cref="Resource"/>s that have been
		/// added to the dynamic assembly. Returns true if at least one new <see cref="Resource"/>
		/// was added to the list.
		/// </summary>
		/// <returns></returns>
		public bool UpdateResources() {
			if (!corModuleDefHelper.IsDynamic)
				return false;
			if (resources == null)
				return false;

			bool addedToList = false;
			for (uint rid = lastManifestResourceRidInList + 1; ; rid++) {
				if (!IsValidToken(new MDToken(Table.ManifestResource, rid).Raw))
					break;
				var resource = CreateResource(rid);
				Resources.Add(resource);
				lastManifestResourceRidInList = rid;
				addedToList = true;
			}
			return addedToList;
		}

		Resource CreateResource(uint rid) {
			uint? implementationToken = MDAPI.GetManifestResourceImplementationToken(mdi as IMetaDataAssemblyImport, new MDToken(Table.ManifestResource, rid).Raw);
			if (implementationToken == null)
				return new EmbeddedResource(UTF8String.Empty, MemoryImageStream.CreateEmpty(), 0) { Rid = rid };
			var token = new MDToken(implementationToken.Value);

			var mr = ResolveManifestResource(rid);
			if (mr == null)
				return new EmbeddedResource(UTF8String.Empty, MemoryImageStream.CreateEmpty(), 0) { Rid = rid };

			if (token.Rid == 0)
				return new EmbeddedResource(mr.Name, CreateResourceStream(mr.Offset), mr.Flags) { Rid = rid, Offset = mr.Offset };

			var file = mr.Implementation as FileDef;
			if (file != null)
				return new LinkedResource(mr.Name, file, mr.Flags) { Rid = rid, Offset = mr.Offset };

			var asmRef = mr.Implementation as AssemblyRef;
			if (asmRef != null)
				return new AssemblyLinkedResource(mr.Name, asmRef, mr.Flags) { Rid = rid, Offset = mr.Offset };

			return new EmbeddedResource(mr.Name, MemoryImageStream.CreateEmpty(), mr.Flags) { Rid = rid, Offset = mr.Offset };
		}

		IImageStream CreateResourceStream(uint offset) => corModuleDefHelper.CreateResourceStream(offset) ?? MemoryImageStream.CreateEmpty();

		/// <summary>
		/// Add all new types that have been added to the module. Returns true if at least one new
		/// type was discovered.
		/// </summary>
		/// <returns></returns>
		public bool UpdateTypes() {
			bool added = false;
			if (!corModuleDefHelper.IsDynamic)
				return added;

			for (uint rid = lastTriedTypeRid + 1; ; rid++) {
				if (!IsValidToken(new MDToken(Table.TypeDef, rid).Raw))
					break;
				bool created;
				InitializeTypeDef(rid, false, out created);
				lastTriedTypeRid = rid;
				added |= created;
			}

			return added;
		}

		/// <summary>
		/// Add any new types, resources, etc that have been added to the module. Returns true if
		/// something new was found.
		/// </summary>
		/// <returns></returns>
		public bool UpdateAll() {
			bool b = false;
			b |= UpdateExportedTypes();
			b |= UpdateResources();
			b |= UpdateTypes();
			return b;
		}

		/// <summary>
		/// Gets owner token of a field
		/// </summary>
		/// <param name="rid">RID of field</param>
		/// <returns></returns>
		public MDToken GetFieldOwnerToken(uint rid) => new MDToken(Table.TypeDef, MDAPI.GetFieldOwnerRid(mdi, new MDToken(Table.Field, rid).Raw));

		/// <summary>
		/// Gets owner token of a method
		/// </summary>
		/// <param name="rid">RID of method</param>
		/// <returns></returns>
		public MDToken GetMethodOwnerToken(uint rid) => new MDToken(Table.TypeDef, MDAPI.GetMethodOwnerRid(mdi, new MDToken(Table.Method, rid).Raw));

		/// <summary>
		/// Gets owner token of a parameter
		/// </summary>
		/// <param name="rid">RID of parameter</param>
		/// <returns></returns>
		public MDToken GetParamOwnerToken(uint rid) => new MDToken(Table.Method, MDAPI.GetParamOwnerRid(mdi, new MDToken(Table.Param, rid).Raw));

		/// <summary>
		/// Gets owner token of an event
		/// </summary>
		/// <param name="rid">RID of event</param>
		/// <returns></returns>
		public MDToken GetEventOwnerToken(uint rid) => new MDToken(Table.TypeDef, MDAPI.GetEventOwnerRid(mdi, new MDToken(Table.Event, rid).Raw));

		/// <summary>
		/// Gets owner token of a property
		/// </summary>
		/// <param name="rid">RID of property</param>
		/// <returns></returns>
		public MDToken GetPropertyOwnerToken(uint rid) => new MDToken(Table.TypeDef, MDAPI.GetPropertyOwnerRid(mdi, new MDToken(Table.Property, rid).Raw));

		/// <summary>
		/// Gets owner token of a generic parameter
		/// </summary>
		/// <param name="rid">RID of generic parameter</param>
		/// <returns></returns>
		public MDToken GetGenericParamOwnerToken(uint rid) => new MDToken(MDAPI.GetGenericParamOwner(mdi2, new MDToken(Table.GenericParam, rid).Raw));

		/// <summary>
		/// Gets owner token of a generic parameter constraint
		/// </summary>
		/// <param name="rid">RID of generic parameter constraint</param>
		/// <returns></returns>
		public MDToken GetGenericParamConstraintOwnerToken(uint rid) => new MDToken(Table.GenericParam, MDAPI.GetGenericParamConstraintOwnerRid(mdi2, new MDToken(Table.GenericParamConstraint, rid).Raw));

		/// <summary>
		/// Force initialization of a type. Can be called if the type has gotten new members but
		/// the type hasn't been loaded yet.
		/// </summary>
		/// <param name="rid">RID of type</param>
		public void ForceInitializeTypeDef(uint rid) {
			const bool calledFromLoadClass = false;
			bool created;
			var ctd = InitializeTypeDef(rid, calledFromLoadClass, out created);
			// If it was created, Initialize() has already been called
			if (!created && ctd != null && !ctd.CompletelyLoaded)
				Initialize(ctd, created, calledFromLoadClass);
		}

		public override void LoadEverything(ICancellationToken cancellationToken) {
			// We sometimes need to execute this a couple of times because fields, methods, etc
			// could be re-initialized due to eg. UpdateFields()/etc having been called.
			int counter = 0;
			do {
				memberNeedsReInitialization = false;
				counter++;
				bool b = counter <= 20;
				Debug.Assert(b);
				if (!b)
					break;
				base.LoadEverything(cancellationToken);
			} while (memberNeedsReInitialization);
		}
		bool memberNeedsReInitialization;
	}
}
