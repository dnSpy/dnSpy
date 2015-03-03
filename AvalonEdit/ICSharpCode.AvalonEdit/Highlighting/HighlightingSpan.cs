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
using System.Text.RegularExpressions;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A highlighting span is a region with start+end expression that has a different RuleSet inside
	/// and colors the region.
	/// </summary>
	[Serializable]
	public class HighlightingSpan
	{
		/// <summary>
		/// Gets/Sets the start expression.
		/// </summary>
		public Regex StartExpression { get; set; }
		
		/// <summary>
		/// Gets/Sets the end expression.
		/// </summary>
		public Regex EndExpression { get; set; }
		
		/// <summary>
		/// Gets/Sets the rule set that applies inside this span.
		/// </summary>
		public HighlightingRuleSet RuleSet { get; set; }
		
		/// <summary>
		/// Gets the color used for the text matching the start expression.
		/// </summary>
		public HighlightingColor StartColor { get; set; }
		
		/// <summary>
		/// Gets the color used for the text between start and end.
		/// </summary>
		public HighlightingColor SpanColor { get; set; }
		
		/// <summary>
		/// Gets the color used for the text matching the end expression.
		/// </summary>
		public HighlightingColor EndColor { get; set; }
		
		/// <summary>
		/// Gets/Sets whether the span color includes the start.
		/// The default is <c>false</c>.
		/// </summary>
		public bool SpanColorIncludesStart { get; set; }
		
		/// <summary>
		/// Gets/Sets whether the span color includes the end.
		/// The default is <c>false</c>.
		/// </summary>
		public bool SpanColorIncludesEnd { get; set; }
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " Start=" + StartExpression + ", End=" + EndExpression + "]";
		}
	}
}
