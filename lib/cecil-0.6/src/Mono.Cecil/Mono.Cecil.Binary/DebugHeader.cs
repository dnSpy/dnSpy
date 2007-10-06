//
// DebugHeader.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Binary {

	using System;

	public sealed class DebugHeader : IHeader, IBinaryVisitable {

		public uint Characteristics;
		public uint TimeDateStamp;
		public ushort MajorVersion;
		public ushort MinorVersion;
		public DebugStoreType Type;
		public uint SizeOfData;
		public RVA AddressOfRawData;
		public uint PointerToRawData;

		public uint Magic;
		public Guid Signature;
		public uint Age;
		public string FileName;

		internal DebugHeader ()
		{
		}

		public void SetDefaultValues ()
		{
			Characteristics = 0;

			this.Magic = 0x53445352;
			this.Age = 0;
			this.Type = DebugStoreType.CodeView;
			this.FileName = string.Empty;
		}

		public uint GetSize ()
		{
			return 0x34 + (uint) FileName.Length + 1;
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitDebugHeader (this);
		}
	}
}
