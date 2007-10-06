//
// MetadataHeap.cs
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

namespace Mono.Cecil.Metadata {

	using System;

	using Mono.Cecil;

	public abstract class MetadataHeap : IMetadataVisitable  {

		MetadataStream m_stream;
		string m_name;
		byte [] m_data;

		public string Name {
			get { return m_name; }
		}

		public byte [] Data {
			get { return m_data; }
			set { m_data = value; }
		}

		public int IndexSize;

		internal MetadataHeap (MetadataStream stream, string name)
		{
			m_name = name;
			m_stream = stream;
		}

		public static MetadataHeap HeapFactory (MetadataStream stream)
		{
			switch (stream.Header.Name) {
			case MetadataStream.Tables :
			case MetadataStream.IncrementalTables :
				return new TablesHeap (stream);
			case MetadataStream.GUID :
				return new GuidHeap (stream);
			case MetadataStream.Strings :
				return new StringsHeap (stream);
			case MetadataStream.UserStrings :
				return new UserStringsHeap (stream);
			case MetadataStream.Blob :
				return new BlobHeap (stream);
			default :
				return null;
			}
		}

		public MetadataStream GetStream ()
		{
			return m_stream;
		}

		protected virtual byte [] ReadBytesFromStream (uint pos)
		{
			int start, length = Utilities.ReadCompressedInteger (m_data, (int) pos, out start);
			byte [] buffer = new byte [length];
			Buffer.BlockCopy (m_data, start, buffer, 0, length);
			return buffer;
		}

		public abstract void Accept (IMetadataVisitor visitor);
	}
}
