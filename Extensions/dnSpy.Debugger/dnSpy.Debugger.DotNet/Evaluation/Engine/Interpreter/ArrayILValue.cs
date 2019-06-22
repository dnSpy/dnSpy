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
	sealed class ArrayILValue : TypeILValueImpl {
		long cachedArrayLength;
		const long cachedArrayLength_uninitialized = -1;
		const long cachedArrayLength_error = -2;

		uint elementCount;
		DbgDotNetArrayDimensionInfo[]? dimensionInfos;

		public ArrayILValue(DebuggerRuntimeImpl runtime, DbgDotNetValue arrayValue)
			: base(runtime, arrayValue) {
			cachedArrayLength = cachedArrayLength_uninitialized;
		}

		void InitializeArrayInfo() {
			if (!(dimensionInfos is null))
				return;
			if (!ObjValue.GetArrayInfo(out elementCount, out dimensionInfos))
				throw new InvalidOperationException();
		}

		public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) {
			switch (method.SpecialMethodKind) {
			case DmdSpecialMethodKind.Array_Get:
				returnValue = LoadArrayElement(GetZeroBasedIndex(arguments, arguments.Length));
				return !(returnValue is null);

			case DmdSpecialMethodKind.Array_Set:
				StoreArrayElement(GetZeroBasedIndex(arguments, arguments.Length - 1), arguments[arguments.Length - 1]);
				returnValue = null;
				return true;

			case DmdSpecialMethodKind.Array_Address:
				uint index = GetZeroBasedIndex(arguments, arguments.Length);
				var addrValue = ObjValue.GetArrayElementAddressAt(index);
				if (!(addrValue is null)) {
					runtime.RecordValue(addrValue.Value);
					Debug.Assert(addrValue.Value.Value!.Type.IsByRef);
					returnValue = new ByRefILValueImpl(runtime, addrValue.Value.Value);
				}
				else
					returnValue = new ArrayElementAddress(runtime, this, index);
				return true;
			}

			return base.Call(isCallvirt, method, arguments, out returnValue);
		}

		uint GetZeroBasedIndex(ILValue[] indexes, int count) {
			if (dimensionInfos is null)
				InitializeArrayInfo();
			if (dimensionInfos!.Length != count)
				throw new InvalidOperationException();

			uint result = 0;

			for (int i = 0; i < dimensionInfos.Length; i++) {
				ref readonly var dim = ref dimensionInfos[i];
				uint index = (uint)(runtime.ToInt32(indexes[i]) - dim.BaseIndex);
				if (index >= dim.Length)
					throw new InvalidOperationException();
				result = checked(result * dim.Length + index);
			}

			return result;
		}

		internal DbgDotNetValue? ReadArrayElement(long index) {
			if ((ulong)index > uint.MaxValue)
				return null;
			return runtime.RecordValue(ObjValue.GetArrayElementAt((uint)index));
		}

		void StoreArrayElement(uint index, ILValue value) => runtime.SetArrayElementAt(ObjValue, index, value);
		internal void StoreArrayElement(uint index, object? value) => runtime.SetArrayElementAt(ObjValue, index, value);
		ILValue LoadArrayElement(uint index) => runtime.CreateILValue(ObjValue.GetArrayElementAt(index));

		public override ILValue? LoadSZArrayElement(LoadValueType loadValueType, long index, DmdType elementType) {
			if (!ObjValue.Type.IsSZArray)
				return null;
			if ((ulong)index > uint.MaxValue)
				return null;
			return LoadArrayElement((uint)index);
		}

		public override bool StoreSZArrayElement(LoadValueType loadValueType, long index, ILValue value, DmdType elementType) {
			if (!ObjValue.Type.IsSZArray)
				return false;
			if ((ulong)index > uint.MaxValue)
				return false;
			runtime.SetArrayElementAt(ObjValue, (uint)index, value);
			return true;
		}

		public override ILValue? LoadSZArrayElementAddress(long index, DmdType elementType) {
			if (!ObjValue.Type.IsSZArray)
				return null;
			if ((ulong)index > uint.MaxValue)
				return null;
			var addrValue = ObjValue.GetArrayElementAddressAt((uint)index);
			if (!(addrValue is null)) {
				runtime.RecordValue(addrValue.Value);
				Debug.Assert(addrValue.Value.Value!.Type.IsByRef);
				return new ByRefILValueImpl(runtime, addrValue.Value.Value);
			}
			return new ArrayElementAddress(runtime, this, (uint)index);
		}

		public override bool GetSZArrayLength(out long length) {
			if (!ObjValue.Type.IsSZArray)
				cachedArrayLength = cachedArrayLength_error;
			if (cachedArrayLength == cachedArrayLength_uninitialized) {
				if (!ObjValue.GetArrayCount(out var arrayCount))
					cachedArrayLength = cachedArrayLength_error;
				else
					cachedArrayLength = arrayCount;
			}
			if (cachedArrayLength >= 0) {
				length = cachedArrayLength;
				return true;
			}
			Debug.Assert(cachedArrayLength == cachedArrayLength_error);
			length = 0;
			return false;
		}
	}
}
