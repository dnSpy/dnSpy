//
// MemoryBinaryWriter.cs
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

	using System.IO;
	using System.Text;

	internal sealed class MemoryBinaryWriter : BinaryWriter {

		public MemoryStream MemoryStream {
			get { return (MemoryStream) this.BaseStream; }
		}

		public MemoryBinaryWriter () : base (new MemoryStream ())
		{
		}

		public MemoryBinaryWriter (Encoding enc) : base (new MemoryStream (), enc)
		{
		}

		public void Empty ()
		{
			this.BaseStream.Position = 0;
			this.BaseStream.SetLength (0);
		}

		public void Write (MemoryBinaryWriter writer)
		{
			writer.MemoryStream.WriteTo (this.BaseStream);
		}

		public byte [] ToArray ()
		{
			return this.MemoryStream.ToArray ();
		}

		public void QuadAlign ()
		{
			this.BaseStream.Position += 3;
			this.BaseStream.Position &= ~3;
		}
	}
}
