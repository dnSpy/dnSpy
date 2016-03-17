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

//TODO: Use the included C# parser

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Debugger.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Locals {
	sealed class ValueStringParser {
		readonly string text;
		readonly bool isTextNullString;

		public bool IsNull {
			get { return isTextNullString; }
		}

		public ValueStringParser(string text) {
			this.text = text.Trim();
			this.isTextNullString = this.text == "null";
		}

		string GetEnumValue(CorType type, out byte[] bytes) {
			bytes = null;
			Debug.Assert(type != null && type.IsEnum);

			if (text == string.Empty)
				return dnSpy_Debugger_Resources.LocalsEditValue_Error_EnterSomeText;

			var etype = type.EnumUnderlyingType;
			if (etype == CorElementType.End)
				return "Internal error: type is not an enum";

			var consts = text.Split('|').Select(a => a.Trim()).ToArray();

			bool isFlagsAttr = type.HasAttribute("System.FlagsAttribute");
			var fields = type.GetFields(false).Where(a => a.Constant != null && (a.Attributes & (FieldAttributes.Literal | FieldAttributes.Static)) == (FieldAttributes.Literal | FieldAttributes.Static));
			bool isInteger = !(etype == CorElementType.R4 || etype == CorElementType.R8);
			if (isFlagsAttr && isInteger) {
				var dict = new Dictionary<string, object>();
				foreach (var f in fields) {
					if (!dict.ContainsKey(f.Name))
						dict[f.Name] = f.Constant;
				}
				ulong value = 0;
				foreach (var c in consts) {
					ulong? newv;
					object o;
					string error;
					if (dict.TryGetValue(c, out o))
						newv = IntegerToUInt64ZeroExtend(o);
					else
						newv = ParseIntegerConstant(etype, c, out error);
					if (newv == null)
						return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_UnknownEnumValue, c);
					value |= newv.Value;
				}

				bytes = Convert(etype, ConvertUInt64(etype, value));
				if (bytes != null)
					return null;
			}
			else {
				if (consts.Length != 1)
					return dnSpy_Debugger_Resources.LocalsEditValue_Error_InvalidEnumValue;

				var c = consts[0];
				foreach (var field in fields) {
					if (field.Name == c) {
						bytes = Convert(etype, field.Constant);
						if (bytes != null)
							return null;
					}
				}

				if (isInteger) {
					string error;
					var newv = ParseIntegerConstant(etype, c, out error);
					if (newv != null) {
						bytes = Convert(etype, ConvertUInt64(etype, newv.Value));
						if (bytes != null)
							return null;
					}
				}
				else {
					if (etype == CorElementType.R4) {
						float v;
						if (float.TryParse(c, out v)) {
							bytes = BitConverter.GetBytes(v);
							return null;
						}
					}
					else {
						Debug.Assert(etype == CorElementType.R8);
						double v;
						if (double.TryParse(c, out v)) {
							bytes = BitConverter.GetBytes(v);
							return null;
						}
					}
				}
			}

			return dnSpy_Debugger_Resources.LocalsEditValue_Error_InvalidEnumValue;
		}

		static object ConvertUInt64(CorElementType etype, ulong v) {
			switch (etype) {
			case CorElementType.Boolean:
				return v != 0;
			case CorElementType.I:
				if (IntPtr.Size == 4)
					goto case CorElementType.I4;
				goto case CorElementType.I8;
			case CorElementType.I1:		return (sbyte)v;
			case CorElementType.I2:		return (short)v;
			case CorElementType.I4:		return (int)v;
			case CorElementType.I8:		return (long)v;
			case CorElementType.U:
				if (UIntPtr.Size == 4)
					goto case CorElementType.U4;
				goto case CorElementType.U8;
			case CorElementType.U1:		return (byte)v;
			case CorElementType.U2:		return (ushort)v;
			case CorElementType.U4:		return (uint)v;
			case CorElementType.U8:		return v;
			}
			return null;
		}

		static ulong? ParseIntegerConstant(CorElementType etype, string c, out string error) {
			error = null;
			long smin, smax;
			ulong max;
			switch (etype) {
			case CorElementType.Boolean:
				smin = 0;
				smax = 1;
				max = 1;
				break;
			case CorElementType.I:
				if (IntPtr.Size == 4)
					goto case CorElementType.I4;
				goto case CorElementType.I8;
			case CorElementType.U:
			case CorElementType.Ptr:
			case CorElementType.FnPtr:
				if (UIntPtr.Size == 4)
					goto case CorElementType.U4;
				goto case CorElementType.U8;
			case CorElementType.I1:
			case CorElementType.U1:
				smin = sbyte.MinValue;
				smax = sbyte.MaxValue;
				max = byte.MaxValue;
				break;
			case CorElementType.I2:
			case CorElementType.U2:
				smin = short.MinValue;
				smax = short.MaxValue;
				max = ushort.MaxValue;
				break;
			case CorElementType.I4:
			case CorElementType.U4:
				smin = int.MinValue;
				smax = int.MaxValue;
				max = uint.MaxValue;
				break;
			case CorElementType.I8:
			case CorElementType.U8:
				smin = long.MinValue;
				smax = long.MaxValue;
				max = ulong.MaxValue;
				break;
			default:
				return null;
			}
			ulong v = NumberVMUtils.ParseUInt64(c, 0, max, out error);
			if (string.IsNullOrEmpty(error))
				return v;

			v = (ulong)NumberVMUtils.ParseInt64(c, smin, smax, out error);
			if (string.IsNullOrEmpty(error))
				return v;

			return null;
		}

		static ulong? IntegerToUInt64ZeroExtend(object o) {
			if (o == null)
				return null;
			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:	return (bool)o ? 1UL : 0UL;
			case TypeCode.Char:		return (char)o;
			case TypeCode.SByte:	return (byte)(sbyte)o;
			case TypeCode.Int16:	return (ushort)(short)o;
			case TypeCode.Int32:	return (uint)(int)o;
			case TypeCode.Int64:	return (ulong)(long)o;
			case TypeCode.Byte:		return (byte)o;
			case TypeCode.UInt16:	return (ushort)o;
			case TypeCode.UInt32:	return (uint)o;
			case TypeCode.UInt64:	return (ulong)o;
			}
			if (o is IntPtr)
				return (ulong)((IntPtr)o).ToInt64();
			if (o is UIntPtr)
				return ((UIntPtr)o).ToUInt64();
			return null;
		}

		static byte[] Convert(CorElementType etype, object c) {
			var tc = c == null ? TypeCode.Empty : Type.GetTypeCode(c.GetType());
			switch (tc) {
			case TypeCode.Boolean:
				if (etype == CorElementType.Boolean)
					return new byte[1] { (byte)((bool)c ? 1 : 0) };
				return null;

			case TypeCode.Char:
				if (etype == CorElementType.Char)
					return BitConverter.GetBytes((char)c);
				return null;

			case TypeCode.SByte:
				if (etype == CorElementType.I1)
					return new byte[1] { (byte)(sbyte)c };
				return null;

			case TypeCode.Int16:
				if (etype == CorElementType.I2)
					return BitConverter.GetBytes((short)c);
				return null;

			case TypeCode.Int32:
				if (etype == CorElementType.I4)
					return BitConverter.GetBytes((int)c);
				return null;

			case TypeCode.Int64:
				if (etype == CorElementType.I8)
					return BitConverter.GetBytes((long)c);
				return null;

			case TypeCode.Byte:
				if (etype == CorElementType.U1)
					return new byte[1] { (byte)c };
				return null;

			case TypeCode.UInt16:
				if (etype == CorElementType.U2)
					return BitConverter.GetBytes((ushort)c);
				return null;

			case TypeCode.UInt32:
				if (etype == CorElementType.U4)
					return BitConverter.GetBytes((uint)c);
				return null;

			case TypeCode.UInt64:
				if (etype == CorElementType.U8)
					return BitConverter.GetBytes((ulong)c);
				return null;

			case TypeCode.Single:
				if (etype == CorElementType.R4)
					return BitConverter.GetBytes((float)c);
				return null;

			case TypeCode.Double:
				if (etype == CorElementType.R8)
					return BitConverter.GetBytes((double)c);
				return null;
			}

			return null;
		}

		public string GetPrimitiveValue(CorType type, out byte[] bytes) {
			bytes = null;
			if (type == null)
				return "Internal error: CorType is null";

			if (type.IsEnum)
				return GetEnumValue(type, out bytes);

			string error;
			var etype = type.TryGetPrimitiveType();

			switch (etype) {
			case CorElementType.Boolean:
				{
					var value = NumberVMUtils.ParseBoolean(text, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(value);
					return null;
				}

			case CorElementType.Char:
				{
					var value = NumberVMUtils.ParseChar(text, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(value);
					return null;
				}

			case CorElementType.R4:
				{
					var value = NumberVMUtils.ParseSingle(text, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(value);
					return null;
				}

			case CorElementType.R8:
				{
					var value = NumberVMUtils.ParseDouble(text, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(value);
					return null;
				}

			case CorElementType.Class:
			case CorElementType.ValueType:
				if (type.IsSystemDecimal) {
					var value = NumberVMUtils.ParseDecimal(text, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = GetBytes(value);
					return null;
				}
				return null;

			case CorElementType.I:
			case CorElementType.U:
			case CorElementType.Ptr:
			case CorElementType.FnPtr:
				if (text.Trim() == "null") {
					bytes = new byte[IntPtr.Size];
					return null;
				}
				break;
			}

			ulong? res = ParseIntegerConstant(etype, text, out error);
			if (res == null)
				return error ?? dnSpy_Debugger_Resources.LocalsEditValue_Error_InvalidNumber;

			switch (etype) {
			case CorElementType.I1:
				bytes = new byte[1] { (byte)res.Value };
				return null;

			case CorElementType.U1:
				bytes = new byte[1] { (byte)res.Value };
				return null;

			case CorElementType.I2:
				bytes = BitConverter.GetBytes((short)res.Value);
				return null;

			case CorElementType.U2:
				bytes = BitConverter.GetBytes((ushort)res.Value);
				return null;

			case CorElementType.I4:
				bytes = BitConverter.GetBytes((int)res.Value);
				return null;

			case CorElementType.U4:
				bytes = BitConverter.GetBytes((uint)res.Value);
				return null;

			case CorElementType.I8:
				bytes = BitConverter.GetBytes((long)res.Value);
				return null;

			case CorElementType.U8:
				bytes = BitConverter.GetBytes(res.Value);
				return null;

			case CorElementType.I:
			case CorElementType.U:
			case CorElementType.Ptr:
			case CorElementType.FnPtr:
				if (IntPtr.Size == 4)
					goto case CorElementType.I4;
				goto case CorElementType.I8;
			}

			return "Unknown number type";
		}

		static byte[] GetBytes(decimal d) {
			var decimalBits = decimal.GetBits(d);
			var bytes = new byte[16];
			WriteInt32(bytes, 0, decimalBits[3]);
			WriteInt32(bytes, 4, decimalBits[2]);
			WriteInt32(bytes, 8, decimalBits[0]);
			WriteInt32(bytes, 12, decimalBits[1]);
			return bytes;
		}

		static void WriteInt32(byte[] dest, int index, int v) {
			dest[index + 0] = (byte)v;
			dest[index + 1] = (byte)(v >> 8);
			dest[index + 2] = (byte)(v >> 16);
			dest[index + 3] = (byte)(v >> 24);
		}

		public string GetString(out string s) {
			string error;
			s = NumberVMUtils.ParseString(text, true, out error);
			return error;
		}
	}
}
