// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Base class for expressions.
	/// </summary>
	/// <remarks>
	/// This class is useful even though it doesn't provide any additional functionality:
	/// It can be used to communicate more information in APIs, e.g. "this subnode will always be an expression"
	/// </remarks>
	public abstract class Expression : AstNode
	{
		#region Null
		public new static readonly Expression Null = new NullExpression ();
		
		sealed class NullExpression : Expression
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitNullNode(this);
			}
			
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitNullNode(this);
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitNullNode(this, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		#region PatternPlaceholder
		public static implicit operator Expression(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : Expression, PatternMatching.INode
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
		
		public override NodeType NodeType {
			get {
				return NodeType.Expression;
			}
		}
		
		public new Expression Clone()
		{
			return (Expression)base.Clone();
		}

		public Expression ReplaceWith(Func<Expression, Expression> replaceFunction)
		{
			if (replaceFunction == null)
				throw new ArgumentNullException("replaceFunction");
			return (Expression)base.ReplaceWith(node => replaceFunction((Expression)node));
		}
		
		#region Builder methods
		/// <summary>
		/// Builds an member reference expression using this expression as target.
		/// </summary>
		public virtual MemberReferenceExpression Member(string memberName)
		{
			return new MemberReferenceExpression { Target = this, MemberName = memberName };
		}
		
		/// <summary>
		/// Builds an indexer expression using this expression as target.
		/// </summary>
		public virtual IndexerExpression Indexer(IEnumerable<Expression> arguments)
		{
			IndexerExpression expr = new IndexerExpression();
			expr.Target = this;
			expr.Arguments.AddRange(arguments);
			return expr;
		}
		
		/// <summary>
		/// Builds an indexer expression using this expression as target.
		/// </summary>
		public virtual IndexerExpression Indexer(params Expression[] arguments)
		{
			IndexerExpression expr = new IndexerExpression();
			expr.Target = this;
			expr.Arguments.AddRange(arguments);
			return expr;
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public virtual InvocationExpression Invoke(string methodName, IEnumerable<Expression> arguments)
		{
			return Invoke(methodName, null, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public virtual InvocationExpression Invoke(string methodName, params Expression[] arguments)
		{
			return Invoke(methodName, null, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public virtual InvocationExpression Invoke(string methodName, IEnumerable<AstType> typeArguments, IEnumerable<Expression> arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			MemberReferenceExpression mre = new MemberReferenceExpression();
			mre.Target = this;
			mre.MemberName = methodName;
			mre.TypeArguments.AddRange(typeArguments);
			ie.Target = mre;
			ie.Arguments.AddRange(arguments);
			return ie;
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public virtual InvocationExpression Invoke(IEnumerable<Expression> arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			ie.Target = this;
			ie.Arguments.AddRange(arguments);
			return ie;
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public virtual InvocationExpression Invoke(params Expression[] arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			ie.Target = this;
			ie.Arguments.AddRange(arguments);
			return ie;
		}
		
		public virtual CastExpression CastTo(AstType type)
		{
			return new CastExpression { Type = type,  Expression = this };
		}
		
		public virtual AsExpression CastAs(AstType type)
		{
			return new AsExpression { Type = type,  Expression = this };
		}
		
		public virtual IsExpression IsType(AstType type)
		{
			return new IsExpression { Type = type,  Expression = this };
		}
		#endregion
	}
}
