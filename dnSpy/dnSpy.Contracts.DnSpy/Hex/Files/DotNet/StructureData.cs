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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// A <see cref="Guid"/>
	/// </summary>
	public class GuidData : StructureData {
		const string NAME = "Guid";

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>Guid.A</summary>
		public StructField<UInt32Data> A { get; }
		/// <summary>Guid.B</summary>
		public StructField<UInt16Data> B { get; }
		/// <summary>Guid.C</summary>
		public StructField<UInt16Data> C { get; }
		/// <summary>Guid.D</summary>
		public StructField<ByteData> D { get; }
		/// <summary>Guid.E</summary>
		public StructField<ByteData> E { get; }
		/// <summary>Guid.F</summary>
		public StructField<ByteData> F { get; }
		/// <summary>Guid.G</summary>
		public StructField<ByteData> G { get; }
		/// <summary>Guid.H</summary>
		public StructField<ByteData> H { get; }
		/// <summary>Guid.I</summary>
		public StructField<ByteData> I { get; }
		/// <summary>Guid.J</summary>
		public StructField<ByteData> J { get; }
		/// <summary>Guid.K</summary>
		public StructField<ByteData> K { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public GuidData(HexBufferSpan span)
			: base(NAME, span) {
			if (span.Length != 16)
				throw new ArgumentOutOfRangeException(nameof(span));
			var buffer = span.Buffer;
			var pos = span.Span.Start;
			A = new StructField<UInt32Data>("A", new UInt32Data(buffer, pos));
			B = new StructField<UInt16Data>("B", new UInt16Data(buffer, pos + 4));
			C = new StructField<UInt16Data>("C", new UInt16Data(buffer, pos + 6));
			D = new StructField<ByteData>("D", new ByteData(buffer, pos + 8));
			E = new StructField<ByteData>("E", new ByteData(buffer, pos + 9));
			F = new StructField<ByteData>("F", new ByteData(buffer, pos + 0x0A));
			G = new StructField<ByteData>("G", new ByteData(buffer, pos + 0x0B));
			H = new StructField<ByteData>("H", new ByteData(buffer, pos + 0x0C));
			I = new StructField<ByteData>("I", new ByteData(buffer, pos + 0x0D));
			J = new StructField<ByteData>("J", new ByteData(buffer, pos + 0x0E));
			K = new StructField<ByteData>("K", new ByteData(buffer, pos + 0x0F));
			Fields = new BufferField[] {
				A,
				B,
				C,
				D,
				E,
				F,
				G,
				H,
				I,
				J,
				K,
			};
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public GuidData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 16))) {
		}
	}
}
