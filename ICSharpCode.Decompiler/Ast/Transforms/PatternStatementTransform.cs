// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Finds the expanded form of using statements using pattern matching and replaces it with a UsingStatement.
	/// </summary>
	public sealed class PatternStatementTransform : ContextTrackingVisitor<AstNode>, IAstTransform
	{
		public PatternStatementTransform(DecompilerContext context) : base(context)
		{
		}
		
		#region Visitor Overrides
		protected override AstNode VisitChildren(AstNode node, object data)
		{
			// Go through the children, and keep visiting a node as long as it changes.
			// Because some transforms delete/replace nodes before and after the node being transformed, we rely
			// on the transform's return value to know where we need to keep iterating.
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				AstNode oldChild;
				do {
					oldChild = child;
					child = child.AcceptVisitor(this, data);
					Debug.Assert(child != null && child.Parent == node);
				} while (child != oldChild);
			}
			return node;
		}
		
		public override AstNode VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			AstNode result;
			if (context.Settings.UsingStatement) {
				result = TransformUsings(expressionStatement);
				if (result != null)
					return result;
				result = TransformNonGenericForEach(expressionStatement);
				if (result != null)
					return result;
			}
			result = TransformFor(expressionStatement);
			if (result != null)
				return result;
			if (context.Settings.LockStatement) {
				result = TransformLock(expressionStatement);
				if (result != null)
					return result;
			}
			return base.VisitExpressionStatement(expressionStatement, data);
		}
		
		public override AstNode VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			if (context.Settings.ForEachStatement) {
				AstNode result = TransformForeach(usingStatement);
				if (result != null)
					return result;
			}
			return base.VisitUsingStatement(usingStatement, data);
		}
		
		public override AstNode VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			return TransformDoWhile(whileStatement) ?? base.VisitWhileStatement(whileStatement, data);
		}
		
		public override AstNode VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			if (context.Settings.SwitchStatementOnString) {
				AstNode result = TransformSwitchOnString(ifElseStatement);
				if (result != null)
					return result;
			}
			return base.VisitIfElseStatement(ifElseStatement, data);
		}
		
		public override AstNode VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			if (context.Settings.AutomaticProperties) {
				AstNode result = TransformAutomaticProperties(propertyDeclaration);
				if (result != null)
					return result;
			}
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}
		
		public override AstNode VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration, object data)
		{
			// first apply transforms to the accessor bodies
			base.VisitCustomEventDeclaration(eventDeclaration, data);
			if (context.Settings.AutomaticEvents) {
				AstNode result = TransformAutomaticEvents(eventDeclaration);
				if (result != null)
					return result;
			}
			return eventDeclaration;
		}
		
		public override AstNode VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			return TransformDestructor(methodDeclaration) ?? base.VisitMethodDeclaration(methodDeclaration, data);
		}
		
		public override AstNode VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			return TransformTryCatchFinally(tryCatchStatement) ?? base.VisitTryCatchStatement(tryCatchStatement, data);
		}
		#endregion
		
		/// <summary>
		/// $variable = $initializer;
		/// </summary>
		static readonly AstNode variableAssignPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("variable", new IdentifierExpression()),
				new AnyNode("initializer")
			));
		
		#region using
		static Expression InvokeDispose(Expression identifier)
		{
			return new Choice {
				identifier.Invoke("Dispose"),
				identifier.Clone().CastTo(new TypePattern(typeof(IDisposable))).Invoke("Dispose")
			};
		}
		
		static readonly AstNode usingTryCatchPattern = new TryCatchStatement {
			TryBlock = new AnyNode(),
			FinallyBlock = new BlockStatement {
				new Choice {
					{ "valueType",
						new ExpressionStatement(InvokeDispose(new NamedNode("ident", new IdentifierExpression())))
					},
					{ "referenceType",
						new IfElseStatement {
							Condition = new BinaryOperatorExpression(
								new NamedNode("ident", new IdentifierExpression()),
								BinaryOperatorType.InEquality,
								new NullReferenceExpression()
							),
							TrueStatement = new BlockStatement {
								new ExpressionStatement(InvokeDispose(new Backreference("ident")))
							}
						}
					}
				}.ToStatement()
			}
		};
		
		public UsingStatement TransformUsings(ExpressionStatement node)
		{
			Match m1 = variableAssignPattern.Match(node);
			if (!m1.Success) return null;
			TryCatchStatement tryCatch = node.NextSibling as TryCatchStatement;
			Match m2 = usingTryCatchPattern.Match(tryCatch);
			if (!m2.Success) return null;
			string variableName = m1.Get<IdentifierExpression>("variable").Single().Identifier;
			if (variableName != m2.Get<IdentifierExpression>("ident").Single().Identifier)
				return null;
			if (m2.Has("valueType")) {
				// if there's no if(x!=null), then it must be a value type
				ILVariable v = m1.Get<AstNode>("variable").Single().Annotation<ILVariable>();
				if (v == null || v.Type == null || !v.Type.IsValueType)
					return null;
			}
			
			// There are two variants of the using statement:
			// "using (var a = init)" and "using (expr)".
			// The former declares a read-only variable 'a', and the latter declares an unnamed read-only variable
			// to store the original value of 'expr'.
			// This means that in order to introduce a using statement, in both cases we need to detect a read-only
			// variable that is used only within that block.
			
			if (HasAssignment(tryCatch, variableName))
				return null;
			
			VariableDeclarationStatement varDecl = FindVariableDeclaration(node, variableName);
			if (varDecl == null || !(varDecl.Parent is BlockStatement))
				return null;
			
			// Validate that the variable is not used after the using statement:
			if (!IsVariableValueUnused(varDecl, tryCatch))
				return null;
			
			node.Remove();
			
			UsingStatement usingStatement = new UsingStatement();
			usingStatement.EmbeddedStatement = tryCatch.TryBlock.Detach();
			tryCatch.ReplaceWith(usingStatement);
			
			// If possible, we'll eliminate the variable completely:
			if (usingStatement.EmbeddedStatement.Descendants.OfType<IdentifierExpression>().Any(ident => ident.Identifier == variableName)) {
				// variable is used, so we'll create a variable declaration
				usingStatement.ResourceAcquisition = new VariableDeclarationStatement {
					Type = (AstType)varDecl.Type.Clone(),
					Variables = {
						new VariableInitializer {
							Name = variableName,
							Initializer = m1.Get<Expression>("initializer").Single().Detach()
						}.CopyAnnotationsFrom(node.Expression)
						.WithAnnotation(m1.Get<AstNode>("variable").Single().Annotation<ILVariable>())
					}
				}.CopyAnnotationsFrom(node);
			} else {
				// the variable is never used; eliminate it:
				usingStatement.ResourceAcquisition = m1.Get<Expression>("initializer").Single().Detach();
			}
			return usingStatement;
		}
		
		internal static VariableDeclarationStatement FindVariableDeclaration(AstNode node, string identifier)
		{
			while (node != null) {
				while (node.PrevSibling != null) {
					node = node.PrevSibling;
					VariableDeclarationStatement varDecl = node as VariableDeclarationStatement;
					if (varDecl != null && varDecl.Variables.Count == 1 && varDecl.Variables.Single().Name == identifier) {
						return varDecl;
					}
				}
				node = node.Parent;
			}
			return null;
		}
		
		/// <summary>
		/// Gets whether the old variable value (assigned inside 'targetStatement' or earlier)
		/// is read anywhere in the remaining scope of the variable declaration.
		/// </summary>
		bool IsVariableValueUnused(VariableDeclarationStatement varDecl, Statement targetStatement)
		{
			Debug.Assert(targetStatement.Ancestors.Contains(varDecl.Parent));
			BlockStatement block = (BlockStatement)varDecl.Parent;
			DefiniteAssignmentAnalysis daa = new DefiniteAssignmentAnalysis(block, context.CancellationToken);
			daa.SetAnalyzedRange(targetStatement, block, startInclusive: false);
			daa.Analyze(varDecl.Variables.Single().Name);
			return daa.UnassignedVariableUses.Count == 0;
		}
		
		// I used this in the first implementation of the using-statement transform, but now no longer
		// because there were problems when multiple using statements were using the same variable
		// - no single using statement could be transformed without making the C# code invalid,
		// but transforming both would work.
		// We now use 'IsVariableValueUnused' which will perform the transform
		// even if it results in two variables with the same name and overlapping scopes.
		// (this issue could be fixed later by renaming one of the variables)
		
		// I'm not sure whether the other consumers of 'CanMoveVariableDeclarationIntoStatement' should be changed the same way.
		bool CanMoveVariableDeclarationIntoStatement(VariableDeclarationStatement varDecl, Statement targetStatement, out Statement declarationPoint)
		{
			Debug.Assert(targetStatement.Ancestors.Contains(varDecl.Parent));
			// Find all blocks between targetStatement and varDecl.Parent
			List<BlockStatement> blocks = targetStatement.Ancestors.TakeWhile(block => block != varDecl.Parent).OfType<BlockStatement>().ToList();
			blocks.Add((BlockStatement)varDecl.Parent); // also handle the varDecl.Parent block itself
			blocks.Reverse(); // go from parent blocks to child blocks
			DefiniteAssignmentAnalysis daa = new DefiniteAssignmentAnalysis(blocks[0], context.CancellationToken);
			declarationPoint = null;
			foreach (BlockStatement block in blocks) {
				if (!DeclareVariables.FindDeclarationPoint(daa, varDecl, block, out declarationPoint)) {
					return false;
				}
			}
			return true;
		}
		
		/// <summary>
		/// Gets whether there is an assignment to 'variableName' anywhere within the given node.
		/// </summary>
		bool HasAssignment(AstNode root, string variableName)
		{
			foreach (AstNode node in root.DescendantsAndSelf) {
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null && ident.Identifier == variableName) {
					if (ident.Parent is AssignmentExpression && ident.Role == AssignmentExpression.LeftRole
					    || ident.Parent is DirectionExpression)
					{
						return true;
					}
				}
			}
			return false;
		}
		#endregion
		
		#region foreach (generic)
		static readonly UsingStatement genericForeachPattern = new UsingStatement {
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
			EmbeddedStatement = new BlockStatement {
				new Repeat(
					new VariableDeclarationStatement { Type = new AnyNode(), Variables = { new VariableInitializer() } }.WithName("variablesOutsideLoop")
				).ToStatement(),
				new WhileStatement {
					Condition = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Invoke("MoveNext"),
					EmbeddedStatement = new BlockStatement {
						new Repeat(
							new VariableDeclarationStatement { Type = new AnyNode(), Variables = { new VariableInitializer() } }.WithName("variablesInsideLoop")
						).ToStatement(),
						new AssignmentExpression {
							Left = new IdentifierExpression().WithName("itemVariable"),
							Operator = AssignmentOperatorType.Assign,
							Right = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Member("Current")
						},
						new Repeat(new AnyNode("statement")).ToStatement()
					}
				}.WithName("loop")
			}};
		
		public ForeachStatement TransformForeach(UsingStatement node)
		{
			Match m = genericForeachPattern.Match(node);
			if (!m.Success)
				return null;
			if (!(node.Parent is BlockStatement) && m.Has("variablesOutsideLoop")) {
				// if there are variables outside the loop, we need to put those into the parent block, and that won't work if the direct parent isn't a block
				return null;
			}
			VariableInitializer enumeratorVar = m.Get<VariableInitializer>("enumeratorVariable").Single();
			IdentifierExpression itemVar = m.Get<IdentifierExpression>("itemVariable").Single();
			WhileStatement loop = m.Get<WhileStatement>("loop").Single();
			
			// Find the declaration of the item variable:
			// Because we look only outside the loop, we won't make the mistake of moving a captured variable across the loop boundary
			VariableDeclarationStatement itemVarDecl = FindVariableDeclaration(loop, itemVar.Identifier);
			if (itemVarDecl == null || !(itemVarDecl.Parent is BlockStatement))
				return null;
			
			// Now verify that we can move the variable declaration in front of the loop:
			Statement declarationPoint;
			CanMoveVariableDeclarationIntoStatement(itemVarDecl, loop, out declarationPoint);
			// We ignore the return value because we don't care whether we can move the variable into the loop
			// (that is possible only with non-captured variables).
			// We just care that we can move it in front of the loop:
			if (declarationPoint != loop)
				return null;
			
			BlockStatement newBody = new BlockStatement();
			foreach (Statement stmt in m.Get<Statement>("variablesInsideLoop"))
				newBody.Add(stmt.Detach());
			foreach (Statement stmt in m.Get<Statement>("statement"))
				newBody.Add(stmt.Detach());
			
			ForeachStatement foreachStatement = new ForeachStatement {
				VariableType = (AstType)itemVarDecl.Type.Clone(),
				VariableName = itemVar.Identifier,
				InExpression = m.Get<Expression>("collection").Single().Detach(),
				EmbeddedStatement = newBody
			}.WithAnnotation(itemVarDecl.Variables.Single().Annotation<ILVariable>());
			if (foreachStatement.InExpression is BaseReferenceExpression) {
				foreachStatement.InExpression = new ThisReferenceExpression().CopyAnnotationsFrom(foreachStatement.InExpression);
			}
			node.ReplaceWith(foreachStatement);
			foreach (Statement stmt in m.Get<Statement>("variablesOutsideLoop")) {
				((BlockStatement)foreachStatement.Parent).Statements.InsertAfter(null, stmt.Detach());
			}
			return foreachStatement;
		}
		#endregion
		
		#region foreach (non-generic)
		ExpressionStatement getEnumeratorPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("left", new IdentifierExpression()),
				new AnyNode("collection").ToExpression().Invoke("GetEnumerator")
			));
		
		TryCatchStatement nonGenericForeachPattern = new TryCatchStatement {
			TryBlock = new BlockStatement {
				new WhileStatement {
					Condition = new IdentifierExpression().WithName("enumerator").Invoke("MoveNext"),
					EmbeddedStatement = new BlockStatement {
						new AssignmentExpression(
							new IdentifierExpression().WithName("itemVar"),
							new Choice {
								new Backreference("enumerator").ToExpression().Member("Current"),
								new CastExpression {
									Type = new AnyNode("castType"),
									Expression = new Backreference("enumerator").ToExpression().Member("Current")
								}
							}
						),
						new Repeat(new AnyNode("stmt")).ToStatement()
					}
				}.WithName("loop")
			},
			FinallyBlock = new BlockStatement {
				new AssignmentExpression(
					new IdentifierExpression().WithName("disposable"),
					new Backreference("enumerator").ToExpression().CastAs(new TypePattern(typeof(IDisposable)))
				),
				new IfElseStatement {
					Condition = new BinaryOperatorExpression {
						Left = new Backreference("disposable"),
						Operator = BinaryOperatorType.InEquality,
						Right = new NullReferenceExpression()
					},
					TrueStatement = new BlockStatement {
						new Backreference("disposable").ToExpression().Invoke("Dispose")
					}
				}
			}};
		
		public ForeachStatement TransformNonGenericForEach(ExpressionStatement node)
		{
			Match m1 = getEnumeratorPattern.Match(node);
			if (!m1.Success) return null;
			AstNode tryCatch = node.NextSibling;
			Match m2 = nonGenericForeachPattern.Match(tryCatch);
			if (!m2.Success) return null;
			
			IdentifierExpression enumeratorVar = m2.Get<IdentifierExpression>("enumerator").Single();
			IdentifierExpression itemVar = m2.Get<IdentifierExpression>("itemVar").Single();
			WhileStatement loop = m2.Get<WhileStatement>("loop").Single();
			
			// verify that the getEnumeratorPattern assigns to the same variable as the nonGenericForeachPattern is reading from
			if (!enumeratorVar.IsMatch(m1.Get("left").Single()))
				return null;
			
			VariableDeclarationStatement enumeratorVarDecl = FindVariableDeclaration(loop, enumeratorVar.Identifier);
			if (enumeratorVarDecl == null || !(enumeratorVarDecl.Parent is BlockStatement))
				return null;
			
			// Find the declaration of the item variable:
			// Because we look only outside the loop, we won't make the mistake of moving a captured variable across the loop boundary
			VariableDeclarationStatement itemVarDecl = FindVariableDeclaration(loop, itemVar.Identifier);
			if (itemVarDecl == null || !(itemVarDecl.Parent is BlockStatement))
				return null;
			
			// Now verify that we can move the variable declaration in front of the loop:
			Statement declarationPoint;
			CanMoveVariableDeclarationIntoStatement(itemVarDecl, loop, out declarationPoint);
			// We ignore the return value because we don't care whether we can move the variable into the loop
			// (that is possible only with non-captured variables).
			// We just care that we can move it in front of the loop:
			if (declarationPoint != loop)
				return null;

			ForeachStatement foreachStatement = new ForeachStatement
			{
				VariableType = itemVarDecl.Type.Clone(),
				VariableName = itemVar.Identifier,
			}.WithAnnotation(itemVarDecl.Variables.Single().Annotation<ILVariable>());
			BlockStatement body = new BlockStatement();
			foreachStatement.EmbeddedStatement = body;
			((BlockStatement)node.Parent).Statements.InsertBefore(node, foreachStatement);
			
			body.Add(node.Detach());
			body.Add((Statement)tryCatch.Detach());
			
			// Now that we moved the whole try-catch into the foreach loop; verify that we can
			// move the enumerator into the foreach loop:
			CanMoveVariableDeclarationIntoStatement(enumeratorVarDecl, foreachStatement, out declarationPoint);
			if (declarationPoint != foreachStatement) {
				// oops, the enumerator variable can't be moved into the foreach loop
				// Undo our AST changes:
				((BlockStatement)foreachStatement.Parent).Statements.InsertBefore(foreachStatement, node.Detach());
				foreachStatement.ReplaceWith(tryCatch);
				return null;
			}
			
			// Now create the correct body for the foreach statement:
			foreachStatement.InExpression = m1.Get<Expression>("collection").Single().Detach();
			if (foreachStatement.InExpression is BaseReferenceExpression) {
				foreachStatement.InExpression = new ThisReferenceExpression().CopyAnnotationsFrom(foreachStatement.InExpression);
			}
			body.Statements.Clear();
			body.Statements.AddRange(m2.Get<Statement>("stmt").Select(stmt => stmt.Detach()));
			
			return foreachStatement;
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
		
		public ForStatement TransformFor(ExpressionStatement node)
		{
			Match m1 = variableAssignPattern.Match(node);
			if (!m1.Success) return null;
			AstNode next = node.NextSibling;
			Match m2 = forPattern.Match(next);
			if (!m2.Success) return null;
			// ensure the variable in the for pattern is the same as in the declaration
			if (m1.Get<IdentifierExpression>("variable").Single().Identifier != m2.Get<IdentifierExpression>("ident").Single().Identifier)
				return null;
			WhileStatement loop = (WhileStatement)next;
			node.Remove();
			BlockStatement newBody = new BlockStatement();
			foreach (Statement stmt in m2.Get<Statement>("statement"))
				newBody.Add(stmt.Detach());
			ForStatement forStatement = new ForStatement();
			forStatement.Initializers.Add(node);
			forStatement.Condition = loop.Condition.Detach();
			forStatement.Iterators.Add(m2.Get<Statement>("increment").Single().Detach());
			forStatement.EmbeddedStatement = newBody;
			loop.ReplaceWith(forStatement);
			return forStatement;
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
		
		public DoWhileStatement TransformDoWhile(WhileStatement whileLoop)
		{
			Match m = doWhilePattern.Match(whileLoop);
			if (m.Success) {
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
				return doLoop;
			}
			return null;
		}
		#endregion
		
		#region lock
		static readonly AstNode lockFlagInitPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("variable", new IdentifierExpression()),
				new PrimitiveExpression(false)
			));
		
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
		
		public LockStatement TransformLock(ExpressionStatement node)
		{
			Match m1 = lockFlagInitPattern.Match(node);
			if (!m1.Success) return null;
			AstNode tryCatch = node.NextSibling;
			Match m2 = lockTryCatchPattern.Match(tryCatch);
			if (!m2.Success) return null;
			if (m1.Get<IdentifierExpression>("variable").Single().Identifier == m2.Get<IdentifierExpression>("flag").Single().Identifier) {
				Expression enter = m2.Get<Expression>("enter").Single();
				IdentifierExpression exit = m2.Get<IdentifierExpression>("exit").Single();
				if (!exit.IsMatch(enter)) {
					// If exit and enter are not the same, then enter must be "exit = ..."
					AssignmentExpression assign = enter as AssignmentExpression;
					if (assign == null)
						return null;
					if (!exit.IsMatch(assign.Left))
						return null;
					enter = assign.Right;
					// TODO: verify that 'obj' variable can be removed
				}
				// TODO: verify that 'flag' variable can be removed
				// transform the code into a lock statement:
				LockStatement l = new LockStatement();
				l.Expression = enter.Detach();
				l.EmbeddedStatement = ((TryCatchStatement)tryCatch).TryBlock.Detach();
				((BlockStatement)l.EmbeddedStatement).Statements.First().Remove(); // Remove 'Enter()' call
				tryCatch.ReplaceWith(l);
				node.Remove(); // remove flag variable
				return l;
			}
			return null;
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
				new IfElseStatement {
					Condition = new Backreference("cachedDict").ToExpression().Invoke(
						"TryGetValue",
						new NamedNode("switchVar", new IdentifierExpression()),
						new DirectionExpression {
							FieldDirection = FieldDirection.Out,
							Expression = new IdentifierExpression().WithName("intVar")
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
		
		public SwitchStatement TransformSwitchOnString(IfElseStatement node)
		{
			Match m = switchOnStringPattern.Match(node);
			if (!m.Success)
				return null;
			// switchVar must be the same as switchExpr; or switchExpr must be an assignment and switchVar the left side of that assignment
			if (!m.Get("switchVar").Single().IsMatch(m.Get("switchExpr").Single())) {
				AssignmentExpression assign = m.Get("switchExpr").Single() as AssignmentExpression;
				if (!(assign != null && m.Get("switchVar").Single().IsMatch(assign.Left)))
					return null;
			}
			FieldReference cachedDictField = m.Get<AstNode>("cachedDict").Single().Annotation<FieldReference>();
			if (cachedDictField == null || !cachedDictField.DeclaringType.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
				return null;
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
			} else if (m.Has("nonNullDefaultStmt")) {
				sw.SwitchSections.Add(
					new SwitchSection {
						CaseLabels = { new CaseLabel { Expression = new NullReferenceExpression() } },
						Statements = { new BlockStatement { new BreakStatement() } }
					});
			}
			if (m.Has("nonNullDefaultStmt")) {
				SwitchSection section = new SwitchSection();
				section.CaseLabels.Add(new CaseLabel());
				BlockStatement block = new BlockStatement();
				block.Statements.AddRange(m.Get<Statement>("nonNullDefaultStmt").Select(s => s.Detach()));
				block.Add(new BreakStatement());
				section.Statements.Add(block);
				sw.SwitchSections.Add(section);
			}
			node.ReplaceWith(sw);
			return sw;
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
			PrivateImplementationType = new OptionalNode(new AnyNode()),
			Getter = new Accessor {
				Attributes = { new Repeat(new AnyNode()) },
				Modifiers = Modifiers.Any,
				Body = new BlockStatement {
					new ReturnStatement {
						Expression = new AnyNode("fieldReference")
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
		
		PropertyDeclaration TransformAutomaticProperties(PropertyDeclaration property)
		{
			PropertyDefinition cecilProperty = property.Annotation<PropertyDefinition>();
			if (cecilProperty == null || cecilProperty.GetMethod == null || cecilProperty.SetMethod == null)
				return null;
			if (!(cecilProperty.GetMethod.IsCompilerGenerated() && cecilProperty.SetMethod.IsCompilerGenerated()))
				return null;
			Match m = automaticPropertyPattern.Match(property);
			if (m.Success) {
				FieldDefinition field = m.Get<AstNode>("fieldReference").Single().Annotation<FieldReference>().ResolveWithinSameModule();
				if (field.IsCompilerGenerated() && field.DeclaringType == cecilProperty.DeclaringType) {
					RemoveCompilerGeneratedAttribute(property.Getter.Attributes);
					RemoveCompilerGeneratedAttribute(property.Setter.Attributes);
					property.Getter.Body = null;
					property.Setter.Body = null;
				}
			}
			// Since the event instance is not changed, we can continue in the visitor as usual, so return null
			return null;
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
				new VariableDeclarationStatement { Type = new AnyNode("type"), Variables = { new AnyNode() } },
				new VariableDeclarationStatement { Type = new Backreference("type"), Variables = { new AnyNode() } },
				new VariableDeclarationStatement { Type = new Backreference("type"), Variables = { new AnyNode() } },
				new AssignmentExpression {
					Left = new NamedNode("var1", new IdentifierExpression()),
					Operator = AssignmentOperatorType.Assign,
					Right = new NamedNode(
						"field",
						new MemberReferenceExpression {
							Target = new Choice { new ThisReferenceExpression(), new TypeReferenceExpression { Type = new AnyNode() } }
						})
				},
				new DoWhileStatement {
					EmbeddedStatement = new BlockStatement {
						new AssignmentExpression(new NamedNode("var2", new IdentifierExpression()), new IdentifierExpressionBackreference("var1")),
						new AssignmentExpression {
							Left = new NamedNode("var3", new IdentifierExpression()),
							Operator = AssignmentOperatorType.Assign,
							Right = new AnyNode("delegateCombine").ToExpression().Invoke(
								new IdentifierExpressionBackreference("var2"),
								new IdentifierExpression("value")
							).CastTo(new Backreference("type"))
						},
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
			if (!m.Success)
				return false;
			if (m.Get<MemberReferenceExpression>("field").Single().MemberName != ev.Name)
				return false; // field name must match event name
			if (!ev.ReturnType.IsMatch(m.Get("type").Single()))
				return false; // variable types must match event type
			var combineMethod = m.Get<AstNode>("delegateCombine").Single().Parent.Annotation<MethodReference>();
			if (combineMethod == null || combineMethod.Name != (isAddAccessor ? "Combine" : "Remove"))
				return false;
			return combineMethod.DeclaringType.FullName == "System.Delegate";
		}
		
		EventDeclaration TransformAutomaticEvents(CustomEventDeclaration ev)
		{
			Match m1 = automaticEventPatternV4.Match(ev.AddAccessor);
			if (!CheckAutomaticEventV4Match(m1, ev, true))
				return null;
			Match m2 = automaticEventPatternV4.Match(ev.RemoveAccessor);
			if (!CheckAutomaticEventV4Match(m2, ev, false))
				return null;
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
					AstBuilder.ConvertAttributes(ed, field, "field");
				}
			}
			
			ev.ReplaceWith(ed);
			return ed;
		}
		#endregion
		
		#region Destructor
		static readonly MethodDeclaration destructorPattern = new MethodDeclaration {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			ReturnType = new PrimitiveType("void"),
			Name = "Finalize",
			Body = new BlockStatement {
				new TryCatchStatement {
					TryBlock = new AnyNode("body"),
					FinallyBlock = new BlockStatement {
						new BaseReferenceExpression().Invoke("Finalize")
					}
				}
			}
		};
		
		DestructorDeclaration TransformDestructor(MethodDeclaration methodDef)
		{
			Match m = destructorPattern.Match(methodDef);
			if (m.Success) {
				DestructorDeclaration dd = new DestructorDeclaration();
				methodDef.Attributes.MoveTo(dd.Attributes);
				dd.Modifiers = methodDef.Modifiers & ~(Modifiers.Protected | Modifiers.Override);
				dd.Body = m.Get<BlockStatement>("body").Single().Detach();
				dd.Name = AstBuilder.CleanName(context.CurrentType.Name);
				methodDef.ReplaceWith(dd);
				return dd;
			}
			return null;
		}
		#endregion
		
		#region Try-Catch-Finally
		static readonly TryCatchStatement tryCatchFinallyPattern = new TryCatchStatement {
			TryBlock = new BlockStatement {
				new TryCatchStatement {
					TryBlock = new AnyNode(),
					CatchClauses = { new Repeat(new AnyNode()) }
				}
			},
			FinallyBlock = new AnyNode()
		};
		
		/// <summary>
		/// Simplify nested 'try { try {} catch {} } finally {}'.
		/// This transformation must run after the using/lock tranformations.
		/// </summary>
		TryCatchStatement TransformTryCatchFinally(TryCatchStatement tryFinally)
		{
			if (tryCatchFinallyPattern.IsMatch(tryFinally)) {
				TryCatchStatement tryCatch = (TryCatchStatement)tryFinally.TryBlock.Statements.Single();
				tryFinally.TryBlock = tryCatch.TryBlock.Detach();
				tryCatch.CatchClauses.MoveTo(tryFinally.CatchClauses);
			}
			// Since the tryFinally instance is not changed, we can continue in the visitor as usual, so return null
			return null;
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
			
			public override bool DoMatch(INode other, Match match)
			{
				AstNode o = other as AstNode;
				if (o == null)
					return false;
				TypeReference tr = o.Annotation<TypeReference>();
				return tr != null && tr.Namespace == ns && tr.Name == name;
			}
		}
		#endregion
	}
}
