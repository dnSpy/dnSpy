// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.NRefactory.Ast
{
	public class BlockStatement : Statement
	{
		// Children in C#: LabelStatement, LocalVariableDeclaration, Statement
		// Children in VB: LabelStatement, EndStatement, Statement
		
		public static new BlockStatement Null {
			get {
				return NullBlockStatement.Instance;
			}
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
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
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return data;
		}
		public override object AcceptChildren(IAstVisitor visitor, object data)
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
