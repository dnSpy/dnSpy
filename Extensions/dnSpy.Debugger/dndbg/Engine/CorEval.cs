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
using System.Diagnostics;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorEval : COMObject<ICorDebugEval>, IEquatable<CorEval> {
		readonly ICorDebugEval2 eval2;

		/// <summary>
		/// Gets the thread or null
		/// </summary>
		public CorThread Thread {
			get {
				ICorDebugThread thread;
				int hr = obj.GetThread(out thread);
				return hr < 0 || thread == null ? null : new CorThread(thread);
			}
		}

		/// <summary>
		/// true if this <see cref="CorEval"/> object is currently executing
		/// </summary>
		public bool IsActive {
			get {
				int act;
				int hr = obj.IsActive(out act);
				return hr >= 0 && act != 0;
			}
		}

		/// <summary>
		/// Gets the result or null. This is an exception object if the eval ended in an exception.
		/// </summary>
		public CorValue Result {
			get {
				ICorDebugValue value;
				int hr = obj.GetResult(out value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		public CorEval(ICorDebugEval eval)
			: base(eval) {
			eval2 = eval as ICorDebugEval2;
		}

		/// <summary>
		/// Aborts the computation this ICorDebugEval object is currently performing
		/// </summary>
		/// <returns></returns>
		public int Abort() => obj.Abort();

		/// <summary>
		/// Aborts the computation that this ICorDebugEval2 is currently performing
		/// </summary>
		/// <returns></returns>
		public int RudeAbort() {
			if (eval2 == null)
				return -1;
			return eval2.RudeAbort();
		}

		/// <summary>
		/// Creates a value of the specified type with an initial value of 0 or null
		/// </summary>
		/// <param name="et">Element type</param>
		/// <param name="cls">Type or null if it's not a Class/ValueType</param>
		/// <returns></returns>
		public CorValue CreateValue(CorElementType et, CorClass cls = null) {
			ICorDebugValue value;
			int hr = obj.CreateValue(et, cls?.RawObject, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Creates a value of the specified type with an initial value of 0 or null
		/// </summary>
		/// <param name="type">A class/value type, not an array or a string type</param>
		/// <returns></returns>
		public CorValue CreateValueForType(CorType type) {
			if (eval2 == null)
				return null;
			ICorDebugValue value;
			int hr = eval2.CreateValueForType(type.RawObject, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Allocates a new object instance and calls the specified constructor method
		/// </summary>
		/// <param name="ctor">Constructor</param>
		/// <param name="args">Constructor arguments</param>
		/// <returns></returns>
		public int NewObject(CorFunction ctor, CorValue[] args) {
			// Same thing as calling NewParameterizedObject(ctor, null, args)
			return obj.NewObject(ctor.RawObject, args.Length, args.ToCorDebugArray());
		}

		/// <summary>
		/// Instantiates a new parameterized type object and calls the object's constructor method
		/// </summary>
		/// <param name="ctor">Constructor</param>
		/// <param name="typeArgs">Type args or null if none required</param>
		/// <param name="args">Constructor arguments</param>
		/// <returns></returns>
		public int NewParameterizedObject(CorFunction ctor, CorType[] typeArgs, CorValue[] args) {
			if (eval2 == null) {
				if (typeArgs == null || typeArgs.Length == 0)
					return NewObject(ctor, args);
				return -1;
			}
			return eval2.NewParameterizedObject(ctor.RawObject, typeArgs == null ? 0 : typeArgs.Length, typeArgs.ToCorDebugArray(), args.Length, args.ToCorDebugArray());
		}

		/// <summary>
		/// Allocates a new object instance of the specified type, without attempting to call a constructor method
		/// </summary>
		/// <param name="cls">Class</param>
		/// <returns></returns>
		public int NewObjectNoConstructor(CorClass cls) {
			// Same thing as calling NewParameterizedObjectNoConstructor(cls, null)
			return obj.NewObjectNoConstructor(cls.RawObject);
		}

		/// <summary>
		/// Instantiates a new parameterized type object of the specified class without attempting to call a constructor method
		/// </summary>
		/// <param name="cls">Class</param>
		/// <param name="typeArgs">Type args or null if none required</param>
		/// <returns></returns>
		public int NewParameterizedObjectNoConstructor(CorClass cls, CorType[] typeArgs) {
			if (eval2 == null) {
				if (typeArgs == null || typeArgs.Length == 0)
					return NewObjectNoConstructor(cls);
				return -1;
			}
			return eval2.NewParameterizedObjectNoConstructor(cls.RawObject, typeArgs == null ? 0 : typeArgs.Length, typeArgs.ToCorDebugArray());
		}

		/// <summary>
		/// Allocates a new array of the specified element type and dimensions
		/// </summary>
		/// <param name="et">Element type</param>
		/// <param name="cls">Class or null if not needed</param>
		/// <param name="dims">Dimensions</param>
		/// <param name="lowBounds">Lower bounds or null (ignored by the CLR debugger at the moment)</param>
		/// <returns></returns>
		public int NewArray(CorElementType et, CorClass cls, uint[] dims, int[] lowBounds = null) {
			Debug.Assert(dims != null && (lowBounds == null || lowBounds.Length == dims.Length));
			return obj.NewArray(et, cls?.RawObject, dims.Length, dims, lowBounds);
		}

		/// <summary>
		/// Allocates a new array of the specified element type and dimensions
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="dims">Dimensions</param>
		/// <param name="lowBounds">Lower bounds or null (ignored by the CLR debugger at the moment)</param>
		/// <returns></returns>
		public int NewParameterizedArray(CorType type, uint[] dims, int[] lowBounds = null) {
			if (eval2 == null)
				return -1;
			Debug.Assert(dims != null && (lowBounds == null || lowBounds.Length == dims.Length));
			return eval2.NewParameterizedArray(type.RawObject, dims.Length, dims, lowBounds);
		}

		/// <summary>
		/// Creates a new string
		/// </summary>
		/// <param name="s">String</param>
		/// <returns></returns>
		public int NewString(string s) {
			if (eval2 != null)
				return eval2.NewStringWithLength(s, s.Length);
			return obj.NewString(s);
		}

		/// <summary>
		/// Sets up a call to the specified function
		/// </summary>
		/// <param name="func">Method</param>
		/// <param name="args">Arguments</param>
		/// <returns></returns>
		public int CallFunction(CorFunction func, CorValue[] args) {
			// Same thing as calling CallParameterizedFunction(func, null, args)
			return obj.CallFunction(func.RawObject, args.Length, args.ToCorDebugArray());
		}

		/// <summary>
		/// Sets up a call to the specified function
		/// </summary>
		/// <param name="func">Method</param>
		/// <param name="typeArgs">Type and method arguments, in that order, or null if none needed</param>
		/// <param name="args">Arguments</param>
		/// <returns></returns>
		public int CallParameterizedFunction(CorFunction func, CorType[] typeArgs, CorValue[] args) {
			if (eval2 == null) {
				if (typeArgs == null || typeArgs.Length == 0)
					return CallFunction(func, args);
				return -1;
			}
			return eval2.CallParameterizedFunction(func.RawObject, typeArgs == null ? 0 : typeArgs.Length, typeArgs.ToCorDebugArray(), args.Length, args.ToCorDebugArray());
		}

		public static bool operator ==(CorEval a, CorEval b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorEval a, CorEval b) => !(a == b);

		public bool Equals(CorEval other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) => Equals(obj as CorEval);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => string.Format("IsActive={0} {1}", IsActive ? 1 : 0, Thread);
	}
}
