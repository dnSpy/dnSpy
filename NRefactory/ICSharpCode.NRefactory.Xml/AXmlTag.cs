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
using System.Collections.ObjectModel;
using System.Globalization;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Represents any markup starting with "&lt;" and (hopefully) ending with ">"
	/// </summary>
	public class AXmlTag : AXmlObject
	{
		/// <summary> These identify the start of DTD elements </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification="ReadOnlyCollection is immutable")]
		public static readonly ReadOnlyCollection<string> DtdNames = new ReadOnlyCollection<string>(
			new string[] {"<!DOCTYPE", "<!NOTATION", "<!ELEMENT", "<!ATTLIST", "<!ENTITY" } );
		
		new readonly InternalTag internalObject;
		
		internal AXmlTag(AXmlObject parent, int startOffset, InternalTag internalObject)
			: base(parent, startOffset, internalObject)
		{
			this.internalObject = internalObject;
		}
		
		/// <summary> Opening bracket - usually "&lt;" </summary>
		public string OpeningBracket {
			get { return internalObject.OpeningBracket; }
		}
		
		/// <summary> Name following the opening bracket </summary>
		public string Name {
			get { return internalObject.Name; }
		}
		
		/// <summary> Gets the segment containing the tag name </summary>
		public ISegment NameSegment {
			get {
				int start = startOffset + internalObject.RelativeNameStart;
				return new XmlSegment(start, start + internalObject.Name.Length);
			}
		}
		
		/// <summary> Closing bracket - usually "&gt;" </summary>
		public string ClosingBracket {
			get { return internalObject.ClosingBracket; }
		}
		
		/// <summary> True if tag starts with "&lt;" </summary>
		public bool IsStartOrEmptyTag       { get { return internalObject.IsStartOrEmptyTag; } }
		/// <summary> True if tag starts with "&lt;" and ends with "&gt;" </summary>
		public bool IsStartTag              { get { return internalObject.IsStartTag; } }
		/// <summary> True if tag starts with "&lt;" and does not end with "&gt;" </summary>
		public bool IsEmptyTag              { get { return internalObject.IsEmptyTag; } }
		/// <summary> True if tag starts with "&lt;/" </summary>
		public bool IsEndTag                { get { return internalObject.IsEndTag; } }
		/// <summary> True if tag starts with "&lt;?" </summary>
		public bool IsProcessingInstruction { get { return internalObject.IsProcessingInstruction; } }
		/// <summary> True if tag starts with "&lt;!--" </summary>
		public bool IsComment               { get { return internalObject.IsComment; } }
		/// <summary> True if tag starts with "&lt;![CDATA[" </summary>
		public bool IsCData                 { get { return internalObject.IsCData; } }
		/// <summary> True if tag starts with one of the DTD starts </summary>
		public bool IsDocumentType          { get { return internalObject.IsDocumentType; } }
		/// <summary> True if tag starts with "&lt;!" </summary>
		public bool IsUnknownBang           { get { return internalObject.IsUnknownBang; } }
		
		/// <inheritdoc/>
		public override void AcceptVisitor(AXmlVisitor visitor)
		{
			visitor.VisitTag(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} '{1}{2}{3}' Attr:{4}]", base.ToString(), this.OpeningBracket, this.Name, this.ClosingBracket, this.Children.Count);
		}
	}
}
