/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Shared.UI.HexEditor {
	[DebuggerDisplay("X={X}, Y={Y}")]
	struct HexPositionUI : IEquatable<HexPositionUI>, IComparable<HexPositionUI> {
		public ulong X;
		public ulong Y;

		public HexPositionUI(ulong x, ulong y) {
			this.X = x;
			this.Y = y;
		}

		public static bool operator <(HexPositionUI a, HexPositionUI b) {
			return a.CompareTo(b) < 0;
		}

		public static bool operator <=(HexPositionUI a, HexPositionUI b) {
			return a.CompareTo(b) <= 0;
		}

		public static bool operator >(HexPositionUI a, HexPositionUI b) {
			return a.CompareTo(b) > 0;
		}

		public static bool operator >=(HexPositionUI a, HexPositionUI b) {
			return a.CompareTo(b) >= 0;
		}

		public static bool operator ==(HexPositionUI a, HexPositionUI b) {
			return a.CompareTo(b) == 0;
		}

		public static bool operator !=(HexPositionUI a, HexPositionUI b) {
			return a.CompareTo(b) != 0;
		}

		public int CompareTo(HexPositionUI other) {
			if (Y != other.Y)
				return Y.CompareTo(other.Y);
			return X.CompareTo(other.X);
		}

		public bool Equals(HexPositionUI other) {
			return CompareTo(other) == 0;
		}

		public override bool Equals(object obj) {
			return obj is HexPositionUI && Equals((HexPositionUI)obj);
		}

		public override int GetHashCode() {
			return (int)X ^ (int)(X >> 32) ^
				(int)Y ^ (int)(Y >> 32);
		}
	}
}
