// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class EventDeclaration : MemberDeclaration
	{
		public bool IsCustom { get; set; }
		
		public static readonly Role<Accessor> AddHandlerRole = new Role<Accessor>("AddHandler", Accessor.Null);
		public static readonly Role<Accessor> RemoveHandlerRole = new Role<Accessor>("RemoveHandler", Accessor.Null);
		public static readonly Role<Accessor> RaiseEventRole = new Role<Accessor>("RaiseEvent", Accessor.Null);
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstType ReturnType {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole(Roles.Parameter); }
		}
		
		public AstNodeCollection<InterfaceMemberSpecifier> ImplementsClause {
			get { return GetChildrenByRole(InterfaceMemberSpecifier.InterfaceMemberSpecifierRole); }
		}
		
		public Accessor AddHandlerBlock {
			get { return GetChildByRole(AddHandlerRole); }
			set { SetChildByRole(AddHandlerRole, value); }
		}
		
		public Accessor RemoveHandlerBlock {
			get { return GetChildByRole(RemoveHandlerRole); }
			set { SetChildByRole(RemoveHandlerRole, value); }
		}
		
		public Accessor RaiseEventBlock {
			get { return GetChildByRole(RaiseEventRole); }
			set { SetChildByRole(RaiseEventRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitEventDeclaration(this, data);
		}
	}
}
