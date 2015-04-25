// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Derive from this class to create visitor for the XML tree
	/// </summary>
	public abstract class AXmlVisitor
	{
		/// <summary> Visit AXmlDocument </summary>
		public virtual void VisitDocument(AXmlDocument document)
		{
			foreach (AXmlObject child in document.Children)
				child.AcceptVisitor(this);
		}
		
		/// <summary> Visit AXmlElement </summary>
		public virtual void VisitElement(AXmlElement element)
		{
			foreach (AXmlObject child in element.Children)
				child.AcceptVisitor(this);
		}
		
		/// <summary> Visit AXmlTag </summary>
		public virtual void VisitTag(AXmlTag tag)
		{
			foreach (AXmlObject child in tag.Children)
				child.AcceptVisitor(this);
		}
		
		/// <summary> Visit AXmlAttribute </summary>
		public virtual void VisitAttribute(AXmlAttribute attribute)
		{
			
		}
		
		/// <summary> Visit AXmlText </summary>
		public virtual void VisitText(AXmlText text)
		{
			
		}
	}
}