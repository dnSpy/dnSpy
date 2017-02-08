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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;

namespace dnSpy.Debugger.Memory {
	abstract class SimpleProcessReader {
		public abstract void Read(IntPtr hProcess, HexPosition position, byte[] destination, long destinationIndex, long length);
	}

	//[Export(typeof(SimpleProcessReader))]
	sealed class SimpleProcessReaderImpl : SimpleProcessReader {
		readonly HexBufferStreamFactoryService hexBufferStreamFactoryService;

		[ImportingConstructor]
		SimpleProcessReaderImpl(HexBufferStreamFactoryService hexBufferStreamFactoryService) {
			this.hexBufferStreamFactoryService = hexBufferStreamFactoryService;
		}

		public override void Read(IntPtr hProcess, HexPosition position, byte[] destination, long destinationIndex, long length) {
			if (hProcess == IntPtr.Zero)
				throw new ArgumentException();
			if (position >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			if (destinationIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(destinationIndex));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (new HexPosition(destinationIndex) + length > destination.LongLength)
				throw new ArgumentOutOfRangeException(nameof(length));
			HexSimpleBufferStream processStream = null;
			HexBufferStream cachedStream = null;
			try {
				processStream = hexBufferStreamFactoryService.CreateSimpleProcessStream(hProcess);
				cachedStream = hexBufferStreamFactoryService.CreateCached(processStream, disposeStream: false);
				cachedStream.ReadBytes(position, destination, destinationIndex, length);
			}
			finally {
				processStream?.Dispose();
				cachedStream?.Dispose();
			}
		}
	}
}
