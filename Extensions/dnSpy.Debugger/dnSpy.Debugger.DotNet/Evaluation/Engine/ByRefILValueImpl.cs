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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class ByRefILValueImpl : ByRefILValue, IDebuggerRuntimeILValue {
		public override DmdType Type => byRefValue.Type;
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => byRefValue;

		readonly DebuggerRuntimeImpl runtime;
		readonly DbgDotNetValue byRefValue;

		public ByRefILValueImpl(DebuggerRuntimeImpl runtime, DbgDotNetValue byRefValue) {
			this.runtime = runtime;
			this.byRefValue = byRefValue;
		}

		public override bool StoreField(DmdFieldInfo field, ILValue value) =>
			runtime.StoreInstanceField(byRefValue, field, value);

		public override ILValue LoadField(DmdFieldInfo field) =>
			runtime.LoadInstanceField(byRefValue, field);

		public override ILValue LoadFieldAddress(DmdFieldInfo field) {
			if (Type.GetElementType().IsValueType)
				return runtime.LoadValueTypeFieldAddress(this, field);
			return null;
		}

		public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			runtime.CallInstance(byRefValue, isCallvirt, method, arguments, out returnValue);

		public override ILValue LoadIndirect(DmdType type, LoadValueType loadValueType) {
			if (byRefValue.IsNullReference)
				return new ByRefNullObjectRefILValueImpl(byRefValue);
			return runtime.CreateILValue(byRefValue.Dereference());
		}

		public override bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) {
			return false;//TODO:
		}

		public override bool InitializeObject(DmdType type) {
			return false;//TODO:
		}

		public override bool CopyObject(DmdType type, ILValue source) {
			return false;//TODO:
		}
	}
}
