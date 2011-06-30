// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// ( Dim | Static | Const ) VariableDeclarator { , VariableDeclarator }
	/// </summary>
	public class LocalDeclarationStatement : Statement
	{
		public AstNodeCollection<VariableDeclarator> Variables {
			get { return GetChildrenByRole(VariableDeclarator.VariableDeclaratorRole); }
		}
		
		public Modifiers Modifiers {
			get { return AttributedNode.GetModifiers(this); }
			set { AttributedNode.SetModifiers(this, value); }
		}
		
		public VBModifierToken ModifierToken {
			get { return GetChildByRole(AttributedNode.ModifierRole); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitLocalDeclarationStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
}
