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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorEval : COMObject<ICorDebugEval>, IEquatable<CorEval> {
		readonly ICorDebugEval2 eval2;

		public CorThread Thread {
			get {
				int hr = obj.GetThread(out var thread);
				return hr < 0 || thread == null ? null : new CorThread(thread);
			}
		}

		public bool IsActive {
			get {
				int hr = obj.IsActive(out int act);
				return hr >= 0 && act != 0;
			}
		}

		public CorValue Result {
			get {
				int hr = obj.GetResult(out var value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		public CorEval(ICorDebugEval eval)
			: base(eval) => eval2 = eval as ICorDebugEval2;

		public int Abort() => obj.Abort();

		public int RudeAbort() {
			if (eval2 == null)
				return -1;
			return eval2.RudeAbort();
		}

		public CorValue CreateValue(CorElementType et, CorClass cls = null) {
			int hr = obj.CreateValue(et, cls?.RawObject, out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public CorValue CreateValueForType(CorType type) {
			if (eval2 == null)
				return null;
			int hr = eval2.CreateValueForType(type.RawObject, out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public int NewObject(CorFunction ctor, CorValue[] args) =>
			// Same thing as calling NewParameterizedObject(ctor, null, args)
			obj.NewObject(ctor.RawObject, args.Length, args.ToCorDebugArray());

		public int NewParameterizedObject(CorFunction ctor, CorType[] typeArgs, CorValue[] args) {
			if (eval2 == null) {
				if (typeArgs == null || typeArgs.Length == 0)
					return NewObject(ctor, args);
				return -1;
			}
			return eval2.NewParameterizedObject(ctor.RawObject, typeArgs == null ? 0 : typeArgs.Length, typeArgs.ToCorDebugArray(), args.Length, args.ToCorDebugArray());
		}

		public int NewObjectNoConstructor(CorClass cls) =>
			// Same thing as calling NewParameterizedObjectNoConstructor(cls, null)
			obj.NewObjectNoConstructor(cls.RawObject);

		public int NewParameterizedObjectNoConstructor(CorClass cls, CorType[] typeArgs) {
			if (eval2 == null) {
				if (typeArgs == null || typeArgs.Length == 0)
					return NewObjectNoConstructor(cls);
				return -1;
			}
			return eval2.NewParameterizedObjectNoConstructor(cls.RawObject, typeArgs == null ? 0 : typeArgs.Length, typeArgs.ToCorDebugArray());
		}

		public int NewArray(CorElementType et, CorClass cls, uint[] dims, int[] lowBounds = null) {
			Debug.Assert(dims != null && (lowBounds == null || lowBounds.Length == dims.Length));
			return obj.NewArray(et, cls?.RawObject, dims.Length, dims, lowBounds);
		}

		public int NewParameterizedArray(CorType type, uint[] dims, int[] lowBounds = null) {
			if (eval2 == null)
				return -1;
			Debug.Assert(dims != null && (lowBounds == null || lowBounds.Length == dims.Length));
			return eval2.NewParameterizedArray(type.RawObject, dims.Length, dims, lowBounds);
		}

		public int NewString(string s) {
			if (eval2 != null)
				return eval2.NewStringWithLength(s, s.Length);
			return obj.NewString(s);
		}

		public int CallFunction(CorFunction func, CorValue[] args) =>
			// Same thing as calling CallParameterizedFunction(func, null, args)
			obj.CallFunction(func.RawObject, args.Length, args.ToCorDebugArray());

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

		public bool Equals(CorEval other) => !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;

		public override bool Equals(object obj) => Equals(obj as CorEval);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"IsActive={(IsActive ? 1 : 0)} {Thread}";
	}
}
