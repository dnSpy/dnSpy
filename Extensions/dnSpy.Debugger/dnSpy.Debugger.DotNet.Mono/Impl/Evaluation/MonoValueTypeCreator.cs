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
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	static class MonoValueTypeCreator {
		public static DmdType CreateType(DbgEngineImpl engine, Value value, DmdType slotType) {
			var res = CreateTypeCore(engine, value, slotType);
			if (slotType.IsByRef && !res.IsByRef)
				res = res.MakeByRefType();
			return res;
		}

		static DmdType CreateTypeCore(DbgEngineImpl engine, Value value, DmdType slotType) {
			var reflectionAppDomain = slotType.AppDomain;
			switch (value) {
			case PrimitiveValue pv:
				switch (pv.Type) {
				case ElementType.Boolean:	return reflectionAppDomain.System_Boolean;
				case ElementType.Char:		return reflectionAppDomain.System_Char;
				case ElementType.I1:		return reflectionAppDomain.System_SByte;
				case ElementType.U1:		return reflectionAppDomain.System_Byte;
				case ElementType.I2:		return reflectionAppDomain.System_Int16;
				case ElementType.U2:		return reflectionAppDomain.System_UInt16;
				case ElementType.I4:		return reflectionAppDomain.System_Int32;
				case ElementType.U4:		return reflectionAppDomain.System_UInt32;
				case ElementType.I8:		return reflectionAppDomain.System_Int64;
				case ElementType.U8:		return reflectionAppDomain.System_UInt64;
				case ElementType.R4:		return reflectionAppDomain.System_Single;
				case ElementType.R8:		return reflectionAppDomain.System_Double;
				case ElementType.I:			return reflectionAppDomain.System_IntPtr;
				case ElementType.U:			return reflectionAppDomain.System_UIntPtr;
				case ElementType.Ptr:		return slotType.IsPointer ? slotType : reflectionAppDomain.System_Void.MakePointerType();
				case ElementType.Object:	return slotType;// This is a null value
				default:					throw new InvalidOperationException();
				}

			case ObjectMirror om:
				return engine.GetReflectionType(reflectionAppDomain, om.Type, slotType);

			case StructMirror sm:
				return engine.GetReflectionType(reflectionAppDomain, sm.Type, slotType);

			default:
				throw new InvalidOperationException();
			}
		}
	}
}
