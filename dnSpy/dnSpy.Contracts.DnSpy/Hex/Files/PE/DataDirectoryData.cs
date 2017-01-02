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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// Data directory
	/// </summary>
	public class DataDirectoryData : StructureData {
		const string NAME = "IMAGE_DATA_DIRECTORY";

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public DataDirectoryData(HexBufferSpan span)
			: base(NAME, span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
			var buffer = span.Buffer;
			var pos = span.Start;
			VirtualAddress = new StructField<RvaData>("VirtualAddress", new RvaData(buffer, pos));
			Size = new StructField<UInt32Data>("Size", new UInt32Data(buffer, pos + 4));
			Fields = new BufferField[] {
				VirtualAddress,
				Size,
			};
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public DataDirectoryData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8))) {
		}

		/// <summary>IMAGE_DATA_DIRECTORY.VirtualAddress</summary>
		public StructField<RvaData> VirtualAddress { get; }
		/// <summary>IMAGE_DATA_DIRECTORY.Size</summary>
		public StructField<UInt32Data> Size { get; }
	}
}
