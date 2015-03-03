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
using System.Windows.Documents;

using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// A snippet element that has sub-elements.
	/// </summary>
	[Serializable]
	public class SnippetContainerElement : SnippetElement
	{
		NullSafeCollection<SnippetElement> elements = new NullSafeCollection<SnippetElement>();
		
		/// <summary>
		/// Gets the list of child elements.
		/// </summary>
		public IList<SnippetElement> Elements {
			get { return elements; }
		}
		
		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			foreach (SnippetElement e in this.Elements) {
				e.Insert(context);
			}
		}
		
		/// <inheritdoc/>
		public override Inline ToTextRun()
		{
			Span span = new Span();
			foreach (SnippetElement e in this.Elements) {
				Inline r = e.ToTextRun();
				if (r != null)
					span.Inlines.Add(r);
			}
			return span;
		}
	}
}
