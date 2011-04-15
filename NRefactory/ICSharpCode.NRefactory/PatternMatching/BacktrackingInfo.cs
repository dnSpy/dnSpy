// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Container for the backtracking info.
	/// </summary>
	public class BacktrackingInfo
	{
		internal Stack<Pattern.PossibleMatch> backtrackingStack = new Stack<Pattern.PossibleMatch>();
	}
}
