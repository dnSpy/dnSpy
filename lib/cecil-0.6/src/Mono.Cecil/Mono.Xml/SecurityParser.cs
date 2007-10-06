//
// Mono.Xml.SecurityParser.cs class implementation
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;
using System.Security;

namespace Mono.Xml {

	// convert an XML document into SecurityElement objects
	internal class SecurityParser : SmallXmlParser, SmallXmlParser.IContentHandler {

		private SecurityElement root;

		public SecurityParser () : base ()
		{
			stack = new Stack ();
		}

		public void LoadXml (string xml)
		{
			root = null;
#if CF_1_0
			stack = new Stack ();
#else
			stack.Clear ();
#endif
			Parse (new StringReader (xml), this);
		}

		public SecurityElement ToXml ()
		{
			return root;
		}

		// IContentHandler

		private SecurityElement current;
		private Stack stack;

		public void OnStartParsing (SmallXmlParser parser) {}

		public void OnProcessingInstruction (string name, string text) {}

		public void OnIgnorableWhitespace (string s) {}

		public void OnStartElement (string name, SmallXmlParser.IAttrList attrs)
		{
			SecurityElement newel = new SecurityElement (name);
			if (root == null) {
				root = newel;
				current = newel;
			}
			else {
				SecurityElement parent = (SecurityElement) stack.Peek ();
				parent.AddChild (newel);
			}
			stack.Push (newel);
			current = newel;
			// attributes
			int n = attrs.Length;
			for (int i=0; i < n; i++)
				current.AddAttribute (attrs.GetName (i), attrs.GetValue (i));
		}

		public void OnEndElement (string name)
		{
			current = (SecurityElement) stack.Pop ();
		}

		public void OnChars (string ch)
		{
			current.Text = ch;
		}

		public void OnEndParsing (SmallXmlParser parser) {}
	}
}

