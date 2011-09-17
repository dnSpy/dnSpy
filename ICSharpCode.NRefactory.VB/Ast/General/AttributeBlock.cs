// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class AttributeBlock : AstNode
	{
		public readonly static Role<AttributeBlock> AttributeBlockRole = new Role<AttributeBlock>("AttributeBlock");
		public readonly static Role<AttributeBlock> ReturnTypeAttributeBlockRole = new Role<AttributeBlock>("ReturnTypeAttributeBlock");
		
		public VBTokenNode LChevron {
			get { return GetChildByRole(Roles.LChevron); }
		}
		
		public AstNodeCollection<Attribute> Attributes {
			get { return GetChildrenByRole(Attribute.AttributeRole); }
		}
		
		public VBTokenNode RChevron {
			get { return GetChildByRole(Roles.RChevron); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var node = other as AttributeBlock;
			return node != null && Attributes.DoMatch(node.Attributes, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAttributeBlock(this, data);
		}
	}
}
