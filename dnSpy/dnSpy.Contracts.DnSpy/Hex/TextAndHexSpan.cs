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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Text span and hex span
	/// </summary>
	public struct TextAndHexSpan {
		/// <summary>
		/// Gets the text span
		/// </summary>
		public Span TextSpan { get; }

		/// <summary>
		/// Gets the buffer span
		/// </summary>
		public HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="textSpan">Text span</param>
		/// <param name="bufferSpan">Buffer span</param>
		public TextAndHexSpan(Span textSpan, HexBufferSpan bufferSpan) {
			TextSpan = textSpan;
			BufferSpan = bufferSpan;
		}
	}
}
