//
// SmallXmlParser.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

//
// small xml parser that is mostly compatible with
//

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace Mono.Xml
{
	internal class DefaultHandler : SmallXmlParser.IContentHandler
	{
		public void OnStartParsing (SmallXmlParser parser)
		{
		}

		public void OnEndParsing (SmallXmlParser parser)
		{
		}

		public void OnStartElement (string name, SmallXmlParser.IAttrList attrs)
		{
		}

		public void OnEndElement (string name)
		{
		}

		public void OnChars (string s)
		{
		}

		public void OnIgnorableWhitespace (string s)
		{
		}

		public void OnProcessingInstruction (string name, string text)
		{
		}
	}

	internal class SmallXmlParser
	{
		public interface IContentHandler
		{
			void OnStartParsing (SmallXmlParser parser);
			void OnEndParsing (SmallXmlParser parser);
			void OnStartElement (string name, IAttrList attrs);
			void OnEndElement (string name);
			void OnProcessingInstruction (string name, string text);
			void OnChars (string text);
			void OnIgnorableWhitespace (string text);
		}

		public interface IAttrList
		{
			int Length { get; }
			bool IsEmpty { get; }
			string GetName (int i);
			string GetValue (int i);
			string GetValue (string name);
			string [] Names { get; }
			string [] Values { get; }
		}

		class AttrListImpl : IAttrList
		{
			public int Length {
				get { return attrNames.Count; }
			}
			public bool IsEmpty {
				get { return attrNames.Count == 0; }
			}
			public string GetName (int i)
			{
				return (string) attrNames [i];
			}
			public string GetValue (int i)
			{
				return (string) attrValues [i];
			}
			public string GetValue (string name)
			{
				for (int i = 0; i < attrNames.Count; i++)
					if ((string) attrNames [i] == name)
						return (string) attrValues [i];
				return null;
			}
			public string [] Names {
				get { return (string []) attrNames.ToArray (typeof (string)); }
			}
			public string [] Values {
				get { return (string []) attrValues.ToArray (typeof (string)); }
			}

			ArrayList attrNames = new ArrayList ();
			ArrayList attrValues = new ArrayList ();

			internal void Clear ()
			{
				attrNames.Clear ();
				attrValues.Clear ();
			}

			internal void Add (string name, string value)
			{
				attrNames.Add (name);
				attrValues.Add (value);
			}
		}

		IContentHandler handler;
		TextReader reader;
		Stack elementNames = new Stack ();
		Stack xmlSpaces = new Stack ();
		string xmlSpace;
		StringBuilder buffer = new StringBuilder (200);
		char [] nameBuffer = new char [30];
		bool isWhitespace;

		AttrListImpl attributes = new AttrListImpl ();
		int line = 1, column;
		bool resetColumn;

		public SmallXmlParser ()
		{
		}

		private Exception Error (string msg)
		{
			return new SmallXmlParserException (msg, line, column);
		}

		private Exception UnexpectedEndError ()
		{
			string [] arr = new string [elementNames.Count];
			// COMPACT FRAMEWORK NOTE: CopyTo is not visible through the Stack class
			(elementNames as ICollection).CopyTo (arr, 0);
			return Error (String.Format (
							  "Unexpected end of stream. Element stack content is {0}", String.Join (",", arr)));
		}


		private bool IsNameChar (char c, bool start)
		{
			switch (c) {
			case ':':
			case '_':
				return true;
			case '-':
			case '.':
				return !start;
			}
			if (c > 0x100) { // optional condition for optimization
				switch (c) {
				case '\u0559':
				case '\u06E5':
				case '\u06E6':
					return true;
				}
				if ('\u02BB' <= c && c <= '\u02C1')
					return true;
			}
			switch (Char.GetUnicodeCategory (c)) {
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.LetterNumber:
				return true;
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.DecimalDigitNumber:
				return !start;
			default:
				return false;
			}
		}

		private bool IsWhitespace (int c)
		{
			switch (c) {
			case ' ':
			case '\r':
			case '\t':
			case '\n':
				return true;
			default:
				return false;
			}
		}


		public void SkipWhitespaces ()
		{
			SkipWhitespaces (false);
		}

		private void HandleWhitespaces ()
		{
			while (IsWhitespace (Peek ()))
				buffer.Append ((char) Read ());
			if (Peek () != '<' && Peek () >= 0)
				isWhitespace = false;
		}

		public void SkipWhitespaces (bool expected)
		{
			while (true) {
				switch (Peek ()) {
				case ' ':
				case '\r':
				case '\t':
				case '\n':
					Read ();
					if (expected)
						expected = false;
					continue;
				}
				if (expected)
					throw Error ("Whitespace is expected.");
				return;
			}
		}


		private int Peek ()
		{
			return reader.Peek ();
		}

		private int Read ()
		{
			int i = reader.Read ();
			if (i == '\n')
				resetColumn = true;
			if (resetColumn) {
				line++;
				resetColumn = false;
				column = 1;
			}
			else
				column++;
			return i;
		}

		public void Expect (int c)
		{
			int p = Read ();
			if (p < 0)
				throw UnexpectedEndError ();
			else if (p != c)
				throw Error (String.Format ("Expected '{0}' but got {1}", (char) c, (char) p));
		}

		private string ReadUntil (char until, bool handleReferences)
		{
			while (true) {
				if (Peek () < 0)
					throw UnexpectedEndError ();
				char c = (char) Read ();
				if (c == until)
					break;
				else if (handleReferences && c == '&')
					ReadReference ();
				else
					buffer.Append (c);
			}
			string ret = buffer.ToString ();
			buffer.Length = 0;
			return ret;
		}

		public string ReadName ()
		{
			int idx = 0;
			if (Peek () < 0 || !IsNameChar ((char) Peek (), true))
				throw Error ("XML name start character is expected.");
			for (int i = Peek (); i >= 0; i = Peek ()) {
				char c = (char) i;
				if (!IsNameChar (c, false))
					break;
				if (idx == nameBuffer.Length) {
					char [] tmp = new char [idx * 2];
					// COMPACT FRAMEWORK NOTE: Array.Copy(sourceArray, destinationArray, count) is not available.
					Array.Copy (nameBuffer, 0, tmp, 0, idx);
					nameBuffer = tmp;
				}
				nameBuffer [idx++] = c;
				Read ();
			}
			if (idx == 0)
				throw Error ("Valid XML name is expected.");
			return new string (nameBuffer, 0, idx);
		}


		public void Parse (TextReader input, IContentHandler handler)
		{
			this.reader = input;
			this.handler = handler;

			handler.OnStartParsing (this);

			while (Peek () >= 0)
				ReadContent ();
			HandleBufferedContent ();
			if (elementNames.Count > 0)
				throw Error (String.Format ("Insufficient close tag: {0}", elementNames.Peek ()));

			handler.OnEndParsing (this);

			Cleanup ();
		}

		private void Cleanup ()
		{
			line = 1;
			column = 0;
			handler = null;
			reader = null;
#if CF_1_0
			elementNames = new Stack ();
			xmlSpaces = new Stack ();
#else
			elementNames.Clear ();
			xmlSpaces.Clear ();
#endif
			attributes.Clear ();
			buffer.Length = 0;
			xmlSpace = null;
			isWhitespace = false;
		}

		public void ReadContent ()
		{
			string name;
			if (IsWhitespace (Peek ())) {
				if (buffer.Length == 0)
					isWhitespace = true;
				HandleWhitespaces ();
			}
			if (Peek () == '<') {
				Read ();
				switch (Peek ()) {
				case '!': // declarations
					Read ();
					if (Peek () == '[') {
						Read ();
						if (ReadName () != "CDATA")
							throw Error ("Invalid declaration markup");
						Expect ('[');
						ReadCDATASection ();
						return;
					}
					else if (Peek () == '-') {
						ReadComment ();
						return;
					}
					else if (ReadName () != "DOCTYPE")
						throw Error ("Invalid declaration markup.");
					else
						throw Error ("This parser does not support document type.");
				case '?': // PIs
					HandleBufferedContent ();
					Read ();
					name = ReadName ();
					SkipWhitespaces ();
					string text = String.Empty;
					if (Peek () != '?') {
						while (true) {
							text += ReadUntil ('?', false);
							if (Peek () == '>')
								break;
							text += "?";
						}
					}
					handler.OnProcessingInstruction (
						name, text);
					Expect ('>');
					return;
				case '/': // end tags
					HandleBufferedContent ();
					if (elementNames.Count == 0)
						throw UnexpectedEndError ();
					Read ();
					name = ReadName ();
					SkipWhitespaces ();
					string expected = (string) elementNames.Pop ();
					xmlSpaces.Pop ();
					if (xmlSpaces.Count > 0)
						xmlSpace = (string) xmlSpaces.Peek ();
					else
						xmlSpace = null;
					if (name != expected)
						throw Error (String.Format ("End tag mismatch: expected {0} but found {1}", expected, name));
					handler.OnEndElement (name);
					Expect ('>');
					return;
				default: // start tags (including empty tags)
					HandleBufferedContent ();
					name = ReadName ();
					while (Peek () != '>' && Peek () != '/')
						ReadAttribute (attributes);
					handler.OnStartElement (name, attributes);
					attributes.Clear ();
					SkipWhitespaces ();
					if (Peek () == '/') {
						Read ();
						handler.OnEndElement (name);
					}
					else {
						elementNames.Push (name);
						xmlSpaces.Push (xmlSpace);
					}
					Expect ('>');
					return;
				}
			}
			else
				ReadCharacters ();
		}

		private void HandleBufferedContent ()
		{
			if (buffer.Length == 0)
				return;
			if (isWhitespace)
				handler.OnIgnorableWhitespace (buffer.ToString ());
			else
				handler.OnChars (buffer.ToString ());
			buffer.Length = 0;
			isWhitespace = false;
		}

		private void ReadCharacters ()
		{
			isWhitespace = false;
			while (true) {
				int i = Peek ();
				switch (i) {
				case -1:
					return;
				case '<':
					return;
				case '&':
					Read ();
					ReadReference ();
					continue;
				default:
					buffer.Append ((char) Read ());
					continue;
				}
			}
		}

		private void ReadReference ()
		{
			if (Peek () == '#') {
				// character reference
				Read ();
				ReadCharacterReference ();
			} else {
				string name = ReadName ();
				Expect (';');
				switch (name) {
				case "amp":
					buffer.Append ('&');
					break;
				case "quot":
					buffer.Append ('"');
					break;
				case "apos":
					buffer.Append ('\'');
					break;
				case "lt":
					buffer.Append ('<');
					break;
				case "gt":
					buffer.Append ('>');
					break;
				default:
					throw Error ("General non-predefined entity reference is not supported in this parser.");
				}
			}
		}

		private int ReadCharacterReference ()
		{
			int n = 0;
			if (Peek () == 'x') { // hex
				Read ();
				for (int i = Peek (); i >= 0; i = Peek ()) {
					if ('0' <= i && i <= '9')
						n = n << 4 + i - '0';
					else if ('A' <= i && i <='F')
						n = n << 4 + i - 'A' + 10;
					else if ('a' <= i && i <='f')
						n = n << 4 + i - 'a' + 10;
					else
						break;
					Read ();
				}
			} else {
				for (int i = Peek (); i >= 0; i = Peek ()) {
					if ('0' <= i && i <= '9')
						n = n << 4 + i - '0';
					else
						break;
					Read ();
				}
			}
			return n;
		}

		private void ReadAttribute (AttrListImpl a)
		{
			SkipWhitespaces (true);
			if (Peek () == '/' || Peek () == '>')
			// came here just to spend trailing whitespaces
				return;

			string name = ReadName ();
			string value;
			SkipWhitespaces ();
			Expect ('=');
			SkipWhitespaces ();
			switch (Read ()) {
			case '\'':
				value = ReadUntil ('\'', true);
				break;
			case '"':
				value = ReadUntil ('"', true);
				break;
			default:
				throw Error ("Invalid attribute value markup.");
			}
			if (name == "xml:space")
				xmlSpace = value;
			a.Add (name, value);
		}

		private void ReadCDATASection ()
		{
			int nBracket = 0;
			while (true) {
				if (Peek () < 0)
					throw UnexpectedEndError ();
				char c = (char) Read ();
				if (c == ']')
					nBracket++;
				else if (c == '>' && nBracket > 1) {
					for (int i = nBracket; i > 2; i--)
						buffer.Append (']');
					break;
				}
				else {
					for (int i = 0; i < nBracket; i++)
						buffer.Append (']');
					nBracket = 0;
					buffer.Append (c);
				}
			}
		}

		private void ReadComment ()
		{
			Expect ('-');
			Expect ('-');
			while (true) {
				if (Read () != '-')
					continue;
				if (Read () != '-')
					continue;
				if (Read () != '>')
					throw Error ("'--' is not allowed inside comment markup.");
				break;
			}
		}
	}

	internal class SmallXmlParserException : SystemException
	{
		int line;
		int column;

		public SmallXmlParserException (string msg, int line, int column)
		: base (String.Format ("{0}. At ({1},{2})", msg, line, column))
		{
			this.line = line;
			this.column = column;
		}

		public int Line {
			get { return line; }
		}

		public int Column {
			get { return column; }
		}
	}
}



