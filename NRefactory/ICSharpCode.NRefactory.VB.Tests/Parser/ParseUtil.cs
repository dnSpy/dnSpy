// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	public class ParseUtil
	{
		public static T ParseGlobal<T>(string code, bool expectErrors = false) where T : AstNode
		{
			VBParser parser = new VBParser();
			CompilationUnit cu = parser.Parse(new StringReader(code));
			
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			
			AstNode node = cu.Children.Single();
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(node.GetType()), String.Format("Parsed node was {0} instead of {1} ({2})", node.GetType(), type, node));
			return (T)node;
		}
		
		public static void AssertGlobal(string code, AstNode expectedNode)
		{
			var node = ParseGlobal<AstNode>(code);
			if (!expectedNode.IsMatch(node)) {
				Assert.Fail("Expected '{0}' but was '{1}'", ToVB(expectedNode), ToVB(node));
			}
		}
		
//		public static T ParseStatement<T>(string stmt, bool expectErrors = false) where T : AstNode
//		{
//			VBParser parser = new VBParser();
//			var statements = parser.ParseStatements(new StringReader(stmt));
//			
//			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
//			
//			AstNode statement = statements.Single();
//			Type type = typeof(T);
//			Assert.IsTrue(type.IsAssignableFrom(statement.GetType()), String.Format("Parsed statement was {0} instead of {1} ({2})", statement.GetType(), type, statement));
//			return (T)statement;
//		}
//		
//		public static void AssertStatement(string code, VB.Ast.Statement expectedStmt)
//		{
//			var stmt = ParseStatement<VB.Ast.Statement>(code);
//			if (!expectedStmt.IsMatch(stmt)) {
//				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedStmt), ToCSharp(stmt));
//			}
//		}
//		
//		public static T ParseExpression<T>(string expr, bool expectErrors = false) where T : AstNode
//		{
//			VBParser parser = new VBParser();
//			AstNode parsedExpression = parser.ParseExpression(new StringReader(expr));
//			
//			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
//			if (expectErrors && parsedExpression == null)
//				return default (T);
//			Type type = typeof(T);
//			Assert.IsTrue(type.IsAssignableFrom(parsedExpression.GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", parsedExpression.GetType(), type, parsedExpression));
//			return (T)parsedExpression;
//		}
//		
//		public static void AssertExpression(string code, VB.Ast.Expression expectedExpr)
//		{
//			var expr = ParseExpression<CSharp.Expression>(code);
//			if (!expectedExpr.IsMatch(expr)) {
//				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedExpr), ToCSharp(expr));
//			}
//		}
//		
//		public static T ParseTypeMember<T>(string expr, bool expectErrors = false) where T : AttributedNode
//		{
//			VBParser parser = new VBParser();
//			var members = parser.ParseTypeMembers(new StringReader(expr));
//			
//			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
//			
//			AttributedNode m = members.Single();
//			Type type = typeof(T);
//			Assert.IsTrue(type.IsAssignableFrom(m.GetType()), String.Format("Parsed member was {0} instead of {1} ({2})", m.GetType(), type, m));
//			return (T)m;
//		}
//		
//		public static void AssertTypeMember(string code, VB.Ast.AttributedNode expectedMember)
//		{
//			var member = ParseTypeMember<VB.Ast.AttributedNode>(code);
//			if (!expectedMember.IsMatch(member)) {
//				Assert.Fail("Expected '{0}' but was '{1}'", ToVB(expectedMember), ToVB(member));
//			}
//		}
		
		static string ToVB(AstNode node)
		{
			StringWriter w = new StringWriter();
			node.AcceptVisitor(new OutputVisitor(w, new VBFormattingOptions()), null);
			return w.ToString();
		}
	}
}
