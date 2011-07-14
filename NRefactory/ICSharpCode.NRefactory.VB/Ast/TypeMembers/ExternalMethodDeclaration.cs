// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Runtime.InteropServices;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class ExternalMethodDeclaration : MemberDeclaration
	{
		public ExternalMethodDeclaration()
		{
		}
		
		public CharsetModifier CharsetModifier { get; set; }
		
		public bool IsSub { get; set; }
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public string Library { get; set; }
		
		public string Alias { get; set; }
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole(Roles.Parameter); }
		}
		
		public AstNodeCollection<AttributeBlock> ReturnTypeAttributes {
			get { return GetChildrenByRole(AttributeBlock.ReturnTypeAttributeBlockRole); }
		}
		
		public AstType ReturnType {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
				
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			// TODO : finish
			var method = other as ExternalMethodDeclaration;
			return method != null &&
				MatchAttributesAndModifiers(method, match) &&
				IsSub == method.IsSub &&
				Name.DoMatch(method.Name, match) &&
				Parameters.DoMatch(method.Parameters, match) &&
				ReturnTypeAttributes.DoMatch(method.ReturnTypeAttributes, match) &&
				ReturnType.DoMatch(method.ReturnType, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitExternalMethodDeclaration(this, data);
		}
	}
	
	///<summary>
	/// Charset types, used in external methods
	/// declarations (VB only).
	///</summary>
	public enum CharsetModifier
	{
		None,
		Auto,
		Unicode,
		Ansi
	}
}
