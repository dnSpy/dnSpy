// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.IO;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class BamlBinaryReader : BinaryReader
	{
		// Methods
		public BamlBinaryReader(Stream stream)
			: base(stream)
		{
		}

		public virtual double ReadCompressedDouble()
		{
			switch (this.ReadByte()) {
				case 1:
					return 0;
				case 2:
					return 1;
				case 3:
					return -1;
				case 4:
					return ReadInt32() * 1E-06;
				case 5:
					return this.ReadDouble();
			}
			throw new NotSupportedException();
		}

		public int ReadCompressedInt32()
		{
			return base.Read7BitEncodedInt();
		}
	}
}