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

using System.Collections.Generic;
using dndbg.COM.CorDebug;
using dnlib.DotNet;

namespace dndbg.Engine {
	static class TypeFormatterUtils {
		public static T Write<T>(this T output, TypeSig type, TypeFormatterFlags flags, IList<CorType> typeGenArgs = null, IList<CorType> methGenArgs = null) where T : ITypeOutput {
			new TypeFormatter(output, flags).Write(type, typeGenArgs, methGenArgs);
			return output;
		}

		public static T Write<T>(this T output, TypeSig type, IList<CorType> typeGenArgs = null, IList<CorType> methGenArgs = null) where T : ITypeOutput => Write(output, type, TypeFormatterFlags.Default, typeGenArgs, methGenArgs);
		public static string ToString(TypeSig type, TypeFormatterFlags flags, IList<CorType> typeGenArgs = null, IList<CorType> methGenArgs = null) => Write(new StringBuilderTypeOutput(), type, flags, typeGenArgs, methGenArgs).ToString();
		public static string ToString(TypeSig type, IList<CorType> typeGenArgs = null, IList<CorType> methGenArgs = null) => ToString(type, TypeFormatterFlags.Default, typeGenArgs, methGenArgs);
		public static T Write<T>(this T output, CorElementType etype, TypeFormatterFlags flags) where T : ITypeOutput => Write(output, ToTypeSig(etype), flags);
		public static T Write<T>(this T output, CorElementType etype) where T : ITypeOutput => Write(output, etype, TypeFormatterFlags.Default);
		public static string ToString(CorElementType etype, TypeFormatterFlags flags) => Write(new StringBuilderTypeOutput(), etype, flags).ToString();
		public static string ToString(CorElementType etype) => ToString(etype, TypeFormatterFlags.Default);

		static TypeSig ToTypeSig(CorElementType etype) {
			var corlib = DebugSignatureReader.CorLibTypes;
			switch (etype) {
			case CorElementType.Void:		return corlib.Void;
			case CorElementType.Boolean:	return corlib.Boolean;
			case CorElementType.Char:		return corlib.Char;
			case CorElementType.I1:			return corlib.SByte;
			case CorElementType.U1:			return corlib.Byte;
			case CorElementType.I2:			return corlib.Int16;
			case CorElementType.U2:			return corlib.UInt16;
			case CorElementType.I4:			return corlib.Int32;
			case CorElementType.U4:			return corlib.UInt32;
			case CorElementType.I8:			return corlib.Int64;
			case CorElementType.U8:			return corlib.UInt64;
			case CorElementType.R4:			return corlib.Single;
			case CorElementType.R8:			return corlib.Double;
			case CorElementType.String:		return corlib.String;
			case CorElementType.TypedByRef:	return corlib.TypedReference;
			case CorElementType.I:			return corlib.IntPtr;
			case CorElementType.U:			return corlib.UIntPtr;
			case CorElementType.Object:		return corlib.Object;
			default:						return null;
			}
		}

		public static T WriteConstant<T>(this T output, TypeSig type, object c, TypeFormatterFlags flags) where T : ITypeOutput {
			new TypeFormatter(output, flags).WriteConstant(type, c);
			return output;
		}

		public static string ConstantToString(TypeSig type, object c, TypeFormatterFlags flags) => WriteConstant(new StringBuilderTypeOutput(), type, c, flags).ToString();
		public static string ConstantToString(TypeSig type, object c) => ConstantToString(type, c, TypeFormatterFlags.Default);

		public static T WriteConstant<T>(this T output, object c, TypeFormatterFlags flags) where T : ITypeOutput {
			new TypeFormatter(output, flags).WriteConstant(c);
			return output;
		}

		public static string ConstantToString(object c, TypeFormatterFlags flags) => WriteConstant(new StringBuilderTypeOutput(), c, flags).ToString();
		public static string ConstantToString(object c) => ConstantToString(c, TypeFormatterFlags.Default);
	}
}
