// 
// VariableDeclarationStatement.cs
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
	public class VariableDeclarationStatement : Statement
	{
		public static readonly Role<CSharpModifierToken> ModifierRole = EntityDeclaration.ModifierRole;
		
		public VariableDeclarationStatement()
		{
		}
		
		public VariableDeclarationStatement(AstType type, string name, Expression initializer = null)
		{
			this.Type = type;
			this.Variables.Add(new VariableInitializer(name, initializer));
		}
		
		public Modifiers Modifiers {
			get { return EntityDeclaration.GetModifiers(this); }
			set { EntityDeclaration.SetModifiers(this, value); }
		}
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public AstNodeCollection<VariableInitializer> Variables {
			get { return GetChildrenByRole (Roles.Variable); }
		}
		
		public CSharpTokenNode SemicolonToken {
			get { return GetChildByRole (Roles.Semicolon); }
		}
		
		public VariableInitializer GetVariable (string name)
		{
			return Variables.FirstOrNullObject (vi => vi.Name == name);
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitVariableDeclarationStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitVariableDeclarationStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVariableDeclarationStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			VariableDeclarationStatement o = other as VariableDeclarationStatement;
			return o != null && this.Modifiers == o.Modifiers && this.Type.DoMatch(o.Type, match) && this.Variables.DoMatch(o.Variables, match);
		}
	}
}
