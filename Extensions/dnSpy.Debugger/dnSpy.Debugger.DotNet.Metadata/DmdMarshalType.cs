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

// .NET Core 3.0:
// 'UnmanagedType.IDispatch' is obsolete: 'Marshalling as IDispatch may be unavailable in future releases.'
// 'UnmanagedType.SafeArray' is obsolete: 'Marshalling as SafeArray may be unavailable in future releases.'
// 'VarEnum' is obsolete: 'Marshalling VARIANTs may be unavailable in future releases.'
#pragma warning disable CS0618

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata {
	sealed class DmdMarshalType {
		public readonly UnmanagedType Value;
		public readonly VarEnum SafeArraySubType;
		public readonly DmdType SafeArrayUserDefinedSubType;
		public readonly int IidParameterIndex;
		public readonly UnmanagedType ArraySubType;
		public readonly short SizeParamIndex;
		public readonly int SizeConst;
		public readonly string MarshalType;
		public readonly DmdType MarshalTypeRef;
		public readonly string MarshalCookie;

		DmdMarshalType(UnmanagedType unmanagedType, int iidParamIndex) {
			Value = unmanagedType;
			IidParameterIndex = iidParamIndex;
		}

		DmdMarshalType(int size) {
			Value = UnmanagedType.ByValTStr;
			SizeConst = size;
		}

		DmdMarshalType(VarEnum safeArraySubType, DmdType safeArrayUserDefinedSubType) {
			Value = UnmanagedType.SafeArray;
			SafeArraySubType = safeArraySubType;
			SafeArrayUserDefinedSubType = safeArrayUserDefinedSubType;
		}

		DmdMarshalType(int size, UnmanagedType arraySubType) {
			Value = UnmanagedType.ByValArray;
			ArraySubType = arraySubType;
			SizeConst = size;
		}

		DmdMarshalType(UnmanagedType arraySubType, short sizeParamIndex, int sizeConst) {
			Value = UnmanagedType.LPArray;
			ArraySubType = arraySubType;
			SizeParamIndex = sizeParamIndex;
			SizeConst = sizeConst;
		}

		DmdMarshalType(string marshalType, DmdType marshalTypeRef, string marshalCookie) {
			Value = UnmanagedType.CustomMarshaler;
			MarshalType = marshalType;
			MarshalTypeRef = marshalTypeRef;
			MarshalCookie = marshalCookie;
		}

		DmdMarshalType(UnmanagedType unmanagedType) => Value = unmanagedType;

		public static DmdMarshalType CreateInterface(UnmanagedType unmanagedType, int iidParamIndex) {
			Debug.Assert(unmanagedType == UnmanagedType.IUnknown || unmanagedType == UnmanagedType.IDispatch || unmanagedType == UnmanagedType.Interface);
			return new DmdMarshalType(unmanagedType, iidParamIndex);
		}

		public static DmdMarshalType CreateFixedSysString(int size) => new DmdMarshalType(size);

		public static DmdMarshalType CreateSafeArray(VarEnum safeArraySubType, DmdType safeArrayUserDefinedSubType) =>
			new DmdMarshalType(safeArraySubType, safeArrayUserDefinedSubType);

		public static DmdMarshalType CreateFixedArray(int size, UnmanagedType arraySubType) =>
			new DmdMarshalType(size, arraySubType);

		public static DmdMarshalType CreateArray(UnmanagedType arraySubType, short sizeParamIndex, int sizeConst) =>
			new DmdMarshalType(arraySubType, sizeParamIndex, sizeConst);

		public static DmdMarshalType CreateCustomMarshaler(string marshalType, DmdType marshalTypeRef, string marshalCookie) =>
			new DmdMarshalType(marshalType, marshalTypeRef, marshalCookie);

		public static DmdMarshalType Create(UnmanagedType unmanagedType) => new DmdMarshalType(unmanagedType);
	}
}
