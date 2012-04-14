// 
// DeclareLocalVariableAction.cs
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

using System.Threading;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Declare local variable", Description = "Declare a local variable out of a selected expression.")]
	public class DeclareLocalVariableAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			if (!context.IsSomethingSelected) {
				yield break;
			}
			var selected = new List<AstNode>(context.GetSelectedNodes());
			if (selected.Count != 1 || !(selected [0] is Expression)) {
				yield break;
			}
			var expr = selected [0] as Expression;
			var visitor = new SearchNodeVisitior(expr);
			
			var node = context.GetNode <BlockStatement>();
			if (node != null) {
				node.AcceptVisitor(visitor);
			}

			yield return new CodeAction(context.TranslateString("Declare local variable"), script => {
				var resolveResult = context.Resolve(expr);
				var guessedType = resolveResult.Type;
				if (resolveResult is MethodGroupResolveResult) {
					guessedType = GetDelegateType(context, ((MethodGroupResolveResult)resolveResult).Methods.First(), expr);
				}
				var name = CreateMethodDeclarationAction.CreateBaseName(expr, guessedType);
				var type = context.UseExplicitTypes ? context.CreateShortType(guessedType) : new SimpleType("var");
				var varDecl = new VariableDeclarationStatement(type, name, expr.Clone());
				if (expr.Parent is ExpressionStatement) {
					script.Replace(expr.Parent, varDecl);
					script.Select(varDecl.Variables.First().NameToken);
				} else {
					var containing = expr.Parent;
					while (!(containing.Parent is BlockStatement)) {
						containing = containing.Parent;
					}

					script.InsertBefore(containing, varDecl);
					var identifierExpression = new IdentifierExpression(name);
					script.Replace(expr, identifierExpression);
					script.Link(varDecl.Variables.First().NameToken, identifierExpression);
				}
			});

			if (visitor.Matches.Count > 1) {
				yield return new CodeAction(string.Format(context.TranslateString("Declare local variable (replace '{0}' occurrences)"), visitor.Matches.Count), script => {
					var resolveResult = context.Resolve(expr);
					var guessedType = resolveResult.Type;
					if (resolveResult is MethodGroupResolveResult) {
						guessedType = GetDelegateType(context, ((MethodGroupResolveResult)resolveResult).Methods.First(), expr);
					}
					var linkedNodes = new List<AstNode>();
					var name = CreateMethodDeclarationAction.CreateBaseName(expr, guessedType);
					var type = context.UseExplicitTypes ? context.CreateShortType(guessedType) : new SimpleType("var");
					var varDecl = new VariableDeclarationStatement(type, name, expr.Clone());
					linkedNodes.Add(varDecl.Variables.First().NameToken);
					var first = visitor.Matches [0];
					if (first.Parent is ExpressionStatement) {
						script.Replace(first.Parent, varDecl);
					} else {
						var containing = first.Parent;
						while (!(containing.Parent is BlockStatement)) {
							containing = containing.Parent;
						}

						script.InsertBefore(containing, varDecl);
						var identifierExpression = new IdentifierExpression(name);
						linkedNodes.Add(identifierExpression);
						script.Replace(first, identifierExpression);
					}
					for (int i = 1; i < visitor.Matches.Count; i++) {
						var identifierExpression = new IdentifierExpression(name);
						linkedNodes.Add(identifierExpression);
						script.Replace(visitor.Matches [i], identifierExpression);
					}
					script.Link(linkedNodes.ToArray ());
				});
			}
		}

		// Gets Action/Func delegate types for a given method.
		IType GetDelegateType(RefactoringContext context, IMethod method, Expression expr)
		{
			var parameters = new List<IType>();
			var invoke = expr.Parent as InvocationExpression;
			if (invoke == null) {
				return null;
			}
			foreach (var arg in invoke.Arguments) {
				parameters.Add(context.Resolve(arg).Type);
			}

			ITypeDefinition genericType;
			if (method.ReturnType.FullName == "System.Void") {
				genericType = context.Compilation.GetAllTypeDefinitions().FirstOrDefault(t => t.FullName == "System.Action" && t.TypeParameterCount == parameters.Count);
			} else {
				parameters.Add(method.ReturnType);
				genericType = context.Compilation.GetAllTypeDefinitions().FirstOrDefault(t => t.FullName == "System.Func" && t.TypeParameterCount == parameters.Count);
			}
			if (genericType == null) {
				return null;
			}
			return new ParameterizedType(genericType, parameters);
		}

				
		class SearchNodeVisitior : DepthFirstAstVisitor
		{
			readonly AstNode searchForNode;
			public readonly List<AstNode> Matches = new List<AstNode> ();
			
			public SearchNodeVisitior (AstNode searchForNode)
			{
				this.searchForNode = searchForNode;
				Matches.Add (searchForNode);
			}
			
			protected override void VisitChildren(AstNode node)
			{

				if (node.StartLocation > searchForNode.StartLocation && node.IsMatch (searchForNode))
					Matches.Add (node);
				base.VisitChildren (node);
			}
		}


	}
}
