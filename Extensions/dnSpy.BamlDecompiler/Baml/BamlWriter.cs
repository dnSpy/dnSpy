/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dnSpy.BamlDecompiler.Baml {
	internal class BamlBinaryWriter : BinaryWriter {
		public BamlBinaryWriter(Stream stream)
			: base(stream) {
		}

		public void WriteEncodedInt(int val) => Write7BitEncodedInt(val);
	}

	internal class BamlWriter {
		public static void WriteDocument(BamlDocument doc, Stream str) {
			var writer = new BamlBinaryWriter(str);
			{
				var wtr = new BinaryWriter(str, Encoding.Unicode);
				int len = doc.Signature.Length * 2;
				wtr.Write(len);
				wtr.Write(doc.Signature.ToCharArray());
				wtr.Write(new byte[((len + 3) & ~3) - len]);
			}
			writer.Write(doc.ReaderVersion.Major);
			writer.Write(doc.ReaderVersion.Minor);
			writer.Write(doc.UpdaterVersion.Major);
			writer.Write(doc.UpdaterVersion.Minor);
			writer.Write(doc.WriterVersion.Major);
			writer.Write(doc.WriterVersion.Minor);

			var defers = new List<int>();
			for (int i = 0; i < doc.Count; i++) {
				BamlRecord rec = doc[i];
				rec.Position = str.Position;
				writer.Write((byte)rec.Type);
				rec.Write(writer);
				if (rec is IBamlDeferRecord) defers.Add(i);
			}
			foreach (int i in defers)
				(doc[i] as IBamlDeferRecord).WriteDefer(doc, i, writer);
		}
	}
}
