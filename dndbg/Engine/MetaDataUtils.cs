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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using dndbg.Engine.COM.CorDebug;
using dndbg.Engine.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	public struct CorFieldInfo {
		public CorType OwnerType;
		public uint Token;
		public string Name;
		public TypeSig FieldType;
		public FieldAttributes Attributes;
		public object Constant;
		public CorElementType ConstantType;

		public CorFieldInfo(CorType ownerType, uint token, string name, TypeSig fieldType, FieldAttributes attrs, object constant, CorElementType constantType) {
			this.OwnerType = ownerType;
			this.Token = token;
			this.Name = name;
			this.FieldType = fieldType;
			this.Attributes = attrs;
			this.Constant = constant;
			this.ConstantType = constantType;
		}

		public override string ToString() {
			return string.Format("{0:X8} {1} {2} {3}", Token, TypePrinterUtils.ToString(FieldType), Name, OwnerType);
		}
	}

	public struct CorPropertyInfo {
		public CorType OwnerType;
		public uint Token;
		public uint GetToken;
		public uint SetToken;
		public string Name;
		public MethodSig GetSig;
		public MethodSig SetSig;
		public MethodAttributes GetMethodAttributes;

		public CorPropertyInfo(CorType ownerType, uint token, uint getToken, uint setToken, string name, MethodSig getSig, MethodSig setSig, MethodAttributes getMethodAttributes) {
			this.OwnerType = ownerType;
			this.Token = token;
			this.GetToken = getToken;
			this.SetToken = setToken;
			this.Name = name;
			this.GetSig = getSig;
			this.SetSig = setSig;
			this.GetMethodAttributes = getMethodAttributes;
		}

		public override string ToString() {
			return string.Format("{0:X8} {1} {2}", Token, Name, OwnerType);
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

		public MDParamInfo(string name, uint token, uint seq, uint flags) {
			this.Name = name;
			this.Token = token;
			this.Sequence = seq;
			this.Flags = (ParamAttributes)flags;
		}
	}

	static class MetaDataUtils {
		public static MDParameters GetParameters(IMetaDataImport mdi, uint token) {
			var ps = new MDParameters();

			var tokens = GetParameterTokens(mdi, token);
			foreach (var ptok in tokens) {
				var info = GetParameterInfo(mdi, ptok);
				if (info != null)
					ps.Add(info.Value);
			}

			return ps;
		}

		unsafe static uint[] GetParameterTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumParams(ref iter, token, IntPtr.Zero, 0, out cTokens);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new uint[0];

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				uint[] tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0]) {
					hr = mdi.EnumParams(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				}
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		unsafe static MDParamInfo? GetParameterInfo(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint ulSequence,chName,dwAttr;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, out ulSequence, IntPtr.Zero, 0, out chName, out dwAttr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetParamProps(token, IntPtr.Zero, out ulSequence, new IntPtr(p), (uint)nameBuf.Length, out chName, out dwAttr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
			}
			if (hr < 0)
				return null;

			var name = chName <= 1 ? string.Empty : new string(nameBuf, 0, (int)chName - 1);
			return new MDParamInfo(name, token, ulSequence, dwAttr);
		}

		public static List<TokenAndName> GetTypeRefFullNames(IMetaDataImport mdi, uint token) {
			var list = new List<TokenAndName>(4);

			while (token != 0) {
				uint enclType;
				var name = GetTypeRefName(mdi, token, out enclType);
				if (name == null)
					break;
				list.Add(new TokenAndName(name, token));
				token = enclType;
			}

			list.Reverse();
			return list;
		}

		static unsafe string GetTypeRefName(IMetaDataImport mdi, uint token, out uint enclType) {
			enclType = 0;
			if (mdi == null)
				return null;
			uint tkResolutionScope, chName;
			char[] nameBuf = null;
			int hr = mdi.GetTypeRefProps(token, out tkResolutionScope, IntPtr.Zero, 0, out chName);
			if (hr >= 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetTypeRefProps(token, out tkResolutionScope, new IntPtr(p), (uint)nameBuf.Length, out chName);
				}
			}
			if (hr < 0)
				return null;

			if ((tkResolutionScope >> 24) == (int)Table.TypeRef)
				enclType = tkResolutionScope;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public static string GetTypeDefFullName(IMetaDataImport mdi, uint token) {
			var list = GetTypeDefFullNames(mdi, token);
			var sb = new StringBuilder();

			for (int i = 0; i < list.Count; i++) {
				if (i > 0)
					sb.Append('.');
				sb.Append(list[i].Name);
			}

			return sb.ToString();
		}

		public static List<TokenAndName> GetTypeDefFullNames(IMetaDataImport mdi, uint token) {
			var list = new List<TokenAndName>(4);

			while (token != 0) {
				uint enclType;
				var name = GetTypeDefName(mdi, token, out enclType);
				if (name == null)
					break;
				list.Add(new TokenAndName(name, token));
				token = enclType;
			}

			list.Reverse();
			return list;
		}

		static unsafe string GetTypeDefName(IMetaDataImport mdi, uint token, out uint enclType) {
			enclType = 0;
			if (mdi == null)
				return null;
			uint chTypeDef, dwTypeDefFlags, tkExtends;
			char[] nameBuf = null;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, out chTypeDef, out dwTypeDefFlags, out tkExtends);
			if (hr >= 0) {
				nameBuf = new char[chTypeDef];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetTypeDefProps(token, new IntPtr(p), (uint)nameBuf.Length, out chTypeDef, out dwTypeDefFlags, out tkExtends);
				}
			}
			if (hr < 0)
				return null;

			if ((dwTypeDefFlags & 7) >= 2) {
				hr = mdi.GetNestedClassProps(token, out enclType);
				if (hr < 0)
					enclType = 0;
			}

			if (chTypeDef <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chTypeDef - 1);
		}

		public static unsafe string GetMethodDefName(IMetaDataImport mdi, uint token) {
			MethodAttributes attrs;
			MethodImplAttributes implAttrs;
			return GetMethodDefName(mdi, token, out attrs, out implAttrs);
		}

		public static unsafe string GetMethodDefName(IMetaDataImport mdi, uint token, out MethodAttributes dwAttr, out MethodImplAttributes dwImplFlags) {
			dwAttr = 0;
			dwImplFlags = 0;
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint chMethod, cbSigBlob, ulCodeRVA;
			IntPtr pvSigBlob;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chMethod, out dwAttr, out pvSigBlob, out cbSigBlob, out ulCodeRVA, out dwImplFlags);
			if (hr >= 0) {
				nameBuf = new char[chMethod];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetMethodProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, out chMethod, out dwAttr, out pvSigBlob, out cbSigBlob, out ulCodeRVA, out dwImplFlags);
				}
			}
			if (hr < 0)
				return null;

			if (chMethod <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chMethod - 1);
		}

		public static int GetCountGenericParameters(IMetaDataImport mdi, uint token) {
			return GetGenericParameterTokens(mdi, token).Count;
		}

		public static List<TokenAndName> GetGenericParameterNames(IMetaDataImport mdi, uint token) {
			var gpTokens = GetGenericParameterTokens(mdi, token);
			var list = new List<TokenAndName>(gpTokens.Count);

			var mdi2 = mdi as IMetaDataImport2;
			if (mdi2 == null)
				return list;

			foreach (var gpTok in gpTokens) {
				var name = GetGenericParameterName(mdi, gpTok);
				list.Add(new TokenAndName(name ?? string.Empty, gpTok));
			}

			return list;
		}

		public static unsafe string GetGenericParameterName(IMetaDataImport mdi, uint token) {
			var mdi2 = mdi as IMetaDataImport2;
			if (mdi2 == null)
				return null;

			char[] nameBuf = null;
			uint ulParamSeq, dwParamFlags, tOwner, reserved, chName;
			int hr = mdi2.GetGenericParamProps(token, out ulParamSeq, out dwParamFlags, out tOwner, out reserved, IntPtr.Zero, 0, out chName);
			if (hr >= 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi2.GetGenericParamProps(token, out ulParamSeq, out dwParamFlags, out tOwner, out reserved, new IntPtr(p), (uint)nameBuf.Length, out chName);
				}
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static List<uint> GetGenericParameterTokens(IMetaDataImport mdi, uint token) {
			var list = new List<uint>();
			var mdi2 = mdi as IMetaDataImport2;
			if (mdi2 == null)
				return list;

			IntPtr iter = IntPtr.Zero;
			try {
				uint cGenericParams;
				int hr = mdi2.EnumGenericParams(ref iter, token, IntPtr.Zero, 0, out cGenericParams);
				if (hr < 0)
					return list;

				uint ulCount = 0;
				hr = mdi2.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return list;

				hr = mdi2.ResetEnum(iter, 0);
				if (hr < 0)
					return list;

				uint[] gpTokens = new uint[ulCount];
				fixed (uint* p = &gpTokens[0]) {
					hr = mdi2.EnumGenericParams(ref iter, token, new IntPtr(p), (uint)gpTokens.Length, out cGenericParams);
				}
				if (hr < 0)
					return list;
				for (uint i = 0; i < cGenericParams; i++)
					list.Add(gpTokens[i]);
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi2.CloseEnum(iter);
			}

			return list;
		}

		public static MethodSig GetMethodSignature(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			MethodAttributes attrs;
			MethodImplAttributes implAttrs;
			uint chMethod, cbSigBlob, ulCodeRVA;
			IntPtr pvSigBlob;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chMethod, out attrs, out pvSigBlob, out cbSigBlob, out ulCodeRVA, out implAttrs);
			if (hr < 0)
				return null;

			byte[] sig = new byte[cbSigBlob];
			Marshal.Copy(pvSigBlob, sig, 0, sig.Length);
			return new DebugSignatureReader().ReadSignature(mdi, sig) as MethodSig;
		}

		public static unsafe CallingConventionSig ReadCallingConventionSig(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			IntPtr pvSig;
			uint cbSig;
			int hr = mdi.GetSigFromToken(token, out pvSig, out cbSig);
			if (hr < 0)
				return null;
			var sig = new byte[cbSig];
			Marshal.Copy(pvSig, sig, 0, sig.Length);
			return new DebugSignatureReader().ReadSignature(mdi, sig);
		}

		public static uint GetGlobalStaticConstructor(IMetaDataImport mdi) {
			var mdTokens = GetMethodTokens(mdi, 0x02000001);
			if (mdTokens == null)
				return 0;

			foreach (uint mdToken in mdTokens) {
				MethodAttributes attrs;
				MethodImplAttributes implAttrs;
				string name = GetMethodDefName(mdi, mdToken, out attrs, out implAttrs);
				if (name != ".cctor")
					continue;
				if ((attrs & MethodAttributes.RTSpecialName) == 0)
					continue;
				if ((attrs & MethodAttributes.Static) == 0)
					continue;

				return mdToken;
			}

			return 0;
		}

		public unsafe static uint[] GetMethodTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumMethods(ref iter, token, IntPtr.Zero, 0, out cTokens);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new uint[0];

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				uint[] tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0]) {
					hr = mdi.EnumMethods(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				}
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public static List<TokenAndName> GetFields(IMetaDataImport mdi, uint token) {
			var fdTokens = GetFieldTokens(mdi, token);
			var list = new List<TokenAndName>(fdTokens.Length);

			foreach (var fdToken in fdTokens) {
				var name = GetFieldName(mdi, fdToken);
				if (name == null)
					continue;
				list.Add(new TokenAndName(name, fdToken));
			}

			return list;
		}

		static unsafe string GetFieldName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chField, dwAttr;
			char[] nameBuf = null;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chField, out dwAttr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0) {
				nameBuf = new char[chField];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetFieldProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, out chField, out dwAttr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
			}
			if (hr < 0)
				return null;

			if (chField <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chField - 1);
		}

		public unsafe static uint[] GetFieldTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumFields(ref iter, token, IntPtr.Zero, 0, out cTokens);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new uint[0];

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				uint[] tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0]) {
					hr = mdi.EnumFields(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				}
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint[] GetPropertyTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumProperties(ref iter, token, IntPtr.Zero, 0, out cTokens);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new uint[0];

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				uint[] tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0]) {
					hr = mdi.EnumProperties(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				}
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		static unsafe byte[] GetFieldSignature(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			uint chField, dwAttr, sigLen = 0;
			IntPtr sigAddr;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chField, out dwAttr, new IntPtr(&sigAddr), new IntPtr(&sigLen), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr < 0)
				return null;

			var buf = new byte[sigLen];
			Marshal.Copy(sigAddr, buf, 0, buf.Length);
			return buf;
		}

		static TypeSig GetFieldTypeSig(IMetaDataImport mdi, uint token) {
			var buf = GetFieldSignature(mdi, token);
			if (buf == null)
				return null;
			var sig = new DebugSignatureReader().ReadSignature(mdi, buf) as FieldSig;
			return sig == null ? null : sig.Type;
		}

		static FieldAttributes GetFieldAttributes(IMetaDataImport mdi, uint token) {
			uint chField, dwAttr;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chField, out dwAttr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			return hr < 0 ? 0 : (FieldAttributes)dwAttr;
		}

		unsafe static object GetFieldConstant(IMetaDataImport mdi, uint token, out CorElementType constantType) {
			constantType = CorElementType.End;
			if (mdi == null)
				return null;
			uint chField, dwAttr, cchValue;
			IntPtr pValue;
			CorElementType constantTypeTmp;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chField, out dwAttr, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pValue), new IntPtr(&cchValue));
			if (hr < 0 || pValue == IntPtr.Zero)
				return null;
			constantType = constantTypeTmp;
			return ReadConstant(pValue, cchValue, constantType);
		}

		unsafe static object ReadConstant(IntPtr addr, uint size, CorElementType elementType) {
			byte* p = (byte*)addr;
			if (p == null)
				return null;

			// size is always 0 unless it's a string...
			switch (elementType) {
			case CorElementType.Boolean:	return *p != 0;
			case CorElementType.Char:		return *(char*)p;
			case CorElementType.I1:			return *(sbyte*)p;
			case CorElementType.U1:			return *p;
			case CorElementType.I2:			return *(short*)p;
			case CorElementType.U2:			return *(ushort*)p;
			case CorElementType.I4:			return *(int*)p;
			case CorElementType.U4:			return *(uint*)p;
			case CorElementType.I8:			return *(long*)p;
			case CorElementType.U8:			return *(ulong*)p;
			case CorElementType.R4:			return *(float*)p;
			case CorElementType.R8:			return *(double*)p;
			case CorElementType.String:		return new string((char*)p, 0, (int)size);
			default:						return null;
			}
		}

		static CorFieldInfo? ReadFieldInfo(IMetaDataImport mdi, uint token, CorType type) {
			if (mdi == null)
				return null;
			var name = GetFieldName(mdi, token);
			if (name == null)
				return null;
			var fieldType = GetFieldTypeSig(mdi, token);
			if (fieldType == null)
				return null;
			var attrs = GetFieldAttributes(mdi, token);
			CorElementType constantType;
			var constant = GetFieldConstant(mdi, token, out constantType);
			return new CorFieldInfo(type, token, name, fieldType, attrs, constant, constantType);
		}

		public static IEnumerable<CorFieldInfo> GetFieldInfos(CorType type, bool checkBaseClasses = true) {
			for (; type != null; type = type.Base) {
				var cls = type.Class;
				var mod = cls == null ? null : cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				var fdTokens = GetFieldTokens(mdi, cls == null ? 0 : cls.Token);
				foreach (var fdToken in fdTokens) {
					var info = ReadFieldInfo(mdi, fdToken, type);
					Debug.Assert(info != null);
					if (info != null)
						yield return info.Value;
				}
				if (!checkBaseClasses)
					break;
			}
		}

		public static IEnumerable<CorFieldInfo> GetFieldInfos(IMetaDataImport mdi, uint token) {
			var fdTokens = GetFieldTokens(mdi, token);
			foreach (var fdToken in fdTokens) {
				var info = ReadFieldInfo(mdi, fdToken, null);
				Debug.Assert(info != null);
				if (info != null)
					yield return info.Value;
			}
		}

		unsafe static CorPropertyInfo? ReadPropertyInfo(IMetaDataImport mdi, uint token, CorType type) {
			if (mdi == null)
				return null;

			uint chProperty, dwPropFlags, mdSetter, mdGetter;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chProperty, out dwPropFlags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out mdSetter, out mdGetter, IntPtr.Zero, 0, IntPtr.Zero);
			char[] nameBuf = null;
			if (hr >= 0) {
				nameBuf = new char[chProperty];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetPropertyProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, out chProperty, out dwPropFlags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out mdSetter, out mdGetter, IntPtr.Zero, 0, IntPtr.Zero);
				}
			}
			if (hr < 0)
				return null;

			string name = chProperty <= 1 ? string.Empty : new string(nameBuf, 0, (int)chProperty - 1);

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
			MethodImplAttributes dwImplAttrs;
			IntPtr pvSigBlob;
			hr = mdi.GetMethodProps(mdGetter, IntPtr.Zero, IntPtr.Zero, 0, out chProperty, out getMethodAttrs, out pvSigBlob, out chProperty, out chProperty, out dwImplAttrs);
			if (hr < 0)
				return null;

			return new CorPropertyInfo(type, token, mdGetter, mdSetter, name, getSig, setSig, getMethodAttrs);
		}

		static bool Equals(TypeSig ts1, TypeSig ts2) {
			return new TypeComparer().Equals(ts1, ts2);
		}

		public static IEnumerable<CorPropertyInfo> GetProperties(CorType type, bool checkBaseClasses = true) {
			for (; type != null; type = type.Base) {
				var cls = type.Class;
				var mod = cls == null ? null : cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				var fdTokens = GetPropertyTokens(mdi, cls == null ? 0 : cls.Token);
				foreach (var fdToken in fdTokens) {
					var info = ReadPropertyInfo(mdi, fdToken, type);
					if (info != null)
						yield return info.Value;
				}
				if (!checkBaseClasses)
					break;
			}
		}

		static uint GetTypeDefExtends(IMetaDataImport mdi, uint token) {
			uint chTypeDef, dwTypeDefFlags, tkExtends;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, out chTypeDef, out dwTypeDefFlags, out tkExtends);
			return hr < 0 ? 0 : tkExtends;
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

			return IsSystemEnum(mdi, GetTypeDefExtends(mdi, token));
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

		public static bool HasAttribute(IMetaDataImport mdi, uint token, string attributeName) {
			if (mdi == null)
				return false;
			return mdi.GetCustomAttributeByName(token, attributeName, IntPtr.Zero, IntPtr.Zero) == 0;
		}
	}
}
