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
			SyntaxTree syntaxTree = parser.Parse(code);
			
			foreach (var error in parser.Errors)
				Console.WriteLine (error.Message);
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			
			AstNode node = syntaxTree.Children.Single();
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
			var statements = parser.ParseStatements(stmt);
			
			foreach (var error in parser.Errors)
				Console.WriteLine (error.Message);
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
			AstNode parsedExpression = parser.ParseExpression(expr);
			
			foreach (var error in parser.Errors)
				Console.WriteLine (error.Message);
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
		
		public static T ParseTypeMember<T>(string expr, bool expectErrors = false) where T : EntityDeclaration
		{
			CSharpParser parser = new CSharpParser();
			var members = parser.ParseTypeMembers(expr);
			foreach (var error in parser.Errors)
				Console.WriteLine (error.Message);
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			EntityDeclaration m = members.Single();
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(m.GetType()), String.Format("Parsed member was {0} instead of {1} ({2})", m.GetType(), type, m));
			return (T)m;
		}
		
		public static void AssertTypeMember(string code, CSharp.EntityDeclaration expectedMember)
		{
			var member = ParseTypeMember<CSharp.EntityDeclaration>(code);
			if (!expectedMember.IsMatch(member)) {
				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedMember), ToCSharp(member));
			}
		}
		
		public static DocumentationReference ParseDocumentationReference(string cref, bool expectErrors = false)
		{
			CSharpParser parser = new CSharpParser();
			var parsedExpression = parser.ParseDocumentationReference(cref);
			
			foreach (var error in parser.Errors)
				Console.WriteLine (error.Message);
			Assert.AreEqual(expectErrors, parser.HasErrors, "HasErrors");
			if (expectErrors && parsedExpression == null)
				return null;
			return parsedExpression;
		}
		
		public static void AssertDocumentationReference(string cref, CSharp.DocumentationReference expectedExpr)
		{
			var expr = ParseDocumentationReference(cref);
			if (!expectedExpr.IsMatch(expr)) {
				Assert.Fail("Expected '{0}' but was '{1}'", ToCSharp(expectedExpr), ToCSharp(expr));
			}
		}
		
		static string ToCSharp(AstNode node)
		{
			return node.GetText();
		}
	}
}
