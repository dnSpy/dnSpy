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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	/// <summary>
	/// Base class of all value locations (no-location, local, argument, array element, static field, reference type field, value type field)
	/// </summary>
	abstract class ValueLocation {
		/// <summary>
		/// Gets the location type. This is not necessarily the type of the value stored in the location.
		/// </summary>
		public abstract DmdType Type { get; }
		public abstract Value Load();
		public abstract string? Store(Value value);
		public ValueLocation Dereference() {
			if (!Type.IsByRef)
				throw new InvalidOperationException();
			var res = DereferenceCore();
			Debug.Assert(!res.Type.IsByRef);
			return res;
		}
		protected abstract ValueLocation DereferenceCore();
	}

	sealed class NoValueLocation : ValueLocation {
		public override DmdType Type { get; }

		Value value;

		public NoValueLocation(DmdType type, Value? value) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public override Value Load() => value;
		public override string? Store(Value value) {
			this.value = value;
			return null;
		}
		protected override ValueLocation DereferenceCore() => new NoValueLocation(Type.GetElementType()!, value);
	}

	sealed class LocalValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ILDbgEngineStackFrame frame;
		readonly int index;

		public LocalValueLocation(DmdType type, ILDbgEngineStackFrame frame, int index) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.frame = frame ?? throw new ArgumentNullException(nameof(frame));
			this.index = index;
		}

		public override Value Load() {
			var locals = frame.MonoFrame.Method.GetLocals();
			if ((uint)index >= (uint)locals.Length)
				throw new AbsentInformationException();
			return frame.MonoFrame.GetValue(locals[index]);
		}

		public override string? Store(Value value) {
			var locals = frame.MonoFrame.Method.GetLocals();
			if ((uint)index >= (uint)locals.Length)
				return PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway;
			frame.MonoFrame.SetValue(locals[index], value);
			return null;
		}

		protected override ValueLocation DereferenceCore() => new LocalValueLocation(Type.GetElementType()!, frame, index);
	}

	sealed class ArgumentValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ILDbgEngineStackFrame frame;
		readonly int index;

		public ArgumentValueLocation(DmdType type, ILDbgEngineStackFrame frame, int index) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.frame = frame ?? throw new ArgumentNullException(nameof(frame));
			this.index = index;
		}

		public override Value Load() {
			var parameters = frame.MonoFrame.Method.GetParameters();
			if ((uint)index >= (uint)parameters.Length)
				throw new AbsentInformationException();
			return frame.MonoFrame.GetValue(parameters[index]);
		}

		public override string? Store(Value value) {
			var parameters = frame.MonoFrame.Method.GetParameters();
			if ((uint)index >= (uint)parameters.Length)
				return PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway;
			frame.MonoFrame.SetValue(parameters[index], value);
			return null;
		}

		protected override ValueLocation DereferenceCore() => new ArgumentValueLocation(Type.GetElementType()!, frame, index);
	}

	sealed class ThisValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ILDbgEngineStackFrame frame;

		public ThisValueLocation(DmdType type, ILDbgEngineStackFrame frame) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.frame = frame ?? throw new ArgumentNullException(nameof(frame));
		}

		public override Value Load() => frame.MonoFrame.GetThis();
		public override string? Store(Value value) {
			if (frame.MonoFrame.VirtualMachine.Version.AtLeast(2, 44)) {
				frame.MonoFrame.SetThis(value);
				return null;
			}
			else
				return dnSpy_Debugger_DotNet_Mono_Resources.Error_CanNotWriteToThisArgument;
		}
		protected override ValueLocation DereferenceCore() => new ThisValueLocation(Type.GetElementType()!, frame);
	}

	sealed class ArrayElementValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ArrayMirror arrayMirror;
		readonly uint index;

		public ArrayElementValueLocation(DmdType type, ArrayMirror arrayMirror, uint index) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.arrayMirror = arrayMirror ?? throw new ArgumentNullException(nameof(arrayMirror));
			this.index = index;
		}

		public override Value Load() => arrayMirror[(int)index];
		public override string? Store(Value value) {
			arrayMirror[(int)index] = value;
			return null;
		}
		protected override ValueLocation DereferenceCore() => new ArrayElementValueLocation(Type.GetElementType()!, arrayMirror, index);
	}

	sealed class StaticFieldValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ThreadMirror thread;
		readonly FieldInfoMirror field;

		public StaticFieldValueLocation(DmdType type, ThreadMirror thread, FieldInfoMirror field) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.field = field ?? throw new ArgumentNullException(nameof(field));
			Debug.Assert(field.IsStatic);
		}

		public override Value Load() => field.DeclaringType.GetValue(field, thread);
		public override string? Store(Value value) {
			field.DeclaringType.SetValue(field, value);
			return null;
		}
		protected override ValueLocation DereferenceCore() => new StaticFieldValueLocation(Type.GetElementType()!, thread, field);
	}

	sealed class ReferenceTypeFieldValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ObjectMirror objectMirror;
		readonly FieldInfoMirror field;

		public ReferenceTypeFieldValueLocation(DmdType type, ObjectMirror objectMirror, FieldInfoMirror field) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.objectMirror = objectMirror ?? throw new ArgumentNullException(nameof(objectMirror));
			this.field = field ?? throw new ArgumentNullException(nameof(field));
			Debug.Assert(!field.IsStatic);
		}

		public override Value Load() => objectMirror.GetValue(field);
		public override string? Store(Value value) {
			objectMirror.SetValue(field, value);
			return null;
		}
		protected override ValueLocation DereferenceCore() => new ReferenceTypeFieldValueLocation(Type.GetElementType()!, objectMirror, field);
	}

	sealed class ValueTypeFieldValueLocation : ValueLocation {
		public override DmdType Type { get; }

		readonly ValueLocation containingLocation;
		readonly FieldInfoMirror field;
		readonly int valueIndex;
		readonly TypeMirror structType;

		public ValueTypeFieldValueLocation(DmdType type, ValueLocation containingLocation, StructMirror structMirror, FieldInfoMirror field) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.containingLocation = containingLocation ?? throw new ArgumentNullException(nameof(containingLocation));
			this.field = field ?? throw new ArgumentNullException(nameof(field));
			structType = structMirror.Type;
			Debug.Assert(!field.IsStatic);
			Debug.Assert(field.DeclaringType == structType);
			Debug.Assert(containingLocation.Load() is StructMirror);
			valueIndex = GetValueIndex(field);
			if ((uint)valueIndex >= (uint)structMirror.Fields.Length)
				throw new InvalidOperationException();
		}

		ValueTypeFieldValueLocation(ValueTypeFieldValueLocation other) {
			Type = other.Type.GetElementType()!;
			containingLocation = other.containingLocation;
			field = other.field;
			valueIndex = other.valueIndex;
			structType = other.structType;
		}

		static int GetValueIndex(FieldInfoMirror field) {
			int index = 0;
			foreach (var f in field.DeclaringType.GetFields()) {
				if (f.IsStatic || f.IsLiteral)
					continue;
				if (f == field)
					return index;
				index++;
			}
			throw new InvalidOperationException();
		}

		public override Value Load() {
			var structMirror = containingLocation.Load() as StructMirror;
			Debug.Assert(structMirror?.Type == structType);
			if (structMirror?.Type != structType)
				throw new InvalidOperationException();
			return structMirror!.Fields[valueIndex];
		}

		public override string? Store(Value value) {
			var structMirror = containingLocation.Load() as StructMirror;
			Debug.Assert(structMirror?.Type == structType);
			if (structMirror?.Type != structType)
				throw new InvalidOperationException();
			structMirror!.Fields[valueIndex] = value;
			return containingLocation.Store(structMirror);
		}

		protected override ValueLocation DereferenceCore() => new ValueTypeFieldValueLocation(this);
	}
}
