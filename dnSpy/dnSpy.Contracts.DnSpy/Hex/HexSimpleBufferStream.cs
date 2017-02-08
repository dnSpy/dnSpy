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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// A buffer stream with less methods
	/// </summary>
	public abstract class HexSimpleBufferStream : IDisposable {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSimpleBufferStream() { }

		/// <summary>
		/// true if the content can change at any time
		/// </summary>
		public abstract bool IsVolatile { get; }

		/// <summary>
		/// true if it's a read-only stream
		/// </summary>
		public abstract bool IsReadOnly { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public abstract HexSpan Span { get; }

		/// <summary>
		/// Gets the name. This could be the filename if the data was read from a file
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the page size of the underlying data store or 0 if it's unknown. Eg. if it's
		/// memory in some process, <see cref="Environment.SystemPageSize"/> can be returned
		/// here. The returned value must be 0 or a power of 2.
		/// </summary>
		public virtual ulong PageSize => 0;

		/// <summary>
		/// Gets information about a position in the stream
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexSpanInfo GetSpanInfo(HexPosition position);

		/// <summary>
		/// Gets information about a position in the stream
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="validSpan">Span of all valid data</param>
		/// <returns></returns>
		protected HexSpanInfo GetSpanInfo(HexPosition position, HexSpan validSpan) {
			if (position >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (position >= validSpan.End)
				return new HexSpanInfo(HexSpan.FromBounds(validSpan.End, HexPosition.MaxEndPosition), HexSpanInfoFlags.None);
			else if (position < validSpan.Start)
				return new HexSpanInfo(HexSpan.FromBounds(HexPosition.Zero, validSpan.Start), HexSpanInfoFlags.None);
			else
				return new HexSpanInfo(validSpan, HexSpanInfoFlags.HasData);
		}

		/// <summary>
		/// Reads bytes. Returns number of bytes read.
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="destination">Destination array</param>
		/// <param name="destinationIndex">Index</param>
		/// <param name="length">Length</param>
		/// <returns></returns>
		public abstract HexPosition Read(HexPosition position, byte[] destination, long destinationIndex, long length);

		/// <summary>
		/// Writes bytes. Returns number of bytes written.
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="source">Data</param>
		/// <param name="sourceIndex">Index</param>
		/// <param name="length">Length</param>
		/// <returns></returns>
		public abstract HexPosition Write(HexPosition position, byte[] source, long sourceIndex, long length);

		/// <summary>
		/// Clears the cache if it uses a cache
		/// </summary>
		public virtual void ClearCache() { }

		/// <summary>
		/// Raised after it is disposed
		/// </summary>
		public event EventHandler Disposed;

		/// <summary>
		/// true if the instance has been disposed
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public void Dispose() {
			if (IsDisposed)
				return;
			IsDisposed = true;
			DisposeCore();
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Disposes this instance
		/// </summary>
		protected virtual void DisposeCore() { }
	}
}
