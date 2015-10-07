// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;
using System.Xml;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Static methods for determining the type of a file.
	/// </summary>
	static class GuessFileType
	{
		public static FileType DetectFileType(Stream stream)
		{
			StreamReader reader;
			if (stream.Length >= 2) {
				int firstByte = stream.ReadByte();
				int secondByte = stream.ReadByte();
				switch ((firstByte << 8) | secondByte) {
					case 0xfffe: // UTF-16 LE BOM / UTF-32 LE BOM
					case 0xfeff: // UTF-16 BE BOM
						stream.Position -= 2;
						reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
						break;
					case 0xefbb: // start of UTF-8 BOM
						if (stream.ReadByte() == 0xbf) {
							reader = new StreamReader(stream, Encoding.UTF8);
							break;
						} else {
							return FileType.Binary;
						}
					default:
						if (IsUTF8(stream, (byte)firstByte, (byte)secondByte)) {
							stream.Position = 0;
							reader = new StreamReader(stream, Encoding.UTF8);
							break;
						} else {
							return FileType.Binary;
						}
				}
			} else {
				return FileType.Binary;
			}
			// Now we got a StreamReader with the correct encoding
			// Check for XML now
			try {
				XmlTextReader xmlReader = new XmlTextReader(reader);
				xmlReader.XmlResolver = null;
				xmlReader.MoveToContent();
				return FileType.Xml;
			} catch (XmlException) {
				return FileType.Text;
			}
		}
		
		static bool IsUTF8(Stream fs, byte firstByte, byte secondByte)
		{
			int max = (int)Math.Min(fs.Length, 500000); // look at max. 500 KB
			const int ASCII = 0;
			const int Error = 1;
			const int UTF8  = 2;
			const int UTF8Sequence = 3;
			int state = ASCII;
			int sequenceLength = 0;
			byte b;
			for (int i = 0; i < max; i++) {
				if (i == 0) {
					b = firstByte;
				} else if (i == 1) {
					b = secondByte;
				} else {
					b = (byte)fs.ReadByte();
				}
				if (b < 0x80) {
					// normal ASCII character
					if (state == UTF8Sequence) {
						state = Error;
						break;
					}
				} else if (b < 0xc0) {
					// 10xxxxxx : continues UTF8 byte sequence
					if (state == UTF8Sequence) {
						--sequenceLength;
						if (sequenceLength < 0) {
							state = Error;
							break;
						} else if (sequenceLength == 0) {
							state = UTF8;
						}
					} else {
						state = Error;
						break;
					}
				} else if (b >= 0xc2 && b < 0xf5) {
					// beginning of byte sequence
					if (state == UTF8 || state == ASCII) {
						state = UTF8Sequence;
						if (b < 0xe0) {
							sequenceLength = 1; // one more byte following
						} else if (b < 0xf0) {
							sequenceLength = 2; // two more bytes following
						} else {
							sequenceLength = 3; // three more bytes following
						}
					} else {
						state = Error;
						break;
					}
				} else {
					// 0xc0, 0xc1, 0xf5 to 0xff are invalid in UTF-8 (see RFC 3629)
					state = Error;
					break;
				}
			}
			return state != Error;
		}
	}
	
	enum FileType
	{
		Binary,
		Text,
		Xml
	}
}
