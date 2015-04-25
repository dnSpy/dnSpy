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

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Whitespace or character data
	/// </summary>
	public class AXmlText : AXmlObject
	{
		internal AXmlText(AXmlObject parent, int startOffset, InternalText internalObject)
			: base(parent, startOffset, internalObject)
		{
		}
		
//		/// <summary> The type of the text node </summary>
//		public TextType Type {
//			get { return ((InternalText)internalObject).Type; }
//		}
		
		/// <summary> The text with all entity references resloved </summary>
		public string Value {
			get { return ((InternalText)internalObject).Value; }
		}
		
		/// <summary> True if the text contains only whitespace characters </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "System.Xml also uses 'Whitespace'")]
		public bool ContainsOnlyWhitespace {
			get { return ((InternalText)internalObject).ContainsOnlyWhitespace; }
		}
		
		/// <inheritdoc/>
		public override void AcceptVisitor(AXmlVisitor visitor)
		{
			visitor.VisitText(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} Text.Length={1}]", base.ToString(), this.Value.Length);
		}
	}
}
