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
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex buffer
	/// </summary>
	public abstract class HexBuffer : IPropertyOwner {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBuffer() {
			Properties = new PropertyCollection();
		}

		/// <summary>
		/// Gets all properties
		/// </summary>
		public PropertyCollection Properties { get; }

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
		/// Gets information about a position in the buffer
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexSpanInfo GetHexSpanInfo(HexPosition position);

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
}
