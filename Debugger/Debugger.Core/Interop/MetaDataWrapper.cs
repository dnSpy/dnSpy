// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

// Missing XML comment for publicly visible type or member
#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Debugger.Interop.MetaData;
using Debugger.Interop.CorDebug;
using Debugger.Interop.CorSym;

namespace Debugger.Interop.MetaData
{
	/// <summary>Wrapper for the unmanaged metadata API.</summary>
	/// <remarks>http://msdn.microsoft.com/en-us/library/ms230172.aspx</remarks>
	public class MetaDataImport
	{
		const int DefaultBufferSize = 8;
		// If buffer is too small, enlarge it
		const int BufferSizeMultiplier = 4;
		
		IMetaDataImport metaData;
		
		public MetaDataImport(ICorDebugModule pModule)
		{
			Guid guid = new Guid("{ 0x7dac8207, 0xd3ae, 0x4c75, { 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44 } }");
			metaData = (IMetaDataImport)pModule.GetMetaDataInterface(ref guid);
			TrackedComObjects.Track(metaData);
		}
		
		public ISymUnmanagedReader GetSymReader(string fullname, string searchPath)
		{
			try {
				ISymUnmanagedBinder symBinder = new Debugger.Interop.CorSym.CorSymBinder_SxSClass();
				TrackedComObjects.Track(symBinder);
				return symBinder.GetReaderForFile(metaData, fullname, searchPath);
			} catch (COMException) {
				return null;
			}
		}
		
		public ISymUnmanagedReader GetSymReader(IStream stream)
		{
			try {
				ISymUnmanagedBinder symBinder = new Debugger.Interop.CorSym.CorSymBinder_SxSClass();
				TrackedComObjects.Track(symBinder);
				return symBinder.GetReaderFromStream(metaData, stream);
			} catch (COMException) {
				return null;
			}
		}
		
		// CloseEnum, CountEnum and ResetEnum are not wrapped, use them directly
		// GetNameFromToken is obsolete
		
		public uint[] EnumCustomAttributes(uint token_Scope, uint token_TypeOfAttributes)
		{
			return EnumerateTokens(metaData.EnumCustomAttributes, token_Scope, token_TypeOfAttributes);
		}
		
