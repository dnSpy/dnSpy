// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Dom;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Dom
{
	public class ParseUtil
	{
		public static T ParseGlobal<T>(string program) where T : INode
		{
			return ParseGlobal<T>(program, false);
		}
		
		public static T ParseGlobal<T>(string program, bool expectErrors) where T : INode
		{
			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
			parser.Parse();
			
			if (expectErrors)
				Assert.IsFalse(parser.Errors.ErrorOutput.Length == 0, "Expected errors, but operation completed successfully");
			else
				Assert.AreEqual("", parser.Errors.ErrorOutput);
			
			Assert.IsNotNull(parser.CompilationUnit);
			Assert.IsNotNull(parser.CompilationUnit.Children);
			Assert.IsNotNull(parser.CompilationUnit.Children[0]);
			Assert.AreEqual(1, parser.CompilationUnit.Children.Count);
			
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(parser.CompilationUnit.Children[0].GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", parser.CompilationUnit.Children[0].GetType(), type, parser.CompilationUnit.Children[0]));
			
			parser.CompilationUnit.AcceptVisitor(new CheckParentVisitor(), null);
			// TODO fix Locations
//			parser.CompilationUnit.AcceptChildren(new LocationAssignmentCheckVisitor(), null);
			
			return (T)parser.CompilationUnit.Children[0];
		}
		
		public static T ParseTypeMember<T>(string typeMember, bool expectErrors) where T : INode
		{
			TypeDeclaration td = ParseGlobal<TypeDeclaration>("Class TestClass\n " + typeMember + "\n End Class\n", expectErrors);
			Assert.AreEqual(1, td.Children.Count);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(td.Children[0].GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", td.GetType(), type, td));
			return (T)td.Children[0];
		}
		
		public static T ParseTypeMember<T>(string typeMember) where T : INode
		{
			return ParseTypeMember<T>(typeMember, false);
		}
		
		public static T ParseStatement<T>(string statement, bool expectErrors) where T : INode
		{
			MethodDeclaration md = ParseTypeMember<MethodDeclaration>("Sub A()\n " + statement + "\nEnd Sub\n", expectErrors);
			Assert.AreEqual(1, md.Body.Children.Count);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(md.Body.Children[0].GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", md.GetType(), type, md));
			return (T)md.Body.Children[0];
		}
		
		public static T ParseStatement<T>(string statement) where T : INode
		{
			return ParseStatement<T>(statement, false);
		}
		
		public static T ParseExpression<T>(string expr) where T : INode
		{
			return ParseExpression<T>(expr, false);
		}
		
		public static T ParseExpression<T>(string expr, bool expectErrors) where T : INode
		{
			VBParser parser = ParserFactory.CreateParser(new StringReader(expr));
			INode parsedExpression = parser.ParseExpression();
			if (expectErrors)
				Assert.IsFalse(parser.Errors.ErrorOutput.Length == 0, "Expected errors, but operation completed successfully");
			else
				Assert.AreEqual("", parser.Errors.ErrorOutput);
			// TODO fix Locations
//			parsedExpression.AcceptVisitor(new LocationAssignmentCheckVisitor(), null);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(parsedExpression.GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", parsedExpression.GetType(), type, parsedExpression));
			return (T)parsedExpression;
		}
	}
}
