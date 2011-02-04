// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Text.RegularExpressions;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A highlighting rule.
	/// </summary>
	[Serializable]
	public class HighlightingRule
	{
		/// <summary>
		/// Gets/Sets the regular expression for the rule.
		/// </summary>
		public Regex Regex { get; set; }
		
		/// <summary>
		/// Gets/Sets the highlighting color.
		/// </summary>
		public HighlightingColor Color { get; set; }
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " " + Regex + "]";
		}
	}
}
