using System;
using System.Runtime.InteropServices;
using System.Text;

#if !READ_ONLY

namespace Mono.Cecil.Pdb {

	[ComImport, InterfaceType (ComInterfaceType.InterfaceIsIUnknown), Guid ("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859")]
	interface IMetaDataEmit {
		void SetModuleProps (string szName);
		void Save (string szFile, uint dwSaveFlags);
		void SaveToStream (IntPtr pIStream, uint dwSaveFlags);
		uint GetSaveSize (uint fSave);
		uint DefineTypeDef (IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements);
		uint DefineNestedType (IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements, uint tdEncloser);
		void SetHandler ([MarshalAs (UnmanagedType.IUnknown), In]object pUnk);
		uint DefineMethod (uint td, IntPtr zName, uint dwMethodFlags, IntPtr pvSigBlob, uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags);
		void DefineMethodImpl (uint td, uint tkBody, uint tkDecl);
		uint DefineTypeRefByName (uint tkResolutionScope, IntPtr szName);
		uint DefineImportType (IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport,
		  uint tdImport, IntPtr pAssemEmit);
		uint DefineMemberRef (uint tkImport, string szName, IntPtr pvSigBlob, uint cbSigBlob);
		uint DefineImportMember (IntPtr pAssemImport, IntPtr /* void* */ pbHashValue, uint cbHashValue,
		  IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent);
		uint DefineEvent (uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr /* uint* */ rmdOtherMethods);
		void SetClassLayout (uint td, uint dwPackSize, IntPtr /*COR_FIELD_OFFSET**/ rFieldOffsets, uint ulClassSize);
		void DeleteClassLayout (uint td);
		void SetFieldMarshal (uint tk, IntPtr /* byte* */ pvNativeType, uint cbNativeType);
		void DeleteFieldMarshal (uint tk);
		uint DefinePermissionSet (uint tk, uint dwAction, IntPtr /* void* */ pvPermission, uint cbPermission);
		void SetRVA (uint md, uint ulRVA);
		uint GetTokenFromSig (IntPtr /* byte* */ pvSig, uint cbSig);
		uint DefineModuleRef (string szName);
		void SetParent (uint mr, uint tk);
		uint GetTokenFromTypeSpec (IntPtr /* byte* */ pvSig, uint cbSig);
		void SaveToMemory (IntPtr /* void* */ pbData, uint cbData);
		uint DefineUserString (string szString, uint cchString);
		void DeleteToken (uint tkObj);
		void SetMethodProps (uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags);
		void SetTypeDefProps (uint td, uint dwTypeDefFlags, uint tkExtends, IntPtr /* uint* */ rtkImplements);
		void SetEventProps (uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr /* uint* */ rmdOtherMethods);
		uint SetPermissionSetProps (uint tk, uint dwAction, IntPtr /* void* */ pvPermission, uint cbPermission);
		void DefinePinvokeMap (uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL);
		void SetPinvokeMap (uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL);
		void DeletePinvokeMap (uint tk);
		uint DefineCustomAttribute (uint tkObj, uint tkType, IntPtr /* void* */ pCustomAttribute, uint cbCustomAttribute);
		void SetCustomAttributeValue (uint pcv, IntPtr /* void* */ pCustomAttribute, uint cbCustomAttribute);
		uint DefineField (uint td, string szName, uint dwFieldFlags, IntPtr /* byte* */ pvSigBlob, uint cbSigBlob, uint dwCPlusTypeFlag, IntPtr /* void* */ pValue, uint cchValue);
		uint DefineProperty (uint td, string szProperty, uint dwPropFlags, IntPtr /* byte* */ pvSig, uint cbSig, uint dwCPlusTypeFlag,
		  IntPtr /* void* */ pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr /* uint*  */ rmdOtherMethods);
		uint DefineParam (uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr /* void* */ pValue, uint cchValue);
		void SetFieldProps (uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, IntPtr /* void* */ pValue, uint cchValue);
		void SetPropertyProps (uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, IntPtr /* void* */ pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr /* uint* */ rmdOtherMethods);
		void SetParamProps (uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr /* void* */ pValue, uint cchValue);
		uint DefineSecurityAttributeSet (uint tkObj, IntPtr rSecAttrs, uint cSecAttrs);
		void ApplyEditAndContinue ([MarshalAs (UnmanagedType.IUnknown)]object pImport);
		uint TranslateSigWithScope (IntPtr pAssemImport, IntPtr /* void* */ pbHashValue, uint cbHashValue,
		  IMetaDataImport import, IntPtr /* byte* */ pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit, IntPtr /* byte* */ pvTranslatedSig, uint cbTranslatedSigMax);
		void SetMethodImplFlags (uint md, uint dwImplFlags);
		void SetFieldRVA (uint fd, uint ulRVA);
		void Merge (IMetaDataImport pImport, IntPtr pHostMapToken, [MarshalAs (UnmanagedType.IUnknown)]object pHandler);
		void MergeEnd ();
	}

