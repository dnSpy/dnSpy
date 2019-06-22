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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter.Hooks {
	sealed class System_Runtime_CompilerServices_RuntimeHelpers : DotNetClassHook {
		readonly IDebuggerRuntime runtime;

		public System_Runtime_CompilerServices_RuntimeHelpers(IDebuggerRuntime runtime) => this.runtime = runtime;

		public override DbgDotNetValue? Call(DotNetClassHookCallOptions options, DbgDotNetValue? objValue, DmdMethodBase method, ILValue[] arguments) {
			switch (method.Name) {
			case "InitializeArray":
				if (!method.IsStatic)
					break;
				var sig = method.GetMethodSignature();
				if (sig.Flags != DmdSignatureCallingConvention.Default)
					break;
				var ps = sig.GetParameterTypes();
				if (ps.Count != 2)
					break;
				var appDomain = method.AppDomain;
				if (ps[0] != appDomain.System_Array || ps[1] != appDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle, isOptional: true))
					break;
				var field = (arguments[1] as RuntimeFieldHandleILValue)?.Field;
				if (field is null)
					break;
				var arrayValue = runtime.ToDotNetValue(arguments[0]);
				if (arrayValue.IsNull)
					break;
				if (!arrayValue.Type.IsArray)
					break;
				var addr = arrayValue.GetRawAddressValue(onlyDataAddress: true);
				if (addr is null)
					break;
				if (!TryGetSize(field.FieldType, out int fieldTypeSize))
					break;
				if (addr.Value.Length != (uint)fieldTypeSize)
					break;
				if (!field.HasFieldRVA || field.FieldRVA == 0)
					break;
				var data = field.Module.ReadMemory(field.FieldRVA, fieldTypeSize);
				if (data is null)
					break;
				var process = field.AppDomain.Runtime.GetDebuggerRuntime().Process;
				process.WriteMemory(addr.Value.Address, data, 0, data.Length);
				return new SyntheticNullValue(field.AppDomain.System_Object);
			}

			return null;
		}

		static bool TryGetSize(DmdType type, out int size) {
			if (type.IsValueType) {
				var attr = type.StructLayoutAttribute;
				if (!(attr is null) && attr.Size >= 0) {
					size = attr.Size;
					return true;
				}
			}

			size = -1;
			return false;
		}
	}
}
