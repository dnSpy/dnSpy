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
	/// <summary>
	/// A position in a <see cref="HexBufferStream"/>
	/// </summary>
	public struct HexPosition : IEquatable<HexPosition>, IComparable<HexPosition> {
		/// <summary>
		/// Gets the value 0
		/// </summary>
		public static readonly HexPosition Zero = new HexPosition(0);

		static readonly HexPosition One = new HexPosition(1);

		/// <summary>
		/// Gets the minimum value (0)
		/// </summary>
		public static readonly HexPosition MinValue = new HexPosition(0);

		/// <summary>
		/// Gets the maximum value (2^128-1)
		/// </summary>
		public static readonly HexPosition MaxValue = new HexPosition(ulong.MaxValue, ulong.MaxValue);

		/// <summary>
		/// Max end position (2^64)
		/// </summary>
		public static readonly HexPosition MaxEndPosition = new HexPosition(1, 0);

		readonly ulong lo;
		readonly ulong hi;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="highValue">High 64 bits of the value</param>
		/// <param name="lowValue">Low 64 bits of the value</param>
		public HexPosition(ulong highValue, ulong lowValue) {
			this.hi = highValue;
			this.lo = lowValue;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public HexPosition(ulong value) {
			lo = value;
			hi = 0;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public HexPosition(long value) {
			lo = (ulong)value;
			hi = value < 0 ? ulong.MaxValue : 0;
		}

		/// <summary>
		/// Converts the value to a <see cref="ulong"/>, truncating the result if it doesn't fit in 64 bits.
		/// </summary>
		/// <returns></returns>
		public ulong ToUInt64() => lo;

		/// <summary>
		/// Returns the larger of two values
		/// </summary>
		/// <param name="val1">First value</param>
		/// <param name="val2">Second value</param>
		/// <returns></returns>
		public static HexPosition Max(HexPosition val1, HexPosition val2) => val1 >= val2 ? val1 : val2;

		/// <summary>
		/// Returns the smaller of two values
		/// </summary>
		/// <param name="val1">First value</param>
		/// <param name="val2">Second value</param>
		/// <returns></returns>
		public static HexPosition Min(HexPosition val1, HexPosition val2) => val1 <= val2 ? val1 : val2;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static implicit operator HexPosition(ulong value) => new HexPosition(value);
		public static implicit operator HexPosition(uint value) => new HexPosition((ulong)value);
		public static implicit operator HexPosition(long value) => new HexPosition(value);
		public static implicit operator HexPosition(int value) => new HexPosition(value);

		public static bool operator <(HexPosition a, HexPosition b) => a.CompareTo(b) < 0;
		public static bool operator <=(HexPosition a, HexPosition b) => a.CompareTo(b) <= 0;
		public static bool operator >(HexPosition a, HexPosition b) => a.CompareTo(b) > 0;
		public static bool operator >=(HexPosition a, HexPosition b) => a.CompareTo(b) >= 0;
		public static bool operator ==(HexPosition a, HexPosition b) => a.Equals(b);
		public static bool operator !=(HexPosition a, HexPosition b) => !a.Equals(b);

		public static HexPosition operator +(HexPosition a, HexPosition b) => Add(a, b);
		public static HexPosition operator -(HexPosition a, HexPosition b) => Subtract(a, b);
		public static HexPosition operator -(HexPosition a) => Subtract(Zero, a);
		public static HexPosition operator ++(HexPosition a) => Add(a, One);
		public static HexPosition operator --(HexPosition a) => Subtract(a, One);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		static HexPosition Add(HexPosition a, HexPosition b) {
			ulong lo = a.lo + b.lo;
			ulong hi = a.hi + b.hi;
			if (lo < a.lo)
				hi++;
			return new HexPosition(hi, lo);
		}

		static HexPosition Subtract(HexPosition a, HexPosition b) {
			var lo = a.lo - b.lo;
			var hi = a.hi - b.hi;
			if (a.lo < b.lo)
				hi--;
			return new HexPosition(hi, lo);
		}

		/// <summary>
		/// Compares this instance with <paramref name="other"/>. Returns less than 0, 0, or greater
		/// than 0 depending on whether this instance is less than, equal to, or greather than
		/// <paramref name="other"/>.
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public int CompareTo(HexPosition other) {
			if (hi != other.hi)
				return hi.CompareTo(other.hi);
			return lo.CompareTo(other.lo);
		}

		/// <summary>
		/// Compares this instance with <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexPosition other) => lo == other.lo && hi == other.hi;

		/// <summary>
		/// Compares this instance with <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexPosition && Equals((HexPosition)obj);

		/// <summary>
		/// Gets the hash code of this instance
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (lo ^ hi).GetHashCode();

		/// <summary>
		/// Gets the value as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (hi == 0)
				return "0x" + lo.ToString("X");
			return "0x" + hi.ToString("X") + lo.ToString("X16");
		}
	}
}
