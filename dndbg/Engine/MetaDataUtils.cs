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
using System.Runtime.InteropServices;
using dndbg.Engine.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	struct TokenAndName {
		public readonly string Name;
		public readonly uint Token;

		public TokenAndName(string name, uint token) {
			this.Name = name;
			this.Token = token;
		}
	}

	sealed class Parameters {
		readonly Dictionary<uint, ParamInfo> dict = new Dictionary<uint, ParamInfo>();

		public void Add(ParamInfo info) {
			if (dict.ContainsKey(info.Sequence))
				return;
			dict.Add(info.Sequence, info);
		}

		public ParamInfo? Get(uint seq) {
			ParamInfo info;
			if (dict.TryGetValue(seq, out info))
				return info;
			return null;
		}
	}

	struct ParamInfo {
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

		public ParamInfo(string name, uint token, uint seq, uint flags) {
			this.Name = name;
			this.Token = token;
			this.Sequence = seq;
			this.Flags = (ParamAttributes)flags;
		}
	}

	static class MetaDataUtils {
		public static Parameters GetParameters(IMetaDataImport mdi, uint token) {
			var ps = new Parameters();

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

		unsafe static ParamInfo? GetParameterInfo(IMetaDataImport mdi, uint token) {
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
			return new ParamInfo(name, token, ulSequence, dwAttr);
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
			uint chMethod, cbSigBlob;
			IntPtr pvSigBlob;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chMethod, out dwAttr, out pvSigBlob, out cbSigBlob, IntPtr.Zero, out dwImplFlags);
			if (hr >= 0) {
				nameBuf = new char[chMethod];
				fixed (char* p = &nameBuf[0]) {
					hr = mdi.GetMethodProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, out chMethod, out dwAttr, out pvSigBlob, out cbSigBlob, IntPtr.Zero, out dwImplFlags);
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
			uint chMethod, cbSigBlob;
			IntPtr pvSigBlob;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, out chMethod, out attrs, out pvSigBlob, out cbSigBlob, IntPtr.Zero, out implAttrs);
			if (hr < 0)
				return null;

			byte[] sig = new byte[cbSigBlob];
			Marshal.Copy(pvSigBlob, sig, 0, sig.Length);
			return new DebugSignatureReader().ReadSignature(mdi, sig) as MethodSig;
		}

		public static uint GetGlobalStaticConstructor(IMetaDataImport mdi) {
			var mdTokens = GetMethods(mdi, 0x02000001);
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

		public unsafe static uint[] GetMethods(IMetaDataImport mdi, uint token) {
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
	}
}
