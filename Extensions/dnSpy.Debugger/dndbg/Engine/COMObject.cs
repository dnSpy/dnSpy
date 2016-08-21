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
using System.Diagnostics;

namespace dndbg.Engine {
	public abstract class COMObject<T> : IEquatable<COMObject<T>> where T : class {
		public T RawObject => obj;
		protected readonly T obj;

		protected COMObject(T obj) {
			Debug.Assert(obj != null);
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
			this.obj = obj;
		}

		public static bool operator ==(COMObject<T> a, COMObject<T> b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(COMObject<T> a, COMObject<T> b) => !(a == b);
		public bool Equals(COMObject<T> other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as COMObject<T>);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
