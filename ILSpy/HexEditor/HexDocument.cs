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
using System.Diagnostics;
using System.IO;

namespace dnSpy.HexEditor {
	[DebuggerDisplay("{Size} {Name}")]
	public class HexDocument : IDisposable, IHexStream {
		readonly IHexStream stream;

		public event EventHandler<HexDocumentModifiedEventArgs> OnDocumentModified;

		public string Name { get; set; }

		public ulong StartOffset {
			get { return stream.StartOffset; }
		}

		public ulong EndOffset {
			get { return stream.EndOffset; }
		}

		public ulong Size {
			get { return stream.Size; }
		}

		public HexDocument(string filename)
			: this(new ByteArrayHexStream(File.ReadAllBytes(filename)), filename) {
		}

		public HexDocument(byte[] data, string name)
			: this(new ByteArrayHexStream(data), name) {
		}

		public HexDocument(IHexStream stream, string name) {
			if (stream == null)
				throw new ArgumentNullException("stream");
			this.stream = stream;
			this.Name = name;
		}

		public int ReadByte(ulong offs) {
			return stream.ReadByte(offs);
		}

		public void Read(ulong offset, byte[] array, long index, int count) {
			stream.Read(offset, array, index, count);
		}

		public void Write(ulong offset, byte b) {
			stream.Write(offset, b);

			var h = OnDocumentModified;
			if (h != null)
				h(this, new HexDocumentModifiedEventArgs(offset, offset));
		}

		public void Write(ulong offset, byte[] array, long index, int count) {
			if (count <= 0)
				return;
			stream.Write(offset, array, index, count);

			var h = OnDocumentModified;
			if (h != null)
				h(this, new HexDocumentModifiedEventArgs(offset, NumberUtils.AddUInt64(offset, (ulong)count - 1)));
		}

		public void Dispose() {
			var id = stream as IDisposable;
			if (id != null)
				id.Dispose();
		}
	}
}
