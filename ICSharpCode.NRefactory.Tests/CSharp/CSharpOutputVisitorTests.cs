// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp
{
	[TestFixture]
	public class CSharpOutputVisitorTests
	{
		void AssertOutput(string expected, AstNode node, CSharpFormattingOptions policy = null)
		{
			if (policy == null)
				policy = new CSharpFormattingOptions();
			StringWriter w = new StringWriter();
			w.NewLine = "\n";
			node.AcceptVisitor(new CSharpOutputVisitor(new TextWriterOutputFormatter(w) { IndentationString = "$" }, policy), null);
			Assert.AreEqual(expected.Replace("\r", ""), w.ToString());
		}
		
		[Test]
		public void AssignmentInCollectionInitializer()
		{
			Expression expr = new ObjectCreateExpression {
				Type = new SimpleType("List"),
				Initializer = new ArrayInitializerExpression(
					new ArrayInitializerExpression(
						new AssignmentExpression(new IdentifierExpression("a"), new PrimitiveExpression(1))
					)
				)
			};
			
			AssertOutput("new List {\n${\n$$a = 1\n$}\n}", expr);
		}
		
		[Test]
		public void EnumDeclarationWithInitializers()
		{
			TypeDeclaration type = new TypeDeclaration {
				ClassType = ClassType.Enum,
				Name = "DisplayFlags",
				Members = {
					new EnumMemberDeclaration {
						Name = "D",
						Initializer = new PrimitiveExpression(4)
					}
				}};
			
			AssertOutput("enum DisplayFlags\n{\n$D = 4\n}\n", type);
		}
		
		[Test]
		public void InlineCommentAtEndOfCondition()
		{
			IfElseStatement condition = new IfElseStatement();
			condition.AddChild(new CSharpTokenNode(new TextLocation(1, 1), 2), IfElseStatement.IfKeywordRole);
			condition.AddChild(new CSharpTokenNode(new TextLocation(1, 4), 1), IfElseStatement.Roles.LPar);
			condition.AddChild(new IdentifierExpression("cond", new TextLocation(1, 5)), IfElseStatement.ConditionRole);
			condition.AddChild(new Comment(CommentType.MultiLine, new TextLocation(1, 9), new TextLocation(1, 14)) { Content = "a" }, IfElseStatement.Roles.Comment);
			condition.AddChild(new CSharpTokenNode(new TextLocation(1, 14), 1), IfElseStatement.Roles.RPar);
			condition.AddChild(new ReturnStatement(), IfElseStatement.TrueRole);
			
			AssertOutput("if (cond/*a*/)\n$return;\n", condition);
		}
	}
}
