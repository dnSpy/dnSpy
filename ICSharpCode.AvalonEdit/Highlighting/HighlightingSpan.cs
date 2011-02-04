// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " Start=" + StartExpression + ", End=" + EndExpression + "]";
		}
	}
}
