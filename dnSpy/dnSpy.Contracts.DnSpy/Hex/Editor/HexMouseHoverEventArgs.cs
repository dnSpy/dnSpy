/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	/// Mouse hover event args
	/// </summary>
	public sealed class HexMouseHoverEventArgs : EventArgs {
		/// <summary>
		/// Gets the hex view
		/// </summary>
		public HexView View { get; }

		/// <summary>
		/// Gets the line
		/// </summary>
		public HexBufferLine Line { get; }

		/// <summary>
		/// Gets the text position
		/// </summary>
		public int TextPosition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="view">Hex view</param>
		/// <param name="line">Line</param>
		/// <param name="textPosition">Text position</param>
		public HexMouseHoverEventArgs(HexView view, HexBufferLine line, int textPosition) {
			if (textPosition < 0)
				throw new ArgumentOutOfRangeException(nameof(textPosition));
			View = view ?? throw new ArgumentNullException(nameof(view));
			Line = line ?? throw new ArgumentNullException(nameof(line));
			TextPosition = textPosition;
		}
	}
}
