// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// A visitor over the XSHD element tree.
	/// </summary>
	public interface IXshdVisitor
	{
		/// <summary/>
		object VisitRuleSet(XshdRuleSet ruleSet);
		
		/// <summary/>
		object VisitColor(XshdColor color);
		
		/// <summary/>
		object VisitKeywords(XshdKeywords keywords);
		
		/// <summary/>
		object VisitSpan(XshdSpan span);
		
		/// <summary/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "A VB programmer implementing a visitor?")]
		object VisitImport(XshdImport import);
		
		/// <summary/>
		object VisitRule(XshdRule rule);
	}
}
