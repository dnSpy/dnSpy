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

	sealed class ConstantStringILValue : TypeILValue {
		public string Value { get; }
		public override DmdType Type { get; }
		public ConstantStringILValue(DmdType type, string value) {
			Type = type;
			Value = value;
		}
		public override string ToString() => "\"" + Value + "\"";
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
			public ILValue? LoadField(DmdFieldInfo field) {
				InitFields(field.ReflectedType);
				return AllFields[field];
			}
			public bool StoreField(DmdFieldInfo field, ILValue value) {
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
			case TypeCode.UInt32:		return new ConstantInt32ILValue(type.AppDomain, 0);
			case TypeCode.Int64:
			case TypeCode.UInt64:		return new ConstantInt64ILValue(type.AppDomain, 0);
			case TypeCode.Single:
			case TypeCode.Double:		return new ConstantFloatILValue(type.AppDomain, 0);
			}
			if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr)
				return IntPtr.Size == 4 ? ConstantNativeIntILValue.Create32(type.AppDomain, 0) : ConstantNativeIntILValue.Create64(type.AppDomain, 0);
			if (type.IsValueType)
				return new FakeValueType(type);
			return new NullObjectRefILValue();
		}

		public override void Initialize(DmdMethodBase method, DmdMethodBody body) { }

		sealed class FakeValueType : TypeILValue {
			readonly Dictionary<DmdFieldInfo, ILValue> fields;
			public override DmdType Type { get; }

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

			const int DUMMY_PTR_BASE = 0x4736DC7B;
			public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) {
				var ad = method.AppDomain;
				var type = method.ReflectedType;
				if (type == ad.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle)) {
					if (method.Name == "get_Value") {
						returnValue = ad.Runtime.PointerSize == 4 ?
							ConstantNativeIntILValue.Create32(ad, DUMMY_PTR_BASE + 0) :
							ConstantNativeIntILValue.Create32(ad, DUMMY_PTR_BASE + 0);
						return true;
					}
				}
				else if (type == ad.GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle)) {
					if (method.Name == "get_Value") {
						returnValue = ad.Runtime.PointerSize == 4 ?
							ConstantNativeIntILValue.Create32(ad, DUMMY_PTR_BASE + 1) :
							ConstantNativeIntILValue.Create32(ad, DUMMY_PTR_BASE + 1);
						return true;
					}
				}
				else if (type == ad.GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle)) {
					if (method.Name == "get_Value") {
						returnValue = ad.Runtime.PointerSize == 4 ?
							ConstantNativeIntILValue.Create32(ad, DUMMY_PTR_BASE + 2) :
							ConstantNativeIntILValue.Create32(ad, DUMMY_PTR_BASE + 2);
						return true;
					}
				}
				else if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct") {
					switch (method.Name) {
					case "InstanceMethod1":
						returnValue = new ConstantInt32ILValue(ad, 123);
						return true;
					case "InstanceMethod2":
						returnValue = arguments[0].Clone();
						return true;
					case "InstanceMethod3":
						returnValue = arguments[1].Clone();
						return true;
					case "InstanceMethod4":
						returnValue = arguments[2].Clone();
						return true;
					}
				}

				returnValue = null;
				return false;
			}

			public override ILValue Clone() => new FakeValueType(this);

			public override ILValue? LoadField(DmdFieldInfo field) => fields[field];

			public override bool StoreField(DmdFieldInfo field, ILValue value) {
				if (!fields.ContainsKey(field))
					return false;
				fields[field] = value;
				return true;
			}

			public override ILValue? LoadFieldAddress(DmdFieldInfo field) {
				if (!fields.ContainsKey(field))
					return null;
				return new FakeFieldAddress(fields, field);
			}

			public override bool InitializeObject(DmdType type) {
				ResetFields();
				return true;
			}
		}

		sealed class FakeReferenceType : TypeILValue {
			readonly Dictionary<DmdFieldInfo, ILValue> fields;
			public override DmdType Type { get; }

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

			public override ILValue? LoadField(DmdFieldInfo field) => fields[field];

			public override bool StoreField(DmdFieldInfo field, ILValue value) {
				if (!fields.ContainsKey(field))
					return false;
				fields[field] = value;
				return true;
			}

			public override ILValue? LoadFieldAddress(DmdFieldInfo field) {
				if (!fields.ContainsKey(field))
					return null;
				return new FakeFieldAddress(fields, field);
			}

			public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) {
				var ad = method.AppDomain;
				var type = method.ReflectedType;
				if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass") {
					switch (method.Name) {
					case "InstanceMethod1":
						returnValue = new ConstantInt32ILValue(ad, 123);
						return true;
					case "InstanceMethod2":
						returnValue = arguments[0].Clone();
						return true;
					case "InstanceMethod3":
						returnValue = arguments[1].Clone();
						return true;
					case "InstanceMethod4":
						returnValue = arguments[2].Clone();
						return true;
					}
				}

				returnValue = null;
				return false;
			}
		}

		sealed class FakeFieldAddress : ByRefILValue, IEquatable<FakeFieldAddress> {
			public Dictionary<DmdFieldInfo, ILValue> Fields { get; }
			public DmdFieldInfo Field { get; }

			public FakeFieldAddress(Dictionary<DmdFieldInfo, ILValue> fields, DmdFieldInfo field) {
				Fields = fields;
				Field = field;
			}

			public override ILValue? LoadIndirect(DmdType type, LoadValueType loadValueType) => Fields[Field];
			public override bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) {
				Fields[Field] = value;
				return true;
			}
			public override bool CopyObject(DmdType type, ILValue source) {
				Fields[Field] = source.LoadIndirect(type.AppDomain.System_Object, LoadValueType.Ref);
				return true;
			}
			public override bool InitializeObject(DmdType type) => Fields[Field].InitializeObject(type);
			public override DmdType Type => Field.FieldType;
			public bool Equals(FakeFieldAddress other) => Fields == other.Fields && Field == other.Field;
		}

		abstract class ArgOrLocalAddress : ByRefILValue, IEquatable<ArgOrLocalAddress> {
			public ILValue[] Collection { get; }
			public long Index { get; }
			protected ArgOrLocalAddress(ILValue[] collection, long index) {
				Collection = collection;
				Index = index;
			}
			public override ILValue? LoadIndirect(DmdType type, LoadValueType loadValueType) => Collection[Index];
			public override bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) {
				Collection[Index] = value;
				return true;
			}
			public override bool CopyObject(DmdType type, ILValue source) {
				Collection[Index] = source.LoadIndirect(type.AppDomain.System_Object, LoadValueType.Ref);
				return true;
			}
			public override bool InitializeObject(DmdType type) => Collection[Index].InitializeObject(type);
			public override ILValue? LoadField(DmdFieldInfo field) => Collection[Index].LoadField(field);
			public override ILValue? LoadFieldAddress(DmdFieldInfo field) => Collection[Index].LoadFieldAddress(field);
			public override bool StoreField(DmdFieldInfo field, ILValue value) => Collection[Index].StoreField(field, value);
			public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) =>
				Collection[Index].Call(isCallvirt, method, arguments, out returnValue);
			public override DmdType Type => Collection[Index].Type.MakeByRefType();
			public bool Equals(ArgOrLocalAddress other) => Collection == other.Collection && Index == other.Index;
		}

		sealed class ArgumentAddress : ArgOrLocalAddress {
			public ArgumentAddress(ILValue[] arguments, long index) : base(arguments, index) { }
		}

		sealed class LocalAddress : ArgOrLocalAddress {
			public LocalAddress(ILValue[] locals, long index) : base(locals, index) { }
		}

		public override int PointerSize => runtime.PointerSize;
		public override ILValue? LoadArgument(int index) => arguments[index];
		public override ILValue? LoadLocal(int index) => locals[index];
		public override ILValue? LoadArgumentAddress(int index, DmdType type) => new ArgumentAddress(arguments, index);
		public override ILValue? LoadLocalAddress(int index, DmdType type) => new LocalAddress(locals, index);
		public override bool StoreArgument(int index, DmdType type, ILValue value) {
			arguments[index] = value;
			return true;
		}
		public override bool StoreLocal(int index, DmdType type, ILValue value) {
			locals[index] = value;
			return true;
		}

		static void InitializeValue(ref ILValue value, DmdType valueType) {
			if (!(value is null))
				return;

			if (!valueType.IsValueType) {
				value = new NullObjectRefILValue();
				return;
			}

			switch (DmdType.GetTypeCode(valueType)) {
			case TypeCode.Boolean:		value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.Char:			value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.SByte:		value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.Byte:			value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.Int16:		value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.UInt16:		value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.Int32:		value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.UInt32:		value = new ConstantInt32ILValue(valueType.AppDomain, 0); return;
			case TypeCode.Int64:		value = new ConstantInt64ILValue(valueType.AppDomain, 0); return;
			case TypeCode.UInt64:		value = new ConstantInt64ILValue(valueType.AppDomain, 0); return;
			case TypeCode.Single:		value = new ConstantFloatILValue(valueType.AppDomain, 0); return;
			case TypeCode.Double:		value = new ConstantFloatILValue(valueType.AppDomain, 0); return;
			}
			if (valueType == valueType.AppDomain.System_IntPtr || valueType == valueType.AppDomain.System_UIntPtr) {
				value = valueType.AppDomain.Runtime.PointerSize == 4 ? ConstantNativeIntILValue.Create32(valueType.AppDomain, 0) : ConstantNativeIntILValue.Create64(valueType.AppDomain, 0);
				return;
			}

			throw new InvalidOperationException();
		}

		sealed class SZArrayILValue : TypeILValue {
			public override DmdType Type { get; }
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

			public override ILValue? LoadSZArrayElement(LoadValueType loadValueType, long index, DmdType elementType) => this[index];
			public override ILValue? LoadSZArrayElementAddress(long index, DmdType elementType) => new SZArrayAddress(this, index);
			public override bool StoreSZArrayElement(LoadValueType loadValueType, long index, ILValue value, DmdType elementType) {
				this[index] = value;
				return true;
			}
			public override bool GetSZArrayLength(out long length) {
				length = elements.Length;
				return true;
			}
		}

		sealed class SZArrayAddress : ByRefILValue, IEquatable<SZArrayAddress> {
			public SZArrayILValue ArrayValue { get; }
			public long Index { get; }

			public SZArrayAddress(SZArrayILValue arrayValue, long index) {
				ArrayValue = arrayValue;
				Index = index;
			}

			public override ILValue? LoadIndirect(DmdType type, LoadValueType loadValueType) => ArrayValue[Index];
			public override bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) {
				ArrayValue[Index] = value;
				return true;
			}
			public override bool CopyObject(DmdType type, ILValue source) {
				ArrayValue[Index] = source.LoadIndirect(type.AppDomain.System_Object, LoadValueType.Ref);
				return true;
			}
			public override bool InitializeObject(DmdType type) => ArrayValue[Index].InitializeObject(type);
			public override ILValue? LoadField(DmdFieldInfo field) => ArrayValue[Index].LoadField(field);
			public override ILValue? LoadFieldAddress(DmdFieldInfo field) => ArrayValue[Index].LoadFieldAddress(field);
			public override bool StoreField(DmdFieldInfo field, ILValue value) => ArrayValue[Index].StoreField(field, value);
			public override bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) =>
				ArrayValue[Index].Call(isCallvirt, method, arguments, out returnValue);
			public override DmdType Type => ArrayValue.Type.MakeByRefType();
			public bool Equals(SZArrayAddress other) => ArrayValue == other.ArrayValue && Index == other.Index;
		}

		public override ILValue CreateSZArray(DmdType elementType, long length) => new SZArrayILValue(elementType, length);
		public override ILValue? CreateRuntimeTypeHandle(DmdType type) {
			var rht = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle);
			return new FakeValueType(rht);
		}
		public override ILValue? CreateRuntimeFieldHandle(DmdFieldInfo field) {
			var rht = field.ReflectedType.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle);
			return new FakeValueType(rht);
		}
		public override ILValue? CreateRuntimeMethodHandle(DmdMethodBase method) {
			var rht = method.ReflectedType.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle);
			return new FakeValueType(rht);
		}

		public override ILValue? CreateTypeNoConstructor(DmdType type) {
			if (type.IsValueType)
				return new FakeValueType(type);
			return new FakeReferenceType(type);
		}

		public override ILValue? Box(ILValue value, DmdType type) => new BoxedValueTypeILValue(value, type);

		public override bool CallStatic(DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) {
			var ad = method.AppDomain;
			var type = method.ReflectedType;
			if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct") {
				switch (method.Name) {
				case "StaticMethod1":
					returnValue = new ConstantInt32ILValue(ad, 123);
					return true;
				case "StaticMethod2":
					returnValue = arguments[0].Clone();
					return true;
				case "StaticMethod3":
					returnValue = arguments[1].Clone();
					return true;
				case "StaticMethod4":
					returnValue = arguments[2].Clone();
					return true;
				}
			}
			else if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass") {
				switch (method.Name) {
				case "StaticMethod1":
					returnValue = new ConstantInt32ILValue(ad, 123);
					return true;
				case "StaticMethod2":
					returnValue = arguments[0].Clone();
					return true;
				case "StaticMethod3":
					returnValue = arguments[1].Clone();
					return true;
				case "StaticMethod4":
					returnValue = arguments[2].Clone();
					return true;
				}
			}

			returnValue = null;
			return false;
		}

		public override ILValue? CreateInstance(DmdConstructorInfo ctor, ILValue[] arguments) {
			var type = ctor.ReflectedType;
			if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct") {
				if (arguments.Length == 2)
					return new FakeValueType(ctor.ReflectedType);
			}
			else if (type.FullName == "dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass") {
				if (arguments.Length == 0)
					return new FakeReferenceType(ctor.ReflectedType);
				if (arguments.Length == 2)
					return new FakeReferenceType(ctor.ReflectedType);
			}

			return null;
		}

		public override bool CallStaticIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue[] arguments, out ILValue? returnValue) => throw new NotImplementedException();

		public override ILValue? LoadStaticField(DmdFieldInfo field) => staticFields.LoadField(field);
		public override ILValue? LoadStaticFieldAddress(DmdFieldInfo field) => new FakeFieldAddress(staticFields.AllFields, field);
		public override bool StoreStaticField(DmdFieldInfo field, ILValue value) {
			staticFields.StoreField(field, value);
			return true;
		}

		public override ILValue LoadString(DmdType type, string value) => new ConstantStringILValue(type, value);

		public override int? CompareSigned(ILValue left, ILValue right) => null;
		public override int? CompareUnsigned(ILValue left, ILValue right) => null;

		public override bool? Equals(ILValue left, ILValue right) {
			if (left is ArgOrLocalAddress addr1 && right is ArgOrLocalAddress addr2)
				return addr1.Equals(addr2);
			if (left is SZArrayAddress arad1 && right is SZArrayAddress arad2)
				return arad1.Equals(arad2);
			return null;
		}
	}

	sealed class BoxedValueTypeILValue : TypeILValue {
		public override DmdType Type { get; }
		readonly ILValue value;
		public BoxedValueTypeILValue(ILValue value, DmdType type) {
			this.value = value.Clone();
			Type = type;
		}
		public override ILValue? UnboxAny(DmdType type) => value.Clone();
	}
}
