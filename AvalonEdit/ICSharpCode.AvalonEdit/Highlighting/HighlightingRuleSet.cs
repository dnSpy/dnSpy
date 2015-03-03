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

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A highlighting rule set describes a set of spans that are valid at a given code location.
	/// </summary>
	[Serializable]
	public class HighlightingRuleSet
	{
		/// <summary>
		/// Creates a new RuleSet instance.
		/// </summary>
		public HighlightingRuleSet()
		{
			this.Spans = new NullSafeCollection<HighlightingSpan>();
			this.Rules = new NullSafeCollection<HighlightingRule>();
		}
		
		/// <summary>
		/// Gets/Sets the name of the rule set.
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Gets the list of spans.
		/// </summary>
		public IList<HighlightingSpan> Spans { get; private set; }
		
		/// <summary>
		/// Gets the list of rules.
		/// </summary>
		public IList<HighlightingRule> Rules { get; private set; }
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " " + Name + "]";
		}
	}
}
