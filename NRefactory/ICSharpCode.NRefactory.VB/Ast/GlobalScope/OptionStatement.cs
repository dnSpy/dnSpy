// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class OptionStatement : AstNode
	{
		public static readonly Role<VBTokenNode> OptionTypeRole = new Role<VBTokenNode>("OptionType");
		public static readonly Role<VBTokenNode> OptionValueRole = new Role<VBTokenNode>("OptionValue");

		public VBTokenNode OptionKeyword {
			get { return GetChildByRole(Roles.Keyword); }
		}
		
		public VBTokenNode OptionTypeKeyword {
			get { return GetChildByRole(OptionTypeRole); }
		}
		
		public VBTokenNode OptionValueKeyword {
			get { return GetChildByRole(OptionValueRole); }
		}

		public OptionType OptionType { get; set; }

		public OptionValue OptionValue { get; set; }
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			var stmt = other as OptionStatement;
			return stmt != null && stmt.OptionType == this.OptionType
				&& stmt.OptionValue == this.OptionValue;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitOptionStatement(this, data);
		}
		
		public override string ToString() {
			return string.Format("[OptionStatement OptionType={0} OptionValue={1}]", OptionType, OptionValue);
		}
	}
	
	public enum OptionType
	{
		Explicit,
		Strict,
		Compare,
		Infer
	}
	
	public enum OptionValue
	{
		On, Off,
		Text, Binary
	}
}
