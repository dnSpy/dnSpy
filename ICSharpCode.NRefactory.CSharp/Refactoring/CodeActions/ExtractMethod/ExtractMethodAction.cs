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
using System.Threading.Tasks;

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
				var task = script.InsertWithCursor(context.TranslateString("Extract method"), Script.InsertPosition.Before, method);

				Action<Task> replaceStatements = delegate {
					var target = new IdentifierExpression(methodName);
					script.Replace(expression, new InvocationExpression(target));
					script.Link(target, method.NameToken);
				};

				if (task.IsCompleted) {
					replaceStatements (null);
				} else {
					task.ContinueWith (replaceStatements, TaskScheduler.FromCurrentSynchronizationContext ());
				}
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
				
				var inExtractedRegion = new VariableUsageAnalyzation (context, usedVariables);
				var lastStatement = statements [statements.Count - 1];
				
				var stmt = statements [0].GetParent<BlockStatement>();
				while (stmt.GetParent<BlockStatement> () != null) {
					stmt = stmt.GetParent<BlockStatement>();
				}
				
				inExtractedRegion.SetAnalyzedRange(statements [0], lastStatement);
				stmt.AcceptVisitor (inExtractedRegion);
				
				var beforeExtractedRegion = new VariableUsageAnalyzation (context, usedVariables);
				beforeExtractedRegion.SetAnalyzedRange(statements [0].Parent, statements [0], true, false);
				stmt.AcceptVisitor (beforeExtractedRegion);
				
				var afterExtractedRegion = new VariableUsageAnalyzation (context, usedVariables);
				afterExtractedRegion.SetAnalyzedRange(lastStatement, stmt.Statements.Last(), false, true);
				stmt.AcceptVisitor (afterExtractedRegion);
				usedVariables.Sort ((l, r) => l.Region.Begin.CompareTo (r.Region.Begin));

				IVariable generatedReturnVariable = null;
				foreach (var variable in usedVariables) {
					if ((variable is IParameter) || beforeExtractedRegion.Has (variable) || !afterExtractedRegion.Has (variable))
						continue;
					generatedReturnVariable = variable;
					method.ReturnType = context.CreateShortType (variable.Type);
					method.Body.Add (new ReturnStatement (new IdentifierExpression (variable.Name)));
					break;
				}

				foreach (var variable in usedVariables) {
					if (!(variable is IParameter) && !beforeExtractedRegion.Has (variable) && !afterExtractedRegion.Has (variable))
						continue;
					if (variable == generatedReturnVariable)
						continue;
					Expression argumentExpression = new IdentifierExpression(variable.Name); 
					
					ParameterModifier mod = ParameterModifier.None;
					if (inExtractedRegion.GetStatus (variable) == VariableState.Changed) {
						if (beforeExtractedRegion.GetStatus (variable) == VariableState.None) {
							mod = ParameterModifier.Out;
							argumentExpression = new DirectionExpression(FieldDirection.Out, argumentExpression);
						} else {
							mod = ParameterModifier.Ref;
							argumentExpression = new DirectionExpression(FieldDirection.Ref, argumentExpression);
						}
					}
					
					method.Parameters.Add(new ParameterDeclaration(context.CreateShortType(variable.Type), variable.Name, mod));
					invocation.Arguments.Add(argumentExpression);
				}
				var task = script.InsertWithCursor(context.TranslateString("Extract method"), Script.InsertPosition.Before, method);
				Action<Task> replaceStatements = delegate {
					foreach (var node in statements.Skip (1)) {
						script.Remove(node);
					}
					foreach (var variable in usedVariables) {
						if ((variable is IParameter) || beforeExtractedRegion.Has (variable) || !afterExtractedRegion.Has (variable))
							continue;
						if (variable == generatedReturnVariable)
							continue;
						script.InsertBefore (statements [0], new VariableDeclarationStatement (context.CreateShortType(variable.Type), variable.Name));
					}
					AstNode invocationStatement;

					if (generatedReturnVariable != null) {
						invocationStatement = new VariableDeclarationStatement (new SimpleType ("var"), generatedReturnVariable.Name, invocation);
					} else {
						invocationStatement = new ExpressionStatement(invocation);
					}
					script.Replace(statements [0], invocationStatement);


					script.Link(target, method.NameToken);
				};

				if (task.IsCompleted) {
					replaceStatements (null);
				} else {
					task.ContinueWith (replaceStatements, TaskScheduler.FromCurrentSynchronizationContext ());
				}
			});
		}
	}
}
