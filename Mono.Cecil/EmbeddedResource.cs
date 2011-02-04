//
// EmbeddedResource.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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
using System.IO;

namespace Mono.Cecil {

	public sealed class EmbeddedResource : Resource {

		readonly MetadataReader reader;

		uint? offset;
		byte [] data;
		Stream stream;

		public override ResourceType ResourceType {
			get { return ResourceType.Embedded; }
		}

		public EmbeddedResource (string name, ManifestResourceAttributes attributes, byte [] data) :
			base (name, attributes)
		{
			this.data = data;
		}

		public EmbeddedResource (string name, ManifestResourceAttributes attributes, Stream stream) :
			base (name, attributes)
		{
			this.stream = stream;
		}

		internal EmbeddedResource (string name, ManifestResourceAttributes attributes, uint offset, MetadataReader reader)
			: base (name, attributes)
		{
			this.offset = offset;
			this.reader = reader;
		}

		public Stream GetResourceStream ()
		{
			if (stream != null)
				return stream;

			if (data != null)
				return new MemoryStream (data);

			if (offset.HasValue)
				return reader.GetManagedResourceStream (offset.Value);

			throw new InvalidOperationException ();
		}

		public byte [] GetResourceData ()
		{
			if (stream != null)
				return ReadStream (stream);

			if (data != null)
				return data;

			if (offset.HasValue)
				return reader.GetManagedResourceStream (offset.Value).ToArray ();

			throw new InvalidOperationException ();
		}

		static byte [] ReadStream (Stream stream)
		{
			var length = (int) stream.Length;
			var data = new byte [length];
			int offset = 0, read;

			while ((read = stream.Read (data, offset, length - offset)) > 0)
				offset += read;

			return data;
		}
	}
}
