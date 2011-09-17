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
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	[TestFixture]
	public class DefiniteAssignmentTests
	{
		[Test]
		public void TryFinally()
		{
			BlockStatement block = new BlockStatement {
				new TryCatchStatement {
					TryBlock = new BlockStatement {
						new GotoStatement("LABEL"),
						new AssignmentExpression(new IdentifierExpression("i"), new PrimitiveExpression(1))
					},
					CatchClauses = {
						new CatchClause {
							Body = new BlockStatement {
								new AssignmentExpression(new IdentifierExpression("i"), new PrimitiveExpression(3))
							}
						}
					},
					FinallyBlock = new BlockStatement {
						new AssignmentExpression(new IdentifierExpression("j"), new PrimitiveExpression(5))
					}
				},
				new LabelStatement { Label = "LABEL" },
				new EmptyStatement()
			};
			TryCatchStatement tryCatchStatement = (TryCatchStatement)block.Statements.First();
			Statement stmt1 = tryCatchStatement.TryBlock.Statements.ElementAt(1);
			Statement stmt3 = tryCatchStatement.CatchClauses.Single().Body.Statements.Single();
			Statement stmt5 = tryCatchStatement.FinallyBlock.Statements.Single();
			LabelStatement label = (LabelStatement)block.Statements.ElementAt(1);
			
			DefiniteAssignmentAnalysis da = new DefiniteAssignmentAnalysis(block, CecilLoaderTests.Mscorlib);
			da.Analyze("i");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(tryCatchStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusBefore(stmt1));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusAfter(stmt1));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(stmt3));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(stmt3));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(stmt5));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(stmt5));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(tryCatchStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(label));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(label));
			
			da.Analyze("j");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(tryCatchStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusBefore(stmt1));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusAfter(stmt1));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(stmt3));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(stmt3));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(stmt5));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(stmt5));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(tryCatchStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(label));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(label));
		}
		
		[Test]
		public void ConditionalAnd()
		{
			IfElseStatement ifStmt = new IfElseStatement {
				Condition = new BinaryOperatorExpression {
					Left = new BinaryOperatorExpression(new IdentifierExpression("x"), BinaryOperatorType.GreaterThan, new PrimitiveExpression(0)),
					Operator = BinaryOperatorType.ConditionalAnd,
					Right = new BinaryOperatorExpression {
						Left = new ParenthesizedExpression {
							Expression = new AssignmentExpression {
								Left = new IdentifierExpression("i"),
								Operator = AssignmentOperatorType.Assign,
								Right = new IdentifierExpression("y")
							}
						},
						Operator = BinaryOperatorType.GreaterThanOrEqual,
						Right = new PrimitiveExpression(0)
					}
				},
				TrueStatement = new BlockStatement(),
				FalseStatement = new BlockStatement()
			};
			DefiniteAssignmentAnalysis da = new DefiniteAssignmentAnalysis(ifStmt, CecilLoaderTests.Mscorlib);
			da.Analyze("i");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(ifStmt));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(ifStmt.TrueStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(ifStmt.FalseStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(ifStmt));
		}
		
		[Test]
		public void ConditionalOr()
		{
			IfElseStatement ifStmt = new IfElseStatement {
				Condition = new BinaryOperatorExpression {
					Left = new BinaryOperatorExpression(new IdentifierExpression("x"), BinaryOperatorType.GreaterThan, new PrimitiveExpression(0)),
					Operator = BinaryOperatorType.ConditionalOr,
					Right = new BinaryOperatorExpression {
						Left = new ParenthesizedExpression {
							Expression = new AssignmentExpression {
								Left = new IdentifierExpression("i"),
								Operator = AssignmentOperatorType.Assign,
								Right = new IdentifierExpression("y")
							}
						},
						Operator = BinaryOperatorType.GreaterThanOrEqual,
						Right = new PrimitiveExpression(0)
					}
				},
				TrueStatement = new BlockStatement(),
				FalseStatement = new BlockStatement()
			};
			DefiniteAssignmentAnalysis da = new DefiniteAssignmentAnalysis(ifStmt, CecilLoaderTests.Mscorlib);
			da.Analyze("i");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(ifStmt));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(ifStmt.TrueStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(ifStmt.FalseStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(ifStmt));
		}
		
		[Test]
		public void WhileTrue()
		{
			WhileStatement loop = new WhileStatement {
				Condition = new PrimitiveExpression(true),
				EmbeddedStatement = new BlockStatement {
					new AssignmentExpression(new IdentifierExpression("i"), new PrimitiveExpression(0)),
					new BreakStatement()
				}
			};
			DefiniteAssignmentAnalysis da = new DefiniteAssignmentAnalysis(loop, CecilLoaderTests.Mscorlib);
			da.Analyze("i");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop.EmbeddedStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusAfter(loop.EmbeddedStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop));
		}
		
		[Test]
		public void ForLoop()
		{
			ForStatement loop = new ForStatement {
				Initializers = {
					new ExpressionStatement(
						new AssignmentExpression(new IdentifierExpression("i"), new PrimitiveExpression(0))
					)
				},
				Condition = new BinaryOperatorExpression(new IdentifierExpression("i"), BinaryOperatorType.LessThan, new PrimitiveExpression(1000)),
				Iterators = {
					new ExpressionStatement(
						new AssignmentExpression {
							Left = new IdentifierExpression("i"),
							Operator = AssignmentOperatorType.Add,
							Right = new IdentifierExpression("j")
						}
					)
				},
				EmbeddedStatement = new ExpressionStatement(
					new AssignmentExpression(new IdentifierExpression("j"), new IdentifierExpression("i"))
				)};
			
			DefiniteAssignmentAnalysis da = new DefiniteAssignmentAnalysis(loop, CecilLoaderTests.Mscorlib);
			da.Analyze("i");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop.Initializers.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop.Initializers.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBeforeLoopCondition(loop));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(loop.EmbeddedStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop.EmbeddedStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(loop.Iterators.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop.Iterators.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop));
			
			da.Analyze("j");
			Assert.AreEqual(0, da.UnassignedVariableUses.Count);
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop.Initializers.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(loop.Initializers.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBeforeLoopCondition(loop));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(loop.EmbeddedStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop.EmbeddedStatement));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(loop.Iterators.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(loop.Iterators.Single()));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(loop));
		}
	}
}
