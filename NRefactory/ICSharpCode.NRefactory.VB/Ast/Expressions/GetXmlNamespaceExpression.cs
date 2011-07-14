// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class GetXmlNamespaceExpression : Expression
	{
		public GetXmlNamespaceExpression(XmlIdentifier namespaceName)
		{
			SetChildByRole(Roles.XmlIdentifier, namespaceName);
		}
		
		public XmlIdentifier NamespaceName {
			get { return GetChildByRole(Roles.XmlIdentifier); }
			set { SetChildByRole(Roles.XmlIdentifier, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var expr = other as GetXmlNamespaceExpression;
			return expr != null &&
				NamespaceName.DoMatch(expr.NamespaceName, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGetXmlNamespaceExpression(this, data);
		}
	}
}
