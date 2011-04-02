// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class IfElseStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN END");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is EndStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
		}
		[Test]
		public void VBNetSimpleIfStatementTest2()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN\n END\n END IF");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
		}
		
		// test for SD2-1201
		[Test]
		public void VBNetIfStatementLocationTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 "DoIt()\n" +
			                                                                                 "ElseIf False Then\n" +
			                                                                                 "DoIt()\n" +
			                                                                                 "End If");
			Assert.AreEqual(3, (ifElseStatement.StartLocation).Line);
			Assert.AreEqual(7, (ifElseStatement.EndLocation).Line);
			Assert.AreEqual(5, (ifElseStatement.ElseIfSections[0].StartLocation).Line);
			Assert.AreEqual(6, (ifElseStatement.ElseIfSections[0].EndLocation).Line);
			Assert.IsNotNull(ifElseStatement.ElseIfSections[0].Parent);
			
		}
		
		[Test]
		public void VBNetElseIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 "END\n" +
			                                                                                 "ElseIf False Then\n" +
			                                                                                 "Stop\n" +
			                                                                                 "End If");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			Assert.IsFalse((bool)(ifElseStatement.ElseIfSections[0].Condition as PrimitiveExpression).Value);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
			Assert.IsTrue(ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0] is StopStatement, "Statement was: " + ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0]);
		}
		[Test]
		public void VBNetElse_IfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 "END\n" +
			                                                                                 "Else If False Then\n" +
			                                                                                 "Stop\n" +
			                                                                                 "End If");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			Assert.IsFalse((bool)(ifElseStatement.ElseIfSections[0].Condition as PrimitiveExpression).Value);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
			Assert.IsTrue(ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0] is StopStatement, "Statement was: " + ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0]);
		}
		[Test]
		public void VBNetMultiStatementIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN Stop : b");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(2, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is StopStatement);
			Assert.IsTrue(ifElseStatement.TrueStatement[1] is ExpressionStatement);
		}
		[Test]
		public void VBNetMultiStatementIfStatementWithEndStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN Stop : End : b");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(3, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is StopStatement);
			Assert.IsTrue(ifElseStatement.TrueStatement[1] is EndStatement);
			Assert.IsTrue(ifElseStatement.TrueStatement[2] is ExpressionStatement);
		}
		
		[Test]
		public void VBNetIfWithEmptyElseTest()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN a Else");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(1, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
		}
		
		[Test]
		public void VBNetIfWithMultipleColons()
		{
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN a : : b");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(2, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
		}
		
		[Test]
		public void VBNetIfWithSingleLineElse()
		{
			// This isn't legal according to the VB spec, but the MS VB compiler seems to allow it.
			IfElseStatement ifElseStatement = ParseUtil.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 " x()\n" +
			                                                                                 "Else y()\n" +
			                                                                                 "End If");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(1, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(1, ifElseStatement.FalseStatement.Count, "false count");
		}
		#endregion
	}
}
