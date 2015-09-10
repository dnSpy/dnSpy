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
using dndbg.Engine.COM.CorDebug;
using dndbg.Engine.COM.MetaData;

namespace dndbg.Engine {
	struct CorValueReader {
		public struct Result {
			public readonly object Value;
			public readonly bool IsValueValid;

			public Result(object value) {
				this.Value = value;
				this.IsValueValid = true;
			}
		}

		public static Result ReadSimpleTypeValue(CorValue value) {
			if (value == null)
				return new Result();

			if (value.IsReference && value.Type == CorElementType.ByRef) {
				if (value.IsNull)
					return new Result(null);
				value = value.DereferencedValue;
				if (value == null)
					return new Result();
			}
			if (value.IsReference) {
				if (value.IsNull)
					return new Result(null);
				if (value.Type == CorElementType.Ptr) {
					if (Utils.DebuggeeIntPtrSize == 4)
						return new Result((uint)value.ReferenceAddress);
					return new Result(value.ReferenceAddress);
				}
				value = value.DereferencedValue;
				if (value == null)
					return new Result();
			}
			if (value.IsBox) {
				value = value.BoxedValue;
				if (value == null)
					return new Result();
				var cls = value.Class;
				if (cls == null)
					return new Result();
				var etype = GetValueClassElementType(cls);
				var vres = GetSimpleResult(value, etype);
				return vres ?? new Result();
			}
			if (value.IsReference)
				return new Result();
			if (value.IsBox)
				return new Result();
			if (value.IsArray)
				return new Result();
			if (value.IsString)
				return new Result(value.String);

			var res = GetSimpleResult(value, value.Type);
			return res ?? new Result();
		}

		static CorElementType GetValueClassElementType(CorClass cls) {
			const CorElementType DEFAULT_VALUE = CorElementType.ValueType;

			if (cls == null)
				return CorElementType.End;
			var module = cls.Module;
			if (module == null)
				return DEFAULT_VALUE;
			var list = MetaDataUtils.GetTypeDefFullNames(module.GetMetaDataInterface<IMetaDataImport>(), cls.Token);
			if (list.Count != 1)
				return DEFAULT_VALUE;

			switch (list[0].Name) {
			case "System.Boolean":	return CorElementType.Boolean;
			case "System.Byte":		return CorElementType.U1;
			case "System.Char":		return CorElementType.Char;
			case "System.Double":	return CorElementType.R8;
			case "System.Int16":	return CorElementType.I2;
			case "System.Int32":	return CorElementType.I4;
			case "System.Int64":	return CorElementType.I8;
			case "System.IntPtr":	return CorElementType.I;
			case "System.Object":	return CorElementType.Object;
			case "System.SByte":	return CorElementType.I1;
			case "System.Single":	return CorElementType.R4;
			case "System.String":	return CorElementType.String;
			case "System.TypedReference": return CorElementType.TypedByRef;
			case "System.UInt16":	return CorElementType.U2;
			case "System.UInt32":	return CorElementType.U4;
			case "System.UInt64":	return CorElementType.U8;
			case "System.UIntPtr":	return CorElementType.U;
			case "System.Void":		return CorElementType.Void;
			default: return DEFAULT_VALUE;
			}
		}

		static Result? GetSimpleResult(CorValue value, CorElementType etype) {
			byte[] data;
			switch (etype) {
			case CorElementType.Boolean:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(data[0] != 0);

			case CorElementType.Char:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToChar(data, 0));

			case CorElementType.I1:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result((sbyte)data[0]);

			case CorElementType.U1:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(data[0]);

			case CorElementType.I2:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToInt16(data, 0));

			case CorElementType.U2:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToUInt16(data, 0));

			case CorElementType.I4:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToInt32(data, 0));

			case CorElementType.U4:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToUInt32(data, 0));

			case CorElementType.I8:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToInt64(data, 0));

			case CorElementType.U8:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToUInt64(data, 0));

			case CorElementType.R4:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToSingle(data, 0));

			case CorElementType.R8:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new Result(BitConverter.ToDouble(data, 0));

			case CorElementType.TypedByRef:
				break;//TODO:

			case CorElementType.I:
				if (value.Size != (uint)Utils.DebuggeeIntPtrSize)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				if (Utils.DebuggeeIntPtrSize == 4)
					return new Result(BitConverter.ToInt32(data, 0));
				return new Result(BitConverter.ToInt64(data, 0));

			case CorElementType.U:
			case CorElementType.Ptr:
				if (value.Size != (uint)Utils.DebuggeeIntPtrSize)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				if (Utils.DebuggeeIntPtrSize == 4)
					return new Result(BitConverter.ToUInt32(data, 0));
				return new Result(BitConverter.ToUInt64(data, 0));

			case CorElementType.ValueType:
				var cls = value.Class;
				if (cls == null)
					break;
				var module = cls.Module;
				if (module == null)
					break;
				var list = MetaDataUtils.GetTypeDefFullNames(module.GetMetaDataInterface<IMetaDataImport>(), cls.Token);
				if (list.Count != 1)
					break;
				if (list[0].Name != "System.Decimal")
					break;
				if (value.Size != 16)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;

				var decimalBits = new int[4];
				decimalBits[3] = BitConverter.ToInt32(data, 0);
				decimalBits[2] = BitConverter.ToInt32(data, 4);
				decimalBits[0] = BitConverter.ToInt32(data, 8);
				decimalBits[1] = BitConverter.ToInt32(data, 12);
				try {
					return new Result(new decimal(decimalBits));
				}
				catch (ArgumentException) {
				}
				break;
			}

			return null;
		}
	}
}
