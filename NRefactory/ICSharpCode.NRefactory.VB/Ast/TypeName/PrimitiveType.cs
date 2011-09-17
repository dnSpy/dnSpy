// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class PrimitiveType : AstType
	{
		public string Keyword { get; set; }
		public TextLocation Location { get; set; }
		
		public PrimitiveType()
		{
		}
		
		public PrimitiveType(string keyword)
		{
			this.Keyword = keyword;
		}
		
		public PrimitiveType(string keyword, TextLocation location)
		{
			this.Keyword = keyword;
			this.Location = location;
		}
		
		public override TextLocation StartLocation {
			get {
				return Location;
			}
		}
		public override TextLocation EndLocation {
			get {
				return new TextLocation (Location.Line, Location.Column + (Keyword != null ? Keyword.Length : 0));
			}
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPrimitiveType(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			PrimitiveType o = other as PrimitiveType;
			return o != null && MatchString(this.Keyword, o.Keyword);
		}
		
		public override string ToString()
		{
			return Keyword ?? base.ToString();
		}
	}
}

