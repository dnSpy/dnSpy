// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// &lt;Import&gt; element.
	/// </summary>
	[Serializable]
	public class XshdImport : XshdElement
	{
		/// <summary>
		/// Gets/sets the referenced rule set.
		/// </summary>
		public XshdReference<XshdRuleSet> RuleSetReference { get; set; }
		
		/// <inheritdoc/>
		public override object AcceptVisitor(IXshdVisitor visitor)
		{
			return visitor.VisitImport(this);
		}
	}
}
