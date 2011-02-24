// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Base class for all patterns.
	/// </summary>
	public abstract class Pattern : AstNode
	{
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		internal struct PossibleMatch
		{
			public readonly AstNode NextOther; // next node after the last matched node
			public readonly int Checkpoint; // checkpoint
			
			public PossibleMatch(AstNode nextOther, int checkpoint)
			{
				this.NextOther = nextOther;
				this.Checkpoint = checkpoint;
			}
		}
		
		internal virtual bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<PossibleMatch> backtrackingStack)
		{
			return DoMatch(pos, match);
		}
		
		public AstType ToType()
		{
			return new TypePlaceholder(this);
		}
		
		public Expression ToExpression()
		{
			return new ExpressionPlaceholder(this);
		}
		
		public Statement ToStatement()
		{
			return new StatementPlaceholder(this);
		}
		
		public BlockStatement ToBlock()
		{
			return new BlockStatementPlaceholder(this);
		}
		
		public VariableInitializer ToVariable()
		{
			return new VariablePlaceholder(this);
		}
		
		// Make debugging easier by giving Patterns a ToString() implementation
		public override string ToString()
		{
			StringWriter w = new StringWriter();
			AcceptVisitor(new OutputVisitor(w, new CSharpFormattingPolicy()), null);
			return w.ToString();
		}
	}
}
