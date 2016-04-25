/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorFunctionBreakpoint : COMObject<ICorDebugFunctionBreakpoint>, IEquatable<CorFunctionBreakpoint> {
		/// <summary>
		/// Gets the function or null
		/// </summary>
		public CorFunction Function {
			get {
				ICorDebugFunction func;
				int hr = obj.GetFunction(out func);
				return hr < 0 || func == null ? null : new CorFunction(func);
			}
		}

		/// <summary>
		/// Gets/sets whether the breakpoint is active
		/// </summary>
		public bool IsActive {
			get {
				int active;
				int hr = obj.IsActive(out active);
				return hr >= 0 && active != 0;
			}
			set {
				int hr = obj.Activate(value ? 1 : 0);
			}
		}

		/// <summary>
		/// Gets the offset of the breakpoint
		/// </summary>
		public uint Offset {
			get { return offset; }
		}
		readonly uint offset;

		public CorFunctionBreakpoint(ICorDebugFunctionBreakpoint functionBreakpoint)
			: base(functionBreakpoint) {
			int hr = functionBreakpoint.GetOffset(out this.offset);
			if (hr < 0)
				this.offset = 0;
		}

		public static bool operator ==(CorFunctionBreakpoint a, CorFunctionBreakpoint b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorFunctionBreakpoint a, CorFunctionBreakpoint b) {
			return !(a == b);
		}

		public bool Equals(CorFunctionBreakpoint other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorFunctionBreakpoint);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[FunctionBreakpoint] Enabled={0}, Offset={1:X4} Method={2}", IsActive ? 1 : 0, Offset, Function);
		}
	}
}
