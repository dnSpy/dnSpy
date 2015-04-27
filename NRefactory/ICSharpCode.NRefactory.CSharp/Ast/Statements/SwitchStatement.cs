// 
// SwitchStatement.cs
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
	/// switch (Expression) { SwitchSections }
	/// </summary>
	public class SwitchStatement : Statement
	{
		public static readonly TokenRole SwitchKeywordRole = new TokenRole ("switch");
		public static readonly Role<SwitchSection> SwitchSectionRole = new Role<SwitchSection>("SwitchSection");
		
		public CSharpTokenNode SwitchToken {
			get { return GetChildByRole (SwitchKeywordRole); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public AstNodeCollection<SwitchSection> SwitchSections {
			get { return GetChildrenByRole (SwitchSectionRole); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitSwitchStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitSwitchStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSwitchStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			SwitchStatement o = other as SwitchStatement;
			return o != null && this.Expression.DoMatch(o.Expression, match) && this.SwitchSections.DoMatch(o.SwitchSections, match);
		}
	}
	
	public class SwitchSection : AstNode
	{
		#region PatternPlaceholder
		public static implicit operator SwitchSection(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : SwitchSection, PatternMatching.INode
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
		
		public static readonly Role<CaseLabel> CaseLabelRole = new Role<CaseLabel>("CaseLabel");
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public AstNodeCollection<CaseLabel> CaseLabels {
			get { return GetChildrenByRole (CaseLabelRole); }
		}
		
		public AstNodeCollection<Statement> Statements {
			get { return GetChildrenByRole (Roles.EmbeddedStatement); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitSwitchSection (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitSwitchSection (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSwitchSection (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			SwitchSection o = other as SwitchSection;
			return o != null && this.CaseLabels.DoMatch(o.CaseLabels, match) && this.Statements.DoMatch(o.Statements, match);
		}
	}
	
	public class CaseLabel : AstNode
	{
		public static readonly TokenRole CaseKeywordRole = new TokenRole ("case");
		public static readonly TokenRole DefaultKeywordRole = new TokenRole ("default");
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		/// <summary>
		/// Gets or sets the expression. The expression can be null - if the expression is null, it's the default switch section.
		/// </summary>
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}

		public CSharpTokenNode ColonToken {
			get { return GetChildByRole (Roles.Colon); }
		}

		public CaseLabel ()
		{
		}
		
		public CaseLabel (Expression expression)
		{
			this.Expression = expression;
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitCaseLabel (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitCaseLabel (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCaseLabel (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CaseLabel o = other as CaseLabel;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
