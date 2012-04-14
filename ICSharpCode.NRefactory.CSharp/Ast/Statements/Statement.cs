// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Base class for statements.
	/// </summary>
	/// <remarks>
	/// This class is useful even though it doesn't provide any additional functionality:
	/// It can be used to communicate more information in APIs, e.g. "this subnode will always be a statement"
	/// </remarks>
	public abstract class Statement : AstNode
	{
		#region Null
		public new static readonly Statement Null = new NullStatement ();
		
		sealed class NullStatement : Statement
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
			}
				
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return default (T);
			}

			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		#region PatternPlaceholder
		public static implicit operator Statement(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : Statement, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder(PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override NodeType NodeType {
				get { return NodeType.Pattern; }
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitPatternPlaceholder(this, child);
			}
				
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitPatternPlaceholder(this, child);
			}

			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitPatternPlaceholder(this, child, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return child.DoMatch(other, match);
			}
			
			bool PatternMatching.INode.DoMatchCollection(Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
			{
				return child.DoMatchCollection(role, pos, match, backtrackingInfo);
			}
		}
		#endregion
		
		public new Statement Clone()
		{
			return (Statement)base.Clone();
		}
		
		public Statement ReplaceWith(Func<Statement, Statement> replaceFunction)
		{
			if (replaceFunction == null)
				throw new ArgumentNullException("replaceFunction");
			return (Statement)base.ReplaceWith(node => replaceFunction((Statement)node));
		}
		
		public override NodeType NodeType {
			get { return NodeType.Statement; }
		}
		
		// Make debugging easier by giving Statements a ToString() implementation
		public override string ToString()
		{
			return DebugToString();
		}
	}
}
