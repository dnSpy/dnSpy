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

// This file contains methods that directly use the MD API (IMetaDataImport, etc). It's used by
// the CorDebug and CorModuleDef code.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dndbg.Engine {
	static class MDAPI {
		static bool IsGlobal(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return false;
			int bGlobal;
			int hr = mdi.IsGlobal(token, out bGlobal);
			return hr == 0 && bGlobal != 0;
		}

		public static bool IsValidToken(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return false;
			return mdi.IsValidToken(token);
		}

		public unsafe static bool GetClassLayout(IMetaDataImport mdi, uint token, out ushort packingSize, out uint classSize) {
			packingSize = 0;
			classSize = 0;
			if (mdi == null)
				return false;

			uint dwPackSize = 0, ulClassSize = 0;
			int hr = mdi.GetClassLayout(token, new IntPtr(&dwPackSize), null, 0, IntPtr.Zero, new IntPtr(&ulClassSize));
			if (hr != 0)
				return false;

			packingSize = (ushort)dwPackSize;
			classSize = ulClassSize;
			return true;
		}

		public unsafe static uint? GetRVA(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			uint ulCodeRVA;
			int hr = mdi.GetRVA(token, out ulCodeRVA, IntPtr.Zero);
			if (hr != 0)
				return null;
			return ulCodeRVA;
		}

		public unsafe static UTF8String GetUtf8Name(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			IntPtr pszUtf8NamePtr;
			int hr = mdi.GetNameFromToken(token, out pszUtf8NamePtr);
			if (hr != 0 || pszUtf8NamePtr == IntPtr.Zero)
				return null;
			const int MAX_LEN = 0x1000;
			byte* p = (byte*)pszUtf8NamePtr;
			for (int i = 0; i < MAX_LEN; i++, p++) {
				if (*p == 0)
					break;
			}
			byte[] buf = new byte[p - (byte*)pszUtf8NamePtr];
			Marshal.Copy(pszUtf8NamePtr, buf, 0, buf.Length);
			return new UTF8String(buf);
		}

		public unsafe static uint[] GetPermissionSetTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumPermissionSets(ref iter, token, 0, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumPermissionSets(ref iter, token, 0, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static SecurityAction GetPermissionSetAction(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint dwAction;
			int hr = mdi.GetPermissionSetProps(token, new IntPtr(&dwAction), IntPtr.Zero, IntPtr.Zero);
			return hr != 0 ? 0 : (SecurityAction)dwAction;
		}

		public unsafe static byte[] GetPermissionSetBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			IntPtr pvPermission;
			uint cbPermission;
			int hr = mdi.GetPermissionSetProps(token, IntPtr.Zero, new IntPtr(&pvPermission), new IntPtr(&cbPermission));
			if (hr != 0 || pvPermission == IntPtr.Zero)
				return null;

			var sig = new byte[cbPermission];
			Marshal.Copy(pvPermission, sig, 0, sig.Length);
			return sig;
		}

		public unsafe static bool GetPinvokeMapProps(IMetaDataImport mdi, uint token, out PInvokeAttributes attrs, out uint moduleToken) {
			attrs = 0;
			moduleToken = 0;
			if (mdi == null)
				return false;

			uint dwMappingFlags, mrImportDLL;
			int hr = mdi.GetPinvokeMap(token, new IntPtr(&dwMappingFlags), IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&mrImportDLL));
			if (hr != 0)
				return false;
			attrs = (PInvokeAttributes)dwMappingFlags;
			moduleToken = mrImportDLL;
			return true;
		}

		public unsafe static string GetPinvokeMapName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint chImportName;
			int hr = mdi.GetPinvokeMap(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chImportName), IntPtr.Zero);
			if (hr >= 0 && chImportName != 0) {
				nameBuf = new char[chImportName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetPinvokeMap(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chImportName), IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chImportName <= 1 ? string.Empty : new string(nameBuf, 0, (int)chImportName - 1);
		}

		public unsafe static string GetMemberRefName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint chMember;
			int hr = mdi.GetMemberRefProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chMember), IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chMember != 0) {
				nameBuf = new char[chMember];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetMemberRefProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chMember), IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chMember <= 1 ? string.Empty : new string(nameBuf, 0, (int)chMember - 1);
		}

		public unsafe static uint GetMemberRefClassToken(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint tk;
			int hr = mdi.GetMemberRefProps(token, new IntPtr(&tk), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			return hr != 0 ? 0 : tk;
		}

		public unsafe static byte[] GetMemberRefSignatureBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			IntPtr pvSigBlob;
			uint cbSig;
			int hr = mdi.GetMemberRefProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&pvSigBlob), new IntPtr(&cbSig));
			if (hr != 0 || pvSigBlob == IntPtr.Zero)
				return null;

			var sig = new byte[cbSig];
			Marshal.Copy(pvSigBlob, sig, 0, sig.Length);
			return sig;
		}

		public unsafe static uint GetMethodOwnerRid(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			if (IsGlobal(mdi, token))
				return 1;
			uint ownerToken;
			int hr = mdi.GetMethodProps(token, new IntPtr(&ownerToken), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.TypeDef ? ownerMdToken.Rid : 0;
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumMethods(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static byte[] GetMethodSignatureBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint cbSigBlob;
			IntPtr pvSigBlob;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&pvSigBlob), new IntPtr(&cbSigBlob), IntPtr.Zero, IntPtr.Zero);
			if (hr < 0 || pvSigBlob == IntPtr.Zero)
				return null;

			var sig = new byte[cbSigBlob];
			Marshal.Copy(pvSigBlob, sig, 0, sig.Length);
			return sig;
		}

		public static unsafe string GetMethodName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint chMethod;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chMethod), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chMethod != 0) {
				nameBuf = new char[chMethod];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetMethodProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chMethod), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			if (chMethod <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chMethod - 1);
		}

		public static unsafe bool GetMethodAttributes(IMetaDataImport mdi, uint token, out MethodAttributes dwAttr, out MethodImplAttributes dwImplFlags) {
			dwAttr = 0;
			dwImplFlags = 0;
			if (mdi == null)
				return false;
			uint dwAttr2, dwImplFlags2;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwAttr2), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwImplFlags2));
			if (hr < 0)
				return false;
			dwAttr = (MethodAttributes)dwAttr2;
			dwImplFlags = (MethodImplAttributes)dwImplFlags2;
			return true;
		}

		public unsafe static MethodOverrideInfo[] GetMethodOverrides(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new MethodOverrideInfo[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumMethodImpls(ref iter, token, IntPtr.Zero, IntPtr.Zero, 0, out cTokens);
				if (hr < 0)
					return new MethodOverrideInfo[0];

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new MethodOverrideInfo[0];

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return new MethodOverrideInfo[0];

				uint[] bodyTokens = new uint[ulCount];
				uint[] declTokens = new uint[ulCount];
				fixed (uint* b = &bodyTokens[0]) {
					fixed (uint* d = &declTokens[0])
						hr = mdi.EnumMethodImpls(ref iter, token, new IntPtr(b), new IntPtr(d), (uint)bodyTokens.Length, out cTokens);
				}
				if (hr < 0)
					return new MethodOverrideInfo[0];
				var infos = new MethodOverrideInfo[ulCount];
				for (int i = 0; i < infos.Length; i++)
					infos[i] = new MethodOverrideInfo(bodyTokens[i], declTokens[i]);
				return infos;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint[] GetMethodSemanticsTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumMethodSemantics(ref iter, token, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumMethodSemantics(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static MethodSemanticsAttributes GetMethodSemanticsAttributes(IMetaDataImport mdi, uint token, uint tkPropEvent) {
			if (mdi == null)
				return 0;
			uint dwSemanticsFlags;
			int hr = mdi.GetMethodSemantics(token, tkPropEvent, out dwSemanticsFlags);
			return hr == 0 ? (MethodSemanticsAttributes)dwSemanticsFlags : 0;
		}

		public unsafe static uint GetInterfaceImplOwnerRid(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;

			uint ownerToken;
			int hr = mdi.GetInterfaceImplProps(token, new IntPtr(&ownerToken), IntPtr.Zero);
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.TypeDef ? ownerMdToken.Rid : 0;
		}

		public unsafe static uint[] GetInterfaceImplTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumInterfaceImpls(ref iter, token, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumInterfaceImpls(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint GetInterfaceImplInterfaceToken(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;

			uint tkIface;
			int hr = mdi.GetInterfaceImplProps(token, IntPtr.Zero, new IntPtr(&tkIface));
			return hr == 0 ? tkIface : 0;
		}

		public unsafe static uint[] GetParamTokens(IMetaDataImport mdi, uint token) {
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumParams(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static string GetParamName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint chName;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetParamProps(token, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static bool GetParamSeqAndAttrs(IMetaDataImport mdi, uint token, out uint seq, out ParamAttributes attrs) {
			seq = uint.MaxValue;
			attrs = 0;
			if (mdi == null)
				return false;

			uint ulSequence, dwAttr;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, new IntPtr(&ulSequence), IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwAttr), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return false;

			seq = ulSequence;
			attrs = (ParamAttributes)dwAttr;
			return true;
		}

		public unsafe static object GetParamConstant(IMetaDataImport mdi, uint token, out CorElementType constantType) {
			constantType = CorElementType.End;
			if (mdi == null)
				return null;
			uint cchValue;
			IntPtr pValue;
			CorElementType constantTypeTmp;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pValue), new IntPtr(&cchValue));
			if (hr < 0 || pValue == IntPtr.Zero)
				return null;
			constantType = constantTypeTmp;
			return ReadConstant(pValue, cchValue, constantType);
		}

		public unsafe static uint GetParamOwnerRid(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint ownerToken;
			int hr = mdi.GetParamProps(token, new IntPtr(&ownerToken), IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.Method ? ownerMdToken.Rid : 0;
		}

		public static unsafe string GetTypeRefName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chName;
			char[] nameBuf = null;
			int hr = mdi.GetTypeRefProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chName));
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetTypeRefProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName));
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public static unsafe uint GetTypeRefResolutionScope(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint tkResolutionScope;
			int hr = mdi.GetTypeRefProps(token, new IntPtr(&tkResolutionScope), IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? tkResolutionScope : 0;
		}

		public unsafe static uint[] GetTypeDefTokens(IMetaDataImport mdi) {
			if (mdi == null)
				return new uint[0];

			IntPtr iter = IntPtr.Zero;
			try {
				uint count;
				int hr = mdi.EnumTypeDefs(ref iter, IntPtr.Zero, 0, out count);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0)
					return new uint[0];

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				// The global type isn't included
				uint[] tokens = new uint[ulCount + 1];
				if (tokens.Length > 1) {
					fixed (uint* p = &tokens[1])
						hr = mdi.EnumTypeDefs(ref iter, new IntPtr(p), ulCount, out count);
				}
				if (hr < 0)
					return new uint[0];
				tokens[0] = 0x02000001;
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint GetTypeDefExtends(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint tkExtends;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&tkExtends));
			return hr != 0 ? 0 : tkExtends;
		}

		public static unsafe string GetTypeDefName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chTypeDef;
			char[] nameBuf = null;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, new IntPtr(&chTypeDef), IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chTypeDef != 0) {
				nameBuf = new char[chTypeDef];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetTypeDefProps(token, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chTypeDef), IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			if (chTypeDef <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chTypeDef - 1);
		}

		public static unsafe TypeAttributes? GetTypeDefAttributes(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint dwTypeDefFlags;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwTypeDefFlags), IntPtr.Zero);
			return hr == 0 ? (TypeAttributes)dwTypeDefFlags : (TypeAttributes?)null;
		}

		public static unsafe uint GetTypeDefEnclosingType(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint dwTypeDefFlags;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwTypeDefFlags), IntPtr.Zero);
			if (hr != 0)
				return 0;

			if ((dwTypeDefFlags & 7) >= 2) {
				uint enclType;
				hr = mdi.GetNestedClassProps(token, out enclType);
				if (hr == 0)
					return enclType;
			}

			return 0;
		}

		public unsafe static uint GetGenericParamOwner(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 == null)
				return 0;
			uint ownerToken;
			int hr = mdi2.GetGenericParamProps(token, IntPtr.Zero, IntPtr.Zero, new IntPtr(&ownerToken), IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.TypeDef || ownerMdToken.Table == Table.Method ? ownerMdToken.Raw : 0;
		}

		public static unsafe string GetGenericParamName(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 == null)
				return null;

			char[] nameBuf = null;
			uint chName;
			int hr = mdi2.GetGenericParamProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chName));
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdi2.GetGenericParamProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName));
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public static unsafe bool GetGenericParamNumAndAttrs(IMetaDataImport2 mdi2, uint token, out ushort number, out GenericParamAttributes attrs) {
			number = ushort.MaxValue;
			attrs = 0;
			if (mdi2 == null)
				return false;

			uint ulParamSeq, dwParamFlags;
			int hr = mdi2.GetGenericParamProps(token, new IntPtr(&ulParamSeq), new IntPtr(&dwParamFlags), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return false;

			number = (ushort)ulParamSeq;
			attrs = (GenericParamAttributes)dwParamFlags;
			return true;
		}

		public unsafe static uint[] GetGenericParamTokens(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 == null)
				return new uint[0];

			IntPtr iter = IntPtr.Zero;
			try {
				uint cGenericParams;
				int hr = mdi2.EnumGenericParams(ref iter, token, IntPtr.Zero, 0, out cGenericParams);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi2.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new uint[0];

				hr = mdi2.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				uint[] gpTokens = new uint[ulCount];
				fixed (uint* p = &gpTokens[0])
					hr = mdi2.EnumGenericParams(ref iter, token, new IntPtr(p), (uint)gpTokens.Length, out cGenericParams);
				if (hr < 0)
					return new uint[0];
				return gpTokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi2.CloseEnum(iter);
			}
		}

		public unsafe static uint GetGenericParamConstraintOwnerRid(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 == null)
				return 0;
			uint ownerToken;
			int hr = mdi2.GetGenericParamConstraintProps(token, new IntPtr(&ownerToken), IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.GenericParam ? ownerMdToken.Rid : 0;
		}

		public unsafe static uint GetGenericParamConstraintTypeToken(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 == null)
				return 0;
			uint tkConstraintType;
			int hr = mdi2.GetGenericParamConstraintProps(token, IntPtr.Zero, new IntPtr(&tkConstraintType));
			if (hr != 0)
				return 0;
			return tkConstraintType;
		}

		public unsafe static uint[] GetGenericParamConstraintTokens(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 == null)
				return new uint[0];

			IntPtr iter = IntPtr.Zero;
			try {
				uint cGenericParamConstraints;
				int hr = mdi2.EnumGenericParamConstraints(ref iter, token, IntPtr.Zero, 0, out cGenericParamConstraints);
				if (hr < 0)
					return new uint[0];

				uint ulCount = 0;
				hr = mdi2.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return new uint[0];

				hr = mdi2.ResetEnum(iter, 0);
				if (hr < 0)
					return new uint[0];

				uint[] gpcTokens = new uint[ulCount];
				fixed (uint* p = &gpcTokens[0])
					hr = mdi2.EnumGenericParamConstraints(ref iter, token, new IntPtr(p), (uint)gpcTokens.Length, out cGenericParamConstraints);
				if (hr < 0)
					return new uint[0];
				return gpcTokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi2.CloseEnum(iter);
			}
		}

		public static unsafe byte[] GetStandAloneSigBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			IntPtr pvSig;
			uint cbSig;
			int hr = mdi.GetSigFromToken(token, out pvSig, out cbSig);
			if (hr < 0 || pvSig == IntPtr.Zero)
				return null;
			var sig = new byte[cbSig];
			Marshal.Copy(pvSig, sig, 0, sig.Length);
			return sig;
		}

		public unsafe static COR_FIELD_OFFSET[] GetFieldOffsets(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			int cFieldOffset = 0;
			int hr = mdi.GetClassLayout(token, IntPtr.Zero, null, 0, new IntPtr(&cFieldOffset), IntPtr.Zero);
			Debug.Assert(hr == 0 || hr == CordbgErrors.CLDB_E_RECORD_NOTFOUND);
			var fieldOffsets = new COR_FIELD_OFFSET[cFieldOffset];
			if (hr == 0 && fieldOffsets.Length != 0)
				hr = mdi.GetClassLayout(token, IntPtr.Zero, fieldOffsets, fieldOffsets.Length, new IntPtr(&cFieldOffset), IntPtr.Zero);
			return hr != 0 ? null : fieldOffsets;
		}

		public unsafe static byte[] GetFieldMarshalBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			IntPtr pvNativeType;
			uint cbNativeType;
			int hr = mdi.GetFieldMarshal(token, out pvNativeType, out cbNativeType);
			Debug.Assert(hr == 0 || hr == CordbgErrors.CLDB_E_RECORD_NOTFOUND);
			if (hr != 0)
				return null;

			var data = new byte[cbNativeType];
			Marshal.Copy(pvNativeType, data, 0, data.Length);
			return data;
		}

		public unsafe static uint GetFieldOwnerRid(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			if (IsGlobal(mdi, token))
				return 1;
			uint ownerToken;
			int hr = mdi.GetFieldProps(token, new IntPtr(&ownerToken), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.TypeDef ? ownerMdToken.Rid : 0;
		}

		public static unsafe string GetFieldName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chField = 0, dwAttr = 0;
			char[] nameBuf = null;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chField), new IntPtr(&dwAttr), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chField != 0) {
				nameBuf = new char[chField];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetFieldProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chField), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumFields(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public static unsafe byte[] GetFieldSignatureBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			uint sigLen = 0;
			IntPtr sigAddr;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&sigAddr), new IntPtr(&sigLen), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr < 0 || sigAddr == IntPtr.Zero)
				return null;

			var buf = new byte[sigLen];
			Marshal.Copy(sigAddr, buf, 0, buf.Length);
			return buf;
		}

		public unsafe static FieldAttributes GetFieldAttributes(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint dwAttr;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwAttr), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			Debug.Assert(hr == 0);
			return hr < 0 ? 0 : (FieldAttributes)dwAttr;
		}

		public unsafe static object GetFieldConstant(IMetaDataImport mdi, uint token, out CorElementType constantType) {
			constantType = CorElementType.End;
			if (mdi == null)
				return null;
			uint cchValue;
			IntPtr pValue;
			CorElementType constantTypeTmp;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pValue), new IntPtr(&cchValue));
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

		public unsafe static uint GetEventOwnerRid(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			if (IsGlobal(mdi, token))
				return 1;
			uint ownerToken;
			int hr = mdi.GetEventProps(token, new IntPtr(&ownerToken), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.TypeDef ? ownerMdToken.Rid : 0;
		}

		public unsafe static EventAttributes GetEventAttributes(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint dwEventFlags;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwEventFlags), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? (EventAttributes)dwEventFlags : 0;
		}

		public unsafe static string GetEventName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chEvent;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chEvent), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			char[] nameBuf = null;
			if (hr >= 0 && chEvent != 0) {
				nameBuf = new char[chEvent];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetEventProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chEvent), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chEvent <= 1 ? string.Empty : new string(nameBuf, 0, (int)chEvent - 1);
		}

		public unsafe static uint GetEventTypeToken(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint tkEventType;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&tkEventType), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? tkEventType : 0;
		}

		public unsafe static bool GetEventAddRemoveFireTokens(IMetaDataImport mdi, uint token, out uint addToken, out uint removeToken, out uint fireToken) {
			addToken = 0;
			removeToken = 0;
			fireToken = 0;
			if (mdi == null)
				return false;
			uint addTokenTmp, removeTokenTmp, fireTokenTmp;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&addTokenTmp), new IntPtr(&removeTokenTmp), new IntPtr(&fireTokenTmp), IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return false;
			addToken = addTokenTmp;
			removeToken = removeTokenTmp;
			fireToken = fireTokenTmp;
			return true;
		}

		public unsafe static uint[] GetEventOtherMethodTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			uint count;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&count));
			uint[] tokens = null;
			if (hr >= 0 && count != 0) {
				tokens = new uint[count];
				fixed (uint* p = &tokens[0])
					hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)tokens.Length, new IntPtr(&count));
			}
			if (hr < 0)
				return new uint[0];
			return tokens ?? new uint[0];
		}

		public unsafe static uint[] GetEventTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumEvents(ref iter, token, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumEvents(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint GetPropertyOwnerRid(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			if (IsGlobal(mdi, token))
				return 1;
			uint ownerToken;
			int hr = mdi.GetPropertyProps(token, new IntPtr(&ownerToken), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return 0;
			var ownerMdToken = new MDToken(ownerToken);
			return ownerMdToken.Table == Table.TypeDef ? ownerMdToken.Rid : 0;
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumProperties(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static string GetPropertyName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chProperty;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chProperty), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			char[] nameBuf = null;
			if (hr >= 0 && chProperty != 0) {
				nameBuf = new char[chProperty];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetPropertyProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chProperty), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chProperty <= 1 ? string.Empty : new string(nameBuf, 0, (int)chProperty - 1);
		}

		public unsafe static bool GetPropertyGetterSetter(IMetaDataImport mdi, uint token, out uint mdGetter, out uint mdSetter) {
			mdGetter = 0;
			mdSetter = 0;
			if (mdi == null)
				return false;
			uint mdSetterTmp, mdGetterTmp;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&mdSetterTmp), new IntPtr(&mdGetterTmp), IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return false;
			mdSetter = mdSetterTmp;
			mdGetter = mdGetterTmp;
			return true;
		}

		public unsafe static uint[] GetPropertyOtherMethodTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			uint count;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&count));
			uint[] tokens = null;
			if (hr >= 0 && count != 0) {
				tokens = new uint[count];
				fixed (uint* p = &tokens[0])
					hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)tokens.Length, new IntPtr(&count));
			}
			if (hr < 0)
				return new uint[0];
			return tokens ?? new uint[0];
		}

		public unsafe static PropertyAttributes GetPropertyAttributes(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return 0;
			uint dwPropFlags;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwPropFlags), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? (PropertyAttributes)dwPropFlags : 0;
		}

		public unsafe static byte[] GetPropertySignatureBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			IntPtr pvSig;
			uint cbSig;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&pvSig), new IntPtr(&cbSig), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return null;

			var data = new byte[cbSig];
			Marshal.Copy(pvSig, data, 0, data.Length);
			return data;
		}

		public unsafe static object GetPropertyConstant(IMetaDataImport mdi, uint token, out CorElementType constantType) {
			constantType = CorElementType.End;
			if (mdi == null)
				return null;
			uint cchDefaultValue;
			IntPtr pDefaultValue;
			CorElementType constantTypeTmp;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pDefaultValue), new IntPtr(&cchDefaultValue), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr < 0 || pDefaultValue == IntPtr.Zero)
				return null;
			constantType = constantTypeTmp;
			return ReadConstant(pDefaultValue, cchDefaultValue, constantType);
		}

		public unsafe static byte[] GetTypeSpecSignatureBlob(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;

			IntPtr pvSig;
			uint cbSig;
			int hr = mdi.GetTypeSpecFromToken(token, out pvSig, out cbSig);
			if (hr != 0 || pvSig == IntPtr.Zero)
				return null;

			byte[] sig = new byte[cbSig];
			Marshal.Copy(pvSig, sig, 0, sig.Length);
			return sig;
		}

		public unsafe static byte[] GetMethodSpecProps(IMetaDataImport2 mdi2, uint token, out uint method) {
			method = 0;
			if (mdi2 == null)
				return null;

			IntPtr pvSigBlob;
			uint cbSigBlob;
			int hr = mdi2.GetMethodSpecProps(token, out method, out pvSigBlob, out cbSigBlob);
			if (hr != 0 || pvSigBlob == IntPtr.Zero)
				return null;

			byte[] sig = new byte[cbSigBlob];
			Marshal.Copy(pvSigBlob, sig, 0, sig.Length);
			return sig;
		}

		public unsafe static string GetAssemblyRefSimpleName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			char[] nameBuf = null;
			uint chName = 0;
			int hr = mdai.GetAssemblyRefProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetAssemblyRefProps(token, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static Version GetAssemblyRefVersionAndLocale(IMetaDataAssemblyImport mdai, uint token, out string locale) {
			locale = null;
			if (mdai == null)
				return null;
			ASSEMBLYMETADATA data;
			char[] nameBuf = null;
			int hr = mdai.GetAssemblyRefProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&data), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && data.cbLocale != 0) {
				nameBuf = new char[data.cbLocale];
				fixed (char* p = &nameBuf[0]) {
					data.szLocale = new IntPtr(p);
					hr = mdai.GetAssemblyRefProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&data), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
			}
			if (hr != 0)
				return null;

			locale = data.cbLocale <= 1 ? string.Empty : new string(nameBuf, 0, (int)data.cbLocale - 1);
			return new Version(data.usMajorVersion, data.usMinorVersion, data.usBuildNumber, data.usRevisionNumber);
		}

		public unsafe static byte[] GetAssemblyRefHash(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			IntPtr pbHashValue;
			uint cbHashValue;
			int hr = mdai.GetAssemblyRefProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&pbHashValue), new IntPtr(&cbHashValue), IntPtr.Zero);
			if (hr != 0 || pbHashValue == IntPtr.Zero)
				return null;

			var data = new byte[cbHashValue];
			Marshal.Copy(pbHashValue, data, 0, data.Length);
			return data;
		}

		public unsafe static PublicKeyBase GetAssemblyRefPublicKeyOrToken(IMetaDataAssemblyImport mdai, uint token, out AssemblyAttributes attrs) {
			attrs = 0;
			if (mdai == null)
				return null;
			IntPtr pbPublicKeyOrToken;
			uint cbPublicKeyOrToken, dwAssemblyFlags;
			int hr = mdai.GetAssemblyRefProps(token, new IntPtr(&pbPublicKeyOrToken), new IntPtr(&cbPublicKeyOrToken), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwAssemblyFlags));
			if (hr != 0)
				return null;
			attrs = (AssemblyAttributes)dwAssemblyFlags;
			if (pbPublicKeyOrToken == IntPtr.Zero)
				return null;
			var data = new byte[cbPublicKeyOrToken];
			Marshal.Copy(pbPublicKeyOrToken, data, 0, data.Length);
			if ((dwAssemblyFlags & (uint)AssemblyAttributes.PublicKey) != 0)
				return new PublicKey(data);
			return new PublicKeyToken(data);
		}

		public unsafe static string GetAssemblySimpleName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			char[] nameBuf = null;
			uint chName = 0;
			int hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static Version GetAssemblyVersionAndLocale(IMetaDataAssemblyImport mdai, uint token, out string locale) {
			locale = null;
			if (mdai == null)
				return null;
			ASSEMBLYMETADATA data;
			char[] nameBuf = null;
			int hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&data), IntPtr.Zero);
			if (hr >= 0 && data.cbLocale != 0) {
				nameBuf = new char[data.cbLocale];
				fixed (char* p = &nameBuf[0]) {
					data.szLocale = new IntPtr(p);
					hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&data), IntPtr.Zero);
				}
			}
			if (hr != 0)
				return null;

			locale = data.cbLocale <= 1 ? string.Empty : new string(nameBuf, 0, (int)data.cbLocale - 1);
			return new Version(data.usMajorVersion, data.usMinorVersion, data.usBuildNumber, data.usRevisionNumber);
		}

		public unsafe static AssemblyHashAlgorithm? GetAssemblyHashAlgorithm(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			uint ulHashAlgId;
			int hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, new IntPtr(&ulHashAlgId), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return null;
			return (AssemblyHashAlgorithm)ulHashAlgId;
		}

		public unsafe static AssemblyAttributes? GetAssemblyAttributes(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			uint dwAssemblyFlags;
			int hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwAssemblyFlags));
			if (hr != 0)
				return null;
			return (AssemblyAttributes)dwAssemblyFlags;
		}

		public unsafe static PublicKey GetAssemblyPublicKey(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			IntPtr pbPublicKey;
			uint cbPublicKey;
			int hr = mdai.GetAssemblyProps(token, new IntPtr(&pbPublicKey), new IntPtr(&cbPublicKey), IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0 || pbPublicKey == IntPtr.Zero)
				return null;
			var data = new byte[cbPublicKey];
			Marshal.Copy(pbPublicKey, data, 0, data.Length);
			return new PublicKey(data);
		}

		public static Machine? GetModuleMachineAndPEKind(IMetaDataImport2 mdi2, out CorPEKind peKind) {
			peKind = 0;
			if (mdi2 == null)
				return null;

			uint dwPEKind, dwMachine;
			int hr = mdi2.GetPEKind(out dwPEKind, out dwMachine);
			if (hr != 0)
				return null;
			peKind = (CorPEKind)dwPEKind;
			return (Machine)dwMachine;
		}

		public unsafe static string GetModuleVersionString(IMetaDataImport2 mdi2) {
			if (mdi2 == null)
				return null;
			char[] nameBuf = null;
			uint ccBufSize = 0;
			int hr = mdi2.GetVersionString(IntPtr.Zero, 0, out ccBufSize);
			if (hr >= 0 && ccBufSize != 0) {
				nameBuf = new char[ccBufSize];
				fixed (char* p = &nameBuf[0])
					hr = mdi2.GetVersionString(new IntPtr(p), ccBufSize, out ccBufSize);
			}
			if (hr < 0)
				return null;

			if (ccBufSize <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)ccBufSize - 1);
		}

		public unsafe static string GetModuleName(IMetaDataImport mdi) {
			if (mdi == null)
				return null;
			char[] nameBuf = null;
			uint cchName = 0;
			int hr = mdi.GetScopeProps(IntPtr.Zero, 0, new IntPtr(&cchName), IntPtr.Zero);
			if (hr >= 0 && cchName != 0) {
				nameBuf = new char[cchName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetScopeProps(new IntPtr(p), cchName, new IntPtr(&cchName), IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			if (cchName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)cchName - 1);
		}

		public unsafe static Guid? GetModuleMvid(IMetaDataImport mdi) {
			if (mdi == null)
				return null;
			Guid guid;
			int hr = mdi.GetScopeProps(IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&guid));
			if (hr < 0)
				return null;

			return guid;
		}

		public unsafe static string GetModuleRefName(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chName;
			char[] nameBuf = null;
			int hr = mdi.GetModuleRefProps(token, IntPtr.Zero, 0, out chName);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetModuleRefProps(token, new IntPtr(p), (uint)nameBuf.Length, out chName);
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static string GetUserString(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return null;
			uint chString;
			char[] stringBuf = null;
			int hr = mdi.GetUserString(token, IntPtr.Zero, 0, out chString);
			if (hr >= 0 && chString != 0) {
				stringBuf = new char[chString];
				fixed (char* p = &stringBuf[0])
					hr = mdi.GetUserString(token, new IntPtr(p), (uint)stringBuf.Length, out chString);
			}
			if (hr < 0)
				return null;

			if (chString == 0)
				return string.Empty;
			return new string(stringBuf, 0, (int)chString);
		}

		public unsafe static byte[] GetCustomAttributeByName(IMetaDataImport mdi, uint token, string name) {
			if (mdi == null)
				return null;
			IntPtr addr;
			uint size;
			int hr = mdi.GetCustomAttributeByName(token, name, new IntPtr(&addr), new IntPtr(&size));
			if (hr < 0 || addr == IntPtr.Zero)
				return null;

			var data = new byte[size];
			Marshal.Copy(addr, data, 0, data.Length);
			return data;
		}

		public static bool HasAttribute(IMetaDataImport mdi, uint token, string attributeName) {
			if (mdi == null)
				return false;
			return mdi.GetCustomAttributeByName(token, attributeName, IntPtr.Zero, IntPtr.Zero) == 0;
		}

		public unsafe static uint[] GetCustomAttributeTokens(IMetaDataImport mdi, uint token) {
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdi.EnumCustomAttributes(ref iter, token, 0, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumCustomAttributes(ref iter, token, 0, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static byte[] GetCustomAttributeBlob(IMetaDataImport mdi, uint token, out uint typeToken) {
			typeToken = 0;
			if (mdi == null)
				return null;

			IntPtr pBlob;
			uint cbSize;
			int hr = mdi.GetCustomAttributeProps(token, IntPtr.Zero, out typeToken, out pBlob, out cbSize);
			if (hr != 0 || pBlob == IntPtr.Zero)
				return null;

			byte[] caBlob = new byte[cbSize];
			Marshal.Copy(pBlob, caBlob, 0, caBlob.Length);
			return caBlob;
		}

		public unsafe static FileAttributes? GetFileAttributes(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			uint dwFileFlags;
			int hr = mdai.GetFileProps(token, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwFileFlags));
			return hr != 0 ? null : (FileAttributes?)dwFileFlags;
		}

		public unsafe static string GetFileName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			char[] nameBuf = null;
			uint chName;
			int hr = mdai.GetFileProps(token, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetFileProps(token, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static byte[] GetFileHash(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			IntPtr pbHashValue;
			uint cbHashValue;
			int hr = mdai.GetFileProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&pbHashValue), new IntPtr(&cbHashValue), IntPtr.Zero);
			if (hr != 0)
				return null;

			var sig = new byte[cbHashValue];
			Marshal.Copy(pbHashValue, sig, 0, sig.Length);
			return sig;
		}

		public unsafe static uint[] GetExportedTypeRids(IMetaDataAssemblyImport mdai) {
			var mdi = mdai as IMetaDataImport;
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdai.EnumExportedTypes(ref iter, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdai.EnumExportedTypes(ref iter, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdai.CloseEnum(iter);
			}
		}

		public unsafe static string GetExportedTypeName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			char[] nameBuf = null;
			uint chName;
			int hr = mdai.GetExportedTypeProps(token, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetExportedTypeProps(token, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static bool GetExportedTypeProps(IMetaDataAssemblyImport mdai, uint token, out uint implementation, out uint typeDefId, out TypeAttributes attrs) {
			implementation = 0;
			typeDefId = 0;
			attrs = 0;
			if (mdai == null)
				return false;

			uint tkImplementation, tkTypeDef, dwExportedTypeFlags;
			int hr = mdai.GetExportedTypeProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&tkImplementation), new IntPtr(&tkTypeDef), new IntPtr(&dwExportedTypeFlags));
			if (hr != 0)
				return false;

			implementation = tkImplementation;
			typeDefId = tkTypeDef;
			attrs = (TypeAttributes)dwExportedTypeFlags;
			return true;
		}

		public unsafe static uint[] GetManifestResourceRids(IMetaDataAssemblyImport mdai) {
			var mdi = mdai as IMetaDataImport;
			if (mdi == null)
				return new uint[0];
			IntPtr iter = IntPtr.Zero;
			try {
				uint cTokens;
				int hr = mdai.EnumManifestResources(ref iter, IntPtr.Zero, 0, out cTokens);
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
				fixed (uint* p = &tokens[0])
					hr = mdai.EnumManifestResources(ref iter, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return new uint[0];
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdai.CloseEnum(iter);
			}
		}

		public unsafe static string GetManifestResourceName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			char[] nameBuf = null;
			uint chName;
			int hr = mdai.GetManifestResourceProps(token, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetManifestResourceProps(token, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf, 0, (int)chName - 1);
		}

		public unsafe static bool GetManifestResourceProps(IMetaDataAssemblyImport mdai, uint token, out uint offset, out uint implementation, out ManifestResourceAttributes attrs) {
			offset = 0;
			implementation = 0;
			attrs = 0;
			if (mdai == null)
				return false;

			uint tkImplementation, dwOffset, dwResourceFlags;
			int hr = mdai.GetManifestResourceProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&tkImplementation), new IntPtr(&dwOffset), new IntPtr(&dwResourceFlags));
			if (hr != 0)
				return false;

			implementation = tkImplementation;
			offset = dwOffset;
			attrs = (ManifestResourceAttributes)dwResourceFlags;
			return true;
		}

		public unsafe static uint? GetManifestResourceImplementationToken(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai == null)
				return null;
			uint tkImplementation;
			int hr = mdai.GetManifestResourceProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&tkImplementation), IntPtr.Zero, IntPtr.Zero);
			return hr == 0 ? tkImplementation : (uint?)null;
		}
	}
}
