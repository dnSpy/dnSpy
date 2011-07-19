// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Parser
{
	public sealed class VBLexerMemento
	{
		public int Line { get; set; }
		public int Column { get; set; }
		public int PrevTokenKind { get; set; }
		public bool LineEnd { get; set; }
		public bool IsAtLineBegin { get; set; }
		public bool MisreadExclamationMarkAsTypeCharacter { get; set; }
		public bool EncounteredLineContinuation { get; set; }
		public ExpressionFinderState ExpressionFinder { get; set; }
		public Stack<XmlModeInfo> XmlModeInfoStack { get; set; }
		public bool InXmlMode { get; set; }
	}
}
