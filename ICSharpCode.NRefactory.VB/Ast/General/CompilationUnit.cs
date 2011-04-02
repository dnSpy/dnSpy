// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class CompilationUnit : AbstractNode
	{
		// Children in C#: UsingAliasDeclaration, UsingDeclaration, AttributeSection, NamespaceDeclaration
		// Children in VB: OptionStatements, ImportsStatement, AttributeSection, NamespaceDeclaration
		
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
