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

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	unsafe abstract class MDWriterStream {
		public abstract long Position { get; set; }
		public abstract long Length { get; set; }
		public abstract void Write(byte value);
		public void Write(sbyte value) => Write((byte)value);
		public abstract void Write(ushort value);
		public void Write(short value) => Write((ushort)value);
		public abstract void Write(uint value);
		public void Write(int value) => Write((uint)value);
		public abstract void Write(ulong value);
		public void Write(long value) => Write((ulong)value);
		public abstract void Write(byte* source, int length);
		public abstract void Write(byte[] source, int sourceIndex, int length);
		public void Write(byte[] data) => Write(data, 0, data.Length);
	}
}
