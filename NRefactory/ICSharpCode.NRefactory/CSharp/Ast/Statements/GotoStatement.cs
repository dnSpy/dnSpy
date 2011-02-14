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
	/// or "goto case LabelExpression;"
	/// or "goto default;"
	/// </summary>
	public class GotoStatement : Statement
	{
		public static readonly Role<CSharpTokenNode> DefaultKeywordRole = new Role<CSharpTokenNode>("DefaultKeyword", CSharpTokenNode.Null);
		public static readonly Role<CSharpTokenNode> CaseKeywordRole = new Role<CSharpTokenNode>("CaseKeyword", CSharpTokenNode.Null);
		
		public GotoType GotoType {
			get;
			set;
		}
		
		public CSharpTokenNode GotoToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public string Label {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				if (string.IsNullOrEmpty(value))
					SetChildByRole(Roles.Identifier, null);
				else
					SetChildByRole(Roles.Identifier, new Identifier(value, AstLocation.Empty));
			}
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
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGotoStatement (this, data);
		}
		
		public GotoStatement ()
		{
		}
		
		public GotoStatement (string label)
		{
			this.Label = label;
		}
	}
	
	public enum GotoType {
		Label,
		Case,
		CaseDefault
	}
}
