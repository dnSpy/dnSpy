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

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Hex tooltip structure tag, the tooltip is shown when hovering over a value in the hex view.
	/// </summary>
	public sealed class HexToolTipStructureSpanTag : HexTag {
		/// <summary>
		/// Span of data
		/// </summary>
		public HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Tooltip to show or null
		/// </summary>
		public object ToolTip { get; }

		/// <summary>
		/// A reference to some high level object that represents the data or null
		/// </summary>
		public object Reference { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferSpan">Span of field</param>
		public HexToolTipStructureSpanTag(HexBufferSpan bufferSpan)
			: this(bufferSpan, null, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferSpan">Span of field</param>
		/// <param name="toolTip">Tooltip to show or null</param>
		/// <param name="reference">A reference to some high level object that represents the data or null</param>
		public HexToolTipStructureSpanTag(HexBufferSpan bufferSpan, object toolTip, object reference) {
			if (bufferSpan.IsDefault)
				throw new ArgumentException();
			BufferSpan = bufferSpan;
			ToolTip = toolTip;
			Reference = reference;
		}
	}
}
