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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	class TypeILValueImpl : TypeILValue, IDebuggerRuntimeILValue {
		public sealed override DmdType Type { get; }
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => ObjValue;

		protected virtual DbgDotNetValue CreateObjValue() => null;
		protected DbgDotNetValue ObjValue {
			get {
				if (__objValue_DONT_USE == null) {
					__objValue_DONT_USE = CreateObjValue();
					if (__objValue_DONT_USE == null)
						throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
				}
				return __objValue_DONT_USE;
			}
		}
		DbgDotNetValue __objValue_DONT_USE;

		protected readonly DebuggerRuntimeImpl runtime;

		protected TypeILValueImpl(DebuggerRuntimeImpl runtime, DmdType type) {
			this.runtime = runtime;
			__objValue_DONT_USE = null;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public TypeILValueImpl(DebuggerRuntimeImpl runtime, DbgDotNetValue objValue, DmdType type = null) {
			this.runtime = runtime;
			__objValue_DONT_USE = objValue ?? throw new ArgumentNullException(nameof(objValue));
			Type = type ?? objValue.Type;
		}

		public override bool StoreField(DmdFieldInfo field, ILValue value) =>
			runtime.StoreInstanceField(ObjValue, field, value);

		public override ILValue LoadField(DmdFieldInfo field) =>
			runtime.LoadInstanceField(ObjValue, field);

		public override ILValue LoadFieldAddress(DmdFieldInfo field) {
			if (!field.ReflectedType.IsValueType)
				return runtime.LoadReferenceTypeFieldAddress(ObjValue, field);
			return null;
		}

		public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			runtime.CallInstance(ObjValue, isCallvirt, method, arguments, out returnValue);
	}

	sealed class ConstantStringILValueImpl : TypeILValueImpl {
		public string Value { get; }
		public ConstantStringILValueImpl(DebuggerRuntimeImpl runtime, DbgDotNetValue value, string s) : base(runtime, value) => Value = s;
	}
}
