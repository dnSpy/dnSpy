// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Identifier As Type = Expression
	/// </summary>
	public class VariableInitializer : AstNode
	{
		public VariableIdentifier Identifier {
			get { return GetChildByRole(VariableIdentifier.VariableIdentifierRole); }
			set { SetChildByRole(VariableIdentifier.VariableIdentifierRole, value); }
		}
		
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public VBTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVariableInitializer(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			VariableInitializer o = other as VariableInitializer;
			return o != null && this.Identifier.DoMatch(o.Identifier, match) && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
