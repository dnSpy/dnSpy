// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Dom
{
	public class BlockStatement : Statement
	{
		// Children in VB: LabelStatement, EndStatement, Statement
		
		public static new BlockStatement Null {
			get {
				return NullBlockStatement.Instance;
			}
		}
		
		public override object AcceptVisitor(IDomVisitor visitor, object data)
		{
			return visitor.VisitBlockStatement(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[BlockStatement: Children={0}]",
			                     GetCollectionString(base.Children));
		}
	}
	
	internal sealed class NullBlockStatement : BlockStatement
	{
		public static readonly NullBlockStatement Instance = new NullBlockStatement();
		
		public override bool IsNull {
			get {
				return true;
			}
		}
		
		public override object AcceptVisitor(IDomVisitor visitor, object data)
		{
			return data;
		}
		public override object AcceptChildren(IDomVisitor visitor, object data)
		{
			return data;
		}
		public override void AddChild(INode childNode)
		{
			throw new InvalidOperationException();
		}
		
		public override string ToString()
		{
			return String.Format("[NullBlockStatement]");
		}
	}
}
