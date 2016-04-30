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

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// Span
	/// </summary>
	public struct Span : IEquatable<Span> {
		/// <summary>
		/// true if the span's empty
		/// </summary>
		public bool IsEmpty => Start == End;

		/// <summary>
		/// Gets the start of the span
		/// </summary>
		public int Start { get; }

		/// <summary>
		/// Gets the end of the span
		/// </summary>
		public int End { get; }

		/// <summary>
		/// Gets the length of the span
		/// </summary>
		public int Length => End - Start;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="length">Length</param>
		public Span(int start, int length) {
			if (start < 0)
				throw new ArgumentException(nameof(start));
			if (start + length < start)
				throw new ArgumentException(nameof(length));
			Start = start;
			End = start + length;
		}

		/// <summary>
		/// Creates a new <see cref="Span"/>
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="end">End</param>
		/// <returns></returns>
		public static Span FromBounds(int start, int end) => new Span(start, end - start);

		/// <summary>
		/// Returns true if <paramref name="position"/> lies within this span
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public bool Contains(int position) => Start <= position && position < End;

		/// <summary>
		/// Returns true if <paramref name="span"/> lies completely within this span
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool Contains(Span span) => Start <= span.Start && span.End <= End;

		/// <summary>
		/// Gets the intersection or null if they don't intersect
		/// </summary>
		/// <param name="span">Other span</param>
		/// <returns></returns>
		public Span? Intersection(Span span) {
			int start = Math.Max(Start, span.Start);
			int end = Math.Min(End, span.End);
			return start <= end ? new Span(start, end - start) : (Span?)null;
		}

		/// <summary>
		/// Returns true if this span intersects with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Other span</param>
		/// <returns></returns>
		public bool IntersectsWith(Span span) => Start <= span.End && End >= span.Start;

		/// <summary>
		/// Gets the overlap or null if they don't overlap
		/// </summary>
		/// <param name="span">Other span</param>
		/// <returns></returns>
		public Span? Overlap(Span span) {
			int start = Math.Max(Start, span.Start);
			int end = Math.Min(End, span.End);
			return start < end ? new Span(start, end - start) : (Span?)null;
		}

		/// <summary>
		/// Returns true if this span overlaps with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Other span</param>
		/// <returns></returns>
		public bool OverlapsWith(Span span) => Math.Max(Start, span.Start) < Math.Min(End, span.End);

		/// <summary>
		/// operator==
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(Span a, Span b) => a.Start == b.Start && a.End == b.End;

		/// <summary>
		/// operator!=
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(Span a, Span b) => a.Start != b.Start || a.End != b.End;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(Span other) => Start == other.Start && End == other.End;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is Span && Equals((Span)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Start ^ (End << 16) ^ (ushort)(End >> 16);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "[" + Start.ToString() + ".." + End.ToString() + ")";
	}
}
