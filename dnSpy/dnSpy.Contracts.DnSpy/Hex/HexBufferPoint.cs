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
	/// Contains a <see cref="HexBuffer"/> and a position
	/// </summary>
	public struct HexBufferPoint : IEquatable<HexBufferPoint>, IComparable<HexBufferPoint> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Buffer == null;

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public HexBuffer Buffer { get; }

		/// <summary>
		/// Gets the position
		/// </summary>
		public ulong Position { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public HexBufferPoint(HexBuffer buffer, ulong position) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			Buffer = buffer;
			Position = position;
		}

		/// <summary>
		/// Converts <paramref name="point"/> to a <see cref="ulong"/>
		/// </summary>
		/// <param name="point">Point</param>
		public static implicit operator ulong(HexBufferPoint point) => point.Position;

		/// <summary>
		/// Gets the byte
		/// </summary>
		/// <returns></returns>
		public byte GetByte() => Buffer.ReadByte(Position);

		/// <summary>
		/// Gets the <see cref="byte"/> or a value less than 0 if there's no data at <see cref="Position"/>
		/// </summary>
		/// <returns></returns>
		public int TryGetByte() => Buffer.TryReadByte(Position);

		/// <summary>
		/// Add <paramref name="value"/>
		/// </summary>
		/// <param name="value">Value to add</param>
		/// <returns></returns>
		public HexBufferPoint Add(ulong value) => new HexBufferPoint(Buffer, Position + value);

		/// <summary>
		/// Subtract <paramref name="value"/>
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public HexBufferPoint Subtract(ulong value) => new HexBufferPoint(Buffer, Position - value);

		/// <summary>
		/// Returns the difference of <paramref name="other"/> with this instance
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public ulong Difference(HexBufferPoint other) => other - this;

		/// <summary>
		/// operator -()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static ulong operator -(HexBufferPoint left, HexBufferPoint right) => left.Position - right.Position;

		/// <summary>
		/// operator -()
		/// </summary>
		/// <param name="point"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static HexBufferPoint operator -(HexBufferPoint point, ulong value) => point.Subtract(value);

		/// <summary>
		/// operator -()
		/// </summary>
		/// <param name="point"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static HexBufferPoint operator -(HexBufferPoint point, long value) => point.Subtract((ulong)value);

		/// <summary>
		/// operator +()
		/// </summary>
		/// <param name="point"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static HexBufferPoint operator +(HexBufferPoint point, ulong value) => point.Add(value);

		/// <summary>
		/// operator +()
		/// </summary>
		/// <param name="point"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static HexBufferPoint operator +(HexBufferPoint point, long value) => point.Add((ulong)value);

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(HexBufferPoint a, HexBufferPoint b) => a.Equals(b);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(HexBufferPoint a, HexBufferPoint b) => !a.Equals(b);

		/// <summary>
		/// operator &lt;()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator <(HexBufferPoint a, HexBufferPoint b) => a.CompareTo(b) < 0;

		/// <summary>
		/// operator &lt;=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator <=(HexBufferPoint a, HexBufferPoint b) => a.CompareTo(b) <= 0;

		/// <summary>
		/// operator &gt;()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator >(HexBufferPoint a, HexBufferPoint b) => a.CompareTo(b) > 0;

		/// <summary>
		/// operator &gt;=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator >=(HexBufferPoint a, HexBufferPoint b) => a.CompareTo(b) >= 0;

		/// <summary>
		/// Compares this instance with <paramref name="other"/>
		/// </summary>
		/// <param name="other">Ohter instance</param>
		/// <returns></returns>
		public int CompareTo(HexBufferPoint other) {
			if (Buffer != other.Buffer)
				throw new ArgumentException();
			return Position.CompareTo(other.Position);
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexBufferPoint other) => Buffer == other.Buffer && Position == other.Position;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexBufferPoint && Equals((HexBufferPoint)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Buffer?.GetHashCode() ?? 0) ^ Position.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (Buffer == null)
				return "uninit";
			var b = TryGetByte();
			var bs = b < 0 ? "??" : b.ToString("X8");
			return "0x" + Position.ToString("X") + "_'" + bs + "'";
		}
	}
}
