// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Finds the expanded form of using statements using pattern matching and replaces it with a UsingStatement.
	/// </summary>
	public class PatternStatementTransform : IAstTransform
	{
		DecompilerContext context;
		
		public PatternStatementTransform(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public void Run(AstNode compilationUnit)
		{
			if (context.Settings.UsingStatement)
				TransformUsings(compilationUnit);
			if (context.Settings.ForEachStatement)
				TransformForeach(compilationUnit);
			TransformFor(compilationUnit);
			TransformDoWhile(compilationUnit);
			if (context.Settings.AutomaticProperties)
				TransformAutomaticProperties(compilationUnit);
		}
		
		/// <summary>
		/// $type $variable = $initializer;
		/// </summary>
		static readonly AstNode variableDeclPattern = new VariableDeclarationStatement {
			Type = new AnyNode("type"),
			Variables = {
				new NamedNode(
					"variable",
					new VariableInitializer {
						Initializer = new AnyNode("initializer")
					}
				)
			}
		};
		
		/// <summary>
		/// Variable declaration without initializer.
		/// </summary>
		static readonly AstNode simpleVariableDefinition = new VariableDeclarationStatement {
			Type = new AnyNode(),
			Variables = {
				new VariableInitializer() // any name but no initializer
			}
		};
		
		#region using
		static readonly AstNode usingTryCatchPattern = new TryCatchStatement {
			TryBlock = new AnyNode("body"),
			FinallyBlock = new BlockStatement {
				new Choice {
					{ "valueType",
						new ExpressionStatement(new NamedNode("ident", new IdentifierExpression()).ToExpression().Invoke("Dispose"))
					},
					{ "referenceType",
						new IfElseStatement {
							Condition = new BinaryOperatorExpression(
								new NamedNode("ident", new IdentifierExpression()),
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
		static readonly UsingStatement foreachPattern = new UsingStatement {
			ResourceAcquisition = new VariableDeclarationStatement {
				Type = new AnyNode("enumeratorType"),
				Variables = {
					new NamedNode(
						"enumeratorVariable",
						new VariableInitializer {
							Initializer = new AnyNode("collection").ToExpression().Invoke("GetEnumerator")
						}
					)
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
									Type = new AnyNode("itemType"),
									Variables = {
										new NamedNode(
											"itemVariable",
											new VariableInitializer {
												Initializer = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Member("Current")
											}
										)
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
							Type = new AnyNode("itemType"),
							Variables = {
								new NamedNode("itemVariable", new VariableInitializer())
							}
						},
						new WhileStatement {
							Condition = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Invoke("MoveNext"),
							EmbeddedStatement = new BlockStatement {
								new AssignmentExpression {
									Left = new IdentifierExpressionBackreference("itemVariable"),
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
		static readonly WhileStatement forPattern = new WhileStatement {
			Condition = new BinaryOperatorExpression {
				Left = new NamedNode("ident", new IdentifierExpression()),
				Operator = BinaryOperatorType.Any,
				Right = new AnyNode("endExpr")
			},
			EmbeddedStatement = new BlockStatement {
				Statements = {
					new Repeat(new AnyNode("statement")),
					new NamedNode(
						"increment",
						new ExpressionStatement(
							new AssignmentExpression {
								Left = new Backreference("ident"),
								Operator = AssignmentOperatorType.Any,
								Right = new AnyNode()
							}))
				}
			}};
		
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
		
		#region doWhile
		static readonly WhileStatement doWhilePattern = new WhileStatement {
			Condition = new PrimitiveExpression(true),
			EmbeddedStatement = new BlockStatement {
				Statements = {
					new Repeat(new AnyNode("statement")),
					new IfElseStatement {
						Condition = new AnyNode("condition"),
						TrueStatement = new BlockStatement { new BreakStatement() }
					}
				}
			}};
		
		public void TransformDoWhile(AstNode compilationUnit)
		{
			foreach (WhileStatement whileLoop in compilationUnit.Descendants.OfType<WhileStatement>().ToArray()) {
				Match m = doWhilePattern.Match(whileLoop);
				if (m != null) {
					DoWhileStatement doLoop = new DoWhileStatement();
					doLoop.Condition = new UnaryOperatorExpression(UnaryOperatorType.Not, m.Get<Expression>("condition").Single().Detach());
					doLoop.Condition.AcceptVisitor(new PushNegation(), null);
					BlockStatement block = (BlockStatement)whileLoop.EmbeddedStatement;
					block.Statements.Last().Remove(); // remove if statement
					doLoop.EmbeddedStatement = block.Detach();
					whileLoop.ReplaceWith(doLoop);
					
					// we may have to extract variable definitions out of the loop if they were used in the condition:
					foreach (var varDecl in block.Statements.OfType<VariableDeclarationStatement>()) {
						VariableInitializer v = varDecl.Variables.Single();
						if (doLoop.Condition.DescendantsAndSelf.OfType<IdentifierExpression>().Any(i => i.Identifier == v.Name)) {
							AssignmentExpression assign = new AssignmentExpression(new IdentifierExpression(v.Name), v.Initializer.Detach());
							// move annotations from v to assign:
							assign.CopyAnnotationsFrom(v);
							v.RemoveAnnotations<object>();
							// remove varDecl with assignment; and move annotations from varDecl to the ExpressionStatement:
							varDecl.ReplaceWith(new ExpressionStatement(assign).CopyAnnotationsFrom(varDecl));
							varDecl.RemoveAnnotations<object>();
							
							// insert the varDecl above the do-while loop:
							doLoop.Parent.InsertChildBefore(doLoop, varDecl, BlockStatement.StatementRole);
						}
					}
				}
			}
		}
		#endregion
		
		#region Automatic Properties
		static readonly PropertyDeclaration automaticPropertyPattern = new PropertyDeclaration {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			ReturnType = new AnyNode(),
			Getter = new Accessor {
				Attributes = { new Repeat(new AnyNode()) },
				Modifiers = Modifiers.Any,
				Body = new BlockStatement {
					new ReturnStatement {
						Expression = new NamedNode("fieldReference", new MemberReferenceExpression { Target = new ThisReferenceExpression() })
					}
				}
			},
			Setter = new Accessor {
				Attributes = { new Repeat(new AnyNode()) },
				Modifiers = Modifiers.Any,
				Body = new BlockStatement {
					new AssignmentExpression {
						Left = new Backreference("fieldReference"),
						Right = new IdentifierExpression("value")
					}
				}}};
		
		void TransformAutomaticProperties(AstNode compilationUnit)
		{
			foreach (var property in compilationUnit.Descendants.OfType<PropertyDeclaration>()) {
				PropertyDefinition cecilProperty = property.Annotation<PropertyDefinition>();
				if (cecilProperty == null || cecilProperty.GetMethod == null || cecilProperty.SetMethod == null)
					continue;
				if (!(cecilProperty.GetMethod.IsCompilerGenerated() && cecilProperty.SetMethod.IsCompilerGenerated()))
					continue;
				Match m = automaticPropertyPattern.Match(property);
				if (m != null) {
					FieldDefinition field = m.Get("fieldReference").Single().Annotation<FieldDefinition>();
					if (field.IsCompilerGenerated()) {
						RemoveCompilerGeneratedAttribute(property.Getter.Attributes);
						RemoveCompilerGeneratedAttribute(property.Setter.Attributes);
						property.Getter.Body = null;
						property.Setter.Body = null;
					}
				}
			}
		}
		
		void RemoveCompilerGeneratedAttribute(AstNodeCollection<AttributeSection> attributeSections)
		{
			foreach (AttributeSection section in attributeSections) {
				foreach (var attr in section.Attributes) {
					TypeReference tr = attr.Type.Annotation<TypeReference>();
					if (tr != null && tr.Namespace == "System.Runtime.CompilerServices" && tr.Name == "CompilerGeneratedAttribute") {
						attr.Remove();
					}
				}
				if (section.Attributes.Count == 0)
					section.Remove();
			}
		}
		#endregion
	}
}
