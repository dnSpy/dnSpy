// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
