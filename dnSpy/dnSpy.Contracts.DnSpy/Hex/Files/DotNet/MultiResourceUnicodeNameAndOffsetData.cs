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
using System.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Multi-file .NET resource name and data offset
	/// </summary>
	public sealed class MultiResourceUnicodeNameAndOffsetData : StructureData {
		const string NAME = "MultiResourceNameData";

		/// <summary>
		/// Gets the owner <see cref="DotNetMultiFileResources"/> instance
		/// </summary>
		public DotNetMultiFileResources ResourceProvider { get; }

		/// <summary>Gets the resource name</summary>
		public StructField<Bit7EncodedStringData> ResourceName { get; }
		/// <summary>Gets the data offset</summary>
		public StructField<UInt32Data> DataOffset { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resourceProvider">Owner</param>
		/// <param name="buffer">Buffer</param>
		/// <param name="lengthSpan">Span of 7-bit encoded length</param>
		/// <param name="stringSpan">Span of string data</param>
		public MultiResourceUnicodeNameAndOffsetData(DotNetMultiFileResources resourceProvider, HexBuffer buffer, HexSpan lengthSpan, HexSpan stringSpan)
			: base(NAME, new HexBufferSpan(buffer, HexSpan.FromBounds(lengthSpan.Start, stringSpan.End + 4))) {
			if (resourceProvider == null)
				throw new ArgumentNullException(nameof(resourceProvider));
			ResourceProvider = resourceProvider;
			ResourceName = new StructField<Bit7EncodedStringData>("ResourceName", new Bit7EncodedStringData(buffer, lengthSpan, stringSpan, Encoding.Unicode));
			DataOffset = new StructField<UInt32Data>("DataOffset", new UInt32Data(buffer, stringSpan.End));
			Fields = new BufferField[] {
				ResourceName,
				DataOffset,
			};
		}
	}
}
