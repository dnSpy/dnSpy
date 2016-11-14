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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Creates <see cref="HexBufferLine"/>s
	/// </summary>
	public abstract class HexBufferLineProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferLineProvider() { }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// Number of lines. There's always at least one line even if <see cref="Span"/> is empty.
		/// </summary>
		public abstract HexPosition LineCount { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public abstract HexSpan Span { get; }

		/// <summary>
		/// Gets the number of characters per line
		/// </summary>
		public abstract int CharsPerLine { get; }

		/// <summary>
		/// Gets the number of bytes per line
		/// </summary>
		public abstract uint BytesPerLine { get; }

		/// <summary>
		/// Returns a line
		/// </summary>
		/// <param name="lineNumber">Line number</param>
		/// <returns></returns>
		public abstract HexBufferLine GetLineFromLineNumber(HexPosition lineNumber);

		/// <summary>
		/// Creates a line
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexBufferLine GetLineFromPosition(HexBufferPoint position);

		/// <summary>
		/// Gets the line number
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexPosition GetLineNumberFromPosition(HexBufferPoint position);

		/// <summary>
		/// Converts a physical (stream) position to a logical position
		/// </summary>
		/// <param name="physicalPosition">Physical (stream) position</param>
		/// <returns></returns>
		public abstract HexPosition ToLogicalPosition(HexPosition physicalPosition);

		/// <summary>
		/// Converts a logical position to a physical (stream) position
		/// </summary>
		/// <param name="logicalPosition">Logical position</param>
		/// <returns></returns>
		public abstract HexPosition ToPhysicalPosition(HexPosition logicalPosition);

		/// <summary>
		/// Returns true if <paramref name="position"/> is a valid position that can be passed to
		/// methods expecting a (physical) position.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public virtual bool IsValidPosition(HexBufferPoint position) {
			if (position.Buffer != Buffer)
				return false;
			return Span.Length == 0 ? Span.Start == position : Span.Contains(position);
		}
	}
}
