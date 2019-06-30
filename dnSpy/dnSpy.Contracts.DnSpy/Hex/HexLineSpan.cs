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
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex line span
	/// </summary>
	public readonly struct HexLineSpan {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => BufferSpan.IsDefault;

		/// <summary>
		/// Buffer span
		/// </summary>
		public HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Selection flags or null
		/// </summary>
		public HexSpanSelectionFlags? SelectionFlags { get; }

		/// <summary>
		/// Line span or null
		/// </summary>
		public VST.Span? TextSpan { get; }

		/// <summary>
		/// true if it's a text span
		/// </summary>
		public bool IsTextSpan => !(TextSpan is null);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="flags">Flags</param>
		public HexLineSpan(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags) {
			if (bufferSpan.IsDefault)
				throw new ArgumentException();
			BufferSpan = bufferSpan;
			SelectionFlags = flags;
			TextSpan = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="textSpan">Text span</param>
		public HexLineSpan(HexBufferLine line, VST.Span textSpan) {
			if (line is null)
				throw new ArgumentNullException(nameof(line));
			BufferSpan = line.BufferSpan;
			SelectionFlags = null;
			TextSpan = textSpan;
		}
	}
}
