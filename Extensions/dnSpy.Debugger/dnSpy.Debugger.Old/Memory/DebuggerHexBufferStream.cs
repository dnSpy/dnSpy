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
using dnSpy.Contracts.Hex;

namespace dnSpy.Debugger.Memory {
	sealed class DebuggerHexBufferStream : HexBufferStream {
		HexBufferStream stream;

		public override bool IsVolatile {
			get {
				CheckDisposed();
				// Since the underlying stream can be changed, we don't care whether the underlying
				// stream is volatile (it is, though), so always return true here.
				return true;
			}
		}

		public override bool IsReadOnly {
			get {
				CheckDisposed();
				return stream?.IsReadOnly ?? true;
			}
		}

		public override HexSpan Span {
			get {
				CheckDisposed();
				return stream?.Span ?? new HexSpan(0, 0);
			}
		}

		public override string Name {
			get {
				CheckDisposed();
				return stream?.Name ?? string.Empty;
			}
		}

		public override event EventHandler<HexBufferStreamSpanInvalidatedEventArgs> BufferStreamSpanInvalidated;

		public override void ClearCache() {
			CheckDisposed();
			stream?.ClearCache();
		}

		public void SetUnderlyingStream(HexBufferStream newStream) {
			CheckDisposed();
			SetUnderlyingStreamCore(newStream);
		}

		void SetUnderlyingStreamCore(HexBufferStream newStream) {
			if (stream == newStream)
				return;
			UnregisterEvents();
			stream?.Dispose();
			stream = newStream;
			RegisterEvents();
			InvalidateAll();
			UnderlyingStreamChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler UnderlyingStreamChanged;
		public void InvalidateAll() => BufferStreamSpanInvalidated?.Invoke(this, new HexBufferStreamSpanInvalidatedEventArgs(HexSpan.FromBounds(HexPosition.Zero, HexPosition.MaxEndPosition)));

		void RegisterEvents() {
			if (stream == null)
				return;
			stream.BufferStreamSpanInvalidated += Stream_BufferStreamSpanInvalidated;
		}

		void UnregisterEvents() {
			if (stream == null)
				return;
			stream.BufferStreamSpanInvalidated -= Stream_BufferStreamSpanInvalidated;
		}

		void Stream_BufferStreamSpanInvalidated(object sender, HexBufferStreamSpanInvalidatedEventArgs e) {
			if (IsDisposed)
				return;
			if (stream != sender)
				return;
		}

		void CheckDisposed() {
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(DebuggerHexBufferStream));
		}

		public override HexSpanInfo GetSpanInfo(HexPosition position) {
			CheckDisposed();
			if (position >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			return stream?.GetSpanInfo(position) ??
				new HexSpanInfo(HexSpan.FromBounds(HexPosition.Zero, HexPosition.MaxEndPosition), HexSpanInfoFlags.None);
		}

		public override int TryReadByte(HexPosition position) {
			CheckDisposed();
			return stream?.TryReadByte(position) ?? -1;
		}

		public override byte ReadByte(HexPosition position) {
			CheckDisposed();
			return stream?.ReadByte(position) ?? 0;
		}

		public override sbyte ReadSByte(HexPosition position) {
			CheckDisposed();
			return stream?.ReadSByte(position) ?? 0;
		}

		public override short ReadInt16(HexPosition position) {
			CheckDisposed();
			return stream?.ReadInt16(position) ?? 0;
		}

		public override ushort ReadUInt16(HexPosition position) {
			CheckDisposed();
			return stream?.ReadUInt16(position) ?? 0;
		}

		public override int ReadInt32(HexPosition position) {
			CheckDisposed();
			return stream?.ReadInt32(position) ?? 0;
		}

		public override uint ReadUInt32(HexPosition position) {
			CheckDisposed();
			return stream?.ReadUInt32(position) ?? 0;
		}

		public override long ReadInt64(HexPosition position) {
			CheckDisposed();
			return stream?.ReadInt64(position) ?? 0;
		}

		public override ulong ReadUInt64(HexPosition position) {
			CheckDisposed();
			return stream?.ReadUInt64(position) ?? 0;
		}

		public override float ReadSingle(HexPosition position) {
			CheckDisposed();
			return stream?.ReadSingle(position) ?? 0;
		}

		public override double ReadDouble(HexPosition position) {
			CheckDisposed();
			return stream?.ReadDouble(position) ?? 0;
		}

		public override short ReadInt16BigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadInt16BigEndian(position) ?? 0;
		}

		public override ushort ReadUInt16BigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadUInt16BigEndian(position) ?? 0;
		}

		public override int ReadInt32BigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadInt32BigEndian(position) ?? 0;
		}

		public override uint ReadUInt32BigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadUInt32BigEndian(position) ?? 0;
		}

		public override long ReadInt64BigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadInt64BigEndian(position) ?? 0;
		}

		public override ulong ReadUInt64BigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadUInt64BigEndian(position) ?? 0;
		}

		public override float ReadSingleBigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadSingleBigEndian(position) ?? 0;
		}

		public override double ReadDoubleBigEndian(HexPosition position) {
			CheckDisposed();
			return stream?.ReadDoubleBigEndian(position) ?? 0;
		}

		public override byte[] ReadBytes(HexPosition position, long length) {
			CheckDisposed();
			if (stream != null)
				return stream.ReadBytes(position, length);
			return new byte[length];
		}

		public override void ReadBytes(HexPosition position, byte[] destination, long destinationIndex, long length) {
			CheckDisposed();
			stream?.ReadBytes(position, destination, destinationIndex, length);
		}

		public override HexBytes ReadHexBytes(HexPosition position, long length) {
			CheckDisposed();
			return stream?.ReadHexBytes(position, length) ?? new HexBytes(new byte[length], false);
		}

		public override void Write(HexPosition position, byte[] source, long sourceIndex, long length) {
			CheckDisposed();
			stream?.Write(position, source, sourceIndex, length);
		}

		protected override void DisposeCore() => SetUnderlyingStreamCore(null);
	}
}
