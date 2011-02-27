// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using Decompiler.Transforms;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;
using Mono.Cecil;

namespace Decompiler.Transforms
{
	/// <summary>
	/// Finds the expanded form of using statements using pattern matching and replaces it with a UsingStatement.
	/// </summary>
	public class PatternStatementTransform : IAstTransform
	{
		public void Run(AstNode compilationUnit)
		{
			TransformUsings(compilationUnit);
			TransformForeach(compilationUnit);
			TransformFor(compilationUnit);
		}
		
		/// <summary>
		/// $type $variable = $initializer;
		/// </summary>
		static readonly AstNode variableDeclPattern = new VariableDeclarationStatement {
			Type = new AnyNode("type").ToType(),
			Variables = {
				new NamedNode(
					"variable",
					new VariableInitializer {
						Initializer = new AnyNode("initializer").ToExpression()
					}
				).ToVariable()
			}
		};
		
		/// <summary>
		/// Variable declaration without initializer.
		/// </summary>
		static readonly AstNode simpleVariableDefinition = new VariableDeclarationStatement {
			Type = new AnyNode().ToType(),
			Variables = {
				new VariableInitializer() // any name but no initializer
			}
		};
		
		#region using
		static readonly AstNode usingTryCatchPattern = new TryCatchStatement {
			TryBlock = new AnyNode("body").ToBlock(),
			FinallyBlock = new BlockStatement {
				new Choice {
					{ "valueType",
						new ExpressionStatement(new NamedNode("ident", new IdentifierExpression()).ToExpression().Invoke("Dispose"))
					},
					{ "referenceType",
						new IfElseStatement {
							Condition = new BinaryOperatorExpression(
								new NamedNode("ident", new IdentifierExpression()).ToExpression(),
								BinaryOperatorType.InEquality,
								new NullReferenceExpression()
							),
							TrueStatement = new BlockStatement {
								new ExpressionStatement(new Backreference("ident").ToExpression().Invoke("Dispose"))
							}
						}
					}
				}.ToStatement()
			}
		};
		
		public void TransformUsings(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.ToArray()) {
				Match m1 = variableDeclPattern.Match(node);
				if (m1 == null) continue;
				AstNode tryCatch = node.NextSibling;
				while (simpleVariableDefinition.Match(tryCatch) != null)
					tryCatch = tryCatch.NextSibling;
				Match m2 = usingTryCatchPattern.Match(tryCatch);
				if (m2 == null) continue;
				if (m1.Get<VariableInitializer>("variable").Single().Name == m2.Get<IdentifierExpression>("ident").Single().Identifier) {
					if (m2.Has("valueType")) {
						// if there's no if(x!=null), then it must be a value type
						TypeReference tr = m1.Get<AstType>("type").Single().Annotation<TypeReference>();
						if (tr == null || !tr.IsValueType)
							continue;
					}
					BlockStatement body = m2.Get<BlockStatement>("body").Single();
					tryCatch.ReplaceWith(
						new UsingStatement {
							ResourceAcquisition = node.Detach(),
							EmbeddedStatement = body.Detach()
						});
				}
			}
		}
		#endregion
		
