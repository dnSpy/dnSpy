// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Label:
	/// </summary>
	public class LabelDeclarationStatement : Statement
	{
		public Identifier Label {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public VBTokenNode Colon {
			get { return GetChildByRole(Roles.Colon); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitLabelDeclarationStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			LabelDeclarationStatement o = other as LabelDeclarationStatement;
			return o != null && MatchString(this.Label.Name, o.Label.Name);
		}
	}
}
