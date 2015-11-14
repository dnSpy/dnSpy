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

using System.Collections.Generic;
using dnSpy.NRefactory;
using ICSharpCode.NRefactory;

namespace ICSharpCode.Decompiler {
	public interface ITextOutput
	{
		TextLocation Location { get; }
		
		void Indent();
		void Unindent();
		void Write(string text, TextTokenType tokenType);
		void WriteLine();
		void WriteDefinition(string text, object definition, TextTokenType tokenType, bool isLocal = true);
		void WriteReference(string text, object reference, TextTokenType tokenType, bool isLocal = false);
		
		void AddDebugSymbols(MemberMapping methodDebugSymbols);
	}
	
	public static class TextOutputExtensions
	{
		public static void WriteLine(this ITextOutput output, string text, TextTokenType tokenType)
		{
			output.Write(text, tokenType);
			output.WriteLine();
		}

		public static void WriteSpace(this ITextOutput output)
		{
			output.Write(" ", TextTokenType.Text);
		}

		public static void WriteLineLeftBrace(this ITextOutput output)
		{
			output.Write("{", TextTokenType.Brace);
			output.WriteLine();
		}

		public static void WriteLineRightBrace(this ITextOutput output)
		{
			output.Write("}", TextTokenType.Brace);
			output.WriteLine();
		}

		public static void WriteLeftBrace(this ITextOutput output)
		{
			output.Write("{", TextTokenType.Brace);
		}

		public static void WriteRightBrace(this ITextOutput output)
		{
			output.Write("}", TextTokenType.Brace);
		}

		public static void WriteXmlDoc(this ITextOutput output, string text)
		{
			foreach (var kv in SimpleXmlParser.Parse(text))
				output.Write(kv.Key, kv.Value);
		}
	}

	// We have to parse it ourselves since we'd get all sorts of exceptions if we let
	// the standard XML reader try to parse it, even if we set the data to Fragment.
	// Since it only operates on one line at a time (no extra context), it won't be
	// able to handle eg. attributes spanning more than one line, but this rarely happens.
	static class SimpleXmlParser
	{
		static readonly char[] specialChars = new char[] { '<' };
		static readonly char[] specialCharsTag = new char[] { '<', '>', '"' };

		public static IEnumerable<KeyValuePair<string, TextTokenType>> Parse(string text)
		{
			bool inTag = true;
			int index = 0;
			while (index < text.Length) {
				int specialIndex = text.IndexOfAny(inTag ? specialCharsTag : specialChars, index);
				if (specialIndex < 0) {
					yield return new KeyValuePair<string, TextTokenType>(text.Substring(index), TextTokenType.XmlDocComment);
					break;
				}

				var c = text[specialIndex];
				if (c == '>') {
					yield return new KeyValuePair<string, TextTokenType>(text.Substring(index, specialIndex - index + 1), TextTokenType.XmlDocTag);
					index = specialIndex + 1;
				}
				else {
					if (specialIndex - index > 0) {
						if (c == '<')
							yield return new KeyValuePair<string, TextTokenType>(text.Substring(index, specialIndex - index), TextTokenType.XmlDocComment);
						else // c == '"'
							yield return new KeyValuePair<string, TextTokenType>(text.Substring(index, specialIndex - index), inTag ? TextTokenType.XmlDocTag : TextTokenType.XmlDocComment);
					}

					index = specialIndex;
					int endIndex = text.IndexOf('>', index);
					endIndex = endIndex < 0 ? text.Length : endIndex + 1;

					while (index < endIndex) {
						int attrIndex = text.IndexOf('"', index, endIndex - index);
						if (attrIndex < 0) {
							yield return new KeyValuePair<string, TextTokenType>(text.Substring(index, endIndex - index), TextTokenType.XmlDocTag);
							break;
						}

						if (attrIndex - index > 0)
							yield return new KeyValuePair<string, TextTokenType>(text.Substring(index, attrIndex - index), TextTokenType.XmlDocTag);

						int endAttrIndex = text.IndexOf('"', attrIndex + 1, endIndex - attrIndex - 1);
						if (endAttrIndex < 0) {
							yield return new KeyValuePair<string, TextTokenType>(text.Substring(attrIndex, endIndex - attrIndex), TextTokenType.XmlDocAttribute);
							break;
						}

						yield return new KeyValuePair<string, TextTokenType>(text.Substring(attrIndex, endAttrIndex - attrIndex + 1), TextTokenType.XmlDocAttribute);
						index = endAttrIndex + 1;
					}

					index = endIndex;
				}
				inTag = false;
			}
		}
	}
}