		#region foreach
		UsingStatement foreachPattern = new UsingStatement {
			ResourceAcquisition = new VariableDeclarationStatement {
				Type = new AnyNode("enumeratorType").ToType(),
				Variables = {
					new NamedNode(
						"enumeratorVariable",
						new VariableInitializer {
							Initializer = new AnyNode("collection").ToExpression().Invoke("GetEnumerator")
						}
					).ToVariable()
				}
			},
			EmbeddedStatement = new Choice {
				// There are two forms of the foreach statement:
				// one where the item variable is declared inside the loop,
				// and one where it is declared outside of the loop.
				// In the former case, we can apply the foreach pattern only if the variable wasn't captured.
				{ "itemVariableInsideLoop",
					new BlockStatement {
						new WhileStatement {
							Condition = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Invoke("MoveNext"),
							EmbeddedStatement = new BlockStatement {
								new VariableDeclarationStatement {
									Type = new AnyNode("itemType").ToType(),
									Variables = {
										new NamedNode(
											"itemVariable",
											new VariableInitializer {
												Initializer = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Member("Current")
											}
										).ToVariable()
									}
								},
								new Repeat(new AnyNode("statement")).ToStatement()
							}
						}
					}
				},
				{ "itemVariableOutsideLoop",
					new BlockStatement {
						new VariableDeclarationStatement {
							Type = new AnyNode("itemType").ToType(),
							Variables = {
								new NamedNode("itemVariable", new VariableInitializer()).ToVariable()
							}
						},
						new WhileStatement {
							Condition = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Invoke("MoveNext"),
							EmbeddedStatement = new BlockStatement {
								new AssignmentExpression {
									Left = new IdentifierExpressionBackreference("itemVariable").ToExpression(),
									Operator = AssignmentOperatorType.Assign,
									Right = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Member("Current")
								},
								new Repeat(new AnyNode("statement")).ToStatement()
							}
						}
					}
				}
			}.ToStatement()
		};
		
		public void TransformForeach(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.ToArray()) {
				Match m = foreachPattern.Match(node);
				if (m == null)
					continue;
				VariableInitializer enumeratorVar = m.Get<VariableInitializer>("enumeratorVariable").Single();
				VariableInitializer itemVar = m.Get<VariableInitializer>("itemVariable").Single();
				if (m.Has("itemVariableInsideLoop") && itemVar.Annotation<DelegateConstruction.CapturedVariableAnnotation>() != null) {
					// cannot move captured variables out of loops
					continue;
				}
				BlockStatement newBody = new BlockStatement();
				foreach (Statement stmt in m.Get<Statement>("statement"))
					newBody.Add(stmt.Detach());
				node.ReplaceWith(
					new ForeachStatement {
						VariableType = m.Get<AstType>("itemType").Single().Detach(),
						VariableName = itemVar.Name,
						InExpression = m.Get<Expression>("collection").Single().Detach(),
						EmbeddedStatement = newBody
					});
			}
		}
		#endregion
		
		#region for
		WhileStatement forPattern = new WhileStatement {
			Condition = new BinaryOperatorExpression {
				Left = new NamedNode("ident", new IdentifierExpression()).ToExpression(),
				Operator = BinaryOperatorType.Any,
				Right = new AnyNode("endExpr").ToExpression()
			},
			EmbeddedStatement = new BlockStatement {
				new Repeat(new AnyNode("statement")).ToStatement(),
				new NamedNode(
					"increment",
					new ExpressionStatement(
						new AssignmentExpression {
							Left = new Backreference("ident").ToExpression(),
							Operator = AssignmentOperatorType.Any,
							Right = new AnyNode().ToExpression()
						})).ToStatement()
			}
		};
		
		public void TransformFor(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.ToArray()) {
				Match m1 = variableDeclPattern.Match(node);
				if (m1 == null) continue;
				AstNode next = node.NextSibling;
				while (simpleVariableDefinition.Match(next) != null)
					next = next.NextSibling;
				Match m2 = forPattern.Match(next);
				if (m2 == null) continue;
				// ensure the variable in the for pattern is the same as in the declaration
				if (m1.Get<VariableInitializer>("variable").Single().Name != m2.Get<IdentifierExpression>("ident").Single().Identifier)
					continue;
				WhileStatement loop = (WhileStatement)next;
				node.Remove();
				BlockStatement newBody = new BlockStatement();
				foreach (Statement stmt in m2.Get<Statement>("statement"))
					newBody.Add(stmt.Detach());
				loop.ReplaceWith(
					new ForStatement {
						Initializers = { (VariableDeclarationStatement)node },
						Condition = loop.Condition.Detach(),
						Iterators = { m2.Get<Statement>("increment").Single().Detach() },
						EmbeddedStatement = newBody
					});
			}
		}
		#endregion
	}
}
