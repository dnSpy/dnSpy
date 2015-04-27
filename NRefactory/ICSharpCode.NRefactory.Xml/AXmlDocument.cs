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
using System.Globalization;
using System.Xml;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// The root object of the XML document
	/// </summary>
	public class AXmlDocument : AXmlObject
	{
		internal AXmlDocument(AXmlObject parent, int startOffset, InternalDocument internalObject)
			: base(parent, startOffset, internalObject)
		{
		}
		
		internal override ObjectIterator CreateIteratorForReader()
		{
			return new ObjectIterator(internalObject.NestedObjects, startOffset);
		}
		
		/// <inheritdoc/>
		public override void AcceptVisitor(AXmlVisitor visitor)
		{
			visitor.VisitDocument(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} Chld:{1}]", base.ToString(), this.Children.Count);
		}
		
		/// <summary>
		/// Represents an empty document.
		/// </summary>
		public readonly static AXmlDocument Empty = new AXmlDocument(null, 0, new InternalDocument());
	}
}
