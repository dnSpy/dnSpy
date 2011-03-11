// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
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
			if (context.Settings.LockStatement)
				TransformLock(compilationUnit);
			if (context.Settings.SwitchStatementOnString)
				TransformSwitchOnString(compilationUnit);
			if (context.Settings.AutomaticProperties)
				TransformAutomaticProperties(compilationUnit);
			if (context.Settings.AutomaticEvents)
				TransformAutomaticEvents(compilationUnit);
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
			foreach (AstNode node in compilationUnit.Descendants.OfType<VariableDeclarationStatement>().ToArray()) {
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
			foreach (AstNode node in compilationUnit.Descendants.OfType<UsingStatement>().ToArray()) {
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
			foreach (AstNode node in compilationUnit.Descendants.OfType<VariableDeclarationStatement>().ToArray()) {
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
		
		#region lock
		static readonly AstNode lockFlagInitPattern = new VariableDeclarationStatement {
			Type = new PrimitiveType("bool"),
			Variables = {
				new NamedNode(
					"variable",
					new VariableInitializer {
						Initializer = new PrimitiveExpression(false)
					}
				)
			}};
		
		static readonly AstNode lockTryCatchPattern = new TryCatchStatement {
			TryBlock = new BlockStatement {
				new TypePattern(typeof(System.Threading.Monitor)).ToType().Invoke(
					"Enter", new AnyNode("enter"),
					new DirectionExpression {
						FieldDirection = FieldDirection.Ref,
						Expression = new NamedNode("flag", new IdentifierExpression())
					}),
				new Repeat(new AnyNode()).ToStatement()
			},
			FinallyBlock = new BlockStatement {
				new IfElseStatement {
					Condition = new Backreference("flag"),
					TrueStatement = new BlockStatement {
						new TypePattern(typeof(System.Threading.Monitor)).ToType().Invoke("Exit", new NamedNode("exit", new IdentifierExpression()))
					}
				}
			}};
		
		public void TransformLock(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.OfType<VariableDeclarationStatement>().ToArray()) {
				Match m1 = lockFlagInitPattern.Match(node);
				if (m1 == null) continue;
				AstNode tryCatch = node.NextSibling;
				while (simpleVariableDefinition.Match(tryCatch) != null)
					tryCatch = tryCatch.NextSibling;
				Match m2 = lockTryCatchPattern.Match(tryCatch);
				if (m2 == null) continue;
				if (m1.Get<VariableInitializer>("variable").Single().Name == m2.Get<IdentifierExpression>("flag").Single().Identifier) {
					Expression enter = m2.Get<Expression>("enter").Single();
					IdentifierExpression exit = m2.Get<IdentifierExpression>("exit").Single();
					if (exit.Match(enter) == null) {
						// If exit and enter are not the same, then enter must be "exit = ..."
						AssignmentExpression assign = enter as AssignmentExpression;
						if (assign == null)
							continue;
						if (exit.Match(assign.Left) == null)
							continue;
						enter = assign.Right;
						// Remove 'exit' variable:
						bool ok = false;
						for (AstNode tmp = node.NextSibling; tmp != tryCatch; tmp = tmp.NextSibling) {
							VariableDeclarationStatement v = (VariableDeclarationStatement)tmp;
							if (v.Variables.Single().Name == exit.Identifier) {
								ok = true;
								v.Remove();
								break;
							}
						}
						if (!ok)
							continue;
					}
					// transform the code into a lock statement:
					LockStatement l = new LockStatement();
					l.Expression = enter.Detach();
					l.EmbeddedStatement = ((TryCatchStatement)tryCatch).TryBlock.Detach();
					((BlockStatement)l.EmbeddedStatement).Statements.First().Remove(); // Remove 'Enter()' call
					tryCatch.ReplaceWith(l);
					node.Remove(); // remove flag variable
				}
			}
		}
		#endregion
		
		#region switch on strings
		static readonly IfElseStatement switchOnStringPattern = new IfElseStatement {
			Condition = new BinaryOperatorExpression {
				Left = new AnyNode("switchExpr"),
				Operator = BinaryOperatorType.InEquality,
				Right = new NullReferenceExpression()
			},
			TrueStatement = new BlockStatement {
				new IfElseStatement {
					Condition = new BinaryOperatorExpression {
						Left = new AnyNode("cachedDict"),
						Operator = BinaryOperatorType.Equality,
						Right = new NullReferenceExpression()
					},
					TrueStatement = new AnyNode("dictCreation")
				},
				new VariableDeclarationStatement {
					Type = new PrimitiveType("int"),
					Variables = { new NamedNode("intVar", new VariableInitializer()) }
				},
				new IfElseStatement {
					Condition = new Backreference("cachedDict").ToExpression().Invoke(
						"TryGetValue",
						new NamedNode("switchVar", new IdentifierExpression()),
						new DirectionExpression {
							FieldDirection = FieldDirection.Out,
							Expression = new IdentifierExpressionBackreference("intVar")
						}),
					TrueStatement = new BlockStatement {
						Statements = {
							new NamedNode(
								"switch", new SwitchStatement {
									Expression = new IdentifierExpressionBackreference("intVar"),
									SwitchSections = { new Repeat(new AnyNode()) }
								})
						}
					}
				},
				new Repeat(new AnyNode("nonNullDefaultStmt")).ToStatement()
			},
			FalseStatement = new OptionalNode("nullStmt", new BlockStatement { Statements = { new Repeat(new AnyNode()) } })
		};
		
		public void TransformSwitchOnString(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.OfType<IfElseStatement>().ToArray()) {
				Match m = switchOnStringPattern.Match(node);
				if (m == null)
					continue;
				if (m.Has("nonNullDefaultStmt") && !m.Has("nullStmt"))
					continue;
				// switchVar must be the same as switchExpr; or switchExpr must be an assignment and switchVar the left side of that assignment
				if (m.Get("switchVar").Single().Match(m.Get("switchExpr").Single()) == null) {
					AssignmentExpression assign = m.Get("switchExpr").Single() as AssignmentExpression;
					if (m.Get("switchVar").Single().Match(assign.Left) == null)
						continue;
				}
				FieldReference cachedDictField = m.Get("cachedDict").Single().Annotation<FieldReference>();
				if (cachedDictField == null || !cachedDictField.DeclaringType.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
					continue;
				List<Statement> dictCreation = m.Get<BlockStatement>("dictCreation").Single().Statements.ToList();
				List<KeyValuePair<string, int>> dict = BuildDictionary(dictCreation);
				SwitchStatement sw = m.Get<SwitchStatement>("switch").Single();
				sw.Expression = m.Get<Expression>("switchExpr").Single().Detach();
				foreach (SwitchSection section in sw.SwitchSections) {
					List<CaseLabel> labels = section.CaseLabels.ToList();
					section.CaseLabels.Clear();
					foreach (CaseLabel label in labels) {
						PrimitiveExpression expr = label.Expression as PrimitiveExpression;
						if (expr == null || !(expr.Value is int))
							continue;
						int val = (int)expr.Value;
						foreach (var pair in dict) {
							if (pair.Value == val)
								section.CaseLabels.Add(new CaseLabel { Expression = new PrimitiveExpression(pair.Key) });
						}
					}
				}
				if (m.Has("nullStmt")) {
					SwitchSection section = new SwitchSection();
					section.CaseLabels.Add(new CaseLabel { Expression = new NullReferenceExpression() });
					BlockStatement block = m.Get<BlockStatement>("nullStmt").Single();
					block.Statements.Add(new BreakStatement());
					section.Statements.Add(block.Detach());
					sw.SwitchSections.Add(section);
					if (m.Has("nonNullDefaultStmt")) {
						section = new SwitchSection();
						section.CaseLabels.Add(new CaseLabel());
						block = new BlockStatement();
						block.Statements.AddRange(m.Get<Statement>("nonNullDefaultStmt").Select(s => s.Detach()));
						block.Add(new BreakStatement());
						section.Statements.Add(block);
						sw.SwitchSections.Add(section);
					}
				}
				node.ReplaceWith(sw);
			}
		}
		
		List<KeyValuePair<string, int>> BuildDictionary(List<Statement> dictCreation)
		{
			List<KeyValuePair<string, int>> dict = new List<KeyValuePair<string, int>>();
			for (int i = 0; i < dictCreation.Count; i++) {
				ExpressionStatement es = dictCreation[i] as ExpressionStatement;
				if (es == null)
					continue;
				InvocationExpression ie = es.Expression as InvocationExpression;
				if (ie == null)
					continue;
				PrimitiveExpression arg1 = ie.Arguments.ElementAtOrDefault(0) as PrimitiveExpression;
				PrimitiveExpression arg2 = ie.Arguments.ElementAtOrDefault(1) as PrimitiveExpression;
				if (arg1 != null && arg2 != null && arg1.Value is string && arg2.Value is int)
					dict.Add(new KeyValuePair<string, int>((string)arg1.Value, (int)arg2.Value));
			}
			return dict;
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
					FieldDefinition field = m.Get("fieldReference").Single().Annotation<FieldReference>().ResolveWithinSameModule();
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
		
		#region Automatic Events
		static readonly Accessor automaticEventPatternV4 = new Accessor {
			Body = new BlockStatement {
				new VariableDeclarationStatement {
					Type = new AnyNode("type"),
					Variables = {
						new NamedNode(
							"var1", new VariableInitializer {
								Initializer = new NamedNode("field", new MemberReferenceExpression { Target = new ThisReferenceExpression() })
							})}
				},
				new VariableDeclarationStatement {
					Type = new Backreference("type"),
					Variables = { new NamedNode("var2", new VariableInitializer()) }
				},
				new DoWhileStatement {
					EmbeddedStatement = new BlockStatement {
						new AssignmentExpression(new IdentifierExpressionBackreference("var2"), new IdentifierExpressionBackreference("var1")),
						new VariableDeclarationStatement {
							Type = new Backreference("type"),
							Variables = {
								new NamedNode(
									"var3", new VariableInitializer {
										Initializer = new AnyNode("delegateCombine").ToExpression().Invoke(
											new IdentifierExpressionBackreference("var2"),
											new IdentifierExpression("value")
										).CastTo(new Backreference("type"))
									})
							}},
						new AssignmentExpression {
							Left = new IdentifierExpressionBackreference("var1"),
							Right = new TypePattern(typeof(System.Threading.Interlocked)).ToType().Invoke(
								"CompareExchange",
								new AstType[] { new Backreference("type") }, // type argument
								new Expression[] { // arguments
									new DirectionExpression { FieldDirection = FieldDirection.Ref, Expression = new Backreference("field") },
									new IdentifierExpressionBackreference("var3"),
									new IdentifierExpressionBackreference("var2")
								}
							)}
					},
					Condition = new BinaryOperatorExpression {
						Left = new IdentifierExpressionBackreference("var1"),
						Operator = BinaryOperatorType.InEquality,
						Right = new IdentifierExpressionBackreference("var2")
					}}
			}};
		
		bool CheckAutomaticEventV4Match(Match m, CustomEventDeclaration ev, bool isAddAccessor)
		{
			if (m == null)
				return false;
			if (m.Get<MemberReferenceExpression>("field").Single().MemberName != ev.Name)
				return false; // field name must match event name
			if (ev.ReturnType.Match(m.Get("type").Single()) == null)
				return false; // variable types must match event type
			var combineMethod = m.Get("delegateCombine").Single().Parent.Annotation<MethodReference>();
			if (combineMethod == null || combineMethod.Name != (isAddAccessor ? "Combine" : "Remove"))
				return false;
			return combineMethod.DeclaringType.FullName == "System.Delegate";
		}
		
		void TransformAutomaticEvents(AstNode compilationUnit)
		{
			foreach (var ev in compilationUnit.Descendants.OfType<CustomEventDeclaration>().ToArray()) {
				Match m1 = automaticEventPatternV4.Match(ev.AddAccessor);
				if (!CheckAutomaticEventV4Match(m1, ev, true))
					continue;
				Match m2 = automaticEventPatternV4.Match(ev.RemoveAccessor);
				if (!CheckAutomaticEventV4Match(m2, ev, false))
					continue;
				EventDeclaration ed = new EventDeclaration();
				ev.Attributes.MoveTo(ed.Attributes);
				ed.ReturnType = ev.ReturnType.Detach();
				ed.Modifiers = ev.Modifiers;
				ed.Variables.Add(new VariableInitializer(ev.Name));
				ed.CopyAnnotationsFrom(ev);
				
				EventDefinition eventDef = ev.Annotation<EventDefinition>();
				if (eventDef != null) {
					FieldDefinition field = eventDef.DeclaringType.Fields.FirstOrDefault(f => f.Name == ev.Name);
					if (field != null) {
						ed.AddAnnotation(field);
						AstBuilder.ConvertAttributes(ed, field, AttributeTarget.Field);
					}
				}
				
				ev.ReplaceWith(ed);
			}
		}
		#endregion
		
		#region Pattern Matching Helpers
		sealed class TypePattern : Pattern
		{
			readonly string ns;
			readonly string name;
			
			public TypePattern(Type type)
			{
				this.ns = type.Namespace;
				this.name = type.Name;
			}
			
			protected override bool DoMatch(AstNode other, Match match)
			{
				if (other == null)
					return false;
				TypeReference tr = other.Annotation<TypeReference>();
				return tr != null && tr.Namespace == ns && tr.Name == name;
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}
}
