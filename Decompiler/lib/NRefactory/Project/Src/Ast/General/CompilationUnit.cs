// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;

namespace ICSharpCode.NRefactory.Ast
{
	public class CompilationUnit : AbstractNode
	{
		// Children in C#: UsingAliasDeclaration, UsingDeclaration, AttributeSection, NamespaceDeclaration
		// Children in VB: OptionStatements, ImportsStatement, AttributeSection, NamespaceDeclaration
		
		Stack blockStack = new Stack();
		
		public CompilationUnit()
		{
			blockStack.Push(this);
		}
		
		public void BlockStart(INode block)
		{
			blockStack.Push(block);
		}
		
		public void BlockEnd()
		{
			blockStack.Pop();
		}
		
		public INode CurrentBock {
			get {
				return blockStack.Count > 0 ? (INode)blockStack.Peek() : null;
			}
		}
		
		public override void AddChild(INode childNode)
		{
			if (childNode != null) {
				INode parent = (INode)blockStack.Peek();
				parent.Children.Add(childNode);
				childNode.Parent = parent;
			}
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.VisitCompilationUnit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[CompilationUnit]");
		}
	}
}
