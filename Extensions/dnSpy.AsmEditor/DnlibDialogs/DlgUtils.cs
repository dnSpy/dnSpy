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

using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.DnlibDialogs {
	static class DlgUtils {
		public static string ValueToString(object value, TypeSig storageType) {
			var t = storageType.RemovePinnedAndModifiers();
			bool addCast = t.GetElementType() == ElementType.Object;
			if (t is SZArraySig)
				addCast = t.Next.RemovePinnedAndModifiers().GetElementType() == ElementType.Object;
			return ValueToString(value, addCast);
		}

		static string AddCast(string s, bool addCast, string cast) => addCast ? string.Format("({0}){1}", cast, s) : s;

		public static string ValueToString(object value, bool addCast) {
			if (value == null)
				return "null";

			switch (ModelUtils.GetElementType(value.GetType())) {
			case ElementType.Boolean:return SimpleTypeConverter.ToString((bool)value);
			case ElementType.Char:	return SimpleTypeConverter.ToString((char)value);
			case ElementType.I1:	return AddCast(SimpleTypeConverter.ToString((sbyte)value), addCast, value.GetType().FullName);
			case ElementType.U1:	return AddCast(SimpleTypeConverter.ToString((byte)value), addCast, value.GetType().FullName);
			case ElementType.I2:	return AddCast(SimpleTypeConverter.ToString((short)value), addCast, value.GetType().FullName);
			case ElementType.U2:	return AddCast(SimpleTypeConverter.ToString((ushort)value), addCast, value.GetType().FullName);
			case ElementType.I4:	return AddCast(SimpleTypeConverter.ToString((int)value), addCast, value.GetType().FullName);
			case ElementType.U4:	return AddCast(SimpleTypeConverter.ToString((uint)value), addCast, value.GetType().FullName);
			case ElementType.I8:	return AddCast(SimpleTypeConverter.ToString((long)value), addCast, value.GetType().FullName);
			case ElementType.U8:	return AddCast(SimpleTypeConverter.ToString((ulong)value), addCast, value.GetType().FullName);
			case ElementType.R4:	return AddCast(SimpleTypeConverter.ToString((float)value), addCast, value.GetType().FullName);
			case ElementType.R8:	return AddCast(SimpleTypeConverter.ToString((double)value), addCast, value.GetType().FullName);
			case ElementType.String:return SimpleTypeConverter.ToString((string)value, true);
			}
			if (value is TypeSig)
				return string.Format("typeof({0})", value);

			var valueType = value.GetType();
			if (value is IList<bool>)
				return ArrayToString(value, typeof(bool));
			if (value is IList<char>)
				return ArrayToString(value, typeof(char));
			if (value is IList<sbyte> && valueType != typeof(byte[]))
				return ArrayToString(value, typeof(sbyte));
			if (value is IList<short> && valueType != typeof(ushort[]))
				return ArrayToString(value, typeof(short));
			if (value is IList<int> && valueType != typeof(uint[]))
				return ArrayToString(value, typeof(int));
			if (value is IList<long> && valueType != typeof(ulong[]))
				return ArrayToString(value, typeof(long));
			if (value is IList<byte> && valueType != typeof(sbyte[]))
				return ArrayToString(value, typeof(byte));
			if (value is IList<ushort> && valueType != typeof(short[]))
				return ArrayToString(value, typeof(ushort));
			if (value is IList<uint> && valueType != typeof(int[]))
				return ArrayToString(value, typeof(uint));
			if (value is IList<ulong> && valueType != typeof(long[]))
				return ArrayToString(value, typeof(ulong));
			if (value is IList<float>)
				return ArrayToString(value, typeof(float));
			if (value is IList<double>)
				return ArrayToString(value, typeof(double));
			if (value is IList<string>)
				return ArrayToString(value, typeof(string));
			if (value is IList<TypeSig>)
				return ArrayToString(value, typeof(Type));
			if (value is IList<object>)
				return ArrayToString(value, typeof(object));

			return value.ToString();
		}

		static string ArrayToString(object value, Type type) {
			var list = value as System.Collections.IList;
			if (list == null)
				return string.Format("({0}[])null", type.FullName);

			var sb = new StringBuilder();
			sb.Append(string.Format("new {0}[] {{", type.FullName));
			for (int i = 0; i < list.Count; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(' ');
				sb.Append(ValueToString(list[i], type == typeof(object)));
			}
			sb.Append(" }");
			return sb.ToString();
		}
	}
}
