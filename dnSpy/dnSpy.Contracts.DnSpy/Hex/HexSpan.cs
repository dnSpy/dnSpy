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
	/// A span in a <see cref="HexBuffer"/>
	/// </summary>
	public struct HexSpan : IEquatable<HexSpan> {
		readonly ulong start;
		readonly ulong length;
		readonly bool isFull;

		/// <summary>
		/// Gets a <see cref="HexSpan"/> instance that covers everything from 0 to 2^64-1, inclusive
		/// </summary>
		public static readonly HexSpan FullSpan = new HexSpan(dummy: false);

		/// <summary>
		/// true if this span covers everything from 0 to 2^64-1, inclusive
		/// </summary>
		public bool IsFull => isFull;

		/// <summary>
		/// true if it's an empty span
		/// </summary>
		public bool IsEmpty => length == 0;

		/// <summary>
		/// Gets the length. If <see cref="IsFull"/> is true, this is one byte less than the actual length
		/// </summary>
		public ulong Length => length;

		/// <summary>
		/// Gets the start of the span
		/// </summary>
		public ulong Start => start;

		/// <summary>
		/// Gets the end of the span. This can be 0 if the last byte is at position 2^64-1
		/// </summary>
		public ulong End => isFull ? 0 : start + length;

		/// <summary>
		/// Gets the last position in this span. If this span is empty or 1 byte in length, this property equals <see cref="Start"/>
		/// </summary>
		public ulong Last => isFull ? ulong.MaxValue : length == 0 ? start : start + length - 1;

		HexSpan(bool dummy) {
			start = 0;
			length = ulong.MaxValue;
			isFull = true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Position</param>
		/// <param name="length">Length</param>
		public HexSpan(ulong start, ulong length) {
			if (start + length < length && start + length != 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			this.start = start;
			this.length = length;
			this.isFull = false;
		}

		/// <summary>
		/// Creates a <see cref="HexSpan"/>
		/// </summary>
		/// <param name="start">Start position</param>
		/// <param name="end">End position</param>
		/// <returns></returns>
		public static HexSpan FromBounds(ulong start, ulong end) {
			if (end < start)
				throw new ArgumentOutOfRangeException(nameof(end));
			return new HexSpan(start, end - start);
		}

		UInt128 End128 {
			get {
				if (Length == 0 || End != 0)
					return new UInt128(End);
				return new UInt128(1, 0);
			}
		}

		/// <summary>
		/// Returns true if <paramref name="span"/> lies within this span
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool Contains(HexSpan span) =>
			span.Start >= Start && span.End128 <= End128;

		/// <summary>
		/// Returns true if <paramref name="position"/> lies within this span
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public bool Contains(ulong position) =>
			position >= Start && new UInt128(position) < End128;

		/// <summary>
		/// Returns the intersection or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexSpan? Intersection(HexSpan span) {
			if (IsFull)
				return span;
			if (span.IsFull)
				return this;
			var newStart = new UInt128(Math.Max(Start, span.Start));
			var newEnd = UInt128.Min(End128, span.End128);
			if (newStart <= newEnd)
				return new HexSpan(newStart.Low, (newEnd - newStart).Low);
			return null;
		}

		/// <summary>
		/// Returns true if <paramref name="span"/> intersects with this instance
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(HexSpan span) {
			if (IsFull)
				return true;
			if (span.IsFull)
				return true;
			return new UInt128(Start) <= span.End128 && End128 >= new UInt128(span.Start);
		}

		/// <summary>
		/// Gets the overlap with <paramref name="span"/> or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexSpan? Overlap(HexSpan span) {
			var newStart = new UInt128(Math.Max(Start, span.Start));
			var newEnd = UInt128.Min(End128, span.End128);
			if (newStart < newEnd)
				return new HexSpan(newStart.Low, (newEnd - newStart).Low);
			return null;
		}

		/// <summary>
		/// Returns true if this instances overlaps with <paramref name="span"/>
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public bool OverlapsWith(HexSpan span) => Overlap(span) != null;

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left">Left</param>
		/// <param name="right">Right</param>
		/// <returns></returns>
		public static bool operator ==(HexSpan left, HexSpan right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left">Left</param>
		/// <param name="right">Right</param>
		/// <returns></returns>
		public static bool operator !=(HexSpan left, HexSpan right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexSpan other) => start == other.start && length == other.length && isFull == other.isFull;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexSpan && Equals((HexSpan)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => start.GetHashCode() ^ length.GetHashCode() ^ (isFull ? int.MinValue : 0);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => isFull ? "[full]" : "[0x" + Start.ToString("X") + ",0x" + End.ToString("X") + ")";
	}
}
