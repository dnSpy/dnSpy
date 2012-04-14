// 
// ExtractMethodAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.Analysis;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring.ExtractMethod
{
	[ContextAction("Extract method", Description = "Creates a new method out of selected text.")]
	public class ExtractMethodAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			if (!context.IsSomethingSelected)
				yield break;
			var selected = new List<AstNode>(context.GetSelectedNodes());
			if (selected.Count == 0)
				yield break;

			if (selected.Count == 1 && selected [0] is Expression) {
				var codeAction = CreateFromExpression(context, (Expression)selected [0]);
				if (codeAction == null)
					yield break;
				yield return codeAction;
			}

			foreach (var node in selected) {
				if (!(node is Statement))
					yield break;
			}
			var action = CreateFromStatements (context, new List<Statement> (selected.OfType<Statement> ()));
			if (action != null)
				yield return action;
		}

		CodeAction CreateFromExpression(RefactoringContext context, Expression expression)
		{
			var resolveResult = context.Resolve(expression);
			if (resolveResult.IsError)
				return null;

			return new CodeAction(context.TranslateString("Extract method"), script => {
				string methodName = "NewMethod";
				var method = new MethodDeclaration() {
					ReturnType = context.CreateShortType(resolveResult.Type),
					Name = methodName,
					Body = new BlockStatement() {
						new ReturnStatement(expression.Clone())
					}
				};
				if (!StaticVisitor.UsesNotStaticMember(context, expression))
					method.Modifiers |= Modifiers.Static;
				script.InsertWithCursor(context.TranslateString("Extract method"), method, Script.InsertPosition.Before);
				var target = new IdentifierExpression(methodName);
				script.Replace(expression, new InvocationExpression(target));
//				script.Link(target, method.NameToken);
			});
		}

		CodeAction CreateFromStatements(RefactoringContext context, List<Statement> statements)
		{
			if (!(statements [0].Parent is Statement))
				return null;

			return new CodeAction(context.TranslateString("Extract method"), script => {
				string methodName = "NewMethod";
				var method = new MethodDeclaration() {
					ReturnType = new PrimitiveType("void"),
					Name = methodName,
					Body = new BlockStatement()
				};
				bool usesNonStaticMember = false;
				foreach (Statement node in statements) {
					usesNonStaticMember |= StaticVisitor.UsesNotStaticMember(context, node);
					method.Body.Add(node.Clone());
				}
				if (!usesNonStaticMember)
					method.Modifiers |= Modifiers.Static;
				
				var target = new IdentifierExpression(methodName);
				var invocation = new InvocationExpression(target);

				var usedVariables = VariableLookupVisitor.Analyze(context, statements);

				var extractedCodeAnalysis = new DefiniteAssignmentAnalysis((Statement)statements [0].Parent, context.Resolver, context.CancellationToken);
				var lastStatement = statements [statements.Count - 1];
				extractedCodeAnalysis.SetAnalyzedRange(statements [0], lastStatement);
				var statusAfterMethod = new List<Tuple<IVariable, DefiniteAssignmentStatus>>();
				
				foreach (var variable in usedVariables) {
					extractedCodeAnalysis.Analyze(variable.Name, DefiniteAssignmentStatus.PotentiallyAssigned, context.CancellationToken);
					statusAfterMethod.Add(Tuple.Create(variable, extractedCodeAnalysis.GetStatusAfter(lastStatement)));
				}
				var stmt = statements [0].GetParent<BlockStatement>();
				while (stmt.GetParent<BlockStatement> () != null) {
					stmt = stmt.GetParent<BlockStatement>();
				}
				
				var wholeCodeAnalysis = new DefiniteAssignmentAnalysis(stmt, context.Resolver, context.CancellationToken);
				var statusBeforeMethod = new Dictionary<IVariable, DefiniteAssignmentStatus>();
				foreach (var variable in usedVariables) {
					wholeCodeAnalysis.Analyze(variable.Name, DefiniteAssignmentStatus.PotentiallyAssigned, context.CancellationToken);
					statusBeforeMethod [variable] = extractedCodeAnalysis.GetStatusBefore(statements [0]);
				}

				var afterCodeAnalysis = new DefiniteAssignmentAnalysis(stmt, context.Resolver, context.CancellationToken);
				var statusAtEnd = new Dictionary<IVariable, DefiniteAssignmentStatus>();
				afterCodeAnalysis.SetAnalyzedRange(lastStatement, stmt.Statements.Last(), false, true);

				foreach (var variable in usedVariables) {
					afterCodeAnalysis.Analyze(variable.Name, DefiniteAssignmentStatus.PotentiallyAssigned, context.CancellationToken);
					statusBeforeMethod [variable] = extractedCodeAnalysis.GetStatusBefore(statements [0]);
				}
				var beforeVisitor = new VariableLookupVisitor(context);
				beforeVisitor.SetAnalyzedRange(stmt, statements [0], true, false);
				stmt.AcceptVisitor(beforeVisitor);
				var afterVisitor = new VariableLookupVisitor(context);
				afterVisitor.SetAnalyzedRange(lastStatement, stmt, false, true);
				stmt.AcceptVisitor(afterVisitor);

				foreach (var status in statusAfterMethod) {
					if (!beforeVisitor.UsedVariables.Contains(status.Item1) && !afterVisitor.UsedVariables.Contains(status.Item1))
						continue;
					Expression argumentExpression = new IdentifierExpression(status.Item1.Name); 
					
					ParameterModifier mod;
					switch (status.Item2) {
						case DefiniteAssignmentStatus.AssignedAfterTrueExpression:
						case DefiniteAssignmentStatus.AssignedAfterFalseExpression:
						case DefiniteAssignmentStatus.PotentiallyAssigned:
							mod = ParameterModifier.Ref;
							argumentExpression = new DirectionExpression(FieldDirection.Ref, argumentExpression);
							break;
						case DefiniteAssignmentStatus.DefinitelyAssigned:
							if (statusBeforeMethod [status.Item1] != DefiniteAssignmentStatus.PotentiallyAssigned)
								goto case DefiniteAssignmentStatus.PotentiallyAssigned;
							mod = ParameterModifier.Out;
							argumentExpression = new DirectionExpression(FieldDirection.Out, argumentExpression);
							break;
//						case DefiniteAssignmentStatus.Unassigned:
						default:
							mod = ParameterModifier.None;
							break;
					}
					method.Parameters.Add(new ParameterDeclaration(context.CreateShortType(status.Item1.Type), status.Item1.Name, mod));
					invocation.Arguments.Add(argumentExpression);
				}

				foreach (var node in statements.Skip (1)) {
					script.Remove(node);
				}
				script.Replace(statements [0], new ExpressionStatement(invocation));
				script.InsertWithCursor(context.TranslateString("Extract method"), method, Script.InsertPosition.Before);
				//script.Link(target, method.NameToken);
			});
		}
	}
}
