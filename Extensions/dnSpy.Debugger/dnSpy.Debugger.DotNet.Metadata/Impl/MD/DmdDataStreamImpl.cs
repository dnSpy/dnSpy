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

using dnlib.IO;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdDataStreamImpl : DmdDataStream {
		DataReader reader;
		public DmdDataStreamImpl(ref DataReader reader) => this.reader = reader;
		public override byte ReadByte() => reader.ReadByte();
		public override ushort ReadUInt16() => reader.ReadUInt16();
		public override uint ReadUInt32() => reader.ReadUInt32();
		public override ulong ReadUInt64() => reader.ReadUInt64();
		public override float ReadSingle() => reader.ReadSingle();
		public override double ReadDouble() => reader.ReadDouble();
		public override byte[] ReadBytes(int length) => reader.ReadBytes(length);
		public override long Position {
			get => reader.Position;
			set => reader.Position = (uint)value;
		}
		public override long Length => reader.Length;
		public override void Dispose() { }
	}
}
