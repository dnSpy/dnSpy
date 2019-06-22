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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Buffer span and selection flags
	/// </summary>
	public readonly struct HexBufferSpanSelection : IEquatable<HexBufferSpanSelection> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => BufferSpan.IsDefault;

		/// <summary>
		/// Buffer span
		/// </summary>
		public HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Selection flags
		/// </summary>
		public HexSpanSelectionFlags SelectionFlags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="flags">Flags</param>
		public HexBufferSpanSelection(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags) {
			if (bufferSpan.IsDefault)
				throw new ArgumentException();
			BufferSpan = bufferSpan;
			SelectionFlags = flags;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexBufferSpanSelection other) => BufferSpan == other.BufferSpan && SelectionFlags == other.SelectionFlags;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is HexBufferSpanSelection && Equals((HexBufferSpanSelection)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => BufferSpan.GetHashCode() ^ (int)SelectionFlags;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => BufferSpan.ToString() + ": " + SelectionFlags.ToString();
	}
}
