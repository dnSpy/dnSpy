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
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	[TestFixture]
	public class DefiniteAssignmentTests
	{
		DefiniteAssignmentAnalysis CreateDefiniteAssignmentAnalysis(Statement rootStatement)
		{
			var resolver = new CSharpAstResolver(new CSharpResolver(new SimpleCompilation(CecilLoaderTests.Mscorlib)), rootStatement);
			return new DefiniteAssignmentAnalysis(rootStatement, resolver, CancellationToken.None);
		}
		
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
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(block);
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
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(ifStmt);
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
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(ifStmt);
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
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(loop);
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
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(loop);
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
		
		[Test]
		public void SwitchWithGotoDefault()
		{
			SwitchStatement @switch = new SwitchStatement {
				SwitchSections = {
					new SwitchSection { // case 0:
						CaseLabels = { new CaseLabel(new PrimitiveExpression(0)) },
						Statements = { new GotoDefaultStatement() }
					},
					new SwitchSection { // default:
						CaseLabels = { new CaseLabel() },
						Statements = {
							new ExpressionStatement(new AssignmentExpression(new IdentifierExpression("a"), new PrimitiveExpression(1))),
							new BreakStatement()
						}
					}
				}};
			
			SwitchSection case0 = @switch.SwitchSections.ElementAt(0);
			SwitchSection defaultSection = @switch.SwitchSections.ElementAt(1);
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(@switch);
			da.Analyze("a");
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(@switch));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(case0.Statements.First()));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(defaultSection.Statements.First()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(defaultSection.Statements.Last()));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusAfter(defaultSection.Statements.Last()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(@switch));
		}
		
		[Test]
		public void SwitchWithGotoCase()
		{
			SwitchStatement @switch = new SwitchStatement {
				Expression = new PrimitiveExpression(1),
				SwitchSections = {
					new SwitchSection { // case 0:
						CaseLabels = { new CaseLabel(new PrimitiveExpression(0)) },
						Statements = { new BreakStatement() }
					},
					new SwitchSection { // case 1:
						CaseLabels = { new CaseLabel(new PrimitiveExpression(1)) },
						Statements = {
							new ExpressionStatement(new AssignmentExpression(new IdentifierExpression("a"), new PrimitiveExpression(0))),
							new GotoCaseStatement { LabelExpression = new PrimitiveExpression(2) }
						}
					},
					new SwitchSection { // case 2:
						CaseLabels = { new CaseLabel(new PrimitiveExpression(2)) },
						Statements = { new BreakStatement() }
					}
				}};
			
			SwitchSection case0 = @switch.SwitchSections.ElementAt(0);
			SwitchSection case1 = @switch.SwitchSections.ElementAt(1);
			SwitchSection case2 = @switch.SwitchSections.ElementAt(2);
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(@switch);
			da.Analyze("a");
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(@switch));
			Assert.AreEqual(DefiniteAssignmentStatus.CodeUnreachable, da.GetStatusBefore(case0.Statements.First()));
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusBefore(case1.Statements.First()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusBefore(case2.Statements.First()));
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(@switch));
		}
		
		[Test]
		public void ConditionalExpression1()
		{
			string code = "int a; int b = X ? (a = 1) : 0;";
			var block = new BlockStatement();
			block.Statements.AddRange(new CSharpParser().ParseStatements(code));
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(block);
			da.Analyze("a");
			Assert.AreEqual(DefiniteAssignmentStatus.PotentiallyAssigned, da.GetStatusAfter(block));
		}
		
		[Test]
		public void ConditionalExpression2()
		{
			string code = "int a; int b = X ? (a = 1) : (a = 2);";
			var block = new BlockStatement();
			block.Statements.AddRange(new CSharpParser().ParseStatements(code));
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(block);
			da.Analyze("a");
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(block));
		}
		
		[Test]
		public void ConditionalExpression3()
		{
			string code = "int a; int b = true ? (a = 1) : 0;";
			var block = new BlockStatement();
			block.Statements.AddRange(new CSharpParser().ParseStatements(code));
			
			DefiniteAssignmentAnalysis da = CreateDefiniteAssignmentAnalysis(block);
			da.Analyze("a");
			Assert.AreEqual(DefiniteAssignmentStatus.DefinitelyAssigned, da.GetStatusAfter(block));
		}
	}
}
