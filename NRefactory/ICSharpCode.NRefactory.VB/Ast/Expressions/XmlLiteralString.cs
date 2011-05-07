// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class XmlLiteralString : AstNode
	{
		public static readonly new XmlLiteralString Null = new XmlLiteralString();
		
		class NullXmlLiteralString : XmlLiteralString
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default(S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		
		public string Value { get; set; }
		
		AstLocation startLocation;
		public override AstLocation StartLocation {
			get { return startLocation; }
		}
		
		AstLocation endLocation;
		public override AstLocation EndLocation {
			get { return endLocation; }
		}
		
		private XmlLiteralString()
		{
			this.Value = string.Empty;
		}
		
		public XmlLiteralString(string value, AstLocation startLocation, AstLocation endLocation)
		{
			this.Value = value;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var ident = other as XmlLiteralString;
			return ident != null
				&& MatchStringXml(Value, ident.Value)
				&& ident.startLocation == startLocation
				&& ident.endLocation == endLocation;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitXmlLiteralString(this, data);
		}
	}
}