	[ComImport, InterfaceType (ComInterfaceType.InterfaceIsIUnknown), Guid ("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
	interface IMetaDataImport {
		[PreserveSig]
		void CloseEnum (uint hEnum);
		uint CountEnum (uint hEnum);
		void ResetEnum (uint hEnum, uint ulPos);
		uint EnumTypeDefs (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rTypeDefs, uint cMax);
		uint EnumInterfaceImpls (ref uint phEnum, uint td, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] uint [] rImpls, uint cMax);
		uint EnumTypeRefs (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rTypeRefs, uint cMax);
		uint FindTypeDefByName (string szTypeDef, uint tkEnclosingClass);
		Guid GetScopeProps (StringBuilder szName, uint cchName, out uint pchName);
		uint GetModuleFromScope ();
		uint GetTypeDefProps (uint td, IntPtr szTypeDef, uint cchTypeDef, out uint pchTypeDef, ref uint pdwTypeDefFlags);
		uint GetInterfaceImplProps (uint iiImpl, out uint pClass);
		uint GetTypeRefProps (uint tr, out uint ptkResolutionScope, StringBuilder szName, uint cchName);
		uint ResolveTypeRef (uint tr, [In] ref Guid riid, [MarshalAs (UnmanagedType.Interface)] out object ppIScope);
		uint EnumMembers (ref uint phEnum, uint cl, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] uint [] rMembers, uint cMax);
		uint EnumMembersWithName (ref uint phEnum, uint cl, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rMembers, uint cMax);
		uint EnumMethods (ref uint phEnum, uint cl, IntPtr /* uint* */ rMethods, uint cMax);
		uint EnumMethodsWithName (ref uint phEnum, uint cl, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rMethods, uint cMax);
		uint EnumFields (ref uint phEnum, uint cl, IntPtr /* uint* */ rFields, uint cMax);
		uint EnumFieldsWithName (ref uint phEnum, uint cl, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rFields, uint cMax);
		uint EnumParams (ref uint phEnum, uint mb, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] uint [] rParams, uint cMax);
		uint EnumMemberRefs (ref uint phEnum, uint tkParent, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] uint [] rMemberRefs, uint cMax);
		uint EnumMethodImpls (ref uint phEnum, uint td, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rMethodBody,
		   [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rMethodDecl, uint cMax);
		uint EnumPermissionSets (ref uint phEnum, uint tk, uint dwActions, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rPermission,
		   uint cMax);
		uint FindMember (uint td, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] byte [] pvSigBlob, uint cbSigBlob);
		uint FindMethod (uint td, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] byte [] pvSigBlob, uint cbSigBlob);
		uint FindField (uint td, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] byte [] pvSigBlob, uint cbSigBlob);
		uint FindMemberRef (uint td, string szName, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] byte [] pvSigBlob, uint cbSigBlob);
		uint GetMethodProps (uint mb, out uint pClass, IntPtr szMethod, uint cchMethod, out uint pchMethod, IntPtr pdwAttr,
		  IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA);
		uint GetMemberRefProps (uint mr, ref uint ptk, StringBuilder szMember, uint cchMember, out uint pchMember, out IntPtr /* byte* */ ppvSigBlob);
		uint EnumProperties (ref uint phEnum, uint td, IntPtr /* uint* */ rProperties, uint cMax);
		uint EnumEvents (ref uint phEnum, uint td, IntPtr /* uint* */ rEvents, uint cMax);
		uint GetEventProps (uint ev, out uint pClass, StringBuilder szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags,
		  out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire,
		  [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 11)] uint [] rmdOtherMethod, uint cMax);
		uint EnumMethodSemantics (ref uint phEnum, uint mb, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] uint [] rEventProp, uint cMax);
		uint GetMethodSemantics (uint mb, uint tkEventProp);
		uint GetClassLayout (uint td, out uint pdwPackSize, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 3)] IntPtr /*COR_FIELD_OFFSET **/ rFieldOffset, uint cMax, out uint pcFieldOffset);
		uint GetFieldMarshal (uint tk, out IntPtr /* byte* */ ppvNativeType);
		uint GetRVA (uint tk, out uint pulCodeRVA);
		uint GetPermissionSetProps (uint pm, out uint pdwAction, out IntPtr /* void* */ ppvPermission);
		uint GetSigFromToken (uint mdSig, out IntPtr /* byte* */ ppvSig);
		uint GetModuleRefProps (uint mur, StringBuilder szName, uint cchName);
		uint EnumModuleRefs (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rModuleRefs, uint cmax);
		uint GetTypeSpecFromToken (uint typespec, out IntPtr /* byte* */ ppvSig);
		uint GetNameFromToken (uint tk);
		uint EnumUnresolvedMethods (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rMethods, uint cMax);
		uint GetUserString (uint stk, StringBuilder szString, uint cchString);
		uint GetPinvokeMap (uint tk, out uint pdwMappingFlags, StringBuilder szImportName, uint cchImportName, out uint pchImportName);
		uint EnumSignatures (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rSignatures, uint cmax);
		uint EnumTypeSpecs (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rTypeSpecs, uint cmax);
		uint EnumUserStrings (ref uint phEnum, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 2)] uint [] rStrings, uint cmax);
		[PreserveSig]
		int GetParamForMethodIndex (uint md, uint ulParamSeq, out uint pParam);
		uint EnumCustomAttributes (ref uint phEnum, uint tk, uint tkType, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 4)] uint [] rCustomAttributes, uint cMax);
		uint GetCustomAttributeProps (uint cv, out uint ptkObj, out uint ptkType, out IntPtr /* void* */ ppBlob);
		uint FindTypeRef (uint tkResolutionScope, string szName);
		uint GetMemberProps (uint mb, out uint pClass, StringBuilder szMember, uint cchMember, out uint pchMember, out uint pdwAttr,
		  out IntPtr /* byte* */ ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr /* void* */ ppValue);
		uint GetFieldProps (uint mb, out uint pClass, StringBuilder szField, uint cchField, out uint pchField, out uint pdwAttr,
		  out IntPtr /* byte* */ ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr /* void* */ ppValue);
		uint GetPropertyProps (uint prop, out uint pClass, StringBuilder szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags,
		  out IntPtr /* byte* */ ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr /* void* */ ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter,
		  out uint pmdGetter, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 14)] uint [] rmdOtherMethod, uint cMax);
		uint GetParamProps (uint tk, out uint pmd, out uint pulSequence, StringBuilder szName, uint cchName, out uint pchName,
		  out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr /* void* */ ppValue);
		uint GetCustomAttributeByName (uint tkObj, string szName, out IntPtr /* void* */ ppData);
		[PreserveSig]
		[return: MarshalAs (UnmanagedType.Bool)]
		bool IsValidToken (uint tk);
		uint GetNestedClassProps (uint tdNestedClass);
		uint GetNativeCallConvFromSig (IntPtr /* void* */ pvSig, uint cbSig);
		int IsGlobal (uint pd);
	}

	class ModuleMetadata : IMetaDataEmit, IMetaDataImport {

		readonly ModuleDefinition module;

		public ModuleMetadata (ModuleDefinition module)
		{
			this.module = module;
		}

		public void SetModuleProps (string szName)
		{
			throw new NotImplementedException ();
		}

		public void Save (string szFile, uint dwSaveFlags)
		{
			throw new NotImplementedException ();
		}

		public void SaveToStream (IntPtr pIStream, uint dwSaveFlags)
		{
			throw new NotImplementedException ();
		}

		public uint GetSaveSize (uint fSave)
		{
			throw new NotImplementedException ();
		}

		public uint DefineTypeDef (IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements)
		{
			throw new NotImplementedException ();
		}

		public uint DefineNestedType (IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements, uint tdEncloser)
		{
			throw new NotImplementedException ();
		}

		public void SetHandler (object pUnk)
		{
			throw new NotImplementedException ();
		}

		public uint DefineMethod (uint td, IntPtr zName, uint dwMethodFlags, IntPtr pvSigBlob, uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags)
		{
			throw new NotImplementedException ();
		}

		public void DefineMethodImpl (uint td, uint tkBody, uint tkDecl)
		{
			throw new NotImplementedException ();
		}

		public uint DefineTypeRefByName (uint tkResolutionScope, IntPtr szName)
		{
			throw new NotImplementedException ();
		}

		public uint DefineImportType (IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint tdImport, IntPtr pAssemEmit)
		{
			throw new NotImplementedException ();
		}

		public uint DefineMemberRef (uint tkImport, string szName, IntPtr pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException ();
		}

		public uint DefineImportMember (IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent)
		{
			throw new NotImplementedException ();
		}

		public uint DefineEvent (uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException ();
		}

		public void SetClassLayout (uint td, uint dwPackSize, IntPtr rFieldOffsets, uint ulClassSize)
		{
			throw new NotImplementedException ();
		}

		public void DeleteClassLayout (uint td)
		{
			throw new NotImplementedException ();
		}

		public void SetFieldMarshal (uint tk, IntPtr pvNativeType, uint cbNativeType)
		{
			throw new NotImplementedException ();
		}

		public void DeleteFieldMarshal (uint tk)
		{
			throw new NotImplementedException ();
		}

		public uint DefinePermissionSet (uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission)
		{
			throw new NotImplementedException ();
		}

		public void SetRVA (uint md, uint ulRVA)
		{
			throw new NotImplementedException ();
		}

		public uint GetTokenFromSig (IntPtr pvSig, uint cbSig)
		{
			throw new NotImplementedException ();
		}

		public uint DefineModuleRef (string szName)
		{
			throw new NotImplementedException ();
		}

		public void SetParent (uint mr, uint tk)
		{
			throw new NotImplementedException ();
		}

		public uint GetTokenFromTypeSpec (IntPtr pvSig, uint cbSig)
		{
			throw new NotImplementedException ();
		}

		public void SaveToMemory (IntPtr pbData, uint cbData)
		{
			throw new NotImplementedException ();
		}

		public uint DefineUserString (string szString, uint cchString)
		{
			throw new NotImplementedException ();
		}

		public void DeleteToken (uint tkObj)
		{
			throw new NotImplementedException ();
		}

		public void SetMethodProps (uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags)
		{
			throw new NotImplementedException ();
		}

		public void SetTypeDefProps (uint td, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements)
		{
			throw new NotImplementedException ();
		}

		public void SetEventProps (uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException ();
		}

		public uint SetPermissionSetProps (uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission)
		{
			throw new NotImplementedException ();
		}

		public void DefinePinvokeMap (uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL)
		{
			throw new NotImplementedException ();
		}

		public void SetPinvokeMap (uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL)
		{
			throw new NotImplementedException ();
		}

		public void DeletePinvokeMap (uint tk)
		{
			throw new NotImplementedException ();
		}

		public uint DefineCustomAttribute (uint tkObj, uint tkType, IntPtr pCustomAttribute, uint cbCustomAttribute)
		{
			throw new NotImplementedException ();
		}

		public void SetCustomAttributeValue (uint pcv, IntPtr pCustomAttribute, uint cbCustomAttribute)
		{
			throw new NotImplementedException ();
		}

		public uint DefineField (uint td, string szName, uint dwFieldFlags, IntPtr pvSigBlob, uint cbSigBlob, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException ();
		}

		public uint DefineProperty (uint td, string szProperty, uint dwPropFlags, IntPtr pvSig, uint cbSig, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException ();
		}

		public uint DefineParam (uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException ();
		}

		public void SetFieldProps (uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException ();
		}

		public void SetPropertyProps (uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException ();
		}

		public void SetParamProps (uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException ();
		}

		public uint DefineSecurityAttributeSet (uint tkObj, IntPtr rSecAttrs, uint cSecAttrs)
		{
			throw new NotImplementedException ();
		}

		public void ApplyEditAndContinue (object pImport)
		{
			throw new NotImplementedException ();
		}

		public uint TranslateSigWithScope (IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport import, IntPtr pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit, IntPtr pvTranslatedSig, uint cbTranslatedSigMax)
		{
			throw new NotImplementedException ();
		}

		public void SetMethodImplFlags (uint md, uint dwImplFlags)
		{
			throw new NotImplementedException ();
		}

		public void SetFieldRVA (uint fd, uint ulRVA)
		{
			throw new NotImplementedException ();
		}

		public void Merge (IMetaDataImport pImport, IntPtr pHostMapToken, object pHandler)
		{
			throw new NotImplementedException ();
		}

		public void MergeEnd ()
		{
			throw new NotImplementedException ();
		}

		public void CloseEnum (uint hEnum)
		{
			throw new NotImplementedException ();
		}

		public uint CountEnum (uint hEnum)
		{
			throw new NotImplementedException ();
		}

		public void ResetEnum (uint hEnum, uint ulPos)
		{
			throw new NotImplementedException ();
		}

		public uint EnumTypeDefs (ref uint phEnum, uint[] rTypeDefs, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumInterfaceImpls (ref uint phEnum, uint td, uint[] rImpls, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumTypeRefs (ref uint phEnum, uint[] rTypeRefs, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint FindTypeDefByName (string szTypeDef, uint tkEnclosingClass)
		{
			throw new NotImplementedException ();
		}

		public Guid GetScopeProps (StringBuilder szName, uint cchName, out uint pchName)
		{
			throw new NotImplementedException ();
		}

		public uint GetModuleFromScope ()
		{
			throw new NotImplementedException ();
		}

		public uint GetTypeDefProps (uint td, IntPtr szTypeDef, uint cchTypeDef, out uint pchTypeDef, ref uint pdwTypeDefFlags)
		{
			pchTypeDef = 0;
			return td;
		}

		public uint GetInterfaceImplProps (uint iiImpl, out uint pClass)
		{
			throw new NotImplementedException ();
		}

		public uint GetTypeRefProps (uint tr, out uint ptkResolutionScope, StringBuilder szName, uint cchName)
		{
			throw new NotImplementedException ();
		}

		public uint ResolveTypeRef (uint tr, ref Guid riid, out object ppIScope)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMembers (ref uint phEnum, uint cl, uint[] rMembers, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMembersWithName (ref uint phEnum, uint cl, string szName, uint[] rMembers, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMethods (ref uint phEnum, uint cl, IntPtr rMethods, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMethodsWithName (ref uint phEnum, uint cl, string szName, uint[] rMethods, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumFields (ref uint phEnum, uint cl, IntPtr rFields, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumFieldsWithName (ref uint phEnum, uint cl, string szName, uint[] rFields, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumParams (ref uint phEnum, uint mb, uint[] rParams, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMemberRefs (ref uint phEnum, uint tkParent, uint[] rMemberRefs, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMethodImpls (ref uint phEnum, uint td, uint[] rMethodBody, uint[] rMethodDecl, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumPermissionSets (ref uint phEnum, uint tk, uint dwActions, uint[] rPermission, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint FindMember (uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException ();
		}

		public uint FindMethod (uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException ();
		}

		public uint FindField (uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException ();
		}

		public uint FindMemberRef (uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException ();
		}

		public uint GetMethodProps (uint mb, out uint pClass, IntPtr szMethod, uint cchMethod, out uint pchMethod, IntPtr pdwAttr, IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA)
		{
			pClass = 0;
			pchMethod = 0;
			return mb;
		}

		public uint GetMemberRefProps (uint mr, ref uint ptk, StringBuilder szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob)
		{
			throw new NotImplementedException ();
		}

		public uint EnumProperties (ref uint phEnum, uint td, IntPtr rProperties, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumEvents (ref uint phEnum, uint td, IntPtr rEvents, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint GetEventProps (uint ev, out uint pClass, StringBuilder szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, uint[] rmdOtherMethod, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumMethodSemantics (ref uint phEnum, uint mb, uint[] rEventProp, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint GetMethodSemantics (uint mb, uint tkEventProp)
		{
			throw new NotImplementedException ();
		}

		public uint GetClassLayout (uint td, out uint pdwPackSize, IntPtr rFieldOffset, uint cMax, out uint pcFieldOffset)
		{
			throw new NotImplementedException ();
		}

		public uint GetFieldMarshal (uint tk, out IntPtr ppvNativeType)
		{
			throw new NotImplementedException ();
		}

		public uint GetRVA (uint tk, out uint pulCodeRVA)
		{
			throw new NotImplementedException ();
		}

		public uint GetPermissionSetProps (uint pm, out uint pdwAction, out IntPtr ppvPermission)
		{
			throw new NotImplementedException ();
		}

		public uint GetSigFromToken (uint mdSig, out IntPtr ppvSig)
		{
			throw new NotImplementedException ();
		}

		public uint GetModuleRefProps (uint mur, StringBuilder szName, uint cchName)
		{
			throw new NotImplementedException ();
		}

		public uint EnumModuleRefs (ref uint phEnum, uint[] rModuleRefs, uint cmax)
		{
			throw new NotImplementedException ();
		}

		public uint GetTypeSpecFromToken (uint typespec, out IntPtr ppvSig)
		{
			throw new NotImplementedException ();
		}

		public uint GetNameFromToken (uint tk)
		{
			throw new NotImplementedException ();
		}

		public uint EnumUnresolvedMethods (ref uint phEnum, uint[] rMethods, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint GetUserString (uint stk, StringBuilder szString, uint cchString)
		{
			throw new NotImplementedException ();
		}

		public uint GetPinvokeMap (uint tk, out uint pdwMappingFlags, StringBuilder szImportName, uint cchImportName, out uint pchImportName)
		{
			throw new NotImplementedException ();
		}

		public uint EnumSignatures (ref uint phEnum, uint[] rSignatures, uint cmax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumTypeSpecs (ref uint phEnum, uint[] rTypeSpecs, uint cmax)
		{
			throw new NotImplementedException ();
		}

		public uint EnumUserStrings (ref uint phEnum, uint[] rStrings, uint cmax)
		{
			throw new NotImplementedException ();
		}

		public int GetParamForMethodIndex (uint md, uint ulParamSeq, out uint pParam)
		{
			throw new NotImplementedException ();
		}

		public uint EnumCustomAttributes (ref uint phEnum, uint tk, uint tkType, uint[] rCustomAttributes, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint GetCustomAttributeProps (uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob)
		{
			throw new NotImplementedException ();
		}

		public uint FindTypeRef (uint tkResolutionScope, string szName)
		{
			throw new NotImplementedException ();
		}

		public uint GetMemberProps (uint mb, out uint pClass, StringBuilder szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue)
		{
			throw new NotImplementedException ();
		}

		public uint GetFieldProps (uint mb, out uint pClass, StringBuilder szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue)
		{
			throw new NotImplementedException ();
		}

		public uint GetPropertyProps (uint prop, out uint pClass, StringBuilder szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, uint[] rmdOtherMethod, uint cMax)
		{
			throw new NotImplementedException ();
		}

		public uint GetParamProps (uint tk, out uint pmd, out uint pulSequence, StringBuilder szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue)
		{
			throw new NotImplementedException ();
		}

		public uint GetCustomAttributeByName (uint tkObj, string szName, out IntPtr ppData)
		{
			throw new NotImplementedException ();
		}

		public bool IsValidToken (uint tk)
		{
			throw new NotImplementedException ();
		}

		public uint GetNestedClassProps (uint tdNestedClass)
		{
			throw new NotImplementedException ();
		}

		public uint GetNativeCallConvFromSig (IntPtr pvSig, uint cbSig)
		{
			throw new NotImplementedException ();
		}

		public int IsGlobal (uint pd)
		{
			throw new NotImplementedException ();
		}
	}
}

#endif
