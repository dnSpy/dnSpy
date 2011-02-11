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
		public static readonly Role<SwitchSection> SwitchSectionRole = new Role<SwitchSection>("SwitchSection");
		
		public CSharpTokenNode SwitchToken {
			get { return GetChildByRole (Roles.Keyword); }
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
		
		public IEnumerable<SwitchSection> SwitchSections {
			get { return GetChildrenByRole (SwitchSectionRole); }
			set { SetChildrenByRole (SwitchSectionRole, value); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSwitchStatement (this, data);
		}
	}
	
	public class SwitchSection : AstNode
	{
		public static readonly Role<CaseLabel> CaseLabelRole = new Role<CaseLabel>("CaseLabel");
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public IEnumerable<CaseLabel> CaseLabels {
			get { return GetChildrenByRole (CaseLabelRole); }
			set { SetChildrenByRole (CaseLabelRole, value); }
		}
		
		public IEnumerable<Statement> Statements {
			get { return GetChildrenByRole (Roles.EmbeddedStatement); }
			set { SetChildrenByRole (Roles.EmbeddedStatement, value); }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSwitchSection (this, data);
		}
	}
	
	public class CaseLabel : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCaseLabel (this, data);
		}
	}
}
