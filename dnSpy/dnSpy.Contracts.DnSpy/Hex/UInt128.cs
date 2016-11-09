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

namespace dnSpy.Contracts.Hex {
	struct UInt128 : IComparable<UInt128>, IEquatable<UInt128> {
		readonly ulong lo, hi;

		public ulong Low => lo;
		public ulong High => hi;

		public UInt128(ulong value) {
			lo = value;
			hi = 0;
		}

		public UInt128(ulong hi, ulong lo) {
			this.lo = lo;
			this.hi = hi;
		}

		public static UInt128 Max(UInt128 a, UInt128 b) => a >= b ? a : b;
		public static UInt128 Min(UInt128 a, UInt128 b) => a <= b ? a : b;
		public static bool operator <(UInt128 a, UInt128 b) => a.CompareTo(b) < 0;
		public static bool operator <=(UInt128 a, UInt128 b) => a.CompareTo(b) <= 0;
		public static bool operator >(UInt128 a, UInt128 b) => a.CompareTo(b) > 0;
		public static bool operator >=(UInt128 a, UInt128 b) => a.CompareTo(b) >= 0;
		public static bool operator ==(UInt128 a, UInt128 b) => a.Equals(b);
		public static bool operator !=(UInt128 a, UInt128 b) => !a.Equals(b);

		public static UInt128 operator -(UInt128 a, UInt128 b) {
			var hi = a.hi - b.hi;
			var lo = a.lo - b.lo;
			if (a.lo < b.lo)
				hi--;
			return new UInt128(hi, lo);
		}

		public int CompareTo(UInt128 other) {
			if (hi != other.hi)
				return hi.CompareTo(other.hi);
			return lo.CompareTo(other.lo);
		}

		public bool Equals(UInt128 other) => lo == other.lo && hi == other.hi;
		public override bool Equals(object obj) => obj is UInt128 && Equals((UInt128)obj);
		public override int GetHashCode() => lo.GetHashCode() ^ hi.GetHashCode();
	}
}
