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
	public class UsingStatementTransform : IAstTransform
	{
		public void Run(AstNode compilationUnit)
		{
			TransformUsings(compilationUnit);
			TransformForeach(compilationUnit);
		}
		
		#region using
		static readonly AstNode usingVarDeclPattern = new VariableDeclarationStatement {
			Type = new AnyNode("type").ToType(),
			Variables = {
				new NamedNode(
					"variable",
					new VariableInitializer {
						Initializer = new AnyNode().ToExpression()
					}
				).ToVariable()
			}
		};
		static readonly AstNode simpleVariableDefinition = new VariableDeclarationStatement {
			Type = new AnyNode().ToType(),
			Variables = {
				new VariableInitializer() // any name but no initializer
			}
		};
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
				Match m1 = usingVarDeclPattern.Match(node);
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
			EmbeddedStatement = new BlockStatement {
				new ForStatement {
					EmbeddedStatement = new BlockStatement {
						new IfElseStatement {
							Condition = new UnaryOperatorExpression(
								UnaryOperatorType.Not,
								new NamedNode("enumeratorIdent", new IdentifierExpression()).ToExpression().Invoke("MoveNext")
							),
							TrueStatement = new BlockStatement {
								new BreakStatement()
							},
							FalseStatement = new BlockStatement {
								new VariableDeclarationStatement {
									Type = new AnyNode("itemType").ToType(),
									Variables = {
										new NamedNode(
											"itemVariable",
											new VariableInitializer {
												Initializer = new Backreference("enumeratorIdent").ToExpression().Member("Current")
											}
										).ToVariable()
									}
								},
								new Repeat(new AnyNode("statement")).ToStatement()
							}
						}
					}
				}
			}
		};
		
		public void TransformForeach(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.ToArray()) {
				Match m = foreachPattern.Match(node);
				if (m == null)
					continue;
				VariableInitializer enumeratorVar = m.Get<VariableInitializer>("enumeratorVariable").Single();
				if (enumeratorVar.Name != m.Get<IdentifierExpression>("enumeratorIdent").Single().Identifier)
					continue;
				VariableInitializer itemVar = m.Get<VariableInitializer>("itemVariable").Single();
				BlockStatement newBody = new BlockStatement();
				foreach (Statement stmt in m.Get<Statement>("statement"))
					newBody.Add(stmt.Detach());
				node.ReplaceWith(
					new ForeachStatement {
						VariableType = m.Get<AstType>("itemType").Single().Detach(),
						VariableName = enumeratorVar.Name,
						InExpression = m.Get<Expression>("collection").Single().Detach(),
						EmbeddedStatement = newBody
					});
			}
		}
		#endregion
	}
}
