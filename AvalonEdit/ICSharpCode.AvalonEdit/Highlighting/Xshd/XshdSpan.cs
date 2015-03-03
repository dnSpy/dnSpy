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

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// Specifies the type of the regex.
	/// </summary>
	public enum XshdRegexType
	{
		/// <summary>
		/// Normal regex. Used when the regex was specified as attribute.
		/// </summary>
		Default,
		/// <summary>
		/// Ignore pattern whitespace / allow regex comments. Used when the regex was specified as text element.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly",
		                                                 Justification = "Using the same case as the RegexOption")]
		IgnorePatternWhitespace
	}
	
	/// <summary>
	/// &lt;Span&gt; element.
	/// </summary>
	[Serializable]
	public class XshdSpan : XshdElement
	{
		/// <summary>
		/// Gets/sets the begin regex.
		/// </summary>
		public string BeginRegex { get; set; }
		
		/// <summary>
		/// Gets/sets the begin regex type.
		/// </summary>
		public XshdRegexType BeginRegexType { get; set; }
		
		/// <summary>
		/// Gets/sets the end regex.
		/// </summary>
		public string EndRegex { get; set; }
		
		/// <summary>
		/// Gets/sets the end regex type.
		/// </summary>
		public XshdRegexType EndRegexType { get; set; }
		
		/// <summary>
		/// Gets/sets whether the span is multiline.
		/// </summary>
		public bool Multiline { get; set; }
		
		/// <summary>
		/// Gets/sets the rule set reference.
		/// </summary>
		public XshdReference<XshdRuleSet> RuleSetReference { get; set; }
		
		/// <summary>
		/// Gets/sets the span color.
		/// </summary>
		public XshdReference<XshdColor> SpanColorReference { get; set; }
		
		/// <summary>
		/// Gets/sets the span begin color.
		/// </summary>
		public XshdReference<XshdColor> BeginColorReference { get; set; }
		
		/// <summary>
		/// Gets/sets the span end color.
		/// </summary>
		public XshdReference<XshdColor> EndColorReference { get; set; }
		
		/// <inheritdoc/>
		public override object AcceptVisitor(IXshdVisitor visitor)
		{
			return visitor.VisitSpan(this);
		}
	}
}
