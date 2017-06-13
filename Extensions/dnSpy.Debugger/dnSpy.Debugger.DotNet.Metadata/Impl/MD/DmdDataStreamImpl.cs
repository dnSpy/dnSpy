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

using dnlib.IO;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdDataStreamImpl : DmdDataStream {
		readonly IImageStream stream;
		public DmdDataStreamImpl(IImageStream stream) => this.stream = stream;
		public override byte ReadByte() => stream.ReadByte();
		public override ushort ReadUInt16() => stream.ReadUInt16();
		public override uint ReadUInt32() => stream.ReadUInt32();
		public override ulong ReadUInt64() => stream.ReadUInt64();
		public override float ReadSingle() => stream.ReadSingle();
		public override double ReadDouble() => stream.ReadDouble();
		public override byte[] ReadBytes(int length) => stream.ReadBytes(length);
		public override long Position {
			get => stream.Position;
			set => stream.Position = value;
		}
		public override long Length => stream.Length;
		public override void Dispose() => stream.Dispose();
	}
}
