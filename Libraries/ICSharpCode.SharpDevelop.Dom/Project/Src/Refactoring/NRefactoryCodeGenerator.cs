// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public abstract class NRefactoryCodeGenerator : CodeGenerator
	{
		public abstract IOutputAstVisitor CreateOutputVisitor();
		
		public override string GenerateCode(AbstractNode node, string indentation)
		{
			IOutputAstVisitor visitor = CreateOutputVisitor();
			int indentCount = 0;
			foreach (char c in indentation) {
				if (c == '\t')
					indentCount += 4;
				else
					indentCount += 1;
			}
			visitor.OutputFormatter.IndentationLevel = indentCount / 4;
			if (node is Statement)
				visitor.OutputFormatter.Indent();
			node.AcceptVisitor(visitor, null);
			string text = visitor.Text;
			if (node is Statement && !text.EndsWith("\n"))
				text += Environment.NewLine;
			return text;
		}
	}
}
