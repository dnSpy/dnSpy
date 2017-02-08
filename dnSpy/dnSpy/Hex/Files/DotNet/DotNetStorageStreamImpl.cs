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

using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetStorageStreamImpl : DotNetStorageStream {
		public override StructField<UInt32Data> Offset { get; }
		public override StructField<UInt32Data> Size { get; }
		public override StructField<StringData> StreamName { get; }

		protected override BufferField[] Fields { get; }

		public DotNetStorageStreamImpl(HexBufferSpan span)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Offset = new StructField<UInt32Data>("iOffset", new UInt32Data(buffer, pos));
			Size = new StructField<UInt32Data>("iSize", new UInt32Data(buffer, pos + 4));
			StreamName = new StructField<StringData>("rcName", new StringData(buffer, pos + 8, (int)(span.Length.ToUInt64() - 8), Encoding.ASCII));
			Fields = new BufferField[] {
				Offset,
				Size,
				StreamName,
			};
		}
	}
}
