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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Text span
	/// </summary>
	public struct TextSpan : IEquatable<TextSpan> {
		readonly int start, end;

		/// <summary>
		/// Start offset
		/// </summary>
		public int Start => start;

		/// <summary>
		/// End offset, exclusive
		/// </summary>
		public int End => end;

		/// <summary>
		/// Length (<see cref="End"/> - <see cref="Start"/>)
		/// </summary>
		public int Length => end - start;

		/// <summary>
		/// true if it's empty (<see cref="Length"/> is 0)
		/// </summary>
		public bool IsEmpty => end == start;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start offset</param>
		/// <param name="length">Length</param>
		public TextSpan(int start, int length) {
			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			this.start = start;
			end = start + length;
			if (end < start)
				throw new ArgumentOutOfRangeException(nameof(length));
		}

		/// <summary>
		/// Creates a new instance
		/// </summary>
		/// <param name="start">Start offset</param>
		/// <param name="end">End offset</param>
		/// <returns></returns>
		public static TextSpan FromBounds(int start, int end) => new TextSpan(start, end - start);

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TextSpan other) => start == other.start && end == other.end;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is TextSpan && Equals((TextSpan)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => start ^ ((end << 16) | (end >> 16));

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "[" + start.ToString() + "," + end.ToString() + ")";
	}
}
