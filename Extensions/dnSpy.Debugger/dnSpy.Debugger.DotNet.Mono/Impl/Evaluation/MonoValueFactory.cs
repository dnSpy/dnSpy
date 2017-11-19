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
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	static class MonoValueFactory {
		public static Value TryCreateSyntheticValue(AppDomainMirror appDomain, object constant) {
			var vm = appDomain.VirtualMachine;
			if (constant == null)
				return new PrimitiveValue(vm, ElementType.Object, null);
			switch (Type.GetTypeCode(constant.GetType())) {
			case TypeCode.Boolean:
				if (constant is bool)
					return new PrimitiveValue(vm, ElementType.Boolean, constant);
				break;

			case TypeCode.Char:
				if (constant is char)
					return new PrimitiveValue(vm, ElementType.Char, constant);
				break;

			case TypeCode.SByte:
				if (constant is sbyte)
					return new PrimitiveValue(vm, ElementType.I1, constant);
				break;

			case TypeCode.Byte:
				if (constant is byte)
					return new PrimitiveValue(vm, ElementType.U1, constant);
				break;

			case TypeCode.Int16:
				if (constant is short)
					return new PrimitiveValue(vm, ElementType.I2, constant);
				break;

			case TypeCode.UInt16:
				if (constant is ushort)
					return new PrimitiveValue(vm, ElementType.U2, constant);
				break;

			case TypeCode.Int32:
				if (constant is int)
					return new PrimitiveValue(vm, ElementType.I4, constant);
				break;

			case TypeCode.UInt32:
				if (constant is uint)
					return new PrimitiveValue(vm, ElementType.U4, constant);
				break;

			case TypeCode.Int64:
				if (constant is long)
					return new PrimitiveValue(vm, ElementType.I8, constant);
				break;

			case TypeCode.UInt64:
				if (constant is ulong)
					return new PrimitiveValue(vm, ElementType.U8, constant);
				break;

			case TypeCode.Single:
				if (constant is float)
					return new PrimitiveValue(vm, ElementType.R4, constant);
				break;

			case TypeCode.Double:
				if (constant is double)
					return new PrimitiveValue(vm, ElementType.R8, constant);
				break;

			case TypeCode.String:
				if (constant is string)
					return appDomain.CreateString((string)constant);
				break;
			}
			return null;
		}
	}
}
