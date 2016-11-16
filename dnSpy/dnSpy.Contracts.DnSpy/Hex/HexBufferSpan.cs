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
using System.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Contains a <see cref="HexBuffer"/> and a <see cref="HexSpan"/>
	/// </summary>
	public struct HexBufferSpan : IEquatable<HexBufferSpan> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Buffer == null;

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public HexBuffer Buffer { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public HexSpan Span { get; }

		/// <summary>
		/// Gets the length
		/// </summary>
		public HexPosition Length => Span.Length;

		/// <summary>
		/// true if this span covers everything from 0 to 2^64-1, inclusive
		/// </summary>
		public bool IsFull => Span.IsFull;

		/// <summary>
		/// true if it's an empty span
		/// </summary>
		public bool IsEmpty => Span.IsEmpty;

		/// <summary>
		/// Gets the start point
		/// </summary>
		public HexBufferPoint Start => new HexBufferPoint(Buffer, Span.Start);

		/// <summary>
		/// Gets the end point. This can be 0 if the last byte is at position 2^64-1
		/// </summary>
		public HexBufferPoint End => new HexBufferPoint(Buffer, Span.End);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="span">Span</param>
		public HexBufferSpan(HexBuffer buffer, HexSpan span) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			Buffer = buffer;
			Span = span;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="start">Start point</param>
		/// <param name="length">Length</param>
		public HexBufferSpan(HexBuffer buffer, HexPosition start, ulong length) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			Buffer = buffer;
			Span = new HexSpan(start, length);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start position</param>
		/// <param name="end">End position</param>
		public HexBufferSpan(HexBufferPoint start, HexBufferPoint end) {
			if (start.Buffer != end.Buffer || start.Buffer == null)
				throw new ArgumentException();
			if (end.Position < start.Position)
				throw new ArgumentOutOfRangeException(nameof(end));
			Buffer = start.Buffer;
			Span = HexSpan.FromBounds(start, end);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start point</param>
		/// <param name="length">Length</param>
		public HexBufferSpan(HexBufferPoint start, ulong length) {
			if (start.Buffer == null)
				throw new ArgumentException();
			Buffer = start.Buffer;
			Span = new HexSpan(start, length);
		}

		/// <summary>
		/// Creates a new <see cref="HexBufferSpan"/> instance
		/// </summary>
		/// <param name="start">Start point</param>
		/// <param name="end">End point</param>
		public static HexBufferSpan FromBounds(HexBufferPoint start, HexBufferPoint end) => new HexBufferSpan(start, end);

		/// <summary>
		/// Converts this instance to a <see cref="HexSpan"/>
		/// </summary>
		/// <param name="hexBufferSpan"></param>
		public static implicit operator HexSpan(HexBufferSpan hexBufferSpan) => hexBufferSpan.Span;

		/// <summary>
		/// Gets the data
		/// </summary>
		/// <returns></returns>
		public byte[] GetData() => Buffer.ReadBytes(Span);

		/// <summary>
		/// Returns true if <paramref name="point"/> lies within this span
		/// </summary>
		/// <param name="point">Point</param>
		/// <returns></returns>
		public bool Contains(HexBufferPoint point) {
			if (point.Buffer != Buffer)
				throw new ArgumentException();
			return Span.Contains(point);
		}

		/// <summary>
		/// Returns true if <paramref name="span"/> lies within this span
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool Contains(HexSpan span) => Span.Contains(span);

		/// <summary>
		/// Returns true if <paramref name="span"/> lies within this span
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool Contains(HexBufferSpan span) {
			if (span.Buffer != Buffer)
				throw new ArgumentException();
			return Span.Contains(span.Span);
		}

		/// <summary>
		/// Returns true if <paramref name="position"/> lies within this span
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public bool Contains(HexPosition position) => Span.Contains(position);

		/// <summary>
		/// Returns true if this instances overlaps with <paramref name="span"/>
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public bool OverlapsWith(HexSpan span) => Span.OverlapsWith(span);

		/// <summary>
		/// Returns true if this instances overlaps with <paramref name="span"/>
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public bool OverlapsWith(HexBufferSpan span) {
			if (span.Buffer != Buffer)
				throw new ArgumentException();
			return Span.OverlapsWith(span.Span);
		}

		/// <summary>
		/// Gets the overlap with <paramref name="span"/> or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexBufferSpan? Overlap(HexSpan span) {
			var res = Span.Overlap(span);
			if (res == null)
				return null;
			return new HexBufferSpan(Buffer, res.Value);
		}

		/// <summary>
		/// Gets the overlap with <paramref name="span"/> or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexBufferSpan? Overlap(HexBufferSpan span) {
			if (span.Buffer != Buffer)
				throw new ArgumentException();
			var res = Span.Overlap(span.Span);
			if (res == null)
				return null;
			return new HexBufferSpan(Buffer, res.Value);
		}

		/// <summary>
		/// Returns true if <paramref name="span"/> intersects with this instance
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(HexSpan span) => Span.IntersectsWith(span);

		/// <summary>
		/// Returns true if <paramref name="span"/> intersects with this instance
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(HexBufferSpan span) {
			if (span.Buffer != Buffer)
				throw new ArgumentException();
			return Span.IntersectsWith(span.Span);
		}

		/// <summary>
		/// Returns the intersection or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexBufferSpan? Intersection(HexSpan span) {
			var res = Span.Intersection(span);
			if (res == null)
				return null;
			return new HexBufferSpan(Buffer, res.Value);
		}

		/// <summary>
		/// Returns the intersection or null if there's none
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public HexBufferSpan? Intersection(HexBufferSpan span) {
			if (span.Buffer != Buffer)
				throw new ArgumentException();
			var res = Span.Intersection(span.Span);
			if (res == null)
				return null;
			return new HexBufferSpan(Buffer, res.Value);
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(HexBufferSpan a, HexBufferSpan b) => a.Equals(b);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(HexBufferSpan a, HexBufferSpan b) => !a.Equals(b);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexBufferSpan other) => Buffer == other.Buffer && Span == other.Span;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexBufferSpan && Equals((HexBufferSpan)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Buffer?.GetHashCode() ?? 0) ^ Span.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (Buffer == null)
				return "uninit";
			const int maxBytes = 16;
			const string ellipsis = "...";
			bool tooMuchData = Length > maxBytes;
			var sb = new StringBuilder();
			sb.Append(Span.ToString());
			sb.Append("_'");
			var pos = Span.Start;
			for (int i = 0; i < Length && i < maxBytes; i++) {
				var c = pos < HexPosition.MaxEndPosition ? Buffer.TryReadByte(pos++) : -1;
				sb.Append(c < 0 ? "??" : c.ToString("X2"));
			}
			if (tooMuchData)
				sb.Append(ellipsis);
			sb.Append("'");
			return sb.ToString();
		}
	}
}
