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

// This file contains helper methods that are used by the CorDebug classes. It uses MDAPI.

using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	sealed class CorFieldInfo {
		public CorType OwnerType { get; }
		public uint Token { get; }
		public string Name { get; }
		public TypeSig FieldType { get; }
		public FieldAttributes Attributes { get; }
		public object Constant { get; }

		public CorFieldInfo(CorType ownerType, uint token, string name, TypeSig fieldType, FieldAttributes attrs, object constant) {
			OwnerType = ownerType;
			Token = token;
			Name = name;
			FieldType = fieldType;
			Attributes = attrs;
			Constant = constant;
		}
	}

	sealed class CorMethodInfo {
		public CorType OwnerType { get; }
		public uint Token { get; }
		public string Name { get; }
		public MethodSig MethodSig { get; }
		public MethodAttributes MethodAttributes { get; }

		public CorMethodInfo(CorType ownerType, uint token, string name, MethodSig methodSig, MethodAttributes attrs) {
			OwnerType = ownerType;
			Token = token;
			Name = name;
			MethodSig = methodSig;
			MethodAttributes = attrs;
		}
	}

	struct TokenAndName {
		public string Name { get; }
		public uint Token { get; }

		public TokenAndName(string name, uint token) {
			Name = name;
			Token = token;
		}
	}

	sealed class MDParameters {
		readonly Dictionary<uint, MDParamInfo> dict = new Dictionary<uint, MDParamInfo>();

		public void Add(MDParamInfo info) {
			if (dict.ContainsKey(info.Sequence))
				return;
			dict.Add(info.Sequence, info);
		}

		public MDParamInfo? Get(uint seq) {
			if (dict.TryGetValue(seq, out var info))
				return info;
			return null;
		}
	}

	struct MDParamInfo {
		public string Name { get; }
		public uint Token { get; }
		public uint Sequence { get; }
		public ParamAttributes Flags { get; }
		public bool IsIn => (Flags & ParamAttributes.In) != 0;
		public bool IsOut => (Flags & ParamAttributes.Out) != 0;

		public MDParamInfo(string name, uint token, uint seq, ParamAttributes flags) {
			Name = name;
			Token = token;
			Sequence = seq;
			Flags = flags;
		}
	}

	struct MethodOverrideInfo {
		public uint BodyToken { get; }
		public uint DeclToken { get; }
		public MethodOverrideInfo(uint b, uint d) {
			BodyToken = b;
			DeclToken = d;
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

			if (!MDAPI.GetParamSeqAndAttrs(mdi, token, out uint seq, out var attrs))
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

		public static int GetCountGenericParameters(IMetaDataImport mdi, uint token) => MDAPI.GetGenericParamTokens(mdi as IMetaDataImport2, token).Length;

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

		public static uint GetGlobalStaticConstructor(IMetaDataImport mdi) {
			var mdTokens = MDAPI.GetMethodTokens(mdi, 0x02000001);
			foreach (uint mdToken in mdTokens) {
				string name = MDAPI.GetMethodName(mdi, mdToken);
				if (name != ".cctor")
					continue;
				if (!MDAPI.GetMethodAttributes(mdi, mdToken, out var attrs, out var implAttrs))
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
			return sig?.Type;
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
			var constant = MDAPI.GetFieldConstant(mdi, token, out var constantType);
			return new CorFieldInfo(type, token, name, fieldType, attrs, constant);
		}

		public static IEnumerable<CorFieldInfo> GetFieldInfos(CorType type, bool checkBaseClasses = true) {
			for (; type != null; type = type.Base) {
				var cls = type.Class;
				var mdi = cls?.Module?.GetMetaDataInterface<IMetaDataImport>();
				var fdTokens = MDAPI.GetFieldTokens(mdi, cls?.Token ?? 0);
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

		static CorMethodInfo ReadMethodInfo(IMetaDataImport mdi, uint token, CorType type) {
			if (mdi == null)
				return null;

			var name = MDAPI.GetMethodName(mdi, token);
			if (name == null)
				return null;

			if (!MDAPI.GetMethodAttributes(mdi, token, out var attrs, out var implAttrs))
				return null;

			var sig = GetMethodSignature(mdi, token);
			if (sig == null)
				return null;

			return new CorMethodInfo(type, token, name, sig, attrs);
		}

		public static CorMethodInfo GetToStringMethod(CorType type) {
			//TODO: Check for method overrides!
			for (; type != null; type = type.Base) {
				var mdi = type.GetMetaDataImport(out uint token);
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
