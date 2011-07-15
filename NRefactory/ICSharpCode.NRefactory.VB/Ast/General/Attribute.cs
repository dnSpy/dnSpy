// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class Attribute : AstNode
	{
		#region PatternPlaceholder
		public static implicit operator Attribute(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : Attribute, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder(PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitPatternPlaceholder(this, child, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return child.DoMatch(other, match);
			}
			
			bool PatternMatching.INode.DoMatchCollection(Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
			{
				return child.DoMatchCollection(role, pos, match, backtrackingInfo);
			}
		}
		#endregion
		
		public static readonly Role<Attribute> AttributeRole = new Role<Attribute>("Attribute");
		public static readonly Role<VBTokenNode> TargetRole = new Role<VBTokenNode>("Target", VBTokenNode.Null);
		
		public AttributeTarget Target { get; set; }
		
		public VBTokenNode TargetKeyword {
			get { return GetChildByRole(TargetRole); }
			set { SetChildByRole(TargetRole, value); }
		}
		
		public VBTokenNode ColonToken {
			get { return GetChildByRole(Roles.StatementTerminator); }
		}
		
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public VBTokenNode LParToken {
			get { return GetChildByRole(Roles.LPar); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole(Roles.Argument); }
		}
		
		public VBTokenNode RParToken {
			get { return GetChildByRole(Roles.RPar); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAttribute(this, data);
		}
		
//		public override string ToString()
//		{
//			return string.Format("[Attribute Type={0} Arguments={1}]", Type, GetCollectionString(Arguments));
//		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var node = other as Attribute;
			return node != null && node.Target == Target && node.TargetKeyword.DoMatch(this.TargetKeyword, match) && node.Type.DoMatch(this.Type, match) && node.Arguments.DoMatch(this.Arguments, match);
		}
	}
	
	public enum AttributeTarget
	{
		None,
		Assembly,
		Module
	}
}
