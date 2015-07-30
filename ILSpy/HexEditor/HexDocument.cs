/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.IO;

namespace dnSpy.HexEditor {
	public sealed class HexDocument : IDisposable {
		readonly IHexStream stream;

		public ulong Size {
			get { return stream.Size; }
		}

		public HexDocument(string filename)
			: this(new ByteArrayHexStream(File.ReadAllBytes(filename))) {
		}

		public HexDocument(byte[] data)
			: this(new ByteArrayHexStream(data)) {
		}

		public HexDocument(IHexStream stream) {
			if (stream == null)
				throw new ArgumentNullException("stream");
			this.stream = stream;
		}

		public int ReadByte(ulong offs) {
			return stream.ReadByte(offs);
		}

		public void Dispose() {
			var id = stream as IDisposable;
			if (id != null)
				id.Dispose();
		}
	}
}