		public IEnumerable<CustomAttributeProps> EnumCustomAttributeProps(uint token_Scope, uint token_TypeOfAttributes)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumCustomAttributes, token_Scope, token_TypeOfAttributes)) {
				yield return GetCustomAttributeProps(token);
			}
		}
		
		public uint[] EnumEvents(uint typeDef)
		{
			return EnumerateTokens(metaData.EnumEvents, typeDef);
		}
		
		public IEnumerable<EventProps> EnumEventProps(uint typeDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumEvents, typeDef)) {
				yield return GetEventProps(token);
			}
		}
		
		public uint[] EnumFields(uint typeDef)
		{
			return EnumerateTokens(metaData.EnumFields, typeDef);
		}
		
		public IEnumerable<FieldProps> EnumFieldProps(uint typeDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumFields, typeDef)) {
				yield return GetFieldProps(token);
			}
		}
		
		public uint[] EnumFieldsWithName(uint typeDef, string name)
		{
			return EnumerateTokens(metaData.EnumFieldsWithName, typeDef, name);
		}
		
		public IEnumerable<FieldProps> EnumFieldPropsWithName(uint typeDef, string name)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumFieldsWithName, typeDef, name)) {
				yield return GetFieldProps(token);
			}
		}
		
		public uint[] EnumGenericParams(uint typeDef_methodDef)
		{
			return EnumerateTokens(metaData.EnumGenericParams, typeDef_methodDef);
		}
		
		/// <returns>MethodDef tokens</returns>
		public uint[] EnumInterfaceImpls(uint typeDef)
		{
			return EnumerateTokens(metaData.EnumInterfaceImpls, typeDef);
		}
		
		public IEnumerable<InterfaceImplProps> EnumInterfaceImplProps(uint typeDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumInterfaceImpls, typeDef)) {
				yield return GetInterfaceImplProps(token);
			}
		}
		
		public uint[] EnumMemberRefs(uint typeDef_typeRef_methodDef_moduleRef)
		{
			return EnumerateTokens(metaData.EnumMemberRefs, typeDef_typeRef_methodDef_moduleRef);
		}
		
		public IEnumerable<MemberRefProps> EnumMemberRefProps(uint typeDef_typeRef_methodDef_moduleRef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumMemberRefs, typeDef_typeRef_methodDef_moduleRef)) {
				yield return GetMemberRefProps(token);
			}
		}
		
		public uint[] EnumMembers(uint typeDef)
		{
			return EnumerateTokens(metaData.EnumMembers, typeDef);
		}
		
		public IEnumerable<MemberProps> EnumMemberProps(uint typeDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumMembers, typeDef)) {
				yield return GetMemberProps(token);
			}
		}
		
		public uint[] EnumMembersWithName(uint typeDef, string name)
		{
			return EnumerateTokens(metaData.EnumMembersWithName, typeDef, name);
		}
		
		public IEnumerable<MemberProps> EnumMemberPropsWithName(uint typeDef, string name)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumMembersWithName, typeDef, name)) {
				yield return GetMemberProps(token);
			}
		}
		
		/// <returns>MethodBody and MethodDeclaration tokens</returns>
		public IEnumerable<MethodImpl> EnumMethodImpls(uint typeDef)
		{
			IntPtr enumerator = IntPtr.Zero;
			while(true) {
				MethodImpl ret = new MethodImpl();
				uint[] body = new uint[1];
				uint[] decl = new uint[1];
				uint fetched;
				metaData.EnumMethodImpls(ref enumerator, typeDef, body, decl, 1, out fetched);
				if (fetched == 0)
					break;
				ret.MethodBody = body[0];
				ret.MethodDecl = decl[0];
				yield return ret;
			}
			metaData.CloseEnum(enumerator);
		}
		
		public uint[] EnumMethods(uint typeDef)
		{
			return EnumerateTokens(metaData.EnumMethods, typeDef);
		}
		
		public IEnumerable<MethodProps> EnumMethodProps(uint typeDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumMethods, typeDef)) {
				yield return GetMethodProps(token);
			}
		}
		
		/// <returns>Events or properties</returns>
		public uint[] EnumMethodSemantics(uint methodDef)
		{
			return EnumerateTokens(metaData.EnumMethodSemantics, methodDef);
		}
		
		public uint[] EnumMethodsWithName(uint typeDef, string name)
		{
			return EnumerateTokens(metaData.EnumMethodsWithName, typeDef, name);
		}
		
		public IEnumerable<MethodProps> EnumMethodPropsWithName(uint typeDef, string name)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumMethodsWithName, typeDef, name)) {
				yield return GetMethodProps(token);
			}
		}
		
		public uint[] EnumModuleRefs()
		{
			return EnumerateTokens(metaData.EnumModuleRefs);
		}
		
		public IEnumerable<ModuleRefProps> EnumModuleRefProps()
		{
			foreach(uint token in EnumerateTokens(metaData.EnumModuleRefs)) {
				yield return GetModuleRefProps(token);
			}
		}
		
		public uint[] EnumParams(uint methodDef)
		{
			return EnumerateTokens(metaData.EnumParams, methodDef);
		}
		
		public IEnumerable<ParamProps> EnumParamProps(uint methodDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumParams, methodDef)) {
				yield return GetParamProps(token);
			}
		}
		
		public uint[] EnumPermissionSets(uint token_scope_nullable, uint actions)
		{
			return EnumerateTokens(metaData.EnumPermissionSets, token_scope_nullable, actions);
		}
		
		public IEnumerable<PermissionSetProps> EnumPermissionSetProps(uint token_scope_nullable, uint actions)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumPermissionSets, token_scope_nullable, actions)) {
				yield return GetPermissionSetProps(token);
			}
		}
		
		public uint[] EnumProperties(uint typeDef)
		{
			return EnumerateTokens(metaData.EnumProperties, typeDef);
		}
		
		public IEnumerable<PropertyProps> EnumPropertyProps(uint typeDef)
		{
			foreach(uint token in EnumerateTokens(metaData.EnumProperties, typeDef)) {
				yield return GetPropertyProps(token);
			}
		}
		
		public uint[] EnumSignatures()
		{
			return EnumerateTokens(metaData.EnumSignatures);
		}
		
		public uint[] EnumTypeDefs()
		{
			return EnumerateTokens(metaData.EnumTypeDefs);
		}
		
		public IEnumerable<TypeDefProps> EnumTypeDefProps()
		{
			foreach(uint token in EnumerateTokens(metaData.EnumTypeDefs)) {
				yield return GetTypeDefProps(token);
			}
		}
		
		public uint[] EnumTypeRefs()
		{
			return EnumerateTokens(metaData.EnumTypeRefs);
		}
		
		public IEnumerable<TypeRefProps> EnumTypeRefProps()
		{
			foreach(uint token in EnumerateTokens(metaData.EnumTypeRefs)) {
				yield return GetTypeRefProps(token);
			}
		}
		
		public uint[] EnumTypeSpecs()
		{
			return EnumerateTokens(metaData.EnumTypeSpecs);
		}
		
		public IEnumerable<Blob> EnumTypeSpecBlobs()
		{
			foreach(uint token in EnumerateTokens(metaData.EnumTypeSpecs)) {
				yield return GetTypeSpecFromToken(token);
			}
		}
		
		public uint[] EnumUnresolvedMethods()
		{
			return EnumerateTokens(metaData.EnumUnresolvedMethods);
		}
		
		public uint[] EnumUserStrings()
		{
			return EnumerateTokens(metaData.EnumUserStrings);
		}
		
		public IEnumerable<string> EnumUserStringProps()
		{
			foreach(uint token in EnumerateTokens(metaData.EnumUserStrings)) {
				yield return GetUserString(token);
			}
		}
		
		public uint FindField(uint typeDef_nullable, string name, Blob sigBlob)
		{
			sigBlob = sigBlob ?? Blob.Empty;
			uint fieldDef;
			metaData.FindField(typeDef_nullable, name, sigBlob.Adress, sigBlob.Size, out fieldDef);
			return fieldDef;
		}
		
		public FieldProps FindFieldProps(uint typeDef_nullable, string name, Blob sigBlob)
		{
			return GetFieldProps(FindField(typeDef_nullable, name, sigBlob));
		}
		
		public uint FindMember(uint typeDef_nullable, string name, Blob sigBlob)
		{
			sigBlob = sigBlob ?? Blob.Empty;
			uint memberDef;
			metaData.FindMember(typeDef_nullable, name, sigBlob.Adress, sigBlob.Size, out memberDef);
			return memberDef;
		}
		
		public MemberProps FindMemberProps(uint typeDef_nullable, string name, Blob sigBlob)
		{
			return GetMemberProps(FindMember(typeDef_nullable, name, sigBlob));
		}
		
		public uint FindMemberRef(uint typeRef_nullable, string name, Blob sigBlob)
		{
			sigBlob = sigBlob ?? Blob.Empty;
			uint memberRef;
			metaData.FindMemberRef(typeRef_nullable, name, sigBlob.Adress, sigBlob.Size, out memberRef);
			return memberRef;
		}
		
		public MemberRefProps FindMemberRefProps(uint typeRef_nullable, string name, Blob sigBlob)
		{
			return GetMemberRefProps(FindMemberRef(typeRef_nullable, name, sigBlob));
		}
		
		public uint FindMethod(uint typeDef_nullable, string name, Blob sigBlob)
		{
			sigBlob = sigBlob ?? Blob.Empty;
			uint methodDef;
			metaData.FindMethod(typeDef_nullable, name, sigBlob.Adress, sigBlob.Size, out methodDef);
			return methodDef;
		}
		
		public MethodProps FindMethodProps(uint typeDef_nullable, string name, Blob sigBlob)
		{
			return GetMethodProps(FindMethod(typeDef_nullable, name, sigBlob));
		}
		
		public uint FindTypeDefByName(string name, uint typeDef_typeRef_enclosingClass_nullable)
		{
			uint typeDef;
			metaData.FindTypeDefByName(name, typeDef_typeRef_enclosingClass_nullable, out typeDef);
			return typeDef;
		}
		
		public TypeDefProps FindTypeDefPropsByName(string name, uint typeDef_typeRef_enclosingClass_nullable)
		{
			return GetTypeDefProps(FindTypeDefByName(name, typeDef_typeRef_enclosingClass_nullable));
		}
		
		public uint FindTypeRef(uint moduleRef_assemblyRef_typeRef_scope, string name)
		{
			uint typeRef;
			metaData.FindTypeRef(moduleRef_assemblyRef_typeRef_scope, name, out typeRef);
			return typeRef;
		}
		
		public TypeRefProps FindTypeRefProps(uint moduleRef_assemblyRef_typeRef_scope, string name)
		{
			return GetTypeRefProps(FindTypeRef(moduleRef_assemblyRef_typeRef_scope, name));
		}
		
		public ClassLayout GetClassLayout(uint typeDef)
		{
			ClassLayout ret = new ClassLayout();
			ret.TypeDef = typeDef;
			ret.FieldOffsets = new COR_FIELD_OFFSET[DefaultBufferSize];
			uint returned;
			while (true) {
				metaData.GetClassLayout(
					ret.TypeDef,
					out ret.PackSize,
					ret.FieldOffsets, (uint)ret.FieldOffsets.Length, out returned,
					out ret.ClassSize
				);
				if (returned < ret.FieldOffsets.Length) break;
				ret.FieldOffsets = new COR_FIELD_OFFSET[ret.FieldOffsets.Length * BufferSizeMultiplier];
			}
			Array.Resize(ref ret.FieldOffsets, (int)returned);
			return ret;
		}
		
		public Blob GetCustomAttributeByName(uint token_owner, string name)
		{
			IntPtr blobPtr;
			uint blobSize;
			metaData.GetCustomAttributeByName(
				token_owner,
				name,
				out blobPtr,
				out blobSize
			);
			return new Blob(blobPtr, blobSize);
		}
		
		public CustomAttributeProps GetCustomAttributeProps(uint token)
		{
			IntPtr blobPtr;
			uint blobSize;
			CustomAttributeProps ret = new CustomAttributeProps();
			ret.Token = token;
			metaData.GetCustomAttributeProps(
				ret.Token,
				out ret.Owner,
				out ret.Type,
				out blobPtr,
				out blobSize
			);
			ret.Data = new Blob(blobPtr, blobSize);
			return ret;
		}
		
		public EventProps GetEventProps(uint eventToken)
		{
			EventProps ret = new EventProps();
			ret.Event = eventToken;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				ret.OtherMethods = new uint[DefaultBufferSize];
				uint returned;
				while(true) {
					metaData.GetEventProps(
						ret.Event,
						out ret.DeclaringClass,
						pString, pStringLenght, out stringLenght,
						out ret.Flags,
						out ret.EventType,
						out ret.AddMethod,
						out ret.RemoveMethod,
						out ret.FireMethod,
						ret.OtherMethods, (uint)ret.OtherMethods.Length, out returned
					);
					if (returned < ret.OtherMethods.Length) break;
					ret.OtherMethods = new uint[ret.OtherMethods.Length * BufferSizeMultiplier];
				}
				Array.Resize(ref ret.OtherMethods, (int)returned);
			});
			return ret;
		}
		
		public Blob GetFieldMarshal(uint token)
		{
			IntPtr blobPtr;
			uint blobSize;
			metaData.GetFieldMarshal(
				token,
				out blobPtr,
				out blobSize
			);
			return new Blob(blobPtr, blobSize);
		}
		
		public FieldProps GetFieldProps(uint fieldDef)
		{
			FieldProps ret = new FieldProps();
			IntPtr sigPtr = IntPtr.Zero;
			uint sigSize = 0;
			ret.Token = fieldDef;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetFieldProps(
					ret.Token,
					out ret.DeclaringClass,
					pString, pStringLenght, out stringLenght,
					out ret.Flags,
					out sigPtr,
					out sigSize,
					out ret.ConstantType,
					out ret.ConstantPtr,
					out ret.ConstantStringLength
				);
			});
			ret.SigBlob = new Blob(sigPtr, sigSize);
			return ret;
		}
		
		public InterfaceImplProps GetInterfaceImplProps(uint method)
		{
			InterfaceImplProps ret = new InterfaceImplProps();
			ret.Method = method;
			metaData.GetInterfaceImplProps(
				ret.Method,
				out ret.Class,
				out ret.Interface
			);
			return ret;
		}
		
		public MemberProps GetMemberProps(uint token)
		{
			MemberProps ret = new MemberProps();
			IntPtr sigPtr = IntPtr.Zero;
			uint sigSize = 0;
			ret.Token = token;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetMemberProps(
					ret.Token,
					out ret.Class,
					pString, pStringLenght, out stringLenght,
					out ret.Flags,
					out sigPtr,
					out sigSize,
					out ret.RVA,
					out ret.ImplFlags,
					out ret.ConstantType,
					out ret.ConstantPtr,
					out ret.ConstantStringLength
				);
			});
			ret.SigBlob = new Blob(sigPtr, sigSize);
			return ret;
		}
		
		public MemberRefProps GetMemberRefProps(uint token)
		{
			MemberRefProps ret = new MemberRefProps();
			IntPtr sigPtr = IntPtr.Zero;
			uint sigSize = 0;
			ret.Token = token;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetMemberRefProps(
					token,
					out ret.DeclaringType,
					pString, pStringLenght, out stringLenght,
					out sigPtr,
					out sigSize
				);
			});
			ret.SigBlob = new Blob(sigPtr, sigSize);
			return ret;
		}
		
		public MethodProps GetMethodProps(uint methodToken)
		{
			MethodProps ret = new MethodProps();
			IntPtr sigPtr = IntPtr.Zero;
			uint sigSize = 0;
			ret.Token = methodToken;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetMethodProps(
					ret.Token,
					out ret.ClassToken,
					pString, pStringLenght, out stringLenght,
					out ret.Flags,
					out sigPtr,
					out sigSize,
					out ret.CodeRVA,
					out ret.ImplFlags
				);
			});
			ret.SigBlob = new Blob(sigPtr, sigSize);
			return ret;
		}
		
		public uint GetMethodSemantics(uint methodDef, uint event_property)
		{
			uint semFlags;
			metaData.GetMethodSemantics(methodDef, event_property, out semFlags);
			return semFlags;
		}
		
		public uint GetModuleFromScope()
		{
			uint module;
			metaData.GetModuleFromScope(out module);
			return module;
		}
		
		public ModuleRefProps GetModuleRefProps(uint token)
		{
			ModuleRefProps ret = new ModuleRefProps();
			ret.Token = token;
			ret.Name =  Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetModuleRefProps(
					token,
					pString, pStringLenght, out stringLenght
				);
			});
			return ret;
		}
		
		public uint GetNativeCallConvFromSig(Blob sigBlob)
		{
			uint callConv;
			metaData.GetNativeCallConvFromSig(sigBlob.Adress, sigBlob.Size, out callConv);
			return callConv;
		}
		
		public NestedClassProps GetNestedClassProps(uint nestedClass)
		{
			NestedClassProps ret = new NestedClassProps();
			ret.NestedClass = nestedClass;
			metaData.GetNestedClassProps(
				ret.NestedClass,
				out ret.EnclosingClass
			);
			return ret;
		}
		
		public uint GetParamForMethodIndex(uint methodToken, uint parameterSequence)
		{
			uint paramToken;
			metaData.GetParamForMethodIndex(methodToken, parameterSequence, out paramToken);
			return paramToken;
		}
		
		public ParamProps GetParamPropsForMethodIndex(uint methodToken, uint parameterSequence)
		{
			return GetParamProps(GetParamForMethodIndex(methodToken, parameterSequence));
		}
		
		public ParamProps GetParamProps(uint paramToken)
		{
			ParamProps ret = new ParamProps();
			ret.ParamDef = paramToken;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetParamProps(
					ret.ParamDef,
					out ret.MethodDef,
					out ret.Sequence,
					pString, pStringLenght, out stringLenght,
					out ret.Flags,
					out ret.ConstantType,
					out ret.ConstantPtr,
					out ret.ConstantStringLength
				);
			});
			return ret;
		}
		
		public PermissionSetProps GetPermissionSetProps(uint permToken)
		{
			PermissionSetProps ret = new PermissionSetProps();
			IntPtr permPtr;
			uint permSize;
			ret.PermToken = permToken;
			metaData.GetPermissionSetProps(
				ret.PermToken,
				out ret.Action,
				out permPtr,
				out permSize
			);
			ret.SigBlob = new Blob(permPtr, permSize);
			return ret;
		}
		
		public PinvokeMap GetPinvokeMap(uint fieldDef_methodDef)
		{
			PinvokeMap ret = new PinvokeMap();
			ret.Token = fieldDef_methodDef;
			ret.ImportName = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetPinvokeMap(
					ret.Token,
					out ret.Flags,
					pString, pStringLenght, out stringLenght,
					out ret.ModuleRef
				);
			});
			return ret;
		}
		
		public PropertyProps GetPropertyProps(uint prop)
		{
			PropertyProps ret = new PropertyProps();
			IntPtr sigPtr = IntPtr.Zero;
			uint sigSize = 0;
			ret.Propery = prop;
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				ret.OtherMethods = new uint[DefaultBufferSize];
				uint returned;
				while(true) {
					metaData.GetPropertyProps(
						ret.Propery,
						out ret.DeclaringClass,
						pString, pStringLenght, out stringLenght,
						out ret.Flags,
						out sigPtr,
						out sigSize,
						out ret.DefaultValueType,
						out ret.DefaultValuePtr,
						out ret.DefaultValueStringLength,
						out ret.SetterMethod,
						out ret.GetterMethod,
						ret.OtherMethods, (uint)ret.OtherMethods.Length, out returned
					);
					if (returned < ret.OtherMethods.Length) break;
					ret.OtherMethods = new uint[ret.OtherMethods.Length * BufferSizeMultiplier];
				}
				Array.Resize(ref ret.OtherMethods, (int)returned);
			});
			ret.SigBlob = new Blob(sigPtr, sigSize);
			return ret;
		}
		
		public RVA GetRVA(uint methodDef_fieldDef)
		{
			RVA ret = new RVA();
			ret.Token = methodDef_fieldDef;
			metaData.GetRVA(
				ret.Token,
				out ret.CodeRVA,
				out ret.ImplFlags
			);
			return ret;
		}
		
		public ScopeProps GetScopeProps()
		{
			ScopeProps ret = new ScopeProps();
			ret.Name = Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetScopeProps(
					pString, pStringLenght, out stringLenght,
					out ret.Guid
				);
			});
			return ret;
		}
		
		public Blob GetSigFromToken(uint token)
		{
			IntPtr sigPtr;
			uint sigSize;
			metaData.GetSigFromToken(
				token,
				out sigPtr,
				out sigSize
			);
			return new Blob(sigPtr, sigSize);
		}
		
		public TypeDefProps GetTypeDefProps(uint typeDef)
		{
			TypeDefProps ret = new TypeDefProps();
			ret.Token = typeDef;
			ret.Name = 
				Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
					metaData.GetTypeDefProps(
						ret.Token,
						pString, pStringLenght, out stringLenght,
						out ret.Flags,
						out ret.SuperClassToken
					);
				});
			
			return ret;
		}
		
		public TypeRefProps GetTypeRefProps(uint typeRef)
		{
			TypeRefProps ret = new TypeRefProps();
			ret.TypeRef = typeRef;
			ret.Name =
				Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
					metaData.GetTypeRefProps(
						ret.TypeRef,
						out ret.ResolutionScope,
						pString, pStringLenght, out stringLenght
					);
				});
			
			return ret;
		}
		
		public Blob GetTypeSpecFromToken(uint typeSpec)
		{
			IntPtr sigPtr;
			uint sigSize;
			metaData.GetTypeSpecFromToken(
				typeSpec,
				out sigPtr,
				out sigSize
			);
			return new Blob(sigPtr, sigSize);
		}
		
		public string GetUserString(uint stringToken)
		{
			return Util.GetString(delegate(uint pStringLenght, out uint stringLenght, System.IntPtr pString) {
				metaData.GetUserString(
					stringToken,
					pString, pStringLenght, out stringLenght
				);
			});
		}
		
		public bool IsGlobal(uint token)
		{
			int isGlobal;
			metaData.IsGlobal(token, out isGlobal);
			return isGlobal != 0;
		}
		
		public bool IsValidToken(uint token)
		{
			return metaData.IsValidToken(token) != 0;
		}
		
		public ResolvedTypeRef ResolveTypeRef(uint typeRef, Guid riid)
		{
			ResolvedTypeRef res = new ResolvedTypeRef();
			metaData.ResolveTypeRef(
				typeRef,
				ref riid,
				out res.Scope,
				out res.TypeDef
			);
			return res;
		}
		
		#region Util
		
		delegate void TokenEnumerator0(ref IntPtr phEnum, uint[] token, uint maxCount, out uint fetched);
		
		uint[] EnumerateTokens(TokenEnumerator0 tokenEnumerator)
		{
			IntPtr enumerator = IntPtr.Zero;
			uint[] buffer = new uint[DefaultBufferSize];
			uint fetched;
			tokenEnumerator(ref enumerator, buffer, (uint)buffer.Length, out fetched);
			if (fetched < buffer.Length) {
				// The tokens did fit the buffer
				Array.Resize(ref buffer, (int)fetched);
			} else {
				// The tokens did not fit the buffer -> Refetch
				uint actualCount;
				metaData.CountEnum(enumerator, out actualCount);
				if (actualCount > buffer.Length) {
					buffer = new uint[actualCount];
					metaData.ResetEnum(enumerator, 0);
					tokenEnumerator(ref enumerator, buffer, (uint)buffer.Length, out fetched);
				}
			}
			metaData.CloseEnum(enumerator);
			return buffer;
		}
		
		delegate void TokenEnumerator1<T>(ref IntPtr phEnum, T parameter, uint[] token, uint maxCount, out uint fetched);
		
		uint[] EnumerateTokens<T>(TokenEnumerator1<T> tokenEnumerator, T parameter)
		{
			IntPtr enumerator = IntPtr.Zero;
			uint[] buffer = new uint[DefaultBufferSize];
			uint fetched;
			tokenEnumerator(ref enumerator, parameter, buffer, (uint)buffer.Length, out fetched);
			if (fetched < buffer.Length) {
				// The tokens did fit the buffer
				Array.Resize(ref buffer, (int)fetched);
			} else {
				// The tokens did not fit the buffer -> Refetch
				uint actualCount;
				metaData.CountEnum(enumerator, out actualCount);
				if (actualCount > buffer.Length) {
					buffer = new uint[actualCount];
					metaData.ResetEnum(enumerator, 0);
					tokenEnumerator(ref enumerator, parameter, buffer, (uint)buffer.Length, out fetched);
				}
			}
			metaData.CloseEnum(enumerator);
			return buffer;
		}
		
		delegate void TokenEnumerator2<T,R>(ref IntPtr phEnum, T parameter1, R parameter2, uint[] token, uint maxCount, out uint fetched);
		
		uint[] EnumerateTokens<T,R>(TokenEnumerator2<T,R> tokenEnumerator, T parameter1, R parameter2)
		{
			IntPtr enumerator = IntPtr.Zero;
			uint[] buffer = new uint[DefaultBufferSize];
			uint fetched;
			tokenEnumerator(ref enumerator, parameter1, parameter2, buffer, (uint)buffer.Length, out fetched);
			if (fetched < buffer.Length) {
				// The tokens did fit the buffer
				Array.Resize(ref buffer, (int)fetched);
			} else {
				// The tokens did not fit the buffer -> Refetch
				uint actualCount;
				metaData.CountEnum(enumerator, out actualCount);
				if (actualCount > buffer.Length) {
					buffer = new uint[actualCount];
					metaData.ResetEnum(enumerator, 0);
					tokenEnumerator(ref enumerator, parameter1, parameter2, buffer, (uint)buffer.Length, out fetched);
				}
			}
			metaData.CloseEnum(enumerator);
			return buffer;
		}
		
		#endregion
	}
	
	public class Blob
	{
		public static readonly Blob Empty = new Blob(IntPtr.Zero, 0);
		
		IntPtr adress;
		uint size;
		
		public IntPtr Adress {
			get { return adress; }
		}
		
		public uint Size {
			get { return size; }
		}
		
		public Blob(IntPtr adress, uint size)
		{
			this.adress = adress;
			this.size = size;
		}
		
		public byte[] GetData()
		{
			byte[] data = new byte[size];
			Marshal.Copy(adress, data, 0, (int)size);
			return data;
		}
	}
	
	public class ClassLayout
	{
		public uint TypeDef;
		public uint PackSize;
		public COR_FIELD_OFFSET[] FieldOffsets;
		public uint ClassSize;
	}
	
	public class CustomAttributeProps
	{
		public uint Token;
		public uint Owner;
		public uint Type;
		public Blob Data;
	}
	
	public class EventProps
	{
		public uint Event;
		public uint DeclaringClass;
		public string Name;
		public uint Flags;
		public uint EventType;
		public uint AddMethod;
		public uint RemoveMethod;
		public uint FireMethod;
		public uint[] OtherMethods;
	}
	
	public class FieldProps
	{
		public uint Token;
		public string Name;
		public uint DeclaringClass;
		public uint Flags;
		public Blob SigBlob;
		public uint ConstantType;
		public IntPtr ConstantPtr;
		public uint ConstantStringLength;
	}
	
	public class InterfaceImplProps
	{
		public uint Method;
		public uint Class;
		public uint Interface;
	}
	
	public class MemberProps
	{
		public uint Token;
		public uint Class;
		public string Name;
		public uint Flags;
		public Blob SigBlob;
		public uint RVA;
		public uint ImplFlags;
		public uint ConstantType;
		public IntPtr ConstantPtr;
		public uint ConstantStringLength;
	}
	
	public class MemberRefProps
	{
		public uint Token;
		public uint DeclaringType;
		public string Name;
		public Blob SigBlob;
	}
	
	public class MethodImpl
	{
		public uint MethodBody;
		public uint MethodDecl;
	}
	
	public class MethodProps
	{
		public uint Token;
		public string Name;
		public uint ClassToken;
		public uint Flags;
		public Blob SigBlob;
		public uint CodeRVA;
		public uint ImplFlags;
	}
	
	public class ModuleRefProps
	{
		public uint Token;
		public string Name;
	}
	
	public class NestedClassProps
	{
		public uint NestedClass;
		public uint EnclosingClass;
	}
	
	public class ParamProps
	{
		public uint ParamDef;
		public uint MethodDef;
		public uint Sequence;
		public string Name;
		public uint Flags;
		public uint ConstantType;
		public IntPtr ConstantPtr;
		public uint ConstantStringLength;
	}
	
	public class PermissionSetProps
	{
		public uint PermToken;
		public uint Action;
		public Blob SigBlob;
	}
	
	public class PinvokeMap
	{
		public uint Token;
		public uint Flags;
		public string ImportName;
		public uint ModuleRef;
	}
	
	public class PropertyProps
	{
		public uint Propery;
		public uint DeclaringClass;
		public string Name;
		public uint Flags;
		public Blob SigBlob;
		public uint DefaultValueType;
		public IntPtr DefaultValuePtr;
		public uint DefaultValueStringLength;
		public uint SetterMethod;
		public uint GetterMethod;
		public uint[] OtherMethods;
	}
	
	public class ResolvedTypeRef
	{
		public object Scope;
		public uint TypeDef;
	}
	
	public class RVA
	{
		public uint Token;
		public uint CodeRVA;
		public uint ImplFlags;
	}
	
	public class ScopeProps
	{
		public string Name;
		public Guid Guid;
	}
	
	public class TypeDefProps
	{
		public uint Token;
		public string Name;
		public uint Flags;
		public uint SuperClassToken;
	}
	
	public class TypeRefProps
	{
		public uint TypeRef;
		public uint ResolutionScope;
		public string Name;
	}
}

#pragma warning restore 1591
