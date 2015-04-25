// 
// ArrayInitializerExpression.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// { Elements }
	/// </summary>
	public class ArrayInitializerExpression : Expression
	{
		/// <summary>
		/// For ease of use purposes in the resolver the ast representation
		/// of { a, b, c }  is { {a}, {b}, {c} }.
		/// If IsSingleElement is true then this array initializer expression is a generated one.
		/// That has no meaning in the source code (and contains no brace tokens).
		/// </summary>
		public virtual bool IsSingleElement {
			get {
				return false;
			}
		}

		public ArrayInitializerExpression()
		{
		}
		
		public ArrayInitializerExpression(IEnumerable<Expression> elements)
		{
			this.Elements.AddRange(elements);
		}
		
		public ArrayInitializerExpression(params Expression[] elements)
		{
			this.Elements.AddRange(elements);
		}
		
		#region Null
		public new static readonly ArrayInitializerExpression Null = new NullArrayInitializerExpression ();
		
		sealed class NullArrayInitializerExpression : ArrayInitializerExpression
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
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public AstNodeCollection<Expression> Elements {
			get { return GetChildrenByRole(Roles.Expression); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitArrayInitializerExpression (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitArrayInitializerExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitArrayInitializerExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ArrayInitializerExpression o = other as ArrayInitializerExpression;
			return o != null && this.Elements.DoMatch(o.Elements, match);
		}

		public static ArrayInitializerExpression CreateSingleElementInitializer ()
		{
			return new SingleArrayInitializerExpression();
		}
		/// <summary>
		/// Single elements in array initializers are represented with this special class.
		/// </summary>
		class SingleArrayInitializerExpression : ArrayInitializerExpression
		{
			public override bool IsSingleElement {
				get {
					return true;
				}
			}
			
		}
		
		#region PatternPlaceholder
		public static implicit operator ArrayInitializerExpression(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : ArrayInitializerExpression, PatternMatching.INode
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
	}
}

