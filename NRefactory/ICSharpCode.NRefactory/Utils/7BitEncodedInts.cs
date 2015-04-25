// Copyright (c) 2011 Daniel Grunwald
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// A binary reader that can read the output of BinaryWriterWith7BitEncodedInts.
	/// </summary>
	public sealed class BinaryReaderWith7BitEncodedInts : BinaryReader
	{
		public BinaryReaderWith7BitEncodedInts(Stream stream) : base(stream)
		{
		}
		
		public override short ReadInt16()
		{
			return unchecked((short)(ushort)base.Read7BitEncodedInt());
		}
		
		[CLSCompliant(false)]
		public override ushort ReadUInt16()
		{
			return unchecked((ushort)base.Read7BitEncodedInt());
		}
		
		public override int ReadInt32()
		{
			return base.Read7BitEncodedInt();
		}
		
		[CLSCompliant(false)]
		public override uint ReadUInt32()
		{
			return unchecked((uint)base.Read7BitEncodedInt());
		}
		
		public override long ReadInt64()
		{
			return unchecked((long)this.ReadUInt64());
		}
		
		[CLSCompliant(false)]
		public override ulong ReadUInt64()
		{
			ulong num = 0;
			int shift = 0;
			while (shift < 64) {
				byte b = this.ReadByte();
				num |= (ulong)(b & 127) << shift;
				shift += 7;
				if ((b & 128) == 0) {
					return num;
				}
			}
			throw new FormatException("Invalid 7-bit int64");
		}
	}

	/// <summary>
	/// A binary writer that encodes all integers as 7-bit-encoded-ints.
	/// </summary>
	public sealed class BinaryWriterWith7BitEncodedInts : BinaryWriter
	{
		public BinaryWriterWith7BitEncodedInts(Stream stream) : base(stream)
		{
		}
		
		public override void Write(short value)
		{
			base.Write7BitEncodedInt(unchecked((ushort)value));
		}
		
		[CLSCompliant(false)]
		public override void Write(ushort value)
		{
			base.Write7BitEncodedInt(value);
		}
		
		public override void Write(int value)
		{
			base.Write7BitEncodedInt(value);
		}
		
		[CLSCompliant(false)]
		public override void Write(uint value)
		{
			base.Write7BitEncodedInt(unchecked((int)value));
		}
		
		public override void Write(long value)
		{
			this.Write(unchecked((ulong)value));
		}
		
		[CLSCompliant(false)]
		public override void Write(ulong value)
		{
			while (value >= 128) {
				this.Write(unchecked((byte)(value | 128u)));
				value >>= 7;
			}
			this.Write(unchecked((byte)value));
		}
	}
}
