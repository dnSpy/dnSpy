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
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	abstract class AddressILValue : ByRefILValue {
		public override DmdType Type { get; }

		protected readonly DebuggerRuntimeImpl runtime;

		protected AddressILValue(DebuggerRuntimeImpl runtime, DmdType locationType) {
			this.runtime = runtime;
			Type = locationType.MakeByRefType();
		}

		protected abstract DbgDotNetValue ReadValue();
		protected abstract void WriteValue(object value);

		DbgDotNetValue ReadValueImpl() => ReadValue() ?? throw new InvalidOperationException();

		public override bool StoreField(DmdFieldInfo field, ILValue value) =>
			runtime.StoreInstanceField(ReadValueImpl(), field, value);

		public override ILValue LoadField(DmdFieldInfo field) =>
			runtime.LoadInstanceField(ReadValueImpl(), field);

		public override ILValue LoadFieldAddress(DmdFieldInfo field) {
			if (field.ReflectedType.IsValueType)
				return runtime.LoadValueTypeFieldAddress(this, field);
			return null;
		}

		public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			runtime.CallInstance(ReadValueImpl(), isCallvirt, method, arguments, out returnValue);

		public ILValue LoadIndirect() => runtime.CreateILValue(ReadValueImpl());
		public override ILValue LoadIndirect(DmdType type, LoadValueType loadValueType) => LoadIndirect();

		public override bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) {
			WriteValue(runtime.GetDebuggerValue(value, Type.GetElementType()));
			return true;
		}

		public override bool InitializeObject(DmdType type) {
			var defaultValue = runtime.GetDefaultValue(type);
			WriteValue(defaultValue);
			return true;
		}

		public override bool CopyObject(DmdType type, ILValue source) {
			var sourceAddr = source as AddressILValue;
			if (sourceAddr == null)
				return false;
			WriteValue(sourceAddr.ReadValueImpl());
			return true;
		}

		public abstract bool Equals(AddressILValue other);
	}

	sealed class StaticFieldAddress : AddressILValue {
		readonly DmdFieldInfo field;

		public StaticFieldAddress(DebuggerRuntimeImpl runtime, DmdFieldInfo field)
			: base(runtime, field.FieldType) {
			this.field = field;
		}

		protected override DbgDotNetValue ReadValue() => runtime.LoadStaticField2(field);
		protected override void WriteValue(object value) => runtime.StoreStaticField(field, value);

		public override bool Equals(AddressILValue other) =>
			other is StaticFieldAddress addr &&
			addr.field == field;
	}

	sealed class ReferenceTypeFieldAddress : AddressILValue {
		readonly DbgDotNetValue objValue;
		readonly DmdFieldInfo field;

		public ReferenceTypeFieldAddress(DebuggerRuntimeImpl runtime, DbgDotNetValue objValue, DmdFieldInfo field)
			: base(runtime, field.FieldType) {
			Debug.Assert(!field.ReflectedType.IsValueType && !objValue.Type.IsArray);
			this.objValue = objValue;
			this.field = field;
		}

		protected override DbgDotNetValue ReadValue() => runtime.LoadInstanceField2(objValue, field);
		protected override void WriteValue(object value) => runtime.StoreInstanceField(objValue, field, value);

		public override bool Equals(AddressILValue other) =>
			other is ReferenceTypeFieldAddress addr &&
			addr.field == field &&
			runtime.Equals(objValue, addr.objValue);
	}

	sealed class ValueTypeFieldAddress : AddressILValue {
		readonly AddressILValue objValue;
		readonly DmdFieldInfo field;

		public ValueTypeFieldAddress(DebuggerRuntimeImpl runtime, AddressILValue objValue, DmdFieldInfo field)
			: base(runtime, field.FieldType) {
			Debug.Assert(field.ReflectedType.IsValueType);
			this.objValue = objValue;
			this.field = field;
		}

		protected override DbgDotNetValue ReadValue() => runtime.LoadInstanceField2(runtime.GetDotNetValue(objValue.LoadIndirect()), field);
		protected override void WriteValue(object value) => runtime.StoreInstanceField(runtime.GetDotNetValue(objValue.LoadIndirect()), field, value);

		public override bool Equals(AddressILValue other) =>
			other is ValueTypeFieldAddress addr &&
			addr.field == field &&
			runtime.Equals(objValue, addr.objValue) == true;
	}

	sealed class ArrayElementAddress : AddressILValue {
		readonly ArrayILValue arrayValue;
		readonly uint index;

		public ArrayElementAddress(DebuggerRuntimeImpl runtime, ArrayILValue arrayValue, uint index)
			: base(runtime, arrayValue.Type.GetElementType()) {
			Debug.Assert(arrayValue.Type.IsArray);
			this.arrayValue = arrayValue;
			this.index = index;
		}

		protected override DbgDotNetValue ReadValue() => arrayValue.ReadArrayElement(index);
		protected override void WriteValue(object value) => arrayValue.StoreArrayElement(index, value);

		public override bool Equals(AddressILValue other) =>
			other is ArrayElementAddress addr &&
			index == addr.index &&
			runtime.Equals(arrayValue, addr.arrayValue) == true;
	}

	sealed class LocalAddress : AddressILValue {
		readonly DmdType localType;
		readonly int index;

		public LocalAddress(DebuggerRuntimeImpl runtime, DmdType localType, int index)
			: base(runtime, localType) {
			this.localType = localType;
			this.index = index;
		}

		protected override DbgDotNetValue ReadValue() => runtime.LoadLocal2(index);
		protected override void WriteValue(object value) => runtime.StoreLocal2(index, localType, value);

		public override bool Equals(AddressILValue other) =>
			other is LocalAddress addr &&
			index == addr.index;
	}

	sealed class ArgumentAddress : AddressILValue {
		readonly DmdType argumentType;
		readonly int index;

		public ArgumentAddress(DebuggerRuntimeImpl runtime, DmdType argumentType, int index)
			: base(runtime, argumentType) {
			this.argumentType = argumentType;
			this.index = index;
		}

		protected override DbgDotNetValue ReadValue() => runtime.LoadArgument2(index);
		protected override void WriteValue(object value) => runtime.StoreArgument2(index, argumentType, value);

		public override bool Equals(AddressILValue other) =>
			other is ArgumentAddress addr &&
			index == addr.index;
	}
}
