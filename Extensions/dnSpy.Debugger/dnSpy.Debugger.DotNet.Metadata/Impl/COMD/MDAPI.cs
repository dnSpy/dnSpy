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
using System.Diagnostics;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	static class MDAPI {
		const int CLDB_E_RECORD_NOTFOUND = unchecked((int)0x80131130);
		const int CLDB_E_INDEX_NOTFOUND = unchecked((int)0x80131124);

		static bool IsGlobal(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return false;
			int hr = mdi.IsGlobal(token, out int bGlobal);
			return hr == 0 && bGlobal != 0;
		}

		public static bool IsValidToken(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return false;
			return mdi.IsValidToken(token);
		}

		public unsafe static bool GetClassLayout(IMetaDataImport2 mdi, uint token, out ushort packingSize, out uint classSize) {
			packingSize = 0;
			classSize = 0;
			if (mdi is null)
				return false;

			uint dwPackSize = 0, ulClassSize = 0;
			int hr = mdi.GetClassLayout(token, new IntPtr(&dwPackSize), null, 0, IntPtr.Zero, new IntPtr(&ulClassSize));
			if (hr != 0)
				return false;

			packingSize = (ushort)dwPackSize;
			classSize = ulClassSize;
			return true;
		}

		public unsafe static uint? GetRVA(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;

			int hr = mdi.GetRVA(token, out uint ulCodeRVA, IntPtr.Zero);
			if (hr != 0)
				return null;
			return ulCodeRVA;
		}

		public unsafe static uint[] GetPermissionSetTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumPermissionSets(ref iter, token, 0, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumPermissionSets(ref iter, token, 0, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static (IntPtr addr, uint size, uint action) GetPermissionSetBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0, 0);
			IntPtr pvPermission;
			uint cbPermission;
			uint dwAction;
			int hr = mdi.GetPermissionSetProps(token, new IntPtr(&dwAction), new IntPtr(&pvPermission), new IntPtr(&cbPermission));
			if (hr != 0 || pvPermission == IntPtr.Zero)
				return (IntPtr.Zero, 0, 0);

			return (pvPermission, cbPermission, dwAction);
		}

		public unsafe static bool GetPinvokeMapProps(IMetaDataImport2 mdi, uint token, out DmdPInvokeAttributes attrs, out uint moduleToken) {
			attrs = 0;
			moduleToken = 0;
			if (mdi is null)
				return false;

			uint dwMappingFlags, mrImportDLL;
			int hr = mdi.GetPinvokeMap(token, new IntPtr(&dwMappingFlags), IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&mrImportDLL));
			if (hr != 0)
				return false;
			attrs = (DmdPInvokeAttributes)dwMappingFlags;
			moduleToken = mrImportDLL;
			return true;
		}

		public unsafe static string? GetPinvokeMapName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			char[]? nameBuf = null;
			uint chImportName;
			int hr = mdi.GetPinvokeMap(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chImportName), IntPtr.Zero);
			if (hr >= 0 && chImportName != 0) {
				nameBuf = new char[chImportName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetPinvokeMap(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chImportName), IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chImportName <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chImportName - 1);
		}

		public unsafe static string? GetMemberRefName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			char[]? nameBuf = null;
			uint chMember;
			int hr = mdi.GetMemberRefProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chMember), IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chMember != 0) {
				nameBuf = new char[chMember];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetMemberRefProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chMember), IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chMember <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chMember - 1);
		}

		public unsafe static uint GetMemberRefClassToken(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint tk;
			int hr = mdi.GetMemberRefProps(token, new IntPtr(&tk), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			return hr != 0 ? 0 : tk;
		}

		public unsafe static (IntPtr addr, uint size) GetMemberRefSignatureBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);
			IntPtr pvSigBlob;
			uint cbSig;
			int hr = mdi.GetMemberRefProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&pvSigBlob), new IntPtr(&cbSig));
			if (hr != 0 || pvSigBlob == IntPtr.Zero)
				return (IntPtr.Zero, 0);

			return (pvSigBlob, cbSig);
		}

		public unsafe static uint GetMethodOwnerRid(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
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

		public unsafe static uint[] GetMethodTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumMethods(ref iter, token, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumMethods(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static (IntPtr addr, uint size) GetMethodSignatureBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);
			uint cbSigBlob;
			IntPtr pvSigBlob;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&pvSigBlob), new IntPtr(&cbSigBlob), IntPtr.Zero, IntPtr.Zero);
			if (hr < 0 || pvSigBlob == IntPtr.Zero)
				return (IntPtr.Zero, 0);

			return (pvSigBlob, cbSigBlob);
		}

		public static unsafe string? GetMethodName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chMethod - 1);
		}

		public static unsafe bool GetMethodAttributes(IMetaDataImport2 mdi, uint token, out DmdMethodAttributes dwAttr, out DmdMethodImplAttributes dwImplFlags) {
			dwAttr = 0;
			dwImplFlags = 0;
			if (mdi is null)
				return false;
			uint dwAttr2, dwImplFlags2;
			int hr = mdi.GetMethodProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwAttr2), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwImplFlags2));
			if (hr < 0)
				return false;
			dwAttr = (DmdMethodAttributes)dwAttr2;
			dwImplFlags = (DmdMethodImplAttributes)dwImplFlags2;
			return true;
		}

		public unsafe static uint[] GetInterfaceImplTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumInterfaceImpls(ref iter, token, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumInterfaceImpls(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint GetInterfaceImplInterfaceToken(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;

			uint tkIface;
			int hr = mdi.GetInterfaceImplProps(token, IntPtr.Zero, new IntPtr(&tkIface));
			return hr == 0 ? tkIface : 0;
		}

		public unsafe static uint[] GetParamTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumParams(ref iter, token, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumParams(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static string? GetParamName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			char[]? nameBuf = null;
			uint chName;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetParamProps(token, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chName - 1);
		}

		public unsafe static bool GetParamSeqAndAttrs(IMetaDataImport2 mdi, uint token, out uint seq, out DmdParameterAttributes attrs) {
			seq = uint.MaxValue;
			attrs = 0;
			if (mdi is null)
				return false;

			uint ulSequence, dwAttr;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, new IntPtr(&ulSequence), IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwAttr), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return false;

			seq = ulSequence;
			attrs = (DmdParameterAttributes)dwAttr;
			return true;
		}

		public unsafe static object? GetParamConstant(IMetaDataImport2 mdi, uint token, out ElementType constantType) {
			constantType = ElementType.End;
			if (mdi is null)
				return null;
			uint cchValue;
			IntPtr pValue;
			ElementType constantTypeTmp;
			int hr = mdi.GetParamProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pValue), new IntPtr(&cchValue));
			if (hr < 0 || pValue == IntPtr.Zero)
				return null;
			constantType = constantTypeTmp;
			return ReadConstant(pValue, cchValue, constantType);
		}

		public static unsafe string? GetTypeRefName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			uint chName;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chName - 1);
		}

		public static unsafe uint GetTypeRefResolutionScope(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint tkResolutionScope;
			int hr = mdi.GetTypeRefProps(token, new IntPtr(&tkResolutionScope), IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? tkResolutionScope : 0;
		}

		public unsafe static uint[] GetTypeDefTokens(IMetaDataImport2 mdi) {
			if (mdi is null)
				return Array.Empty<uint>();

			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumTypeDefs(ref iter, IntPtr.Zero, 0, out uint count);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				// The global type isn't included
				var tokens = new uint[ulCount + 1];
				if (tokens.Length > 1) {
					fixed (uint* p = &tokens[1])
						hr = mdi.EnumTypeDefs(ref iter, new IntPtr(p), ulCount, out count);
				}
				if (hr < 0)
					return Array.Empty<uint>();
				tokens[0] = 0x02000001;
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint GetTypeDefExtends(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint tkExtends;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&tkExtends));
			return hr != 0 ? 0 : tkExtends;
		}

		public static unsafe string? GetTypeDefName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			uint chTypeDef;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chTypeDef - 1);
		}

		public static unsafe DmdTypeAttributes? GetTypeDefAttributes(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			uint dwTypeDefFlags;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwTypeDefFlags), IntPtr.Zero);
			return hr == 0 ? (DmdTypeAttributes)dwTypeDefFlags : (DmdTypeAttributes?)null;
		}

		public static unsafe uint GetTypeDefEnclosingType(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint dwTypeDefFlags;
			int hr = mdi.GetTypeDefProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwTypeDefFlags), IntPtr.Zero);
			if (hr != 0)
				return 0;

			if ((dwTypeDefFlags & 7) >= 2) {
				hr = mdi.GetNestedClassProps(token, out uint enclType);
				if (hr == 0)
					return enclType;
			}

			return 0;
		}

		public static unsafe string? GetGenericParamName(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 is null)
				return null;

			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chName - 1);
		}

		public static unsafe bool GetGenericParamNumAndAttrs(IMetaDataImport2 mdi2, uint token, out ushort number, out DmdGenericParameterAttributes attrs) {
			number = ushort.MaxValue;
			attrs = 0;
			if (mdi2 is null)
				return false;

			uint ulParamSeq, dwParamFlags;
			int hr = mdi2.GetGenericParamProps(token, new IntPtr(&ulParamSeq), new IntPtr(&dwParamFlags), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return false;

			number = (ushort)ulParamSeq;
			attrs = (DmdGenericParameterAttributes)dwParamFlags;
			return true;
		}

		public unsafe static uint[] GetGenericParamTokens(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 is null)
				return Array.Empty<uint>();

			var iter = IntPtr.Zero;
			try {
				int hr = mdi2.EnumGenericParams(ref iter, token, IntPtr.Zero, 0, out uint cGenericParams);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi2.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi2.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var gpTokens = new uint[ulCount];
				fixed (uint* p = &gpTokens[0])
					hr = mdi2.EnumGenericParams(ref iter, token, new IntPtr(p), (uint)gpTokens.Length, out cGenericParams);
				if (hr < 0)
					return Array.Empty<uint>();
				return gpTokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi2.CloseEnum(iter);
			}
		}

		public unsafe static uint GetGenericParamConstraintTypeToken(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 is null)
				return 0;
			uint tkConstraintType;
			int hr = mdi2.GetGenericParamConstraintProps(token, IntPtr.Zero, new IntPtr(&tkConstraintType));
			if (hr != 0)
				return 0;
			return tkConstraintType;
		}

		public unsafe static uint[] GetGenericParamConstraintTokens(IMetaDataImport2 mdi2, uint token) {
			if (mdi2 is null)
				return Array.Empty<uint>();

			var iter = IntPtr.Zero;
			try {
				int hr = mdi2.EnumGenericParamConstraints(ref iter, token, IntPtr.Zero, 0, out uint cGenericParamConstraints);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi2.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi2.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var gpcTokens = new uint[ulCount];
				fixed (uint* p = &gpcTokens[0])
					hr = mdi2.EnumGenericParamConstraints(ref iter, token, new IntPtr(p), (uint)gpcTokens.Length, out cGenericParamConstraints);
				if (hr < 0)
					return Array.Empty<uint>();
				return gpcTokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi2.CloseEnum(iter);
			}
		}

		public static unsafe (IntPtr addr, uint size) GetStandAloneSigBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);

			int hr = mdi.GetSigFromToken(token, out var pvSig, out uint cbSig);
			if (hr < 0 || pvSig == IntPtr.Zero)
				return (IntPtr.Zero, 0);
			return (pvSig, cbSig);
		}

		public unsafe static COR_FIELD_OFFSET[]? GetFieldOffsets(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			if ((token & 0x00FFFFFF) == 0)
				return null;

			int cFieldOffset = 0;
			int hr = mdi.GetClassLayout(token, IntPtr.Zero, null, 0, new IntPtr(&cFieldOffset), IntPtr.Zero);
			Debug.Assert(hr == 0 || hr == CLDB_E_RECORD_NOTFOUND || hr == CLDB_E_INDEX_NOTFOUND);
			var fieldOffsets = cFieldOffset == 0 ? Array.Empty<COR_FIELD_OFFSET>() : new COR_FIELD_OFFSET[cFieldOffset];
			if (hr == 0 && fieldOffsets.Length != 0)
				hr = mdi.GetClassLayout(token, IntPtr.Zero, fieldOffsets, fieldOffsets.Length, new IntPtr(&cFieldOffset), IntPtr.Zero);
			return hr != 0 ? null : fieldOffsets;
		}

		public static uint? GetFieldOffset(IMetaDataImport2 mdi, uint token, uint fieldToken) {
			var offsets = GetFieldOffsets(mdi, token);
			if (offsets is null)
				return null;
			foreach (var info in offsets) {
				if (info.FieldToken == fieldToken) {
					if (info.Offset != uint.MaxValue)
						return info.Offset;
					return null;
				}
			}
			return null;
		}

		public unsafe static (IntPtr addr, uint size) GetFieldMarshalBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);
			if ((token & 0x00FFFFFF) == 0)
				return (IntPtr.Zero, 0);

			int hr = mdi.GetFieldMarshal(token, out var pvNativeType, out uint cbNativeType);
			Debug.Assert(hr == 0 || hr == CLDB_E_RECORD_NOTFOUND);
			if (hr != 0)
				return (IntPtr.Zero, 0);

			return (pvNativeType, cbNativeType);
		}

		public unsafe static uint GetFieldOwnerRid(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
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

		public static unsafe string? GetFieldName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			uint chField = 0, dwAttr = 0;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chField - 1);
		}

		public unsafe static uint[] GetFieldTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumFields(ref iter, token, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumFields(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public static unsafe (IntPtr addr, uint size) GetFieldSignatureBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);

			uint sigLen = 0;
			IntPtr sigAddr;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&sigAddr), new IntPtr(&sigLen), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr < 0 || sigAddr == IntPtr.Zero)
				return (IntPtr.Zero, 0);

			return (sigAddr, sigLen);
		}

		public unsafe static DmdFieldAttributes GetFieldAttributes(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint dwAttr;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwAttr), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			Debug.Assert(hr == 0);
			return hr < 0 ? 0 : (DmdFieldAttributes)dwAttr;
		}

		public unsafe static object? GetFieldConstant(IMetaDataImport2 mdi, uint token, out ElementType constantType) {
			constantType = ElementType.End;
			if (mdi is null)
				return null;
			uint cchValue;
			IntPtr pValue;
			ElementType constantTypeTmp;
			int hr = mdi.GetFieldProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pValue), new IntPtr(&cchValue));
			if (hr < 0 || pValue == IntPtr.Zero)
				return null;
			constantType = constantTypeTmp;
			return ReadConstant(pValue, cchValue, constantType);
		}

		unsafe static object? ReadConstant(IntPtr addr, uint size, ElementType elementType) {
			var p = (byte*)addr;
			if (p is null)
				return null;

			// size is always 0 unless it's a string...
			switch (elementType) {
			case ElementType.Boolean:	return *p != 0;
			case ElementType.Char:		return *(char*)p;
			case ElementType.I1:		return *(sbyte*)p;
			case ElementType.U1:		return *p;
			case ElementType.I2:		return *(short*)p;
			case ElementType.U2:		return *(ushort*)p;
			case ElementType.I4:		return *(int*)p;
			case ElementType.U4:		return *(uint*)p;
			case ElementType.I8:		return *(long*)p;
			case ElementType.U8:		return *(ulong*)p;
			case ElementType.R4:		return *(float*)p;
			case ElementType.R8:		return *(double*)p;
			case ElementType.String:	return new string((char*)p, 0, (int)size);
			default:					return null;
			}
		}

		public unsafe static uint GetEventOwnerRid(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
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

		public unsafe static DmdEventAttributes GetEventAttributes(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint dwEventFlags;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwEventFlags), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? (DmdEventAttributes)dwEventFlags : 0;
		}

		public unsafe static string? GetEventName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			uint chEvent;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chEvent), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			char[]? nameBuf = null;
			if (hr >= 0 && chEvent != 0) {
				nameBuf = new char[chEvent];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetEventProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chEvent), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chEvent <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chEvent - 1);
		}

		public unsafe static uint GetEventTypeToken(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint tkEventType;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&tkEventType), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? tkEventType : 0;
		}

		public unsafe static bool GetEventAddRemoveFireTokens(IMetaDataImport2 mdi, uint token, out uint addToken, out uint removeToken, out uint fireToken) {
			addToken = 0;
			removeToken = 0;
			fireToken = 0;
			if (mdi is null)
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

		public unsafe static uint[] GetEventOtherMethodTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			uint count;
			int hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&count));
			uint[]? tokens = null;
			if (hr >= 0 && count != 0) {
				tokens = new uint[count];
				fixed (uint* p = &tokens[0])
					hr = mdi.GetEventProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)tokens.Length, new IntPtr(&count));
			}
			if (hr < 0)
				return Array.Empty<uint>();
			return tokens ?? Array.Empty<uint>();
		}

		public unsafe static uint[] GetEventTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumEvents(ref iter, token, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumEvents(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static uint GetPropertyOwnerRid(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
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

		public unsafe static uint[] GetPropertyTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumProperties(ref iter, token, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumProperties(ref iter, token, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static string? GetPropertyName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			uint chProperty;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&chProperty), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			char[]? nameBuf = null;
			if (hr >= 0 && chProperty != 0) {
				nameBuf = new char[chProperty];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetPropertyProps(token, IntPtr.Zero, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chProperty), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chProperty <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chProperty - 1);
		}

		public unsafe static bool GetPropertyGetterSetter(IMetaDataImport2 mdi, uint token, out uint mdGetter, out uint mdSetter) {
			mdGetter = 0;
			mdSetter = 0;
			if (mdi is null)
				return false;
			uint mdSetterTmp, mdGetterTmp;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&mdSetterTmp), new IntPtr(&mdGetterTmp), IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return false;
			mdSetter = mdSetterTmp;
			mdGetter = mdGetterTmp;
			return true;
		}

		public unsafe static uint[] GetPropertyOtherMethodTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			uint count;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, new IntPtr(&count));
			uint[]? tokens = null;
			if (hr >= 0 && count != 0) {
				tokens = new uint[count];
				fixed (uint* p = &tokens[0])
					hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(p), (uint)tokens.Length, new IntPtr(&count));
			}
			if (hr < 0)
				return Array.Empty<uint>();
			return tokens ?? Array.Empty<uint>();
		}

		public unsafe static DmdPropertyAttributes GetPropertyAttributes(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return 0;
			uint dwPropFlags;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&dwPropFlags), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			return hr == 0 ? (DmdPropertyAttributes)dwPropFlags : 0;
		}

		public unsafe static (IntPtr addr, uint size) GetPropertySignatureBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);
			IntPtr pvSig;
			uint cbSig;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&pvSig), new IntPtr(&cbSig), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr != 0)
				return (IntPtr.Zero, 0);

			return (pvSig, cbSig);
		}

		public unsafe static object? GetPropertyConstant(IMetaDataImport2 mdi, uint token, out ElementType constantType) {
			constantType = ElementType.End;
			if (mdi is null)
				return null;
			uint cchDefaultValue;
			IntPtr pDefaultValue;
			ElementType constantTypeTmp;
			int hr = mdi.GetPropertyProps(token, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&constantTypeTmp), new IntPtr(&pDefaultValue), new IntPtr(&cchDefaultValue), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
			if (hr < 0 || pDefaultValue == IntPtr.Zero)
				return null;
			constantType = constantTypeTmp;
			return ReadConstant(pDefaultValue, cchDefaultValue, constantType);
		}

		public unsafe static (IntPtr addr, uint size) GetTypeSpecSignatureBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0);

			int hr = mdi.GetTypeSpecFromToken(token, out var pvSig, out uint cbSig);
			if (hr != 0 || pvSig == IntPtr.Zero)
				return (IntPtr.Zero, 0);

			return (pvSig, cbSig);
		}

		public unsafe static (IntPtr addr, uint size) GetMethodSpecProps(IMetaDataImport2 mdi2, uint token, out uint method) {
			method = 0;
			if (mdi2 is null)
				return (IntPtr.Zero, 0);

			int hr = mdi2.GetMethodSpecProps(token, out method, out var pvSigBlob, out uint cbSigBlob);
			if (hr != 0 || pvSigBlob == IntPtr.Zero)
				return (IntPtr.Zero, 0);

			return (pvSigBlob, cbSigBlob);
		}

		public unsafe static string? GetAssemblyRefSimpleName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chName - 1);
		}

		public unsafe static Version? GetAssemblyRefVersionAndLocale(IMetaDataAssemblyImport mdai, uint token, out string? locale) {
			locale = null;
			if (mdai is null)
				return null;
			ASSEMBLYMETADATA data;
			char[]? nameBuf = null;
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

			locale = data.cbLocale <= 1 ? string.Empty : new string(nameBuf!, 0, (int)data.cbLocale - 1);
			return new Version(data.usMajorVersion, data.usMinorVersion, data.usBuildNumber, data.usRevisionNumber);
		}

		public unsafe static byte[]? GetAssemblyRefPublicKeyOrToken(IMetaDataAssemblyImport mdai, uint token, out DmdAssemblyNameFlags attrs) {
			attrs = 0;
			if (mdai is null)
				return null;
			IntPtr pbPublicKeyOrToken;
			uint cbPublicKeyOrToken, dwAssemblyFlags;
			int hr = mdai.GetAssemblyRefProps(token, new IntPtr(&pbPublicKeyOrToken), new IntPtr(&cbPublicKeyOrToken), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwAssemblyFlags));
			if (hr != 0)
				return null;
			attrs = (DmdAssemblyNameFlags)dwAssemblyFlags;
			if (pbPublicKeyOrToken == IntPtr.Zero)
				return null;
			var data = new byte[cbPublicKeyOrToken];
			Marshal.Copy(pbPublicKeyOrToken, data, 0, data.Length);
			return data;
		}

		public unsafe static string? GetAssemblySimpleName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)chName - 1);
		}

		public unsafe static Version? GetAssemblyVersionAndLocale(IMetaDataAssemblyImport mdai, uint token, out string? locale) {
			locale = null;
			if (mdai is null)
				return null;
			ASSEMBLYMETADATA data;
			char[]? nameBuf = null;
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

			locale = data.cbLocale <= 1 ? string.Empty : new string(nameBuf!, 0, (int)data.cbLocale - 1);
			return new Version(data.usMajorVersion, data.usMinorVersion, data.usBuildNumber, data.usRevisionNumber);
		}

		public unsafe static DmdAssemblyHashAlgorithm? GetAssemblyHashAlgorithm(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			uint ulHashAlgId;
			int hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, new IntPtr(&ulHashAlgId), IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0)
				return null;
			return (DmdAssemblyHashAlgorithm)ulHashAlgId;
		}

		public unsafe static DmdAssemblyNameFlags? GetAssemblyAttributes(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			uint dwAssemblyFlags;
			int hr = mdai.GetAssemblyProps(token, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, new IntPtr(&dwAssemblyFlags));
			if (hr != 0)
				return null;
			return (DmdAssemblyNameFlags)dwAssemblyFlags;
		}

		public unsafe static byte[]? GetAssemblyPublicKey(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			IntPtr pbPublicKey;
			uint cbPublicKey;
			int hr = mdai.GetAssemblyProps(token, new IntPtr(&pbPublicKey), new IntPtr(&cbPublicKey), IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr != 0 || pbPublicKey == IntPtr.Zero)
				return null;
			var data = new byte[cbPublicKey];
			Marshal.Copy(pbPublicKey, data, 0, data.Length);
			return data;
		}

		public static DmdImageFileMachine? GetModuleMachineAndPEKind(IMetaDataImport2 mdi2, out DmdPortableExecutableKinds peKind) {
			peKind = 0;
			if (mdi2 is null)
				return null;
			int hr = mdi2.GetPEKind(out uint dwPEKind, out uint dwMachine);
			if (hr != 0)
				return null;
			peKind = (DmdPortableExecutableKinds)dwPEKind;
			return (DmdImageFileMachine)dwMachine;
		}

		public unsafe static string? GetModuleVersionString(IMetaDataImport2 mdi2) {
			if (mdi2 is null)
				return null;
			char[]? nameBuf = null;
			int hr = mdi2.GetVersionString(IntPtr.Zero, 0, out uint ccBufSize);
			if (hr >= 0 && ccBufSize != 0) {
				nameBuf = new char[ccBufSize];
				fixed (char* p = &nameBuf[0])
					hr = mdi2.GetVersionString(new IntPtr(p), ccBufSize, out ccBufSize);
			}
			if (hr < 0)
				return null;

			if (ccBufSize <= 1)
				return string.Empty;
			return new string(nameBuf!, 0, (int)ccBufSize - 1);
		}

		public unsafe static string? GetModuleName(IMetaDataImport2 mdi) {
			if (mdi is null)
				return null;
			char[]? nameBuf = null;
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
			return new string(nameBuf!, 0, (int)cchName - 1);
		}

		public unsafe static Guid? GetModuleMvid(IMetaDataImport2 mdi) {
			if (mdi is null)
				return null;
			Guid guid;
			int hr = mdi.GetScopeProps(IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&guid));
			if (hr < 0)
				return null;

			return guid;
		}

		public unsafe static string? GetModuleRefName(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			char[]? nameBuf = null;
			int hr = mdi.GetModuleRefProps(token, IntPtr.Zero, 0, out uint chName);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdi.GetModuleRefProps(token, new IntPtr(p), (uint)nameBuf.Length, out chName);
			}
			if (hr < 0)
				return null;

			if (chName <= 1)
				return string.Empty;
			return new string(nameBuf!, 0, (int)chName - 1);
		}

		public unsafe static string? GetUserString(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return null;
			char[]? stringBuf = null;
			int hr = mdi.GetUserString(token, IntPtr.Zero, 0, out uint chString);
			if (hr >= 0 && chString != 0) {
				stringBuf = new char[chString];
				fixed (char* p = &stringBuf[0])
					hr = mdi.GetUserString(token, new IntPtr(p), (uint)stringBuf.Length, out chString);
			}
			if (hr < 0)
				return null;

			if (chString == 0)
				return string.Empty;
			return new string(stringBuf!, 0, (int)chString);
		}

		public unsafe static uint[] GetCustomAttributeTokens(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return Array.Empty<uint>();
			var iter = IntPtr.Zero;
			try {
				int hr = mdi.EnumCustomAttributes(ref iter, token, 0, IntPtr.Zero, 0, out uint cTokens);
				if (hr < 0)
					return Array.Empty<uint>();

				uint ulCount = 0;
				hr = mdi.CountEnum(iter, ref ulCount);
				if (hr < 0 || ulCount == 0)
					return Array.Empty<uint>();

				hr = mdi.ResetEnum(iter, 0);
				if (hr < 0)
					return Array.Empty<uint>();

				var tokens = new uint[ulCount];
				fixed (uint* p = &tokens[0])
					hr = mdi.EnumCustomAttributes(ref iter, token, 0, new IntPtr(p), (uint)tokens.Length, out cTokens);
				if (hr < 0)
					return Array.Empty<uint>();
				return tokens;
			}
			finally {
				if (iter != IntPtr.Zero)
					mdi.CloseEnum(iter);
			}
		}

		public unsafe static (IntPtr addr, int size, int typeToken) GetCustomAttributeBlob(IMetaDataImport2 mdi, uint token) {
			if (mdi is null)
				return (IntPtr.Zero, 0, 0);

			int hr = mdi.GetCustomAttributeProps(token, IntPtr.Zero, out var typeToken, out var pBlob, out uint cbSize);
			if (hr != 0 || pBlob == IntPtr.Zero)
				return (IntPtr.Zero, 0, 0);

			return (pBlob, (int)cbSize, (int)typeToken);
		}

		public unsafe static string? GetFileName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			char[]? nameBuf = null;
			uint chName;
			int hr = mdai.GetFileProps(token, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetFileProps(token, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chName - 1);
		}

		public unsafe static string? GetExportedTypeName(IMetaDataAssemblyImport mdai, uint token) {
			if (mdai is null)
				return null;
			char[]? nameBuf = null;
			uint chName;
			int hr = mdai.GetExportedTypeProps(token, IntPtr.Zero, 0, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (hr >= 0 && chName != 0) {
				nameBuf = new char[chName];
				fixed (char* p = &nameBuf[0])
					hr = mdai.GetExportedTypeProps(token, new IntPtr(p), (uint)nameBuf.Length, new IntPtr(&chName), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}
			if (hr < 0)
				return null;

			return chName <= 1 ? string.Empty : new string(nameBuf!, 0, (int)chName - 1);
		}

		public unsafe static bool GetExportedTypeProps(IMetaDataAssemblyImport mdai, uint token, out uint implementation, out uint typeDefId, out DmdTypeAttributes attrs) {
			implementation = 0;
			typeDefId = 0;
			attrs = 0;
			if (mdai is null)
				return false;

			uint tkImplementation, tkTypeDef, dwExportedTypeFlags;
			int hr = mdai.GetExportedTypeProps(token, IntPtr.Zero, 0, IntPtr.Zero, new IntPtr(&tkImplementation), new IntPtr(&tkTypeDef), new IntPtr(&dwExportedTypeFlags));
			if (hr != 0)
				return false;

			implementation = tkImplementation;
			typeDefId = tkTypeDef;
			attrs = (DmdTypeAttributes)dwExportedTypeFlags;
			return true;
		}
	}
}
