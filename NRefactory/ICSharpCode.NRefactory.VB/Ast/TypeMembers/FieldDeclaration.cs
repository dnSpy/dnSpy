// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
		#region Null
		public new static readonly VariableIdentifier Null = new NullVariableIdentifier();
		
		sealed class NullVariableIdentifier : VariableIdentifier
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		public static readonly Role<VariableIdentifier> VariableIdentifierRole = new Role<VariableIdentifier>("VariableIdentifier", VariableIdentifier.Null);
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public bool HasNullableSpecifier { get; set; }
		
		public AstNodeCollection<Expression> ArraySizeSpecifiers {
			get { return GetChildrenByRole (Roles.Argument); }
		}
		
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
