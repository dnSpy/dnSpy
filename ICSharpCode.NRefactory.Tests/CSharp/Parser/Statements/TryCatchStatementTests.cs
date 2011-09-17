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
