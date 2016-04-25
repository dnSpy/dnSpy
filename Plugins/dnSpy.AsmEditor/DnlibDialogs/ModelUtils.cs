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

using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.DnlibDialogs {
	static class ModelUtils {
		public const uint COMPRESSED_UINT32_MIN	= 0;
		public const uint COMPRESSED_UINT32_MAX	= 0x1FFFFFFF;
		public const int COMPRESSED_INT32_MIN	=-0x10000000;
		public const int COMPRESSED_INT32_MAX	= 0x0FFFFFFF;

		public static bool IsSystemType(this ITypeDefOrRef tdr) {
			return tdr != null &&
				tdr.DeclaringType == null &&
				tdr.Namespace == "System" &&
				tdr.Name == "Type" &&
				tdr.DefinitionAssembly.IsCorLib();
		}

		public static ElementType GetElementType(Type type) {
			var tc = type == null ? TypeCode.Empty : System.Type.GetTypeCode(type);
			switch (tc) {
			case TypeCode.Boolean:	return ElementType.Boolean;
			case TypeCode.Char:		return ElementType.Char;
			case TypeCode.SByte:	return ElementType.I1;
			case TypeCode.Byte:		return ElementType.U1;
			case TypeCode.Int16:	return ElementType.I2;
			case TypeCode.UInt16:	return ElementType.U2;
			case TypeCode.Int32:	return ElementType.I4;
			case TypeCode.UInt32:	return ElementType.U4;
			case TypeCode.Int64:	return ElementType.I8;
			case TypeCode.UInt64:	return ElementType.U8;
			case TypeCode.Single:	return ElementType.R4;
			case TypeCode.Double:	return ElementType.R8;
			case TypeCode.String:	return ElementType.String;
			default:				return ElementType.End;
			}
		}

		public static string GetEnumFieldName(TypeDef td, object value) {
			if (td == null || value == null)
				return null;
			foreach (var fd in td.Fields) {
				if (fd.IsLiteral && fd.Constant != null && value.Equals(fd.Constant.Value))
					return fd.Name;
			}
			return null;
		}

		public static object GetDefaultValue(TypeSig type, bool classValueTypeIsEnum = false) {
			var t = type.RemovePinnedAndModifiers();
			switch (t.GetElementType()) {
			case ElementType.Boolean:return false;
			case ElementType.Char:	return (char)0;
			case ElementType.I1:	return (sbyte)0;
			case ElementType.U1:	return (byte)0;
			case ElementType.I2:	return (short)0;
			case ElementType.U2:	return (ushort)0;
			case ElementType.I4:	return (int)0;
			case ElementType.U4:	return (uint)0;
			case ElementType.I8:	return (long)0;
			case ElementType.U8:	return (ulong)0;
			case ElementType.R4:	return (float)0;
			case ElementType.R8:	return (double)0;
			case ElementType.Class:
			case ElementType.ValueType:
				var tdr = ((ClassOrValueTypeSig)t).TypeDefOrRef;
				if (tdr.IsSystemType())
					break;
				var td = tdr.ResolveTypeDef();
				if (td == null) {
					if (classValueTypeIsEnum)
						return (int)0;
					break;
				}
				if (!td.IsEnum)
					break;
				switch (td.GetEnumUnderlyingType().RemovePinnedAndModifiers().GetElementType()) {
				case ElementType.Boolean:return false;
				case ElementType.Char:	return (char)0;
				case ElementType.I1:	return (sbyte)0;
				case ElementType.U1:	return (byte)0;
				case ElementType.I2: 	return (short)0;
				case ElementType.U2: 	return (ushort)0;
				case ElementType.I4: 	return (int)0;
				case ElementType.U4: 	return (uint)0;
				case ElementType.I8: 	return (long)0;
				case ElementType.U8: 	return (ulong)0;
				case ElementType.R4: 	return (float)0;
				case ElementType.R8: 	return (double)0;
				}
				break;
			}
			return null;
		}

		/// <summary>
		/// Gets the HasSecurity bit. Should be called each time the <see cref="DeclSecurity"/> list
		/// or the <see cref="CustomAttribute"/> list has been modified.
		/// </summary>
		/// <param name="declSecs">The <see cref="DeclSecurity"/> list</param>
		/// <param name="cas">The <see cref="CustomAttribute"/> list</param>
		/// <returns></returns>
		public static bool GetHasSecurityBit(IList<DeclSecurity> declSecs, IList<CustomAttribute> cas) {
			if (declSecs.Count > 0)
				return true;

			foreach (var ca in cas) {
				if (ca.TypeFullName == "System.Security.SuppressUnmanagedCodeSecurityAttribute")
					return true;
			}

			return false;
		}
	}
}
