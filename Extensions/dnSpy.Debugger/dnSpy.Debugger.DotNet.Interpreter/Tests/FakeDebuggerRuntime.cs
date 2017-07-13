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

// Test class that doesn't call a real CLR so only the interpreter code can be tested. It doesn't
// support calling methods so all called methods are hard coded to return certain values. If a test
// adds a new call, this class would need to be updated.

using System;
using System.Collections.Generic;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter.Tests.Fake {
	sealed class TestRuntimeImpl : TestRuntime {
		public override DmdRuntime Runtime { get; }
		public override DebuggerRuntime DebuggerRuntime => debuggerRuntime;

		readonly FakeDebuggerRuntime debuggerRuntime;

		public TestRuntimeImpl(DmdRuntime runtime) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			debuggerRuntime = new FakeDebuggerRuntime(runtime);
		}

		public override void SetMethodExecState(ILValue[] arguments, DmdMethodBody body) => debuggerRuntime.SetMethodExecState(arguments, body);
	}

	sealed class FakeDebuggerRuntime : DebuggerRuntime {
		readonly DmdRuntime runtime;
		public FakeDebuggerRuntime(DmdRuntime runtime) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			staticFields = new StaticFields();
		}

		sealed class StaticFields {
			public Dictionary<DmdFieldInfo, ILValue> AllFields { get; } = new Dictionary<DmdFieldInfo, ILValue>(DmdMemberInfoEqualityComparer.DefaultMember);
			readonly HashSet<DmdType> initdTypes = new HashSet<DmdType>(DmdMemberInfoEqualityComparer.DefaultType);
			public void Clear() {
				AllFields.Clear();
				initdTypes.Clear();
			}
			void InitFields(DmdType type) {
				if (!initdTypes.Contains(type)) {
					initdTypes.Add(type);
					foreach (var f in type.Fields) {
						if (f.IsStatic)
							AllFields[f] = CreateDefaultValue(f.FieldType);
					}
				}
			}
			public ILValue GetField(DmdFieldInfo field) {
				InitFields(field.ReflectedType);
				return AllFields[field];
			}
			public bool SetField(DmdFieldInfo field, ILValue value) {
				InitFields(field.ReflectedType);
				if (!AllFields.ContainsKey(field))
					return false;
				AllFields[field] = value;
				return true;
			}
		}

		ILValue[] arguments;
		ILValue[] locals;
		readonly StaticFields staticFields;
		public void SetMethodExecState(ILValue[] arguments, DmdMethodBody body) {
			staticFields.Clear();
			this.arguments = (ILValue[])arguments.Clone();
			locals = new ILValue[body.LocalVariables.Count];
			for (int i = 0; i < locals.Length; i++)
				locals[i] = CreateDefaultValue(body.LocalVariables[i].LocalType);
		}

		static ILValue CreateDefaultValue(DmdType type) {
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:		return new ConstantInt32ILValue(0);
			case TypeCode.Int64:
			case TypeCode.UInt64:		return new ConstantInt64ILValue(0);
			case TypeCode.Single:
			case TypeCode.Double:		return new ConstantFloatILValue(0);
			}
			if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr)
				return IntPtr.Size == 4 ? ConstantNativeIntILValue.Create32(0) : ConstantNativeIntILValue.Create64(0);
			if (type.IsValueType)
				return new FakeValueType(type);
			return NullObjectRefILValue.Instance;
		}

		sealed class FakeValueType : ValueTypeILValue {
			readonly Dictionary<DmdFieldInfo, ILValue> fields;
			public DmdType Type { get; }

			public FakeValueType(FakeValueType other) {
				Type = other.Type;
				fields = new Dictionary<DmdFieldInfo, ILValue>(other.fields, DmdMemberInfoEqualityComparer.DefaultMember);
			}

			public FakeValueType(DmdType type) {
				Type = type;
				fields = new Dictionary<DmdFieldInfo, ILValue>(DmdMemberInfoEqualityComparer.DefaultMember);
				ResetFields();
			}

			public void ResetFields() {
				foreach (var f in Type.Fields) {
					if (!f.IsStatic)
						fields[f] = CreateDefaultValue(f.FieldType);
				}
			}

			public override ILValue Clone() => new FakeValueType(this);
			public override DmdType GetType(DmdAppDomain appDomain) => Type;

			public ILValue GetField(DmdFieldInfo field) => fields[field];

			public bool SetField(DmdFieldInfo field, ILValue value) {
				if (!fields.ContainsKey(field))
					return false;
				fields[field] = value;
				return true;
			}

			public ILValue GetFieldAddress(DmdFieldInfo field) {
				if (!fields.ContainsKey(field))
					return null;
				return new FakeFieldAddress(fields, field);
			}
		}

		sealed class FakeReferenceType : ObjectRefILValue {
			readonly Dictionary<DmdFieldInfo, ILValue> fields;
			public DmdType Type { get; }

			public FakeReferenceType(DmdType type) {
				Type = type;
				fields = new Dictionary<DmdFieldInfo, ILValue>(DmdMemberInfoEqualityComparer.DefaultMember);
				ResetFields();
			}

			void ResetFields() {
				foreach (var f in Type.Fields) {
					if (!f.IsStatic)
						fields[f] = CreateDefaultValue(f.FieldType);
				}
			}

			public override DmdType GetType(DmdAppDomain appDomain) => Type;

			public ILValue GetField(DmdFieldInfo field) => fields[field];

			public bool SetField(DmdFieldInfo field, ILValue value) {
				if (!fields.ContainsKey(field))
					return false;
				fields[field] = value;
				return true;
			}

			public ILValue GetFieldAddress(DmdFieldInfo field) {
				if (!fields.ContainsKey(field))
					return null;
				return new FakeFieldAddress(fields, field);
			}
		}

		sealed class FakeFieldAddress : ByRefILValue, IEquatable<FakeFieldAddress> {
			public Dictionary<DmdFieldInfo, ILValue> Fields { get; }
			public DmdFieldInfo Field { get; }

			public FakeFieldAddress(Dictionary<DmdFieldInfo, ILValue> fields, DmdFieldInfo field) {
				Fields = fields;
				Field = field;
			}

			public bool Equals(FakeFieldAddress other) => Fields == other.Fields && Field == other.Field;
		}

		abstract class ArgOrLocalAddress : ByRefILValue, IEquatable<ArgOrLocalAddress> {
			public ILValue[] Collection { get; }
			public long Index { get; }
			protected ArgOrLocalAddress(ILValue[] collection, long index) {
				Collection = collection;
				Index = index;
			}
			public bool Equals(ArgOrLocalAddress other) => Collection == other.Collection && Index == other.Index;
		}

		sealed class ArgumentAddress : ArgOrLocalAddress {
			public ArgumentAddress(ILValue[] arguments, long index) : base(arguments, index) { }
		}

		sealed class LocalAddress : ArgOrLocalAddress {
			public LocalAddress(ILValue[] locals, long index) : base(locals, index) { }
		}

		public override int PointerSize => runtime.PointerSize;
		public override ILValue GetArgument(int index) => arguments[index];
		public override ILValue GetLocal(int index) => locals[index];
		public override ILValue GetArgumentAddress(int index) => new ArgumentAddress(arguments, index);
		public override ILValue GetLocalAddress(int index) => new LocalAddress(locals, index);
		public override bool SetArgument(int index, ILValue value) {
			arguments[index] = value;
			return true;
		}
		public override bool SetLocal(int index, ILValue value) {
			locals[index] = value;
			return true;
		}

		static void InitializeValue(ref ILValue value, DmdType valueType) {
			if (value != null)
				return;

			if (!valueType.IsValueType) {
				value = NullObjectRefILValue.Instance;
				return;
			}

			switch (DmdType.GetTypeCode(valueType)) {
			case TypeCode.Boolean:		value = new ConstantInt32ILValue(0); return;
			case TypeCode.Char:			value = new ConstantInt32ILValue(0); return;
			case TypeCode.SByte:		value = new ConstantInt32ILValue(0); return;
			case TypeCode.Byte:			value = new ConstantInt32ILValue(0); return;
			case TypeCode.Int16:		value = new ConstantInt32ILValue(0); return;
			case TypeCode.UInt16:		value = new ConstantInt32ILValue(0); return;
			case TypeCode.Int32:		value = new ConstantInt32ILValue(0); return;
			case TypeCode.UInt32:		value = new ConstantInt32ILValue(0); return;
			case TypeCode.Int64:		value = new ConstantInt64ILValue(0); return;
			case TypeCode.UInt64:		value = new ConstantInt64ILValue(0); return;
			case TypeCode.Single:		value = new ConstantFloatILValue(0); return;
			case TypeCode.Double:		value = new ConstantFloatILValue(0); return;
			}
			if (valueType == valueType.AppDomain.System_IntPtr || valueType == valueType.AppDomain.System_UIntPtr) {
				value = valueType.AppDomain.Runtime.PointerSize == 4 ? ConstantNativeIntILValue.Create32(0) : ConstantNativeIntILValue.Create64(0);
				return;
			}

			throw new InvalidOperationException();
		}

		sealed class SZArrayILValue : ObjectRefILValue {
			public DmdType Type { get; }
			public long Length => elements.Length;
			readonly ILValue[] elements;

			public ILValue this[long index] {
				get {
					ref var value = ref elements[index];
					InitializeValue(ref value, Type.GetElementType());
					return value;
				}
				set => elements[index] = value ?? throw new InvalidOperationException();
			}

			public SZArrayILValue(DmdType elementType, long length) {
				Type = elementType.MakeArrayType();
				elements = new ILValue[length];
			}

			public override DmdType GetType(DmdAppDomain appDomain) => Type;
		}

		sealed class SZArrayAddress : ObjectRefILValue, IEquatable<SZArrayAddress> {
			public SZArrayILValue ArrayValue { get; }
			public long Index { get; }

			public SZArrayAddress(SZArrayILValue arrayValue, long index) {
				ArrayValue = arrayValue;
				Index = index;
			}

			public override DmdType GetType(DmdAppDomain appDomain) => ArrayValue.GetType(appDomain).MakeByRefType();
			public bool Equals(SZArrayAddress other) => ArrayValue == other.ArrayValue && Index == other.Index;
		}

		public override ILValue CreateSZArray(DmdType elementType, long length) => new SZArrayILValue(elementType, length);
		public override ILValue GetSZArrayElement(PointerOpCodeType pointerType, ILValue arrayValue, long index, DmdType elementType) {
			if (arrayValue is SZArrayILValue ar)
				return ar[index];
			return null;
		}
		public override ILValue GetSZArrayElementAddress(ILValue arrayValue, long index) => new SZArrayAddress((SZArrayILValue)arrayValue, index);
		public override bool SetSZArrayElement(ILValue arrayValue, long index, ILValue elementValue) {
			if (arrayValue is SZArrayILValue ar) {
				ar[index] = elementValue;
				return true;
			}
			return false;
		}
		public override bool GetSZArrayLength(ILValue value, out long length) {
			if (value is SZArrayILValue ar) {
				length = ar.Length;
				return true;
			}
			length = -1;
			return false;
		}
		public override ILValue CreateRuntimeTypeHandle(DmdType type) {
			var rht = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle);
			return new FakeValueType(rht);
		}
		public override ILValue CreateRuntimeFieldHandle(DmdFieldInfo field) {
			var rht = field.ReflectedType.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle);
			return new FakeValueType(rht);
		}
		public override ILValue CreateRuntimeMethodHandle(DmdMethodBase method) {
			var rht = method.ReflectedType.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle);
			return new FakeValueType(rht);
		}

		public override ILValue CreateTypeNoConstructor(DmdType type) {
			if (type.IsValueType)
				return new FakeValueType(type);
			return new FakeReferenceType(type);
		}

		const int DUMMY_PTR_BASE = 0x4736DC7B;
		public override bool Call(bool isVirtual, DmdMethodBase method, ILValue obj, ILValue[] parameters, out ILValue returnValue) {
			var ad = method.AppDomain;
			var type = method.ReflectedType;
			if (type == ad.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle)) {
				if (method.Name == "get_Value") {
					returnValue = ad.Runtime.PointerSize == 4 ?
						ConstantNativeIntILValue.Create32(DUMMY_PTR_BASE + 0) :
						ConstantNativeIntILValue.Create32(DUMMY_PTR_BASE + 0);
					return true;
				}
			}
			else if (type == ad.GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle)) {
				if (method.Name == "get_Value") {
					returnValue = ad.Runtime.PointerSize == 4 ?
						ConstantNativeIntILValue.Create32(DUMMY_PTR_BASE + 1) :
						ConstantNativeIntILValue.Create32(DUMMY_PTR_BASE + 1);
					return true;
				}
			}
			else if (type == ad.GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle)) {
				if (method.Name == "get_Value") {
					returnValue = ad.Runtime.PointerSize == 4 ?
						ConstantNativeIntILValue.Create32(DUMMY_PTR_BASE + 2) :
						ConstantNativeIntILValue.Create32(DUMMY_PTR_BASE + 2);
					return true;
				}
			}
			else if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct") {
				switch (method.Name) {
				case ".ctor":
					if (parameters.Length == 2) {
						returnValue = NullObjectRefILValue.Instance;
						return true;
					}
					break;
				case "InstanceMethod1":
					returnValue = new ConstantInt32ILValue(123);
					return true;
				case "InstanceMethod2":
					returnValue = parameters[0].Clone();
					return true;
				case "InstanceMethod3":
					returnValue = parameters[1].Clone();
					return true;
				case "InstanceMethod4":
					returnValue = parameters[2].Clone();
					return true;
				case "StaticMethod1":
					returnValue = new ConstantInt32ILValue(123);
					return true;
				case "StaticMethod2":
					returnValue = parameters[0].Clone();
					return true;
				case "StaticMethod3":
					returnValue = parameters[1].Clone();
					return true;
				case "StaticMethod4":
					returnValue = parameters[2].Clone();
					return true;
				}
			}
			else if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass") {
				switch (method.Name) {
				case ".ctor":
					if (parameters.Length == 0) {
						returnValue = NullObjectRefILValue.Instance;
						return true;
					}
					if (parameters.Length == 2) {
						returnValue = NullObjectRefILValue.Instance;
						return true;
					}
					break;
				case "InstanceMethod1":
					returnValue = new ConstantInt32ILValue(123);
					return true;
				case "InstanceMethod2":
					returnValue = parameters[0].Clone();
					return true;
				case "InstanceMethod3":
					returnValue = parameters[1].Clone();
					return true;
				case "InstanceMethod4":
					returnValue = parameters[2].Clone();
					return true;
				case "StaticMethod1":
					returnValue = new ConstantInt32ILValue(123);
					return true;
				case "StaticMethod2":
					returnValue = parameters[0].Clone();
					return true;
				case "StaticMethod3":
					returnValue = parameters[1].Clone();
					return true;
				case "StaticMethod4":
					returnValue = parameters[2].Clone();
					return true;
				}
			}

			returnValue = null;
			return false;
		}

		public override bool CallIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue obj, ILValue[] parameters, out ILValue returnValue) => throw new NotImplementedException();
		public override ILValue GetField(DmdFieldInfo field, ILValue obj) {
			if (obj is FakeValueType fakeVT)
				return fakeVT.GetField(field);
			if (obj is FakeReferenceType fakeRT)
				return fakeRT.GetField(field);
			if (field.IsStatic)
				return staticFields.GetField(field);
			if (field.ReflectedType.IsValueType) {
				if (obj is ArgOrLocalAddress addr)
					return ((FakeValueType)addr.Collection[addr.Index]).GetField(field);
				if (obj is SZArrayAddress addr2)
					return ((FakeValueType)addr2.ArrayValue[addr2.Index]).GetField(field);
			}
			return null;
		}
		public override ILValue GetFieldAddress(DmdFieldInfo field, ILValue obj) {
			if (obj is ArgOrLocalAddress addr) {
				if (addr.Collection[addr.Index] is FakeValueType fakeVT)
					return fakeVT.GetFieldAddress(field);
				if (addr.Collection[addr.Index] is FakeReferenceType fakeRT)
					return fakeRT.GetFieldAddress(field);
				throw new InvalidOperationException();
			}
			if (obj is SZArrayAddress arAddr) {
				if (arAddr.ArrayValue[arAddr.Index] is FakeValueType fakeVT)
					return fakeVT.GetFieldAddress(field);
				if (arAddr.ArrayValue[arAddr.Index] is FakeReferenceType fakeRT)
					return fakeRT.GetFieldAddress(field);
				throw new InvalidOperationException();
			}
			if (field.IsStatic)
				return new FakeFieldAddress(staticFields.AllFields, field);
			return null;
		}
		public override bool SetField(DmdFieldInfo field, ILValue obj, ILValue value) {
			if (obj is ArgOrLocalAddress addr) {
				if (addr.Collection[addr.Index] is FakeValueType fakeVT) {
					fakeVT.SetField(field, value);
					return true;
				}
				if (addr.Collection[addr.Index] is FakeReferenceType fakeRT) {
					fakeRT.SetField(field, value);
					return true;
				}
				throw new InvalidOperationException();
			}
			if (obj is SZArrayAddress arAddr) {
				if (arAddr.ArrayValue[arAddr.Index] is FakeValueType fakeVT) {
					fakeVT.SetField(field, value);
					return true;
				}
				if (arAddr.ArrayValue[arAddr.Index] is FakeReferenceType fakeRT) {
					fakeRT.SetField(field, value);
					return true;
				}
				throw new InvalidOperationException();
			}
			if (field.IsStatic)
				return staticFields.SetField(field, value);
			return false;
		}
		public override ILValue ReadPointer(PointerOpCodeType pointerType, ILValue address) {
			if (address is ArgOrLocalAddress addr1)
				return addr1.Collection[addr1.Index];
			if (address is SZArrayAddress addr2)
				return addr2.ArrayValue[addr2.Index];
			if (address is FakeFieldAddress fdAddr)
				return fdAddr.Fields[fdAddr.Field];
			return null;
		}
		public override bool WritePointer(PointerOpCodeType pointerType, ILValue address, ILValue value) {
			if (address is ArgOrLocalAddress addr1) {
				addr1.Collection[addr1.Index] = value;
				return true;
			}
			if (address is SZArrayAddress addr2) {
				addr2.ArrayValue[addr2.Index] = value;
				return true;
			}
			if (address is FakeFieldAddress fdAddr) {
				fdAddr.Fields[fdAddr.Field] = value;
				return true;
			}
			return false;
		}
		ILValue GetFakeType(ILValue address) {
			if (address is ArgOrLocalAddress addr1)
				return (ILValue)(addr1.Collection[addr1.Index] as FakeValueType) ?? addr1.Collection[addr1.Index] as FakeReferenceType;
			if (address is SZArrayAddress addr2)
				return (ILValue)(addr2.ArrayValue[addr2.Index] as FakeValueType) ?? addr2.ArrayValue[addr2.Index] as FakeReferenceType;
			if (address is FakeFieldAddress fdAddr)
				return (ILValue)(fdAddr.Fields[fdAddr.Field] as FakeValueType) ?? fdAddr.Fields[fdAddr.Field] as FakeReferenceType;
			return null;
		}
		ILValue GetValue(ILValue address) {
			if (address is ArgOrLocalAddress addr1)
				return addr1.Collection[addr1.Index];
			if (address is SZArrayAddress addr2)
				return addr2.ArrayValue[addr2.Index];
			if (address is FakeFieldAddress fdAddr)
				return fdAddr.Fields[fdAddr.Field];
			return null;
		}
		public override ILValue LoadTypeObject(ILValue address, DmdType type) {
			if (GetFakeType(address) is ILValue fakeT)
				return fakeT;
			if (address is ArgOrLocalAddress addr1)
				return addr1.Collection[addr1.Index];
			if (address is SZArrayAddress addr2)
				return addr2.ArrayValue[addr2.Index];
			if (address is FakeFieldAddress fdAddr)
				return fdAddr.Fields[fdAddr.Field];
			return null;
		}
		public override bool StoreTypeObject(ILValue address, DmdType type, ILValue value) {
			if (address is ArgOrLocalAddress addr1) {
				addr1.Collection[addr1.Index] = value;
				return true;
			}
			if (address is SZArrayAddress addr2) {
				addr2.ArrayValue[addr2.Index] = value;
				return true;
			}
			if (address is FakeFieldAddress fdAddr) {
				fdAddr.Fields[fdAddr.Field] = value;
				return true;
			}
			return false;
		}
		public override bool CopyObject(ILValue destination, ILValue source, DmdType type) {
			if (destination is ArgOrLocalAddress addr1) {
				addr1.Collection[addr1.Index] = GetValue(source) ?? throw new InvalidOperationException();
				return true;
			}
			if (destination is SZArrayAddress addr2) {
				addr2.ArrayValue[addr2.Index] = GetValue(source) ?? throw new InvalidOperationException();
				return true;
			}
			if (destination is FakeFieldAddress fdAddr) {
				fdAddr.Fields[fdAddr.Field] = GetValue(source) ?? throw new InvalidOperationException();
				return true;
			}
			return false;
		}
		public override bool InitializeObject(ILValue address, DmdType type) {
			if (address is ArgOrLocalAddress addr) {
				if (addr.Collection[addr.Index] is FakeValueType fakeVT) {
					fakeVT.ResetFields();
					return true;
				}
				throw new InvalidOperationException();
			}
			if (address is SZArrayAddress arAddr) {
				if (arAddr.ArrayValue[arAddr.Index] is FakeValueType fakeVT) {
					fakeVT.ResetFields();
					return true;
				}
				throw new InvalidOperationException();
			}
			return false;
		}
		public override bool CopyMemory(ILValue destination, ILValue source, long size) => throw new NotImplementedException();
		public override bool InitializeMemory(ILValue address, byte value, long size) => throw new NotImplementedException();

		public override ILValue Box(ILValue value, DmdType type) {
			if (type.IsValueType) {
				switch (value.Kind) {
				case ILValueKind.Int32:
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.NativeInt:
					return new BoxedValueTypeILValue(value, type);

				case ILValueKind.ByRef:
				case ILValueKind.ObjectRef:
					break;

				case ILValueKind.ValueType:
					return new BoxedValueTypeILValue((ValueTypeILValue)value);

				default:
					throw new InvalidOperationException();
				}
			}
			return value;
		}

		public override ILValue UnboxAny(ILValue value, DmdType type) => null;

		public override ILValue BinaryAdd(ILValue left, ILValue right) => throw new NotImplementedException();
		public override ILValue BinaryAddOvf(ILValue left, ILValue right) => throw new NotImplementedException();
		public override ILValue BinaryAddOvfUn(ILValue left, ILValue right) => throw new NotImplementedException();
		public override ILValue BinarySub(ILValue left, ILValue right) => throw new NotImplementedException();
		public override ILValue BinarySubOvf(ILValue left, ILValue right) => throw new NotImplementedException();
		public override ILValue BinarySubOvfUn(ILValue left, ILValue right) => throw new NotImplementedException();
		public override ILValue ConvI(ILValue value) => throw new NotImplementedException();
		public override ILValue ConvOvfI(ILValue value) => throw new NotImplementedException();
		public override ILValue ConvOvfIUn(ILValue value) => throw new NotImplementedException();
		public override ILValue ConvU(ILValue value) => throw new NotImplementedException();
		public override ILValue ConvOvfU(ILValue value) => throw new NotImplementedException();
		public override ILValue ConvOvfUUn(ILValue value) => throw new NotImplementedException();
		public override int? CompareSigned(ILValue left, ILValue right) => throw new NotImplementedException();
		public override int? CompareUnsigned(ILValue left, ILValue right) => throw new NotImplementedException();

		public override bool? Equals(ILValue left, ILValue right) {
			if (left is ArgOrLocalAddress addr1 && right is ArgOrLocalAddress addr2)
				return addr1.Equals(addr2);
			if (left is SZArrayAddress arad1 && right is SZArrayAddress arad2)
				return arad1.Equals(arad2);
			return false;
		}
	}
}
