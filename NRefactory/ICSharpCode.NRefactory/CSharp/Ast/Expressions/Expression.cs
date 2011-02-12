// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

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
			
			public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
			{
				return default (S);
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
		public MemberReferenceExpression Member(string memberName)
		{
			return new MemberReferenceExpression { Target = this, MemberName = memberName };
		}
		
		/// <summary>
		/// Builds an indexer expression using this expression as target.
		/// </summary>
		public IndexerExpression Indexer(IEnumerable<Expression> arguments)
		{
			return new IndexerExpression { Target = this, Arguments = arguments };
		}
		
		/// <summary>
		/// Builds an indexer expression using this expression as target.
		/// </summary>
		public IndexerExpression Indexer(params Expression[] arguments)
		{
			return new IndexerExpression { Target = this, Arguments = arguments };
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, IEnumerable<Expression> arguments)
		{
			return Invoke(methodName, null, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, params Expression[] arguments)
		{
			return Invoke(methodName, null, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this expression as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, IEnumerable<AstType> typeArguments, IEnumerable<Expression> arguments)
		{
			return new InvocationExpression {
				Target = new MemberReferenceExpression {
					Target = this,
					MemberName = methodName,
					TypeArguments = typeArguments
				},
				Arguments = arguments
			};
		}
		
		public CastExpression CastTo(AstType type)
		{
			return new CastExpression { Type = type,  Expression = this };
		}
		
		public AsExpression CastAs(AstType type)
		{
			return new AsExpression { Type = type,  Expression = this };
		}
		
		public IsExpression IsType(AstType type)
		{
			return new IsExpression { Type = type,  Expression = this };
		}
		#endregion
	}
}
