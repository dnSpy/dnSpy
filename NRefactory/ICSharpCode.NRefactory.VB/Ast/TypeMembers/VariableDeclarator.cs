// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <remarks>
	/// VariableIdentifiers As ObjectCreationExpression <br />
	/// VariableIdentifiers ( As TypeName )? ( Equals Expression )?
	/// </remarks>
	public abstract class VariableDeclarator : AstNode
	{
		public static readonly Role<VariableDeclarator> VariableDeclaratorRole = new Role<VariableDeclarator>("VariableDeclarator");
		
		public AstNodeCollection<VariableIdentifier> Identifiers {
			get { return GetChildrenByRole(VariableIdentifier.VariableIdentifierRole); }
		}
	}
	
	public class VariableDeclaratorWithTypeAndInitializer : VariableDeclarator
	{
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public Expression Initializer {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVariableDeclaratorWithTypeAndInitializer(this, data);
		}
	}
	
	public class VariableDeclaratorWithObjectCreation : VariableDeclarator
	{
		public static readonly Role<ObjectCreationExpression> InitializerRole = new Role<ObjectCreationExpression>("InitializerRole");
		
		public ObjectCreationExpression Initializer {
			get { return GetChildByRole(InitializerRole); }
			set { SetChildByRole(InitializerRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVariableDeclaratorWithObjectCreation(this, data);
		}
	}
}
