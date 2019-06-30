/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Caret position
	/// </summary>
	public readonly struct HexCaretPosition : IEquatable<HexCaretPosition> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Position.IsDefault;

		/// <summary>
		/// Gets the position
		/// </summary>
		public HexColumnPosition Position { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position">Position</param>
		public HexCaretPosition(HexColumnPosition position) {
			// Don't throw if position is the default value. Could happen if none of values and ASCII columns are shown.
			Position = position;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(HexCaretPosition a, HexCaretPosition b) => a.Equals(b);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(HexCaretPosition a, HexCaretPosition b) => !a.Equals(b);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexCaretPosition other) => Position.Equals(other.Position);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is HexCaretPosition && Equals((HexCaretPosition)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Position.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Position.ToString();
	}
}
