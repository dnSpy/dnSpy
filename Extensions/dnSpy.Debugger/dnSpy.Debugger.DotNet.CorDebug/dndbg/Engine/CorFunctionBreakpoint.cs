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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorFunctionBreakpoint : COMObject<ICorDebugFunctionBreakpoint>, IEquatable<CorFunctionBreakpoint?> {
		public CorFunction? Function {
			get {
				int hr = obj.GetFunction(out var func);
				return hr < 0 || func is null ? null : new CorFunction(func);
			}
		}

		public bool IsActive {
			get {
				int hr = obj.IsActive(out int active);
				return hr >= 0 && active != 0;
			}
			set {
				int hr = obj.Activate(value ? 1 : 0);
			}
		}

		public uint Offset => offset;
		readonly uint offset;

		public CorFunctionBreakpoint(ICorDebugFunctionBreakpoint functionBreakpoint)
			: base(functionBreakpoint) {
			int hr = functionBreakpoint.GetOffset(out offset);
			if (hr < 0)
				offset = 0;
		}

		public bool Equals(CorFunctionBreakpoint? other) => other is not null && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorFunctionBreakpoint);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"[FunctionBreakpoint] Enabled={(IsActive ? 1 : 0)}, Offset={Offset:X4} Method={Function}";
	}
}
