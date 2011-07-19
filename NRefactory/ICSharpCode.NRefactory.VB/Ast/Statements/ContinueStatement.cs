// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Continue ( Do | For | While )
	/// </summary>
	public class ContinueStatement : Statement
	{
		public static readonly Role<VBTokenNode> ContinueKindTokenRole = new Role<VBTokenNode>("ContinueKindToken");
		
		public ContinueKind ContinueKind { get; set; }
		
		public VBTokenNode ContinueToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public VBTokenNode ContinueKindToken {
			get { return GetChildByRole (ContinueKindTokenRole); }
		}
		
		public ContinueStatement(ContinueKind kind)
		{
			this.ContinueKind = kind;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitContinueStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ContinueStatement o = other as ContinueStatement;
			return o != null && this.ContinueKind == o.ContinueKind;
		}
	}
	
	public enum ContinueKind
	{
		None,
		Do,
		For,
		While
	}
}
