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
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	static class ModelUtils
	{
		public static bool IsSystemType(this ITypeDefOrRef tdr)
		{
			return tdr != null &&
				tdr.DeclaringType == null &&
				tdr.Namespace == "System" &&
				tdr.Name == "Type" &&
				tdr.DefinitionAssembly.IsCorLib();
		}

		public static ElementType GetElementType(Type type)
		{
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

		public static string GetEnumFieldName(TypeDef td, object value)
		{
			if (td == null || value == null)
				return null;
			foreach (var fd in td.Fields) {
				if (fd.IsLiteral && fd.Constant != null && value.Equals(fd.Constant.Value))
					return fd.Name;
			}
			return null;
		}
	}
}
