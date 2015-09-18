/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnlib.DotNet;

#pragma warning disable 0108 // Member hides inherited member; missing new keyword
namespace dndbg.Engine.COM.MetaData {
	[Guid("7DAC8207-D3AE-4C75-9B67-92801A497D44"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
	public interface IMetaDataImport {
		[PreserveSig]
		int CloseEnum(IntPtr hEnum);
		[PreserveSig]
		int CountEnum(IntPtr hEnum, ref uint pulCount);
		[PreserveSig]
		int ResetEnum(IntPtr hEnum, uint ulPos);
		void EnumTypeDefs(IntPtr phEnum, uint[] rTypeDefs, uint cMax, out uint pcTypeDefs);
		void EnumInterfaceImpls(ref IntPtr phEnum, uint td, uint[] rImpls, uint cMax, ref uint pcImpls);
		void EnumTypeRefs(ref IntPtr phEnum, uint[] rTypeRefs, uint cMax, ref uint pcTypeRefs);
		void FindTypeDefByName([In, MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] uint tkEnclosingClass, [Out] out uint ptd);
		void GetScopeProps([Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName, [Out] out Guid pmvid);
		void GetModuleFromScope([Out] out uint pmd);
		[PreserveSig]
		int GetTypeDefProps([In] uint td, [In] IntPtr szTypeDef, [In] uint cchTypeDef, out uint pchTypeDef, out uint pdwTypeDefFlags, out uint ptkExtends);
		void GetInterfaceImplProps([In] uint iiImpl, [Out] out uint pClass, [Out] out uint ptkIface);
		[PreserveSig]
		int GetTypeRefProps([In] uint tr, [Out] out uint ptkResolutionScope, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
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
		void EnumMethodImpls([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rMethodBody, [Out] uint[] rMethodDecl, [In] uint cMax, [Out] out uint pcTokens);
		void EnumPermissionSets([In, Out] ref IntPtr phEnum, [In] uint tk, [In] uint dwActions, [Out] uint[] rPermission, [In] uint cMax, [Out] out uint pcTokens);
		void FindMember([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		[PreserveSig]
		int FindMethod([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindField([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindMemberRef([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmr);
		[PreserveSig]
		int GetMethodProps(uint mb, IntPtr pClass, [In] IntPtr szMethod, uint cchMethod, out uint pchMethod, out MethodAttributes pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out MethodImplAttributes pdwImplFlags);
		void GetMemberRefProps([In] uint mr, [Out] out uint ptk, [Out] IntPtr szMember, [In] uint cchMember, [Out] out uint pchMember, [Out] out IntPtr ppvSigBlob, [Out] out uint pbSig);
		void EnumProperties([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rProperties, [In] uint cMax, [Out] out uint pcProperties);
		void EnumEvents([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rEvents, [In] uint cMax, [Out] out uint pcEvents);
		void GetEventProps([In] uint ev, [Out] out uint pClass, [Out] [MarshalAs(UnmanagedType.LPWStr)] string szEvent, [In] uint cchEvent, [Out] out uint pchEvent, [Out] out uint pdwEventFlags, [Out] out uint ptkEventType, [Out] out uint pmdAddOn, [Out] out uint pmdRemoveOn, [Out] out uint pmdFire, [In, Out] uint[] rmdOtherMethod, [In] uint cMax, [Out] out uint pcOtherMethod);
		void EnumMethodSemantics([In, Out] ref IntPtr phEnum, [In] uint mb, [In, Out] uint[] rEventProp, [In] uint cMax, [Out] out uint pcEventProp);
		void GetMethodSemantics([In] uint mb, [In] uint tkEventProp, [Out] out uint pdwSemanticsFlags);
		void GetClassLayout([In] uint td, [Out] out uint pdwPackSize, [Out] out IntPtr rFieldOffset, [In] uint cMax, [Out] out uint pcFieldOffset, [Out] out uint pulClassSize);
		void GetFieldMarshal([In] uint tk, [Out] out IntPtr ppvNativeType, [Out] out uint pcbNativeType);
		void GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags);
		void GetPermissionSetProps([In] uint pm, [Out] out uint pdwAction, [Out] out IntPtr ppvPermission, [Out] out uint pcbPermission);
		[PreserveSig]
		int GetSigFromToken([In] uint mdSig, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		void GetModuleRefProps([In] uint mur, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
		void EnumModuleRefs([In, Out] ref IntPtr phEnum, [Out] uint[] rModuleRefs, [In] uint cmax, [Out] out uint pcModuleRefs);
		void GetTypeSpecFromToken([In] uint typespec, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		void GetNameFromToken([In] uint tk, [Out] out IntPtr pszUtf8NamePtr);
		void EnumUnresolvedMethods([In, Out] ref IntPtr phEnum, [Out] uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		void GetUserString([In] uint stk, [Out] IntPtr szString, [In] uint cchString, [Out] out uint pchString);
		void GetPinvokeMap([In] uint tk, [Out] out uint pdwMappingFlags, [Out] IntPtr szImportName, [In] uint cchImportName, [Out] out uint pchImportName, [Out] out uint pmrImportDLL);
		void EnumSignatures([In, Out] ref IntPtr phEnum, [Out] uint[] rSignatures, [In] uint cmax, [Out] out uint pcSignatures);
		void EnumTypeSpecs([In, Out] ref IntPtr phEnum, [Out] uint[] rTypeSpecs, [In] uint cmax, [Out] out uint pcTypeSpecs);
		void EnumUserStrings([In, Out] ref IntPtr phEnum, [Out] uint[] rStrings, [In] uint cmax, [Out] out uint pcStrings);
		void GetParamForMethodIndex([In] uint md, [In] uint ulParamSeq, [Out] out uint ppd);
		void EnumCustomAttributes([In, Out] IntPtr phEnum, [In] uint tk, [In] uint tkType, [Out] uint[] rCustomAttributes, [In] uint cMax, [Out] out uint pcCustomAttributes);
		void GetCustomAttributeProps([In] uint cv, [Out] out uint ptkObj, [Out] out uint ptkType, [Out] out IntPtr ppBlob, [Out] out uint pcbSize);
		void FindTypeRef([In] uint tkResolutionScope, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out uint ptr);
		void GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, [Out] out IntPtr ppvSigBlob, [Out] out uint pcbSigBlob, [Out] out uint pulCodeRVA, [Out] out uint pdwImplFlags, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppValue, [Out] out uint pcchValue);
		[PreserveSig]
		int GetFieldProps(uint mb, IntPtr pClass, IntPtr szField, uint cchField, out uint pchField, out uint pdwAttr, [Out] IntPtr ppvSigBlob, [Out] IntPtr pcbSigBlob, [Out] IntPtr pdwCPlusTypeFlag, [Out] IntPtr ppValue, [Out] IntPtr pcchValue);
		void GetPropertyProps([In] uint prop, [Out] out uint pClass, [Out] IntPtr szProperty, [In] uint cchProperty, [Out] out uint pchProperty, [Out] out uint pdwPropFlags, [Out] out IntPtr ppvSig, [Out] out uint pbSig, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppDefaultValue, [Out] out uint pcchDefaultValue, [Out] out uint pmdSetter, [Out] out uint pmdGetter, [In, Out] uint[] rmdOtherMethod, [In] uint cMax, [Out] out uint pcOtherMethod);
		[PreserveSig]
		int GetParamProps([In] uint tk, [Out] IntPtr pmd, [Out] out uint pulSequence, [Out] IntPtr szName, [Out] uint cchName, [Out] out uint pchName, [Out] out uint pdwAttr, [Out] IntPtr pdwCPlusTypeFlag, [Out] IntPtr ppValue, [Out] IntPtr pcchValue);
		void GetCustomAttributeByName([In] uint tkObj, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out IntPtr ppData, [Out] out uint pcbData);
		bool IsValidToken([In] uint tk);
		[PreserveSig]
		int GetNestedClassProps([In] uint tdNestedClass, out uint ptdEnclosingClass);
		void GetNativeCallConvFromSig([In] IntPtr pvSig, [In] uint cbSig, [Out] out uint pCallConv);
		void IsGlobal([In] uint pd, [Out] out int pbGlobal);
	}
	[Guid("FCE5EFA0-8BBA-4f8e-A036-8F2022B08466"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
	public interface IMetaDataImport2 : IMetaDataImport {
		// IMetaDataImport members
		[PreserveSig]
		int CloseEnum(IntPtr hEnum);
		[PreserveSig]
		int CountEnum(IntPtr hEnum, ref uint pulCount);
		[PreserveSig]
		int ResetEnum(IntPtr hEnum, uint ulPos);
		void EnumTypeDefs(IntPtr phEnum, uint[] rTypeDefs, uint cMax, out uint pcTypeDefs);
		void EnumInterfaceImpls(ref IntPtr phEnum, uint td, uint[] rImpls, uint cMax, ref uint pcImpls);
		void EnumTypeRefs(ref IntPtr phEnum, uint[] rTypeRefs, uint cMax, ref uint pcTypeRefs);
		void FindTypeDefByName([In, MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] uint tkEnclosingClass, [Out] out uint ptd);
		void GetScopeProps([Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName, [Out] out Guid pmvid);
		void GetModuleFromScope([Out] out uint pmd);
		[PreserveSig]
		int GetTypeDefProps([In] uint td, [In] IntPtr szTypeDef, [In] uint cchTypeDef, out uint pchTypeDef, out uint pdwTypeDefFlags, out uint ptkExtends);
		void GetInterfaceImplProps([In] uint iiImpl, [Out] out uint pClass, [Out] out uint ptkIface);
		[PreserveSig]
		int GetTypeRefProps([In] uint tr, [Out] out uint ptkResolutionScope, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
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
		void EnumMethodImpls([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rMethodBody, [Out] uint[] rMethodDecl, [In] uint cMax, [Out] out uint pcTokens);
		void EnumPermissionSets([In, Out] ref IntPtr phEnum, [In] uint tk, [In] uint dwActions, [Out] uint[] rPermission, [In] uint cMax, [Out] out uint pcTokens);
		void FindMember([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		[PreserveSig]
		int FindMethod([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindField([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindMemberRef([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmr);
		[PreserveSig]
		int GetMethodProps(uint mb, IntPtr pClass, [In] IntPtr szMethod, uint cchMethod, out uint pchMethod, out MethodAttributes pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out MethodImplAttributes pdwImplFlags);
		void GetMemberRefProps([In] uint mr, [Out] out uint ptk, [Out] IntPtr szMember, [In] uint cchMember, [Out] out uint pchMember, [Out] out IntPtr ppvSigBlob, [Out] out uint pbSig);
		void EnumProperties([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rProperties, [In] uint cMax, [Out] out uint pcProperties);
		void EnumEvents([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rEvents, [In] uint cMax, [Out] out uint pcEvents);
		void GetEventProps([In] uint ev, [Out] out uint pClass, [Out] [MarshalAs(UnmanagedType.LPWStr)] string szEvent, [In] uint cchEvent, [Out] out uint pchEvent, [Out] out uint pdwEventFlags, [Out] out uint ptkEventType, [Out] out uint pmdAddOn, [Out] out uint pmdRemoveOn, [Out] out uint pmdFire, [In, Out] uint[] rmdOtherMethod, [In] uint cMax, [Out] out uint pcOtherMethod);
		void EnumMethodSemantics([In, Out] ref IntPtr phEnum, [In] uint mb, [In, Out] uint[] rEventProp, [In] uint cMax, [Out] out uint pcEventProp);
		void GetMethodSemantics([In] uint mb, [In] uint tkEventProp, [Out] out uint pdwSemanticsFlags);
		void GetClassLayout([In] uint td, [Out] out uint pdwPackSize, [Out] out IntPtr rFieldOffset, [In] uint cMax, [Out] out uint pcFieldOffset, [Out] out uint pulClassSize);
		void GetFieldMarshal([In] uint tk, [Out] out IntPtr ppvNativeType, [Out] out uint pcbNativeType);
		void GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags);
		void GetPermissionSetProps([In] uint pm, [Out] out uint pdwAction, [Out] out IntPtr ppvPermission, [Out] out uint pcbPermission);
		[PreserveSig]
		int GetSigFromToken([In] uint mdSig, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		void GetModuleRefProps([In] uint mur, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
		void EnumModuleRefs([In, Out] ref IntPtr phEnum, [Out] uint[] rModuleRefs, [In] uint cmax, [Out] out uint pcModuleRefs);
		void GetTypeSpecFromToken([In] uint typespec, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		void GetNameFromToken([In] uint tk, [Out] out IntPtr pszUtf8NamePtr);
		void EnumUnresolvedMethods([In, Out] ref IntPtr phEnum, [Out] uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		void GetUserString([In] uint stk, [Out] IntPtr szString, [In] uint cchString, [Out] out uint pchString);
		void GetPinvokeMap([In] uint tk, [Out] out uint pdwMappingFlags, [Out] IntPtr szImportName, [In] uint cchImportName, [Out] out uint pchImportName, [Out] out uint pmrImportDLL);
		void EnumSignatures([In, Out] ref IntPtr phEnum, [Out] uint[] rSignatures, [In] uint cmax, [Out] out uint pcSignatures);
		void EnumTypeSpecs([In, Out] ref IntPtr phEnum, [Out] uint[] rTypeSpecs, [In] uint cmax, [Out] out uint pcTypeSpecs);
		void EnumUserStrings([In, Out] ref IntPtr phEnum, [Out] uint[] rStrings, [In] uint cmax, [Out] out uint pcStrings);
		void GetParamForMethodIndex([In] uint md, [In] uint ulParamSeq, [Out] out uint ppd);
		void EnumCustomAttributes([In, Out] IntPtr phEnum, [In] uint tk, [In] uint tkType, [Out] uint[] rCustomAttributes, [In] uint cMax, [Out] out uint pcCustomAttributes);
		void GetCustomAttributeProps([In] uint cv, [Out] out uint ptkObj, [Out] out uint ptkType, [Out] out IntPtr ppBlob, [Out] out uint pcbSize);
		void FindTypeRef([In] uint tkResolutionScope, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out uint ptr);
		void GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, [Out] out IntPtr ppvSigBlob, [Out] out uint pcbSigBlob, [Out] out uint pulCodeRVA, [Out] out uint pdwImplFlags, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppValue, [Out] out uint pcchValue);
		[PreserveSig]
		int GetFieldProps(uint mb, IntPtr pClass, IntPtr szField, uint cchField, out uint pchField, out uint pdwAttr, [Out] IntPtr ppvSigBlob, [Out] IntPtr pcbSigBlob, [Out] IntPtr pdwCPlusTypeFlag, [Out] IntPtr ppValue, [Out] IntPtr pcchValue);
		void GetPropertyProps([In] uint prop, [Out] out uint pClass, [Out] IntPtr szProperty, [In] uint cchProperty, [Out] out uint pchProperty, [Out] out uint pdwPropFlags, [Out] out IntPtr ppvSig, [Out] out uint pbSig, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppDefaultValue, [Out] out uint pcchDefaultValue, [Out] out uint pmdSetter, [Out] out uint pmdGetter, [In, Out] uint[] rmdOtherMethod, [In] uint cMax, [Out] out uint pcOtherMethod);
		[PreserveSig]
		int GetParamProps([In] uint tk, [Out] IntPtr pmd, [Out] out uint pulSequence, [Out] IntPtr szName, [Out] uint cchName, [Out] out uint pchName, [Out] out uint pdwAttr, [Out] IntPtr pdwCPlusTypeFlag, [Out] IntPtr ppValue, [Out] IntPtr pcchValue);
		void GetCustomAttributeByName([In] uint tkObj, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out IntPtr ppData, [Out] out uint pcbData);
		bool IsValidToken([In] uint tk);
		[PreserveSig]
		int GetNestedClassProps([In] uint tdNestedClass, out uint ptdEnclosingClass);
		void GetNativeCallConvFromSig([In] IntPtr pvSig, [In] uint cbSig, [Out] out uint pCallConv);
		void IsGlobal([In] uint pd, [Out] out int pbGlobal);
		// end of inherited members

		[PreserveSig]
		int EnumGenericParams([In] [Out] ref IntPtr phEnum, [In] uint tk, [Out] IntPtr rGenericParams, [In] uint cMax, out uint pcGenericParams);
		[PreserveSig]
		int GetGenericParamProps([In] uint gp, out uint pulParamSeq, out uint pdwParamFlags, out uint ptOwner, out uint reserved, [Out] IntPtr wzname, [In] uint cchName, out uint pchName);
		void GetMethodSpecProps([In] uint mi, out uint tkParent, [Out] IntPtr ppvSigBlob, out uint pcbSigBlob);
		void EnumGenericParamConstraints([In] [Out] ref IntPtr phEnum, [In] uint tk, [Out] IntPtr rGenericParamConstraints, [In] uint cMax, out uint pcGenericParamConstraints);
		void GetGenericParamConstraintProps([In] uint gpc, out uint ptGenericParam, out uint ptkConstraintType);
		void GetPEKind(out uint pdwPEKind, out uint pdwMAchine);
		void GetVersionString([Out] IntPtr pwzBuf, [In] uint ccBufSize, out uint pccBufSize);
		void EnumMethodSpecs([In] [Out] ref IntPtr phEnum, [In] uint tk, [Out] IntPtr rMethodSpecs, [In] uint cMax, out uint pcMethodSpecs);
	}
}
#pragma warning restore 0108 // Member hides inherited member; missing new keyword
