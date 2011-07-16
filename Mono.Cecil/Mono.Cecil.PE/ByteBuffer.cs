//
// ByteBuffer.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Mono.Cecil.PE {

	class ByteBuffer {

		internal byte [] buffer;
		internal int length;
		internal int position;

		public ByteBuffer ()
		{
			this.buffer = Empty<byte>.Array;
		}

		public ByteBuffer (int length)
		{
			this.buffer = new byte [length];
		}

		public ByteBuffer (byte [] buffer)
		{
			this.buffer = buffer ?? Empty<byte>.Array;
			this.length = this.buffer.Length;
		}

		public void Reset (byte [] buffer)
		{
			this.buffer = buffer ?? Empty<byte>.Array;
			this.length = this.buffer.Length;
		}

		public void Advance (int length)
		{
			position += length;
		}

		public byte ReadByte ()
		{
			return buffer [position++];
		}

		public sbyte ReadSByte ()
		{
			return (sbyte) ReadByte ();
		}

		public byte [] ReadBytes (int length)
		{
			var bytes = new byte [length];
			Buffer.BlockCopy (buffer, position, bytes, 0, length);
			position += length;
			return bytes;
		}

		public ushort ReadUInt16 ()
		{
			ushort value = (ushort) (buffer [position]
				| (buffer [position + 1] << 8));
			position += 2;
			return value;
		}

		public short ReadInt16 ()
		{
			return (short) ReadUInt16 ();
		}

		public uint ReadUInt32 ()
		{
			uint value = (uint) (buffer [position]
				| (buffer [position + 1] << 8)
				| (buffer [position + 2] << 16)
				| (buffer [position + 3] << 24));
			position += 4;
			return value;
		}

		public int ReadInt32 ()
		{
			return (int) ReadUInt32 ();
		}

		public ulong ReadUInt64 ()
		{
			uint low = ReadUInt32 ();
			uint high = ReadUInt32 ();

			return (((ulong) high) << 32) | low;
		}

		public long ReadInt64 ()
		{
			return (long) ReadUInt64 ();
		}

		public uint ReadCompressedUInt32 ()
		{
			byte first = ReadByte ();
			if ((first & 0x80) == 0)
				return first;

			if ((first & 0x40) == 0)
				return ((uint) (first & ~0x80) << 8)
					| ReadByte ();

			return ((uint) (first & ~0xc0) << 24)
				| (uint) ReadByte () << 16
				| (uint) ReadByte () << 8
				| ReadByte ();
		}

		public int ReadCompressedInt32 ()
		{
			var value = (int) (ReadCompressedUInt32 () >> 1);
			if ((value & 1) == 0)
				return value;
			if (value < 0x40)
				return value - 0x40;
			if (value < 0x2000)
				return value - 0x2000;
			if (value < 0x10000000)
				return value - 0x10000000;
			return value - 0x20000000;
		}

		public float ReadSingle ()
		{
			if (!BitConverter.IsLittleEndian) {
				var bytes = ReadBytes (4);
				Array.Reverse (bytes);
				return BitConverter.ToSingle (bytes, 0);
			}

			float value = BitConverter.ToSingle (buffer, position);
			position += 4;
			return value;
		}

		public double ReadDouble ()
		{
			if (!BitConverter.IsLittleEndian) {
				var bytes = ReadBytes (8);
				Array.Reverse (bytes);
				return BitConverter.ToDouble (bytes, 0);
			}

			double value = BitConverter.ToDouble (buffer, position);
			position += 8;
			return value;
		}

#if !READ_ONLY

		public void WriteByte (byte value)
		{
			if (position == buffer.Length)
				Grow (1);

			buffer [position++] = value;

			if (position > length)
				length = position;
		}

		public void WriteSByte (sbyte value)
		{
			WriteByte ((byte) value);
		}

		public void WriteUInt16 (ushort value)
		{
			if (position + 2 > buffer.Length)
				Grow (2);

			buffer [position++] = (byte) value;
			buffer [position++] = (byte) (value >> 8);

			if (position > length)
				length = position;
		}

		public void WriteInt16 (short value)
		{
			WriteUInt16 ((ushort) value);
		}

		public void WriteUInt32 (uint value)
		{
			if (position + 4 > buffer.Length)
				Grow (4);

			buffer [position++] = (byte) value;
			buffer [position++] = (byte) (value >> 8);
			buffer [position++] = (byte) (value >> 16);
			buffer [position++] = (byte) (value >> 24);

			if (position > length)
				length = position;
		}

		public void WriteInt32 (int value)
		{
			WriteUInt32 ((uint) value);
		}

		public void WriteUInt64 (ulong value)
		{
			if (position + 8 > buffer.Length)
				Grow (8);

			buffer [position++] = (byte) value;
			buffer [position++] = (byte) (value >> 8);
			buffer [position++] = (byte) (value >> 16);
			buffer [position++] = (byte) (value >> 24);
			buffer [position++] = (byte) (value >> 32);
			buffer [position++] = (byte) (value >> 40);
			buffer [position++] = (byte) (value >> 48);
			buffer [position++] = (byte) (value >> 56);

			if (position > length)
				length = position;
		}

		public void WriteInt64 (long value)
		{
			WriteUInt64 ((ulong) value);
		}

		public void WriteCompressedUInt32 (uint value)
		{
			if (value < 0x80)
				WriteByte ((byte) value);
			else if (value < 0x4000) {
				WriteByte ((byte) (0x80 | (value >> 8)));
				WriteByte ((byte) (value & 0xff));
			} else {
				WriteByte ((byte) ((value >> 24) | 0xc0));
				WriteByte ((byte) ((value >> 16) & 0xff));
				WriteByte ((byte) ((value >> 8) & 0xff));
				WriteByte ((byte) (value & 0xff));
			}
		}

		public void WriteCompressedInt32 (int value)
		{
			if (value >= 0) {
				WriteCompressedUInt32 ((uint) (value << 1));
				return;
			}

			if (value > -0x40)
				value = 0x40 + value;
			else if (value >= -0x2000)
				value = 0x2000 + value;
			else if (value >= -0x20000000)
				value = 0x20000000 + value;

			WriteCompressedUInt32 ((uint) ((value << 1) | 1));
		}

		public void WriteBytes (byte [] bytes)
		{
			var length = bytes.Length;
			if (position + length > buffer.Length)
				Grow (length);

			Buffer.BlockCopy (bytes, 0, buffer, position, length);
			position += length;

			if (position > this.length)
				this.length = position;
		}

		public void WriteBytes (int length)
		{
			if (position + length > buffer.Length)
				Grow (length);

			position += length;

			if (position > this.length)
				this.length = position;
		}

		public void WriteBytes (ByteBuffer buffer)
		{
			if (position + buffer.length > this.buffer.Length)
				Grow (buffer.length);

			Buffer.BlockCopy (buffer.buffer, 0, this.buffer, position, buffer.length);
			position += buffer.length;

			if (position > this.length)
				this.length = position;
		}

		public void WriteSingle (float value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (!BitConverter.IsLittleEndian)
				Array.Reverse (bytes);

			WriteBytes (bytes);
		}

		public void WriteDouble (double value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (!BitConverter.IsLittleEndian)
				Array.Reverse (bytes);

			WriteBytes (bytes);
		}

		void Grow (int desired)
		{
			var current = this.buffer;
			var current_length = current.Length;

			var buffer = new byte [System.Math.Max (current_length + desired, current_length * 2)];
			Buffer.BlockCopy (current, 0, buffer, 0, current_length);
			this.buffer = buffer;
		}

#endif

	}
}
