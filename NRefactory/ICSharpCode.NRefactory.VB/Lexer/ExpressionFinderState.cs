// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.VB.Parser
{
	public class ExpressionFinderState
	{
		public bool WasQualifierTokenAtStart { get; set; }
		public bool NextTokenIsPotentialStartOfExpression { get; set; }
		public bool ReadXmlIdentifier { get; set; }
		public bool IdentifierExpected { get; set; }
		public bool NextTokenIsStartOfImportsOrAccessExpression { get; set; }
		public Stack<int> StateStack { get; set; }
		public Stack<Block> BlockStack { get; set; }
		public int CurrentState { get; set; }
	}
}
