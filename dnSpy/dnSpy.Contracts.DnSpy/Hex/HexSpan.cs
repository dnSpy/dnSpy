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
		public static readonly HexSpan FullSpan = new HexSpan(HexPosition.Zero, HexPosition.MaxEndPosition);

		/// <summary>
		/// true if this span covers everything from 0 to 2^64-1, inclusive
		/// </summary>
		public bool IsFull => isFull;

		/// <summary>
		/// true if it's an empty span
		/// </summary>
		public bool IsEmpty => length == 0;

		/// <summary>
		/// Gets the length
		/// </summary>
		public HexPosition Length => isFull ? HexPosition.MaxEndPosition : new HexPosition(length);

		/// <summary>
		/// Gets the start of the span
		/// </summary>
		public HexPosition Start => new HexPosition(start);

		/// <summary>
		/// Gets the end of the span
		/// </summary>
		public HexPosition End => isFull ? HexPosition.MaxEndPosition : new HexPosition(start + length);

		// It's not public because public ctors should only take start and length params
		HexSpan(HexPosition start, HexPosition end) {
			if (start > end)
				throw new ArgumentOutOfRangeException(nameof(end));
			if (end > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(end));
			this.start = start.ToUInt64();
			if (start == HexPosition.Zero && end == HexPosition.MaxEndPosition) {
				isFull = true;
				length = ulong.MaxValue;
			}
			else {
				isFull = false;
				length = (end - start).ToUInt64();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Position</param>
		/// <param name="length">Length</param>
		public HexSpan(HexPosition start, ulong length) {
			if (start > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(start));
			if ((start + length) > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(length));
			this.start = start.ToUInt64();
			this.length = length;
			this.isFull = false;
		}

		/// <summary>
		/// Creates a <see cref="HexSpan"/>
		/// </summary>
		/// <param name="start">Start position</param>
		/// <param name="end">End position</param>
		/// <returns></returns>
		public static HexSpan FromBounds(HexPosition start, HexPosition end) => new HexSpan(start, end);

		/// <summary>
		/// Returns true if <paramref name="span"/> lies within this span
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool Contains(HexSpan span) =>
			span.Start >= Start && span.End <= End;

		/// <summary>
		/// Returns true if <paramref name="position"/> lies within this span
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public bool Contains(HexPosition position) =>
			position >= Start && position < End;

		/// <summary>
		/// Returns the intersection or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexSpan? Intersection(HexSpan span) {
			var newStart = HexPosition.Max(Start, span.Start);
			var newEnd = HexPosition.Min(End, span.End);
			if (newStart <= newEnd)
				return FromBounds(newStart, newEnd);
			return null;
		}

		/// <summary>
		/// Returns true if <paramref name="span"/> intersects with this instance
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(HexSpan span) =>
			Start <= span.End && End >= span.Start;

		/// <summary>
		/// Gets the overlap with <paramref name="span"/> or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexSpan? Overlap(HexSpan span) {
			var newStart = HexPosition.Max(Start, span.Start);
			var newEnd = HexPosition.Min(End, span.End);
			if (newStart < newEnd)
				return FromBounds(newStart, newEnd);
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
		public override int GetHashCode() => (start ^ length).GetHashCode() ^ (isFull ? int.MinValue : 0);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => isFull ? "[full]" : "[" + Start.ToString() + "," + End.ToString() + ")";
	}
}
