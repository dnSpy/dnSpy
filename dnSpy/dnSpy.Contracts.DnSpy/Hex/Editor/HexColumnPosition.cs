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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// A position in a hex column
	/// </summary>
	public struct HexColumnPosition : IEquatable<HexColumnPosition> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => ValuePosition.IsDefault;

		/// <summary>
		/// Active column
		/// </summary>
		public HexColumnType ActiveColumn { get; }

		/// <summary>
		/// Position in the values column
		/// </summary>
		public HexCellPosition ValuePosition { get; }

		/// <summary>
		/// Position in the ASCII column
		/// </summary>
		public HexCellPosition AsciiPosition { get; }

		/// <summary>
		/// Gets the active position (<see cref="ValuePosition"/> or <see cref="AsciiPosition"/>)
		/// </summary>
		public HexCellPosition ActivePosition {
			get {
				switch (ActiveColumn) {
				case HexColumnType.Values:	return ValuePosition;
				case HexColumnType.Ascii:	return AsciiPosition;
				case HexColumnType.Offset:
				default:
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="activeColumn">Active column</param>
		/// <param name="valuePosition">Position in the values column</param>
		/// <param name="asciiPosition">Position in the ASCII column</param>
		public HexColumnPosition(HexColumnType activeColumn, HexCellPosition valuePosition, HexCellPosition asciiPosition) {
			if (activeColumn != HexColumnType.Values && activeColumn != HexColumnType.Ascii)
				throw new ArgumentOutOfRangeException(nameof(activeColumn));
			if (valuePosition.IsDefault)
				throw new ArgumentException();
			if (asciiPosition.IsDefault)
				throw new ArgumentException();
			ActiveColumn = activeColumn;
			ValuePosition = valuePosition;
			AsciiPosition = asciiPosition;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(HexColumnPosition a, HexColumnPosition b) => a.Equals(b);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(HexColumnPosition a, HexColumnPosition b) => !a.Equals(b);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexColumnPosition other) => ActiveColumn == other.ActiveColumn && ValuePosition.Equals(other.ValuePosition) && AsciiPosition.Equals(other.AsciiPosition);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexColumnPosition && Equals((HexColumnPosition)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (int)ActiveColumn ^ ValuePosition.GetHashCode() ^ AsciiPosition.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "[" + ActiveColumn.ToString() + ",V=" + ValuePosition.ToString() + ",A=" + AsciiPosition.ToString() + "]";
	}
}
