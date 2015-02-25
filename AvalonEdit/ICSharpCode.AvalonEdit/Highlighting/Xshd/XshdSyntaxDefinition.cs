// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// A &lt;SyntaxDefinition&gt; element.
	/// </summary>
	[Serializable]
	public class XshdSyntaxDefinition
	{
		/// <summary>
		/// Creates a new XshdSyntaxDefinition object.
		/// </summary>
		public XshdSyntaxDefinition()
		{
			this.Elements = new NullSafeCollection<XshdElement>();
			this.Extensions = new NullSafeCollection<string>();
		}
		
		/// <summary>
		/// Gets/sets the definition name
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Gets the associated extensions.
		/// </summary>
		public IList<string> Extensions { get; private set; }
		
		/// <summary>
		/// Gets the collection of elements.
		/// </summary>
		public IList<XshdElement> Elements { get; private set; }
		
		/// <summary>
		/// Applies the visitor to all elements.
		/// </summary>
		public void AcceptElements(IXshdVisitor visitor)
		{
			foreach (XshdElement element in Elements) {
				element.AcceptVisitor(visitor);
			}
		}
	}
}
