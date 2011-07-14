// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public abstract class ImportsClause : AstNode
	{
		public new static readonly ImportsClause Null = new NullImportsClause();
		
		class NullImportsClause : ImportsClause
		{
			public override bool IsNull {
				get { return true; }
			}
			
			protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
			{
				return other != null && other.IsNull;
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return default(S);
			}
		}
	}
	
	public class AliasImportsClause : ImportsClause
	{
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstType Alias {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var clause = other as AliasImportsClause;
			return clause != null
				&& Name.DoMatch(clause.Name, match)
				&& Alias.DoMatch(clause.Alias, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAliasImportsClause(this, data);
		}
		
		public override string ToString() {
			return string.Format("[AliasImportsClause Name={0} Alias={1}]", Name, Alias);
		}
	}
	
	public class MemberImportsClause : ImportsClause
	{
		public AstType Member {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var node = other as MemberImportsClause;
			return node != null
				&& Member.DoMatch(node.Member, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitMemberImportsClause(this, data);
		}
		
		public override string ToString()
		{
			return string.Format("[MemberImportsClause Member={0}]", Member);
		}
	}
	
	public class XmlNamespaceImportsClause : ImportsClause
	{
		public XmlIdentifier Prefix {
			get { return GetChildByRole(Roles.XmlIdentifier); }
			set { SetChildByRole(Roles.XmlIdentifier, value); }
		}
		
		public XmlLiteralString Namespace {
			get { return GetChildByRole(Roles.XmlLiteralString); }
			set { SetChildByRole(Roles.XmlLiteralString, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var clause = other as XmlNamespaceImportsClause;
			return clause != null && Namespace.DoMatch(clause.Namespace, match) && Prefix.DoMatch(clause.Prefix, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitXmlNamespaceImportsClause(this, data);
		}
		
		public override string ToString()
		{
			return string.Format("[XmlNamespaceImportsClause Prefix={0}, Namespace={1}]", Prefix, Namespace);
		}
	}
}
