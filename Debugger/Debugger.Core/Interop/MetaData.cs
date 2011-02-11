// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#pragma warning disable 108, 1591 

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Debugger.Interop.MetaData
{
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct COR_FIELD_OFFSET
    {
        public uint ridOfField;
        public uint ulOffset;
    }
	[System.Flags()]
	public enum ClassFieldAttribute: uint
	{
		// member access mask - Use this mask to retrieve accessibility information.
		fdFieldAccessMask           =   0x0007,
		fdPrivateScope              =   0x0000,     // Member not referenceable.
		fdPrivate                   =   0x0001,     // Accessible only by the parent type.  
		fdFamANDAssem               =   0x0002,     // Accessible by sub-types only in this Assembly.
		fdAssembly                  =   0x0003,     // Accessibly by anyone in the Assembly.
		fdFamily                    =   0x0004,     // Accessible only by type and sub-types.    
		fdFamORAssem                =   0x0005,     // Accessibly by sub-types anywhere, plus anyone in assembly.
		fdPublic                    =   0x0006,     // Accessibly by anyone who has visibility to this scope.    
		// end member access mask

		// field contract attributes.
		fdStatic                    =   0x0010,     // Defined on type, else per instance.
		fdInitOnly                  =   0x0020,     // Field may only be initialized, not written to after init.
		fdLiteral                   =   0x0040,     // Value is compile time constant.
		fdNotSerialized             =   0x0080,     // Field does not have to be serialized when type is remoted.

		fdSpecialName               =   0x0200,     // field is special.  Name describes how.

		// interop attributes
		fdPinvokeImpl               =   0x2000,     // Implementation is forwarded through pinvoke.

		// Reserved flags for runtime use only.
		fdReservedMask              =   0x9500,
		fdRTSpecialName             =   0x0400,     // Runtime(metadata internal APIs) should check name encoding.
		fdHasFieldMarshal           =   0x1000,     // Field has marshalling information.
		fdHasDefault                =   0x8000,     // Field has default.
		fdHasFieldRVA               =   0x0100,     // Field has RVA.
	}
	public enum CorCallingConvention: uint
	{
	    IMAGE_CEE_CS_CALLCONV_DEFAULT   = 0x0,  
	
	    IMAGE_CEE_CS_CALLCONV_VARARG    = 0x5,  
	    IMAGE_CEE_CS_CALLCONV_FIELD     = 0x6,  
	    IMAGE_CEE_CS_CALLCONV_LOCAL_SIG = 0x7,
	    IMAGE_CEE_CS_CALLCONV_PROPERTY  = 0x8,
	    IMAGE_CEE_CS_CALLCONV_UNMGD     = 0x9,
	    IMAGE_CEE_CS_CALLCONV_MAX       = 0x10,  // first invalid calling convention    
	
	
	        // The high bits of the calling convention convey additional info   
	    IMAGE_CEE_CS_CALLCONV_MASK      = 0x0f,  // Calling convention is bottom 4 bits 
	    IMAGE_CEE_CS_CALLCONV_HASTHIS   = 0x20,  // Top bit indicates a 'this' parameter    
	    IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS = 0x40,  // This parameter is explicitly in the signature
	}
	public enum CorMethodAttr: uint
	{
	    // member access mask - Use this mask to retrieve accessibility information.
	    mdMemberAccessMask          =   0x0007,
	    mdPrivateScope              =   0x0000,     // Member not referenceable.
	    mdPrivate                   =   0x0001,     // Accessible only by the parent type.  
	    mdFamANDAssem               =   0x0002,     // Accessible by sub-types only in this Assembly.
	    mdAssem                     =   0x0003,     // Accessibly by anyone in the Assembly.
	    mdFamily                    =   0x0004,     // Accessible only by type and sub-types.    
	    mdFamORAssem                =   0x0005,     // Accessibly by sub-types anywhere, plus anyone in assembly.
	    mdPublic                    =   0x0006,     // Accessibly by anyone who has visibility to this scope.    
	    // end member access mask
	
	    // method contract attributes.
	    mdStatic                    =   0x0010,     // Defined on type, else per instance.
	    mdFinal                     =   0x0020,     // Method may not be overridden.
	    mdVirtual                   =   0x0040,     // Method virtual.
	    mdHideBySig                 =   0x0080,     // Method hides by name+sig, else just by name.
	
	    // vtable layout mask - Use this mask to retrieve vtable attributes.
	    mdVtableLayoutMask          =   0x0100,
	    mdReuseSlot                 =   0x0000,     // The default.
	    mdNewSlot                   =   0x0100,     // Method always gets a new slot in the vtable.
	    // end vtable layout mask
	
	    // method implementation attributes.
	    mdCheckAccessOnOverride     =   0x0200,     // Overridability is the same as the visibility.
	    mdAbstract                  =   0x0400,     // Method does not provide an implementation.
	    mdSpecialName               =   0x0800,     // Method is special.  Name describes how.
	    
	    // interop attributes
	    mdPinvokeImpl               =   0x2000,     // Implementation is forwarded through pinvoke.
	    mdUnmanagedExport           =   0x0008,     // Managed method exported via thunk to unmanaged code.
	
	    // Reserved flags for runtime use only.
	    mdReservedMask              =   0xd000,
	    mdRTSpecialName             =   0x1000,     // Runtime should check name encoding.
	    mdHasSecurity               =   0x4000,     // Method has security associate with it.
	    mdRequireSecObject          =   0x8000,     // Method calls another method containing security code.
	
	}
	public enum CorTokenType: uint
	{
		Module               = 0x00000000,       //          
		TypeRef              = 0x01000000,       //          
		TypeDef              = 0x02000000,       //          
		FieldDef             = 0x04000000,       //           
		MethodDef            = 0x06000000,       //       
		ParamDef             = 0x08000000,       //           
		InterfaceImpl        = 0x09000000,       //  
		MemberRef            = 0x0a000000,       //       
		CustomAttribute      = 0x0c000000,       //      
		Permission           = 0x0e000000,       //       
		Signature            = 0x11000000,       //       
		Event                = 0x14000000,       //           
		Property             = 0x17000000,       //           
		ModuleRef            = 0x1a000000,       //       
		TypeSpec             = 0x1b000000,       //           
		Assembly             = 0x20000000,       //
		AssemblyRef          = 0x23000000,       //
		File                 = 0x26000000,       //
		ExportedType         = 0x27000000,       //
		ManifestResource     = 0x28000000,       //
		
		String               = 0x70000000,       //          
		Name                 = 0x71000000,       //
		BaseType             = 0x72000000,       // Leave this on the high end value. This does not correspond to metadata table
	}

    [ComImport, InterfaceType((short) 1), Guid("7DAC8207-D3AE-4C75-9B67-92801A497D44"), ComConversionLoss]
    public interface IMetaDataImport
    {
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void CloseEnum(IntPtr hEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void CountEnum(IntPtr hEnum, out uint pulCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void ResetEnum(IntPtr hEnum, uint ulPos);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumTypeDefs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rTypeDefs, uint cMax, out uint pcTypeDefs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumInterfaceImpls(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rImpls, uint cMax, out uint pcImpls);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumTypeRefs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rTypeRefs, uint cMax, out uint pcTypeRefs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindTypeDefByName([In, MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, uint tkEnclosingClass, out uint ptd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetScopeProps(IntPtr szName, uint cchName, out uint pchName, out Guid pmvid);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetModuleFromScope(out uint pmd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetTypeDefProps(uint td, IntPtr szTypeDef, uint cchTypeDef, out uint pchTypeDef, out uint pdwTypeDefFlags, out uint ptkExtends);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetInterfaceImplProps(uint iiImpl, out uint pClass, out uint ptkIface);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetTypeRefProps(uint tr, out uint ptkResolutionScope, IntPtr szName, uint cchName, out uint pchName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppIScope, out uint ptd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMembers(ref IntPtr phEnum, uint cl, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMembers, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMembersWithName(ref IntPtr phEnum, uint cl, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMembers, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethods(ref IntPtr phEnum, uint cl, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethods, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethodsWithName(ref IntPtr phEnum, uint cl, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethods, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumFields(ref IntPtr phEnum, uint cl, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rFields, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumFieldsWithName(ref IntPtr phEnum, uint cl, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rFields, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumParams(ref IntPtr phEnum, uint mb, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rParams, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMemberRefs(ref IntPtr phEnum, uint tkParent, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMemberRefs, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethodImpls(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethodBody, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethodDecl, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumPermissionSets(ref IntPtr phEnum, uint tk, uint dwActions, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rPermission, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindMember(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindMethod(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindField(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindMemberRef(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMethodProps(uint mb, out uint pClass, IntPtr szMethod, uint cchMethod, out uint pchMethod, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMemberRefProps(uint mr, out uint ptk, IntPtr szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob, out uint pbSig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumProperties(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rProperties, uint cMax, out uint pcProperties);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumEvents(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rEvents, uint cMax, out uint pcEvents);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetEventProps(uint ev, out uint pClass, IntPtr  szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethodSemantics(ref IntPtr phEnum, uint mb, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rEventProp, uint cMax, out uint pcEventProp);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMethodSemantics(uint mb, uint tkEventProp, out uint pdwSemanticsFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetClassLayout(uint td, out uint pdwPackSize, [Out] COR_FIELD_OFFSET[] rFieldOffset, uint cMax, out uint pcFieldOffset, out uint pulClassSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetFieldMarshal(uint tk, out IntPtr ppvNativeType, out uint pcbNativeType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission, out uint pcbPermission);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetSigFromToken(uint mdSig, out IntPtr ppvSig, out uint pcbSig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetModuleRefProps(uint mur, IntPtr szName, uint cchName, out uint pchName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumModuleRefs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rModuleRefs, uint cMax, out uint pcModuleRefs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig, out uint pcbSig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetNameFromToken(uint tk, IntPtr pszUtf8NamePtr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumUnresolvedMethods(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethods, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetUserString(uint stk, IntPtr szString, uint cchString, out uint pchString);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetPinvokeMap(uint tk, out uint pdwMappingFlags, IntPtr szImportName, uint cchImportName, out uint pchImportName, out uint pmrImportDLL);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumSignatures(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rSignatures, uint cMax, out uint pcSignatures);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumTypeSpecs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rTypeSpecs, uint cMax, out uint pcTypeSpecs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumUserStrings(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rStrings, uint cMax, out uint pcStrings);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetParamForMethodIndex(uint md, uint ulParamSeq, out uint ppd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumCustomAttributes(ref IntPtr phEnum, uint tk, uint tkType, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rCustomAttributes, uint cMax, out uint pcCustomAttributes);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindTypeRef(uint tkResolutionScope, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, out uint ptr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetFieldProps(uint mb, out uint pClass, [In] IntPtr szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetPropertyProps(uint prop, out uint pClass, IntPtr szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetParamProps(uint tk, out uint pmd, out uint pulSequence, IntPtr szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetCustomAttributeByName(uint tkObj, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, out IntPtr ppData, out uint pcbData);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsValidToken(uint tk);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetNestedClassProps(uint tdNestedClass, out uint ptdEnclosingClass);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetNativeCallConvFromSig([In] IntPtr pvSig, uint cbSig, out uint pCallConv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void IsGlobal(uint pd, out int pbGlobal);
        
        // IMetaDataImport2
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumGenericParams(ref IntPtr hEnum, uint tk, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rGenericParams, uint cMax, out uint pcGenericParams);    
        void GetGenericParamProps();
        void GetMethodSpecProps();
        void EnumGenericParamConstraints();
        void GetGenericParamConstraintProps();
        void GetPEKind();
        void GetVersionString();
        void EnumMethodSpecs();
    }
}

#pragma warning restore 108, 1591
