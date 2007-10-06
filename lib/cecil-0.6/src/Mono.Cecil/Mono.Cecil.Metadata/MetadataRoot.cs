//
// MetadataRoot.cs
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

	using Mono.Cecil.Binary;

	public sealed class MetadataRoot : IMetadataVisitable {

		MetadataRootHeader m_header;
		Image m_image;

		MetadataStreamCollection m_streams;

		public MetadataRootHeader Header {
			get { return m_header; }
			set { m_header = value; }
		}

		public MetadataStreamCollection Streams {
			get { return m_streams; }
			set { m_streams = value; }
		}

		internal MetadataRoot (Image img)
		{
			m_image = img;
		}

		public Image GetImage ()
		{
			return m_image;
		}

		public void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitMetadataRoot (this);

			m_header.Accept (visitor);
			m_streams.Accept (visitor);

			visitor.TerminateMetadataRoot (this);
		}

		public sealed class MetadataRootHeader : IHeader, IMetadataVisitable {

			public const uint StandardSignature = 0x424a5342;

			public uint Signature;
			public ushort MinorVersion;
			public ushort MajorVersion;
			public uint Reserved;
			public string Version;
			public ushort Flags;
			public ushort Streams;

			internal MetadataRootHeader ()
			{
			}

			public void SetDefaultValues ()
			{
				Signature = StandardSignature;
				Reserved = 0;
				Flags = 0;
			}

			public void Accept (IMetadataVisitor visitor)
			{
				visitor.VisitMetadataRootHeader (this);
			}
		}
	}
}
