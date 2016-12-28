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
using System.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// 7-bit encoded string (UTF-8)
	/// </summary>
	public sealed class Bit7EncodedStringData : StructureData {
		const string NAME = "Bit7EncString";

		/// <summary>
		/// Gets the length
		/// </summary>
		public StructField<Bit7EncodedInt32Data> Length { get; }

		/// <summary>
		/// Gets the string data
		/// </summary>
		public StructField<StringData> String { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="lengthSpan">Span of length</param>
		/// <param name="stringSpan">Span of string data</param>
		/// <param name="encoding">Encoding</param>
		public Bit7EncodedStringData(HexBuffer buffer, HexSpan lengthSpan, HexSpan stringSpan, Encoding encoding)
			: base(NAME, new HexBufferSpan(buffer, HexSpan.FromBounds(lengthSpan.Start, stringSpan.End))) {
			if (lengthSpan.End != stringSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			Length = new StructField<Bit7EncodedInt32Data>("Length", new Bit7EncodedInt32Data(new HexBufferSpan(buffer, lengthSpan)));
			String = new StructField<StringData>("String", new StringData(new HexBufferSpan(buffer, stringSpan), encoding));
			Fields = new BufferField[] {
				Length,
				String,
			};
		}
	}
}
