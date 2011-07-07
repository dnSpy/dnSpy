// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <remarks>
	/// Attributes? VariableModifier+ VariableDeclarators StatementTerminator
	/// </remarks>
	public class FieldDeclaration : MemberDeclaration
	{
		public AstNodeCollection<VariableDeclarator> Variables {
			get { return GetChildrenByRole(VariableDeclarator.VariableDeclaratorRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitFieldDeclaration(this, data);
		}
	}
	

	
	/// <remarks>
	/// Identifier IdentifierModifiers
	/// </remarks>
	public class VariableIdentifier : AstNode
	{
		public static readonly Role<VariableIdentifier> VariableIdentifierRole = new Role<VariableIdentifier>("VariableIdentifier");
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public bool HasNullableSpecifier { get; set; }
		
		public AstNodeCollection<ArraySpecifier> ArraySpecifiers {
			get { return GetChildrenByRole(ComposedType.ArraySpecifierRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVariableIdentifier(this, data);
		}
	}
}
