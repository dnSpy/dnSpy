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

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// Creates a sequence of text and adornment elements
	/// </summary>
	public abstract class HexAndAdornmentSequencer {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexAndAdornmentSequencer() { }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// Raised after a sequence has changed
		/// </summary>
		public abstract event EventHandler<HexAndAdornmentSequenceChangedEventArgs> SequenceChanged;

		/// <summary>
		/// Creates a <see cref="HexAndAdornmentCollection"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexAndAdornmentCollection CreateHexAndAdornmentCollection(HexBufferPoint position);

		/// <summary>
		/// Creates a <see cref="HexAndAdornmentCollection"/>
		/// </summary>
		/// <param name="line">Line</param>
		/// <returns></returns>
		public abstract HexAndAdornmentCollection CreateHexAndAdornmentCollection(HexBufferLine line);
	}

	/// <summary>
	/// Event args
	/// </summary>
	public sealed class HexAndAdornmentSequenceChangedEventArgs : EventArgs {
		/// <summary>
		/// Gets the span
		/// </summary>
		public HexBufferSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public HexAndAdornmentSequenceChangedEventArgs(HexBufferSpan span) {
			if (span.IsDefault)
				throw new ArgumentException();
			Span = span;
		}
	}
}
