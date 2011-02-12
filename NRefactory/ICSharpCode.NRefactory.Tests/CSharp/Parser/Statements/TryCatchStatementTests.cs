// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture, Ignore]
	public class TryCatchStatementTests
	{
		[Test]
		public void SimpleTryCatchStatementTest()
		{
			TryCatchStatement tryCatchStatement = ParseUtilCSharp.ParseStatement<TryCatchStatement>("try { } catch { } ");
			Assert.IsTrue(tryCatchStatement.FinallyBlock.IsNull);
			Assert.AreEqual(1, tryCatchStatement.CatchClauses.Count());
			Assert.IsTrue(tryCatchStatement.CatchClauses.Single().Type.IsNull);
			Assert.AreEqual(string.Empty, tryCatchStatement.CatchClauses.Single().VariableName);
		}
		
		/* TODO port tests
		[Test]
		public void SimpleTryCatchStatementTest2()
		{
			TryCatchStatement tryCatchStatement = ParseUtilCSharp.ParseStatement<TryCatchStatement>("try { } catch (Exception e) { } ");
			Assert.IsTrue(tryCatchStatement.FinallyBlock.IsNull);
			Assert.AreEqual(1, tryCatchStatement.CatchClauses.Count);
			Assert.AreEqual("Exception", tryCatchStatement.CatchClauses[0].TypeReference.Type);
			Assert.AreEqual("e", tryCatchStatement.CatchClauses[0].VariableName);
		}
		
		[Test]
		public void SimpleTryCatchFinallyStatementTest()
		{
			TryCatchStatement tryCatchStatement = ParseUtilCSharp.ParseStatement<TryCatchStatement>("try { } catch (Exception) { } catch { } finally { } ");
			Assert.IsFalse(tryCatchStatement.FinallyBlock.IsNull);
			Assert.AreEqual(2, tryCatchStatement.CatchClauses.Count);
			Assert.AreEqual("Exception", tryCatchStatement.CatchClauses[0].TypeReference.Type);
			Assert.IsEmpty(tryCatchStatement.CatchClauses[0].VariableName);
			Assert.IsTrue(tryCatchStatement.CatchClauses[1].TypeReference.IsNull);
			Assert.IsEmpty(tryCatchStatement.CatchClauses[1].VariableName);
		}
		*/
	}
}
