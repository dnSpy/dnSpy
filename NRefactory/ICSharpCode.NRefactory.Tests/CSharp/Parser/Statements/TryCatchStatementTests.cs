// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
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
		
		[Test]
		public void SimpleTryCatchStatementTest2()
		{
			ParseUtilCSharp.AssertStatement(
				"try { } catch (Exception e) { } ",
				new TryCatchStatement {
					TryBlock = new BlockStatement(),
					CatchClauses = {
						new CatchClause {
							Type = new SimpleType("Exception"),
							VariableName = "e",
							Body = new BlockStatement()
						}
					}});
		}
		
		[Test]
		public void SimpleTryCatchFinallyStatementTest()
		{
			ParseUtilCSharp.AssertStatement(
				"try { } catch (Exception) { } catch { } finally { } ",
				new TryCatchStatement {
					TryBlock = new BlockStatement(),
					CatchClauses = {
						new CatchClause {
							Type = new SimpleType("Exception"),
							Body = new BlockStatement()
						},
						new CatchClause { Body = new BlockStatement() }
					},
					FinallyBlock = new BlockStatement()
				});
		}
		
		[Test]
		public void TestEmptyFinallyDoesNotMatchNullFinally()
		{
			TryCatchStatement c1 = new TryCatchStatement {
				TryBlock = new BlockStatement(),
				CatchClauses = { new CatchClause { Body = new BlockStatement() } }
			};
			TryCatchStatement c2 = new TryCatchStatement {
				TryBlock = new BlockStatement(),
				CatchClauses = { new CatchClause { Body = new BlockStatement() } },
				FinallyBlock = new BlockStatement()
			};
			Assert.IsFalse(c1.IsMatch(c2));
			Assert.IsFalse(c2.IsMatch(c1)); // and vice versa
		}
	}
}
