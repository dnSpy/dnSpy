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
	sealed class CorClass : COMObject<ICorDebugClass>, IEquatable<CorClass?> {
		public uint Token => token;
		readonly uint token;

		public CorModule? Module {
			get {
				int hr = obj.GetModule(out var module);
				return hr < 0 || module is null ? null : new CorModule(module);
			}
		}

		public CorClass(ICorDebugClass cls)
			: base(cls) {
			int hr = cls.GetToken(out token);
			if (hr < 0)
				token = 0;
		}

		public CorType? GetParameterizedType(CorElementType etype, CorType[]? typeArgs = null) {
			Debug.Assert(etype == CorElementType.Class || etype == CorElementType.ValueType);
			var c2 = obj as ICorDebugClass2;
			if (c2 is null)
				return null;
			int hr = c2.GetParameterizedType(etype, typeArgs?.Length ?? 0, typeArgs.ToCorDebugArray(), out var value);
			return hr < 0 || value is null ? null : new CorType(value);
		}

		public bool Equals(CorClass? other) => other is not null && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorClass);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
