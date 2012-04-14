// 
// GotoStatement.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// "goto Label;"
	/// </summary>
	public class GotoStatement : Statement
	{
		public static readonly TokenRole GotoKeywordRole = new TokenRole ("goto");
		
		public GotoStatement ()
		{
		}
		
		public GotoStatement (string label)
		{
			this.Label = label;
		}
		
		public CSharpTokenNode GotoToken {
			get { return GetChildByRole (GotoKeywordRole); }
		}
		
		public string Label {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				if (string.IsNullOrEmpty(value))
					SetChildByRole(Roles.Identifier, null);
				else
					SetChildByRole(Roles.Identifier, Identifier.Create (value));
			}
		}
		
		public CSharpTokenNode SemicolonToken {
			get { return GetChildByRole (Roles.Semicolon); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitGotoStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitGotoStatement (this);
		}

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGotoStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			GotoStatement o = other as GotoStatement;
			return o != null && MatchString(this.Label, o.Label);
		}
	}
	
	/// <summary>
	/// or "goto case LabelExpression;"
	/// </summary>
	public class GotoCaseStatement : Statement
	{
		public static readonly TokenRole GotoKeywordRole = new TokenRole ("goto");
		public static readonly TokenRole CaseKeywordRole = new TokenRole ("case");
		
		public CSharpTokenNode GotoToken {
			get { return GetChildByRole (GotoKeywordRole); }
		}
		
		public CSharpTokenNode CaseToken {
			get { return GetChildByRole (CaseKeywordRole); }
		}
		
		/// <summary>
		/// Used for "goto case LabelExpression;"
		/// </summary>
		public Expression LabelExpression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public CSharpTokenNode SemicolonToken {
			get { return GetChildByRole (Roles.Semicolon); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitGotoCaseStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitGotoCaseStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGotoCaseStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			GotoCaseStatement o = other as GotoCaseStatement;
			return o != null && this.LabelExpression.DoMatch(o.LabelExpression, match);
		}
	}
	
	/// <summary>
	/// or "goto default;"
	/// </summary>
	public class GotoDefaultStatement : Statement
	{
		public static readonly TokenRole GotoKeywordRole = new TokenRole ("goto");
		public static readonly TokenRole DefaultKeywordRole = new TokenRole ("default");
		
		public CSharpTokenNode GotoToken {
			get { return GetChildByRole (GotoKeywordRole); }
		}
		
		public CSharpTokenNode DefaultToken {
			get { return GetChildByRole (DefaultKeywordRole); }
		}
		
		public CSharpTokenNode SemicolonToken {
			get { return GetChildByRole (Roles.Semicolon); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitGotoDefaultStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitGotoDefaultStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGotoDefaultStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			GotoDefaultStatement o = other as GotoDefaultStatement;
			return o != null;
		}
	}
}
