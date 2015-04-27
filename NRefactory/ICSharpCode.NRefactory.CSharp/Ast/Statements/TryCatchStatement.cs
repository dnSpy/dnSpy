// 
// TryCatchStatement.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// try TryBlock CatchClauses finally FinallyBlock
	/// </summary>
	public class TryCatchStatement : Statement
	{
		public static readonly TokenRole TryKeywordRole = new TokenRole ("try");
		public static readonly Role<BlockStatement> TryBlockRole = new Role<BlockStatement>("TryBlock", BlockStatement.Null);
		public static readonly Role<CatchClause> CatchClauseRole = new Role<CatchClause>("CatchClause", CatchClause.Null);
		public static readonly TokenRole FinallyKeywordRole = new TokenRole ("finally");
		public static readonly Role<BlockStatement> FinallyBlockRole = new Role<BlockStatement>("FinallyBlock", BlockStatement.Null);
		
		public CSharpTokenNode TryToken {
			get { return GetChildByRole (TryKeywordRole); }
		}
		
		public BlockStatement TryBlock {
			get { return GetChildByRole (TryBlockRole); }
			set { SetChildByRole (TryBlockRole, value); }
		}
		
		public AstNodeCollection<CatchClause> CatchClauses {
			get { return GetChildrenByRole (CatchClauseRole); }
		}
		
		public CSharpTokenNode FinallyToken {
			get { return GetChildByRole (FinallyKeywordRole); }
		}
		
		public BlockStatement FinallyBlock {
			get { return GetChildByRole (FinallyBlockRole); }
			set { SetChildByRole (FinallyBlockRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitTryCatchStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitTryCatchStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTryCatchStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			TryCatchStatement o = other as TryCatchStatement;
			return o != null && this.TryBlock.DoMatch(o.TryBlock, match) && this.CatchClauses.DoMatch(o.CatchClauses, match) && this.FinallyBlock.DoMatch(o.FinallyBlock, match);
		}
	}
	
	/// <summary>
	/// catch (Type VariableName) { Body }
	/// </summary>
	public class CatchClause : AstNode
	{
		public static readonly TokenRole CatchKeywordRole = new TokenRole ("catch");

		#region Null
		public new static readonly CatchClause Null = new NullCatchClause ();
		
		sealed class NullCatchClause : CatchClause
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
		public static implicit operator CatchClause(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : CatchClause, PatternMatching.INode
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
				return NodeType.Unknown;
			}
		}
		
		public CSharpTokenNode CatchToken {
			get { return GetChildByRole (CatchKeywordRole); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public string VariableName {
			get { return GetChildByRole (Roles.Identifier).Name; }
			set {
				if (string.IsNullOrEmpty(value))
					SetChildByRole (Roles.Identifier, null);
				else
					SetChildByRole (Roles.Identifier, Identifier.Create (value));
			}
		}
		
		public Identifier VariableNameToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole(Roles.Identifier, value);
			}
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole (Roles.Body); }
			set { SetChildByRole (Roles.Body, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitCatchClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitCatchClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCatchClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CatchClause o = other as CatchClause;
			return o != null && this.Type.DoMatch(o.Type, match) && MatchString(this.VariableName, o.VariableName) && this.Body.DoMatch(o.Body, match);
		}
	}
}
