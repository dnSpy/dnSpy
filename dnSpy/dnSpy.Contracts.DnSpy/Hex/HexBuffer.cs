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
using System.Collections.Generic;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex buffer
	/// </summary>
	public abstract class HexBuffer : VSUTIL.IPropertyOwner {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBuffer() {
			Properties = new VSUTIL.PropertyCollection();
		}

		/// <summary>
		/// Gets all properties
		/// </summary>
		public VSUTIL.PropertyCollection Properties { get; }

		/// <summary>
		/// true if the content can change at any time
		/// </summary>
		public abstract bool IsVolatile { get; }

		/// <summary>
		/// true if the buffer is read-only
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
		/// Raised when a span of data got modified by other code
		/// </summary>
		public abstract event EventHandler<HexBufferSpanInvalidatedEventArgs> BufferSpanInvalidated;

		/// <summary>
		/// Gets the version
		/// </summary>
		public abstract HexVersion Version { get; }

		/// <summary>
		/// true if an edit is in progress
		/// </summary>
		public abstract bool EditInProgress { get; }

		/// <summary>
		/// Returns true if the current thread is allowed to modify the buffer
		/// </summary>
		/// <returns></returns>
		public abstract bool CheckEditAccess();

		/// <summary>
		/// Claims ownership of this buffer for the current thread
		/// </summary>
		public abstract void TakeThreadOwnership();

		/// <summary>
		/// Gets information about a position in the buffer. The returned info isn't
		/// normalized, there may be consecutive spans with the same flags. It's the
		/// responsibility of the caller to merge such spans.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexSpanInfo GetSpanInfo(HexPosition position);

		/// <summary>
		/// Gets the next valid span or null if there's none left. This includes the input
		/// (<paramref name="position"/>) if it happens to lie within this valid span.
		/// This method merges all consecutive valid spans.
		/// </summary>
		/// <param name="position">Start position to check</param>
		/// <returns></returns>
		public HexSpan? GetNextValidSpan(HexPosition position) => GetNextValidSpan(position, HexPosition.MaxEndPosition);

		/// <summary>
		/// Gets the next valid span or null if there's none left. This includes the input
		/// (<paramref name="position"/>) if it happens to lie within this valid span.
		/// This method merges all consecutive valid spans.
		/// </summary>
		/// <param name="position">Start position to check</param>
		/// <param name="endPosition">End position</param>
		/// <returns></returns>
		public HexSpan? GetNextValidSpan(HexPosition position, HexPosition endPosition) {
			if (position >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (endPosition > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(endPosition));
			while (position < endPosition) {
				var info = GetSpanInfo(position);
				if (info.HasData) {
					var start = info.Span.Start;
					var end = info.Span.End;
					// We use MaxEndPosition and not endPosition here since we must merge
					// all consecutive spans even if some of them happen to be outside the
					// requested range.
					while (end < HexPosition.MaxEndPosition) {
						info = GetSpanInfo(end);
						if (!info.HasData)
							break;
						end = info.Span.End;
					}
					return HexSpan.FromBounds(start, end);
				}
				position = info.Span.End;
			}
			return null;
		}

		/// <summary>
		/// Gets all valid spans. This could be empty if it's a 0-byte buffer stream.
		/// This method merges all consecutive valid spans.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<HexSpan> GetValidSpans() => GetValidSpans(HexSpan.FullSpan);

		/// <summary>
		/// Gets all valid spans overlapping <paramref name="span"/>. This method merges all
		/// consecutive valid spans.
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public IEnumerable<HexSpan> GetValidSpans(HexSpan span) {
			var pos = span.Start;
			for (;;) {
				var info = GetNextValidSpan(pos, span.End);
				if (info == null)
					break;
				yield return info.Value;
				pos = info.Value.End;
			}
		}

		/// <summary>
		/// Creates a <see cref="HexEdit"/> object
		/// </summary>
		/// <returns></returns>
		public abstract HexEdit CreateEdit();

		/// <summary>
		/// Creates a <see cref="HexEdit"/> object
		/// </summary>
		/// <param name="reiteratedVersionNumber">Use by undo/redo to restore a previous version</param>
		/// <param name="editTag">Edit tag, can be anything</param>
		/// <returns></returns>
		public abstract HexEdit CreateEdit(int? reiteratedVersionNumber, object editTag);

		/// <summary>
		/// Replaces the <see cref="byte"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, byte value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="sbyte"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, sbyte value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="short"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, short value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="ushort"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, ushort value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="int"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, int value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="uint"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, uint value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="long"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, long value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="ulong"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, ulong value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="float"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, float value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the <see cref="double"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		public void Replace(HexPosition position, double value) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, value);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the data at <paramref name="position"/> with <paramref name="data"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="data">New data</param>
		public void Replace(HexPosition position, byte[] data) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, data);
				ed.Apply();
			}
		}

		/// <summary>
		/// Replaces the data at <paramref name="position"/> with <paramref name="data"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="data">New data</param>
		/// <param name="index">Index</param>
		/// <param name="length">Length</param>
		public void Replace(HexPosition position, byte[] data, long index, long length) {
			using (var ed = CreateEdit()) {
				ed.Replace(position, data, index, length);
				ed.Apply();
			}
		}

		/// <summary>
		/// Tries to read a <see cref="byte"/>. If there's no data, a value less than 0 is returned.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract int TryReadByte(HexPosition position);

		/// <summary>
		/// Reads a <see cref="byte"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract byte ReadByte(HexPosition position);

		/// <summary>
		/// Reads a <see cref="sbyte"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract sbyte ReadSByte(HexPosition position);

		/// <summary>
		/// Reads a <see cref="short"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract short ReadInt16(HexPosition position);

		/// <summary>
		/// Reads a <see cref="ushort"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract ushort ReadUInt16(HexPosition position);

		/// <summary>
		/// Reads a <see cref="int"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract int ReadInt32(HexPosition position);

		/// <summary>
		/// Reads a <see cref="uint"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract uint ReadUInt32(HexPosition position);

		/// <summary>
		/// Reads a <see cref="long"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract long ReadInt64(HexPosition position);

		/// <summary>
		/// Reads a <see cref="ulong"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract ulong ReadUInt64(HexPosition position);

		/// <summary>
		/// Reads a <see cref="float"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract float ReadSingle(HexPosition position);

		/// <summary>
		/// Reads a <see cref="double"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract double ReadDouble(HexPosition position);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="length">Number of bytes to read</param>
		/// <returns></returns>
		public abstract byte[] ReadBytes(HexPosition position, long length);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="length">Number of bytes to read</param>
		/// <returns></returns>
		public abstract byte[] ReadBytes(HexPosition position, ulong length);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public abstract byte[] ReadBytes(HexSpan span);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="destination">Destination array</param>
		/// <param name="destinationIndex">Index</param>
		/// <param name="length">Length</param>
		public abstract void ReadBytes(HexPosition position, byte[] destination, long destinationIndex, long length);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="length">Length</param>
		/// <returns></returns>
		public abstract HexBytes ReadHexBytes(HexPosition position, long length);

		/// <summary>
		/// Raised before the text buffer gets changed
		/// </summary>
		public abstract event EventHandler<HexContentChangingEventArgs> Changing;

		/// <summary>
		/// Raised when the buffer has changed
		/// </summary>
		public abstract event EventHandler<HexContentChangedEventArgs> ChangedHighPriority;

		/// <summary>
		/// Raised when the buffer has changed
		/// </summary>
		public abstract event EventHandler<HexContentChangedEventArgs> Changed;

		/// <summary>
		/// Raised when the buffer has changed
		/// </summary>
		public abstract event EventHandler<HexContentChangedEventArgs> ChangedLowPriority;

		/// <summary>
		/// Raised after an edit operation has completed or after it has been canceled
		/// </summary>
		public abstract event EventHandler PostChanged;
	}

	/// <summary>
	/// Invalidated span event args
	/// </summary>
	public sealed class HexBufferSpanInvalidatedEventArgs : EventArgs {
		/// <summary>
		/// Gets the span
		/// </summary>
		public HexSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public HexBufferSpanInvalidatedEventArgs(HexSpan span) {
			Span = span;
		}
	}
}
