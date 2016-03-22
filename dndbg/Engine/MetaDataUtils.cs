/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

// This file contains helper methods that are used by the CorDebug classes. It uses MDAPI.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	public class CorFieldInfo {
		public CorType OwnerType;
		public uint Token;
		public string Name;
		public TypeSig FieldType;
		public FieldAttributes Attributes;
		public object Constant;
		public CorElementType ConstantType;
		public DebuggerBrowsableState? DebuggerBrowsableState;
		public bool CompilerGeneratedAttribute;

		public CorFieldInfo(CorType ownerType, uint token, string name, TypeSig fieldType, FieldAttributes attrs, object constant, CorElementType constantType, DebuggerBrowsableState? debuggerBrowsableState, bool compilerGeneratedAttribute) {
			this.OwnerType = ownerType;
			this.Token = token;
			this.Name = name;
			this.FieldType = fieldType;
			this.Attributes = attrs;
			this.Constant = constant;
			this.ConstantType = constantType;
			this.DebuggerBrowsableState = debuggerBrowsableState;
			this.CompilerGeneratedAttribute = compilerGeneratedAttribute;
		}

		public override string ToString() {
			return string.Format("{0:X8} {1} {2} {3}", Token, TypePrinterUtils.ToString(FieldType), Name, OwnerType);
		}
	}

	public class CorPropertyInfo {
		public CorType OwnerType;
		public uint Token;
		public uint GetToken;
		public uint SetToken;
		public string Name;
		public MethodSig GetSig;
		public MethodSig SetSig;
		public MethodAttributes GetMethodAttributes;
		public DebuggerBrowsableState? DebuggerBrowsableState;

		public CorPropertyInfo(CorType ownerType, uint token, uint getToken, uint setToken, string name, MethodSig getSig, MethodSig setSig, MethodAttributes getMethodAttributes, DebuggerBrowsableState? debuggerBrowsableState) {
			this.OwnerType = ownerType;
			this.Token = token;
			this.GetToken = getToken;
			this.SetToken = setToken;
			this.Name = name;
			this.GetSig = getSig;
			this.SetSig = setSig;
			this.GetMethodAttributes = getMethodAttributes;
			this.DebuggerBrowsableState = debuggerBrowsableState;
		}

		public override string ToString() {
			return string.Format("{0:X8} {1} {2}", Token, Name, OwnerType);
		}
	}

	public class CorMethodInfo {
		public CorType OwnerType;
		public uint Token;
		public string Name;
		public MethodSig MethodSig;
		public MethodAttributes MethodAttributes;
		public MethodImplAttributes MethodImplAttributes;
		public bool CompilerGeneratedAttribute;

		public CorMethodInfo(CorType ownerType, uint token, string name, MethodSig methodSig, MethodAttributes attrs, MethodImplAttributes implAttrs, bool compilerGeneratedAttribute) {
			this.OwnerType = ownerType;
			this.Token = token;
			this.Name = name;
			this.MethodSig = methodSig;
			this.MethodAttributes = attrs;
			this.MethodImplAttributes = implAttrs;
			this.CompilerGeneratedAttribute = compilerGeneratedAttribute;
		}
	}

	public struct TokenAndName {
		public readonly string Name;
		public readonly uint Token;

		public TokenAndName(string name, uint token) {
			this.Name = name;
			this.Token = token;
		}
	}

	public sealed class MDParameters {
		readonly Dictionary<uint, MDParamInfo> dict = new Dictionary<uint, MDParamInfo>();

		public void Add(MDParamInfo info) {
			if (dict.ContainsKey(info.Sequence))
				return;
			dict.Add(info.Sequence, info);
		}

		public MDParamInfo? Get(uint seq) {
			MDParamInfo info;
			if (dict.TryGetValue(seq, out info))
				return info;
			return null;
		}
	}

	public struct MDParamInfo {
		public readonly string Name;
		public readonly uint Token;
		public readonly uint Sequence;
		public readonly ParamAttributes Flags;

		public bool IsIn {
			get { return (Flags & ParamAttributes.In) != 0; }
		}

		public bool IsOut {
			get { return (Flags & ParamAttributes.Out) != 0; }
		}

		public MDParamInfo(string name, uint token, uint seq, ParamAttributes flags) {
			this.Name = name;
			this.Token = token;
			this.Sequence = seq;
			this.Flags = flags;
		}
	}

	struct MethodOverrideInfo {
		public readonly uint BodyToken;
		public readonly uint DeclToken;
		public MethodOverrideInfo(uint b, uint d) {
			this.BodyToken = b;
			this.DeclToken = d;
		}
	}

	static class MetaDataUtils {
		public static MDParameters GetParameters(IMetaDataImport mdi, uint token) {
			var ps = new MDParameters();

			var tokens = MDAPI.GetParamTokens(mdi, token);
			foreach (var ptok in tokens) {
				var info = GetParameterInfo(mdi, ptok);
				if (info != null)
					ps.Add(info.Value);
			}

			return ps;
		}

		unsafe static MDParamInfo? GetParameterInfo(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			uint seq;
			ParamAttributes attrs;
			if (!MDAPI.GetParamSeqAndAttrs(mdi, token, out seq, out attrs))
				return null;
			var name = MDAPI.GetParamName(mdi, token);
			if (name == null)
				return null;

			return new MDParamInfo(name, token, seq, attrs);
		}

		public static List<TokenAndName> GetTypeRefFullNames(IMetaDataImport mdi, uint token) {
			var list = new List<TokenAndName>(4);

			while ((token & 0x00FFFFFF) != 0) {
				var name = MDAPI.GetTypeRefName(mdi, token);
				if (name == null)
					break;
				list.Add(new TokenAndName(name, token));
				uint tkResolutionScope = MDAPI.GetTypeRefResolutionScope(mdi, token);
				if ((tkResolutionScope >> 24) != (int)Table.TypeRef)
					break;
				token = tkResolutionScope;
			}

			list.Reverse();
			return list;
		}

		public static List<TokenAndName> GetTypeDefFullNames(IMetaDataImport mdi, uint token) {
			var list = new List<TokenAndName>(4);

			while ((token & 0x00FFFFFF) != 0) {
				var name = MDAPI.GetTypeDefName(mdi, token);
				if (name == null)
					break;
				list.Add(new TokenAndName(name, token));
				token = MDAPI.GetTypeDefEnclosingType(mdi, token);
			}

			list.Reverse();
			return list;
		}

		public static int GetCountGenericParameters(IMetaDataImport mdi, uint token) {
			return MDAPI.GetGenericParamTokens(mdi as IMetaDataImport2, token).Length;
		}

		public static List<TokenAndName> GetGenericParameterNames(IMetaDataImport mdi, uint token) {
			var gpTokens = MDAPI.GetGenericParamTokens(mdi as IMetaDataImport2, token);
			var list = new List<TokenAndName>(gpTokens.Length);

			var mdi2 = mdi as IMetaDataImport2;
			if (mdi2 == null)
				return list;

			foreach (var gpTok in gpTokens) {
				var name = MDAPI.GetGenericParamName(mdi as IMetaDataImport2, gpTok);
				list.Add(new TokenAndName(name ?? string.Empty, gpTok));
			}

			return list;
		}

		public unsafe static MethodSig GetMethodSignature(IMetaDataImport mdi, uint token) {
			var sig = MDAPI.GetMethodSignatureBlob(mdi, token);
			if (sig == null)
				return null;
			return new DebugSignatureReader().ReadSignature(mdi, sig) as MethodSig;
		}

		public static unsafe CallingConventionSig ReadStandAloneSig(IMetaDataImport mdi, uint token) {
			var sig = MDAPI.GetStandAloneSigBlob(mdi, token);
			if (sig == null)
				return null;
			return new DebugSignatureReader().ReadSignature(mdi, sig);
		}

		public static unsafe FieldSig ReadFieldSig(IMetaDataImport mdi, uint token) {
			var sig = MDAPI.GetFieldSignatureBlob(mdi, token);
			if (sig == null)
				return null;
			return new DebugSignatureReader().ReadSignature(mdi, sig) as FieldSig;
		}

		public static unsafe PropertySig ReadPropertySig(IMetaDataImport mdi, uint token) {
			var sig = MDAPI.GetPropertySignatureBlob(mdi, token);
			if (sig == null)
				return null;
			return new DebugSignatureReader().ReadSignature(mdi, sig) as PropertySig;
		}

		public static uint GetGlobalStaticConstructor(IMetaDataImport mdi) {
			var mdTokens = MDAPI.GetMethodTokens(mdi, 0x02000001);
			foreach (uint mdToken in mdTokens) {
				string name = MDAPI.GetMethodName(mdi, mdToken);
				if (name != ".cctor")
					continue;
				MethodAttributes attrs;
				MethodImplAttributes implAttrs;
				if (!MDAPI.GetMethodAttributes(mdi, mdToken, out attrs, out implAttrs))
					continue;
				if ((attrs & MethodAttributes.RTSpecialName) == 0)
					continue;
				if ((attrs & MethodAttributes.Static) == 0)
					continue;

				return mdToken;
			}

			return 0;
		}

		public static List<TokenAndName> GetFields(IMetaDataImport mdi, uint token) {
			var fdTokens = MDAPI.GetFieldTokens(mdi, token);
			var list = new List<TokenAndName>(fdTokens.Length);

			foreach (var fdToken in fdTokens) {
				var name = MDAPI.GetFieldName(mdi, fdToken);
				if (name == null)
					continue;
				list.Add(new TokenAndName(name, fdToken));
			}

			return list;
		}

		static TypeSig GetFieldTypeSig(IMetaDataImport mdi, uint token) {
			var buf = MDAPI.GetFieldSignatureBlob(mdi, token);
			if (buf == null)
				return null;
			var sig = new DebugSignatureReader().ReadSignature(mdi, buf) as FieldSig;
			return sig == null ? null : sig.Type;
		}

		static DebuggerBrowsableState? GetDebuggerBrowsableState(IMetaDataImport mdi, uint token) {
			Debug.Assert(new MDToken(token).Table == Table.Field || new MDToken(token).Table == Table.Property);
			if (mdi == null)
				return null;

			var data = MDAPI.GetCustomAttributeByName(mdi, token, "System.Diagnostics.DebuggerBrowsableAttribute");
			const int expectedLength = 8;
			if (data == null || data.Length != expectedLength)
				return null;
			if (BitConverter.ToUInt16(data, 0) != 1)
				return null;
			var state = (DebuggerBrowsableState)BitConverter.ToInt32(data, 2);
			if (BitConverter.ToUInt16(data, 6) != 0)
				return null;

			return state;
		}

		static bool GetCompilerGeneratedAttribute(IMetaDataImport mdi, uint token) {
			return MDAPI.HasAttribute(mdi, token, "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
		}

		static CorFieldInfo ReadFieldInfo(IMetaDataImport mdi, uint token, CorType type) {
			if (mdi == null)
				return null;
			var name = MDAPI.GetFieldName(mdi, token);
			if (name == null)
				return null;
			var fieldType = GetFieldTypeSig(mdi, token);
			if (fieldType == null)
				return null;
			var attrs = MDAPI.GetFieldAttributes(mdi, token);
			CorElementType constantType;
			var constant = MDAPI.GetFieldConstant(mdi, token, out constantType);
			var browseState = GetDebuggerBrowsableState(mdi, token);
			bool compilerGeneratedAttribute = GetCompilerGeneratedAttribute(mdi, token);
			return new CorFieldInfo(type, token, name, fieldType, attrs, constant, constantType, browseState, compilerGeneratedAttribute);
		}

		public static IEnumerable<CorFieldInfo> GetFieldInfos(CorType type, bool checkBaseClasses = true) {
			for (; type != null; type = type.Base) {
				var cls = type.Class;
				var mod = cls == null ? null : cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				var fdTokens = MDAPI.GetFieldTokens(mdi, cls == null ? 0 : cls.Token);
				foreach (var fdToken in fdTokens) {
					var info = ReadFieldInfo(mdi, fdToken, type);
					Debug.Assert(info != null);
					if (info != null)
						yield return info;
				}
				if (!checkBaseClasses)
					break;
			}
		}

		public static IEnumerable<CorFieldInfo> GetFieldInfos(IMetaDataImport mdi, uint token) {
			var fdTokens = MDAPI.GetFieldTokens(mdi, token);
			foreach (var fdToken in fdTokens) {
				var info = ReadFieldInfo(mdi, fdToken, null);
				Debug.Assert(info != null);
				if (info != null)
					yield return info;
			}
		}

		unsafe static CorPropertyInfo ReadPropertyInfo(IMetaDataImport mdi, uint token, CorType type) {
			if (mdi == null)
				return null;

			var name = MDAPI.GetPropertyName(mdi, token);
			if (name == null)
				return null;
			uint mdSetter, mdGetter;
			if (!MDAPI.GetPropertyGetterSetter(mdi, token, out mdGetter, out mdSetter))
				return null;

			var getSig = GetMethodSignature(mdi, mdGetter);
			var setSig = GetMethodSignature(mdi, mdSetter);

			if (getSig == null)
				return null;
			if (getSig.ParamsAfterSentinel != null)
				return null;
			if (getSig.GenParamCount != 0)
				return null;
			if (getSig.Params.Count != 0)
				return null;
			if (getSig.RetType.RemovePinnedAndModifiers().GetElementType() == ElementType.Void)
				return null;

			if (setSig != null && setSig.ParamsAfterSentinel != null)
				setSig = null;
			if (setSig != null && setSig.GenParamCount != 0)
				setSig = null;
			if (setSig != null && setSig.Params.Count != 1)
				setSig = null;
			if (setSig != null && setSig.RetType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
				setSig = null;

			if (setSig != null && getSig.HasThis != setSig.HasThis)
				setSig = null;
			if (setSig != null && !Equals(getSig.RetType.RemovePinnedAndModifiers(), setSig.Params[0].RemovePinnedAndModifiers()))
				setSig = null;

			if (setSig == null)
				mdSetter = 0;

			MethodAttributes getMethodAttrs;
			MethodImplAttributes getMethodImplAttrs;
			if (!MDAPI.GetMethodAttributes(mdi, mdGetter, out getMethodAttrs, out getMethodImplAttrs))
				return null;

			var browseState = GetDebuggerBrowsableState(mdi, token);

			return new CorPropertyInfo(type, token, mdGetter, mdSetter, name, getSig, setSig, getMethodAttrs, browseState);
		}

		static bool Equals(TypeSig ts1, TypeSig ts2) {
			return new TypeComparer().Equals(ts1, ts2);
		}

		public static IEnumerable<CorPropertyInfo> GetProperties(CorType type, bool checkBaseClasses = true) {
			for (; type != null; type = type.Base) {
				uint token;
				var mdi = type.GetMetaDataImport(out token);
				var pdTokens = MDAPI.GetPropertyTokens(mdi, token);
				foreach (var pdToken in pdTokens) {
					var info = ReadPropertyInfo(mdi, pdToken, type);
					if (info != null)
						yield return info;
				}
				if (!checkBaseClasses)
					break;
			}
		}

		static CorMethodInfo ReadMethodInfo(IMetaDataImport mdi, uint token, CorType type) {
			if (mdi == null)
				return null;

			var name = MDAPI.GetMethodName(mdi, token);
			if (name == null)
				return null;

			MethodAttributes attrs;
			MethodImplAttributes implAttrs;
			if (!MDAPI.GetMethodAttributes(mdi, token, out attrs, out implAttrs))
				return null;

			var sig = GetMethodSignature(mdi, token);
			if (sig == null)
				return null;

			bool compilerGeneratedAttribute = GetCompilerGeneratedAttribute(mdi, token);

			return new CorMethodInfo(type, token, name, sig, attrs, implAttrs, compilerGeneratedAttribute);
		}

		public static CorMethodInfo GetToStringMethod(CorType type) {
			//TODO: Check for method overrides!
			for (; type != null; type = type.Base) {
				uint token;
				var mdi = type.GetMetaDataImport(out token);
				var mdTokens = MDAPI.GetMethodTokens(mdi, token);
				foreach (var mdToken in mdTokens) {
					var info = ReadMethodInfo(mdi, mdToken, type);
					if (info == null)
						continue;
					if (IsToString(info))
						return info;
				}
			}
			return null;
		}

		static bool IsToString(CorMethodInfo info) {
			if ((info.MethodAttributes & (MethodAttributes.Virtual | MethodAttributes.Static)) != MethodAttributes.Virtual)
				return false;
			if (info.Name != "ToString")
				return false;
			var sig = info.MethodSig;
			if (sig == null || sig.Generic || !sig.HasThis || sig.ExplicitThis)
				return false;
			if (sig.GenParamCount != 0 || sig.Params.Count != 0 || sig.ParamsAfterSentinel != null)
				return false;
			if (sig.RetType.GetElementType() != ElementType.String)
				return false;

			return true;
		}

		public static bool IsEnum(IMetaDataImport mdi, uint token) {
			switch ((Table)(token >> 24)) {
			case Table.TypeDef: return IsEnumTypeDef(mdi, token);
			case Table.TypeRef: return false;	//TODO: need to resolve it...
			case Table.TypeSpec:return false;
			default:			return false;
			}
		}

		static bool IsEnumTypeDef(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return false;

			return IsSystemEnum(mdi, MDAPI.GetTypeDefExtends(mdi, token));
		}

		public static bool IsSystemEnum(IMetaDataImport mdi, uint token) {
			switch ((Table)(token >> 24)) {
			case Table.TypeDef: return IsSystemEnumTypeDef(mdi, token);
			case Table.TypeRef: return IsSystemEnumTypeRef(mdi, token);
			case Table.TypeSpec:return false;
			default:			return false;
			}
		}

		static bool IsSystemEnumTypeDef(IMetaDataImport mdi, uint token) {
			var names = GetTypeDefFullNames(mdi, token);
			//TODO: Verify that it's in the corlib
			return names.Count == 1 && names[0].Name == "System.Enum";
		}

		static bool IsSystemEnumTypeRef(IMetaDataImport mdi, uint token) {
			var names = GetTypeRefFullNames(mdi, token);
			//TODO: Verify that it's in the corlib
			return names.Count == 1 && names[0].Name == "System.Enum";
		}

		public static bool IsSystemNullable(IMetaDataImport mdi, uint token) {
			switch ((Table)(token >> 24)) {
			case Table.TypeDef: return IsSystemNullableTypeDef(mdi, token);
			case Table.TypeRef: return IsSystemNullableTypeRef(mdi, token);
			case Table.TypeSpec:return false;
			default:			return false;
			}
		}

		static bool IsSystemNullableTypeDef(IMetaDataImport mdi, uint token) {
			var names = GetTypeDefFullNames(mdi, token);
			if (names.Count != 1 || names[0].Name != "System.Nullable`1")
				return false;
			var fields = GetFields(mdi, token);
			if (fields.Count != 2)
				return false;
			if (fields[0].Name != "hasValue")
				return false;
			if (fields[1].Name != "value")
				return false;

			return true;
		}

		static bool IsSystemNullableTypeRef(IMetaDataImport mdi, uint token) {
			var names = GetTypeRefFullNames(mdi, token);
			if (names.Count != 1 || names[0].Name != "System.Nullable`1")
				return false;
			return true;
		}
	}
}
