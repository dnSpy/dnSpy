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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetEmbeddedResourceImpl : DotNetEmbeddedResource {
		public override StructField<UInt32Data> Size { get; }
		public override StructField<VirtualArrayData<ByteData>> Content { get; }

		protected override BufferField[] Fields { get; }

		public DotNetEmbeddedResourceImpl(HexBufferSpan span, uint token)
			: base(span, token) {
			if (span.Length < 4)
				throw new ArgumentOutOfRangeException(nameof(span));
			Size = new StructField<UInt32Data>("Size", new UInt32Data(span.Buffer, span.Start.Position));
			Content = new StructField<VirtualArrayData<ByteData>>("Content", ArrayData.CreateVirtualByteArray(HexBufferSpan.FromBounds(span.Start + 4, span.End)));
			Fields = new BufferField[] {
				Size,
				Content,
			};
		}
	}
}
