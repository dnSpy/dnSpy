// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Text;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// Derive from this class to create visitor for the XML tree
	/// </summary>
	public abstract class AbstractAXmlVisitor : IAXmlVisitor
	{
		/// <summary> Visit RawDocument </summary>
		public virtual void VisitDocument(AXmlDocument document)
		{
			foreach(AXmlObject child in document.Children) child.AcceptVisitor(this);
		}
		
		/// <summary> Visit RawElement </summary>
		public virtual void VisitElement(AXmlElement element)
		{
			foreach(AXmlObject child in element.Children) child.AcceptVisitor(this);
		}
		
		/// <summary> Visit RawTag </summary>
		public virtual void VisitTag(AXmlTag tag)
		{
			foreach(AXmlObject child in tag.Children) child.AcceptVisitor(this);
		}
		
		/// <summary> Visit RawAttribute </summary>
		public virtual void VisitAttribute(AXmlAttribute attribute)
		{
			
		}
		
		/// <summary> Visit RawText </summary>
		public virtual void VisitText(AXmlText text)
		{
			
		}
	}
}
