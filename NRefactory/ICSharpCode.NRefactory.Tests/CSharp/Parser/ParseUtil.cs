// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.PatternMatching;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser
{
	/// <summary>
	/// Helper methods for parser unit tests.
	/// </summary>
	public static class ParseUtilCSharp
	{
		public static T ParseGlobal<T>(string code, bool expectErrors = false) where T : AstNode
		{
			CSharpParser parser = new CSharpParser();
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
				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedNode), ToCSharp(node));
			}
		}
		
		public static T ParseStatement<T>(string stmt, bool expectErrors = false) where T : AstNode
		{
			CSharpParser parser = new CSharpParser();
			var statements = parser.ParseStatements(new StringReader(stmt));
			
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			
			AstNode statement = statements.Single();
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(statement.GetType()), String.Format("Parsed statement was {0} instead of {1} ({2})", statement.GetType(), type, statement));
			return (T)statement;
		}
		
		public static void AssertStatement(string code, CSharp.Statement expectedStmt)
		{
			var stmt = ParseStatement<CSharp.Statement>(code);
			if (!expectedStmt.IsMatch(stmt)) {
				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedStmt), ToCSharp(stmt));
			}
		}
		
		public static T ParseExpression<T>(string expr, bool expectErrors = false) where T : AstNode
		{
			CSharpParser parser = new CSharpParser();
			AstNode parsedExpression = parser.ParseExpression(new StringReader(expr));
			
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			if (expectErrors && parsedExpression == null)
				return default (T);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(parsedExpression.GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", parsedExpression.GetType(), type, parsedExpression));
			return (T)parsedExpression;
		}
		
		public static void AssertExpression(string code, CSharp.Expression expectedExpr)
		{
			var expr = ParseExpression<CSharp.Expression>(code);
			if (!expectedExpr.IsMatch(expr)) {
				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedExpr), ToCSharp(expr));
			}
		}
		
		public static T ParseTypeMember<T>(string expr, bool expectErrors = false) where T : AttributedNode
		{
			CSharpParser parser = new CSharpParser();
			var members = parser.ParseTypeMembers(new StringReader(expr));
			
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			
			AttributedNode m = members.Single();
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(m.GetType()), String.Format("Parsed member was {0} instead of {1} ({2})", m.GetType(), type, m));
			return (T)m;
		}
		
		public static void AssertTypeMember(string code, CSharp.AttributedNode expectedMember)
		{
			var member = ParseTypeMember<CSharp.AttributedNode>(code);
			if (!expectedMember.IsMatch(member)) {
				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedMember), ToCSharp(member));
			}
		}
		
		static string ToCSharp(AstNode node)
		{
			StringWriter w = new StringWriter();
			node.AcceptVisitor(new OutputVisitor(w, new CSharpFormattingOptions()), null);
			return w.ToString();
		}
	}
}
