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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// A <see cref="HexBuffer"/> change
	/// </summary>
	public abstract class HexChange {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexChange() { }

		/// <summary>
		/// Gets the difference in buffer length after the change
		/// </summary>
		public abstract long Delta { get; }

		/// <summary>
		/// Gets the new data
		/// </summary>
		public abstract byte[] NewData { get; }

		/// <summary>
		/// Gets the new position after the hex change
		/// </summary>
		public abstract HexPosition NewPosition { get; }

		/// <summary>
		/// Gets the new end position after the hex change
		/// </summary>
		public abstract HexPosition NewEnd { get; }

		/// <summary>
		/// Gets the new length
		/// </summary>
		public abstract HexPosition NewLength { get; }

		/// <summary>
		/// Gets the span after the hex change
		/// </summary>
		public abstract HexSpan NewSpan { get; }

		/// <summary>
		/// Gets the old data
		/// </summary>
		public abstract byte[] OldData { get; }

		/// <summary>
		/// Gets the old position before the hex change
		/// </summary>
		public abstract HexPosition OldPosition { get; }

		/// <summary>
		/// Gets the old end position before the hex change
		/// </summary>
		public abstract HexPosition OldEnd { get; }

		/// <summary>
		/// Gets the old length
		/// </summary>
		public abstract HexPosition OldLength { get; }

		/// <summary>
		/// Gets the span before the hex change
		/// </summary>
		public abstract HexSpan OldSpan { get; }
	}
}
