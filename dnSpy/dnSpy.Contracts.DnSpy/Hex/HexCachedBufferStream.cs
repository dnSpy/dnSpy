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
	/// A cached buffer stream
	/// </summary>
	public abstract class HexCachedBufferStream : HexBufferStream {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexCachedBufferStream() { }

		/// <summary>
		/// Invalidates all memory
		/// </summary>
		public void InvalidateAll() => Invalidate(Span);

		/// <summary>
		/// Invalidates a region of memory
		/// </summary>
		/// <param name="span">Span</param>
		public abstract void Invalidate(HexSpan span);
	}
}
