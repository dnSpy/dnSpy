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
using System.Runtime.InteropServices;
using System.Security;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
#pragma warning disable CS0649
namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	struct COR_FIELD_OFFSET {
		public uint FieldToken;
		public uint Offset;
	}
	struct ASSEMBLYMETADATA {
		public ushort usMajorVersion;
		public ushort usMinorVersion;
		public ushort usBuildNumber;
		public ushort usRevisionNumber;
		public IntPtr szLocale;
		public uint cbLocale;
		public IntPtr rProcessor;
		public uint ulProcessor;
		public IntPtr rOS;
		public uint ulOS;
	}
	[Guid("EE62470B-E94B-424e-9B7C-2F00C9249F93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
	[SuppressUnmanagedCodeSecurity]
	interface IMetaDataAssemblyImport {
		[PreserveSig]
		int GetAssemblyProps(uint mda, IntPtr ppbPublicKey, IntPtr pcbPublicKey, IntPtr pulHashAlgId, IntPtr szName, uint cchName, IntPtr pchName, IntPtr pMetaData, IntPtr pdwAssemblyFlags);
		[PreserveSig]
		int GetAssemblyRefProps(uint mdar, IntPtr ppbPublicKeyOrToken, IntPtr pcbPublicKeyOrToken, IntPtr szName, uint cchName, IntPtr pchName, IntPtr pMetaData, IntPtr ppbHashValue, IntPtr pcbHashValue, IntPtr pdwAssemblyRefFlags);
		[PreserveSig]
		int GetFileProps(uint mdf, IntPtr szName, uint cchName, IntPtr pchName, IntPtr ppbHashValue, IntPtr pcbHashValue, IntPtr pdwFileFlags);
		[PreserveSig]
		int GetExportedTypeProps(uint mdct, IntPtr szName, uint cchName, IntPtr pchName, IntPtr ptkImplementation, IntPtr ptkTypeDef, IntPtr pdwExportedTypeFlags);
		[PreserveSig]
		int GetManifestResourceProps(uint mdmr, IntPtr szName, uint cchName, IntPtr pchName, IntPtr ptkImplementation, IntPtr pdwOffset, IntPtr pdwResourceFlags);
		[PreserveSig]
		int EnumAssemblyRefs(ref IntPtr phEnum, IntPtr rAssemblyRefs, uint cMax, out uint pcTokens);
		[PreserveSig]
		int EnumFiles(ref IntPtr phEnum, IntPtr rFiles, uint cMax, out uint pcTokens);
		[PreserveSig]
		int EnumExportedTypes(ref IntPtr phEnum, IntPtr rExportedTypes, uint cMax, out uint pcTokens);
		[PreserveSig]
		int EnumManifestResources(ref IntPtr phEnum, IntPtr rManifestResources, uint cMax, out uint pcTokens);
		[PreserveSig]
		int GetAssemblyFromScope(IntPtr ptkAssembly);
		[PreserveSig]
		int FindExportedTypeByName([MarshalAs(UnmanagedType.LPWStr)] string szName, uint mdtExportedType, IntPtr ptkExportedType);
		[PreserveSig]
		int FindManifestResourceByName([MarshalAs(UnmanagedType.LPWStr)] string szName, IntPtr ptkManifestResource);
		[PreserveSig]
		void CloseEnum(IntPtr hEnum);
		[PreserveSig]
		int FindAssembliesByName([MarshalAs(UnmanagedType.LPWStr)] string szAppBase, [MarshalAs(UnmanagedType.LPWStr)] string szPrivateBin, [MarshalAs(UnmanagedType.LPWStr)] string szAssemblyName, IntPtr ppIUnk, uint cMax, IntPtr pcAssemblies);
	}
	[Guid("FCE5EFA0-8BBA-4f8e-A036-8F2022B08466"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
	[SuppressUnmanagedCodeSecurity]
	interface IMetaDataImport2 {
		[PreserveSig]
		int CloseEnum(IntPtr hEnum);
		[PreserveSig]
		int CountEnum(IntPtr hEnum, ref uint pulCount);
		[PreserveSig]
		int ResetEnum(IntPtr hEnum, uint ulPos);
		[PreserveSig]
		int EnumTypeDefs(ref IntPtr phEnum, IntPtr rTypeDefs, uint cMax, out uint pcTypeDefs);
		[PreserveSig]
		int EnumInterfaceImpls(ref IntPtr phEnum, uint td, IntPtr rImpls, uint cMax, out uint pcImpls);
		void EnumTypeRefs(ref IntPtr phEnum, uint[] rTypeRefs, uint cMax, ref uint pcTypeRefs);
		void FindTypeDefByName([In, MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] uint tkEnclosingClass, [Out] out uint ptd);
		[PreserveSig]
		int GetScopeProps([Out] IntPtr szName, [In] uint cchName, IntPtr pchName, IntPtr pmvid);
		void GetModuleFromScope([Out] out uint pmd);
		[PreserveSig]
		int GetTypeDefProps([In] uint td, [In] IntPtr szTypeDef, [In] uint cchTypeDef, IntPtr pchTypeDef, IntPtr pdwTypeDefFlags, IntPtr ptkExtends);
		[PreserveSig]
		int GetInterfaceImplProps(uint iiImpl, IntPtr pClass, IntPtr ptkIface);
		[PreserveSig]
		int GetTypeRefProps([In] uint tr, IntPtr ptkResolutionScope, IntPtr szName, uint cchName, IntPtr pchName);
		void ResolveTypeRef(uint tr, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppIScope, out uint ptd);
		void EnumMembers([In, Out] ref IntPtr phEnum, [In] uint cl, [Out] uint[] rMembers, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMembersWithName([In, Out] ref IntPtr phEnum, [In] uint cl, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] uint[] rMembers, [In] uint cMax, [Out] out uint pcTokens);
		[PreserveSig]
		int EnumMethods([In, Out] ref IntPtr phEnum, [In] uint cl, [Out] IntPtr rMethods, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMethodsWithName([In, Out] ref IntPtr phEnum, [In] uint cl, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		[PreserveSig]
		int EnumFields([In, Out] ref IntPtr phEnum, [In] uint cl, [Out] IntPtr rFields, [In] uint cMax, [Out] out uint pcTokens);
		void EnumFieldsWithName([In, Out] ref IntPtr phEnum, [In] uint cl, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] uint[] rFields, [In] uint cMax, [Out] out uint pcTokens);
		[PreserveSig]
		int EnumParams([In, Out] ref IntPtr phEnum, [In] uint mb, [Out] IntPtr rParams, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMemberRefs([In, Out] ref IntPtr phEnum, [In] uint tkParent, [Out] uint[] rMemberRefs, [In] uint cMax, [Out] out uint pcTokens);
		[PreserveSig]
		int EnumMethodImpls(ref IntPtr phEnum, uint td, IntPtr rMethodBody, IntPtr rMethodDecl, uint cMax, out uint pcTokens);
		[PreserveSig]
		int EnumPermissionSets(ref IntPtr phEnum, uint tk, uint dwActions, IntPtr rPermission, uint cMax, out uint pcTokens);
		void FindMember([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		[PreserveSig]
		int FindMethod([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindField([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindMemberRef([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmr);
		[PreserveSig]
		int GetMethodProps(uint mb, IntPtr pClass, [In] IntPtr szMethod, uint cchMethod, IntPtr pchMethod, IntPtr pdwAttr, IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA, IntPtr pdwImplFlags);
		[PreserveSig]
		int GetMemberRefProps(uint mr, IntPtr ptk, IntPtr szMember, uint cchMember, IntPtr pchMember, IntPtr ppvSigBlob, IntPtr pbSig);
		[PreserveSig]
		int EnumProperties([In, Out] ref IntPtr phEnum, [In] uint td, [Out] IntPtr rProperties, [In] uint cMax, [Out] out uint pcProperties);
		[PreserveSig]
		int EnumEvents(ref IntPtr phEnum, uint td, IntPtr rEvents, uint cMax, out uint pcEvents);
		[PreserveSig]
		int GetEventProps(uint ev, IntPtr pClass, IntPtr szEvent, uint cchEvent, IntPtr pchEvent, IntPtr pdwEventFlags, IntPtr ptkEventType, IntPtr pmdAddOn, IntPtr pmdRemoveOn, IntPtr pmdFire, IntPtr rmdOtherMethod, uint cMax, IntPtr pcOtherMethod);
		[PreserveSig]
		int EnumMethodSemantics(ref IntPtr phEnum, uint mb, IntPtr rEventProp, uint cMax, out uint pcEventProp);
		[PreserveSig]
		int GetMethodSemantics([In] uint mb, [In] uint tkEventProp, [Out] out uint pdwSemanticsFlags);
		[PreserveSig]
		int GetClassLayout([In] uint td, IntPtr pdwPackSize, [Out] [MarshalAs(UnmanagedType.LPArray)] COR_FIELD_OFFSET[] rFieldOffset, [In] int cMax, IntPtr pcFieldOffset, IntPtr pulClassSize);
		[PreserveSig]
		int GetFieldMarshal([In] uint tk, [Out] out IntPtr ppvNativeType, [Out] out uint pcbNativeType);
		[PreserveSig]
		int GetRVA(uint tk, out uint pulCodeRVA, IntPtr pdwImplFlags);
		[PreserveSig]
		int GetPermissionSetProps([In] uint pm, IntPtr pdwAction, IntPtr ppvPermission, IntPtr pcbPermission);
		[PreserveSig]
		int GetSigFromToken([In] uint mdSig, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		[PreserveSig]
		int GetModuleRefProps([In] uint mur, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
		void EnumModuleRefs([In, Out] ref IntPtr phEnum, [Out] uint[] rModuleRefs, [In] uint cmax, [Out] out uint pcModuleRefs);
		[PreserveSig]
		int GetTypeSpecFromToken([In] uint typespec, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		[PreserveSig]
		int GetNameFromToken([In] uint tk, [Out] out IntPtr pszUtf8NamePtr);
		void EnumUnresolvedMethods([In, Out] ref IntPtr phEnum, [Out] uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		[PreserveSig]
		int GetUserString([In] uint stk, [Out] IntPtr szString, [In] uint cchString, [Out] out uint pchString);
		[PreserveSig]
		int GetPinvokeMap(uint tk, IntPtr pdwMappingFlags, IntPtr szImportName, uint cchImportName, IntPtr pchImportName, IntPtr pmrImportDLL);
		void EnumSignatures([In, Out] ref IntPtr phEnum, [Out] uint[] rSignatures, [In] uint cmax, [Out] out uint pcSignatures);
		void EnumTypeSpecs([In, Out] ref IntPtr phEnum, [Out] uint[] rTypeSpecs, [In] uint cmax, [Out] out uint pcTypeSpecs);
		void EnumUserStrings([In, Out] ref IntPtr phEnum, [Out] uint[] rStrings, [In] uint cmax, [Out] out uint pcStrings);
		void GetParamForMethodIndex([In] uint md, [In] uint ulParamSeq, [Out] out uint ppd);
		[PreserveSig]
		int EnumCustomAttributes([In, Out] ref IntPtr phEnum, [In] uint tk, [In] uint tkType, IntPtr rCustomAttributes, [In] uint cMax, [Out] out uint pcCustomAttributes);
		[PreserveSig]
		int GetCustomAttributeProps([In] uint cv, [Out] IntPtr ptkObj, [Out] out uint ptkType, [Out] out IntPtr ppBlob, [Out] out uint pcbSize);
		void FindTypeRef([In] uint tkResolutionScope, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out uint ptr);
		void GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, [Out] out IntPtr ppvSigBlob, [Out] out uint pcbSigBlob, [Out] out uint pulCodeRVA, [Out] out uint pdwImplFlags, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppValue, [Out] out uint pcchValue);
		[PreserveSig]
		int GetFieldProps(uint mb, IntPtr pClass, IntPtr szField, uint cchField, IntPtr pchField, IntPtr pdwAttr, [Out] IntPtr ppvSigBlob, [Out] IntPtr pcbSigBlob, [Out] IntPtr pdwCPlusTypeFlag, [Out] IntPtr ppValue, [Out] IntPtr pcchValue);
		[PreserveSig]
		int GetPropertyProps(uint prop, IntPtr pClass, IntPtr szProperty, uint cchProperty, IntPtr pchProperty, IntPtr pdwPropFlags, IntPtr ppvSig, IntPtr pbSig, IntPtr pdwCPlusTypeFlag, IntPtr ppDefaultValue, IntPtr pcchDefaultValue, IntPtr pmdSetter, IntPtr pmdGetter, IntPtr rmdOtherMethod, uint cMax, IntPtr pcOtherMethod);
		[PreserveSig]
		int GetParamProps([In] uint tk, IntPtr pmd, IntPtr pulSequence, IntPtr szName, uint cchName, IntPtr pchName, IntPtr pdwAttr, IntPtr pdwCPlusTypeFlag, IntPtr ppValue, IntPtr pcchValue);
		[PreserveSig]
		int GetCustomAttributeByName([In] uint tkObj, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, IntPtr ppData, IntPtr pcbData);
		[PreserveSig]
		bool IsValidToken([In] uint tk);
		[PreserveSig]
		int GetNestedClassProps([In] uint tdNestedClass, out uint ptdEnclosingClass);
		void GetNativeCallConvFromSig([In] IntPtr pvSig, [In] uint cbSig, [Out] out uint pCallConv);
		[PreserveSig]
		int IsGlobal([In] uint pd, [Out] out int pbGlobal);

		[PreserveSig]
		int EnumGenericParams([In] [Out] ref IntPtr phEnum, [In] uint tk, [Out] IntPtr rGenericParams, [In] uint cMax, out uint pcGenericParams);
		[PreserveSig]
		int GetGenericParamProps([In] uint gp, IntPtr pulParamSeq, IntPtr pdwParamFlags, IntPtr ptOwner, IntPtr reserved, IntPtr wzname, uint cchName, IntPtr pchName);
		[PreserveSig]
		int GetMethodSpecProps([In] uint mi, out uint tkParent, out IntPtr ppvSigBlob, out uint pcbSigBlob);
		[PreserveSig]
		int EnumGenericParamConstraints([In] [Out] ref IntPtr phEnum, [In] uint tk, [Out] IntPtr rGenericParamConstraints, [In] uint cMax, out uint pcGenericParamConstraints);
		[PreserveSig]
		int GetGenericParamConstraintProps([In] uint gpc, IntPtr ptGenericParam, IntPtr ptkConstraintType);
		[PreserveSig]
		int GetPEKind(out uint pdwPEKind, out uint pdwMachine);
		[PreserveSig]
		int GetVersionString([Out] IntPtr pwzBuf, [In] uint ccBufSize, out uint pccBufSize);
		void EnumMethodSpecs([In] [Out] ref IntPtr phEnum, [In] uint tk, [Out] IntPtr rMethodSpecs, [In] uint cMax, out uint pcMethodSpecs);
	}
}
#pragma warning restore CS0649
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
