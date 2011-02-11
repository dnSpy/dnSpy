// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.Parser
{
	public abstract class LexerMemento
	{
		public int Line { get; set; }
		public int Column { get; set; }
		public int PrevTokenKind { get; set; }
	}
}
