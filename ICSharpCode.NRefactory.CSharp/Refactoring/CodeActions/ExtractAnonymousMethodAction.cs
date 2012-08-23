// 
// ExtractAnonymousMethodAction.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Extract anonymous method",
					Description = "Extract anonymous method to method of the containing type")]
	public class ExtractAnonymousMethodAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions (RefactoringContext context)
		{
			// lambda
			var lambda = context.GetNode<LambdaExpression> ();
			if (lambda != null && lambda.ArrowToken.Contains(context.Location)) {
				if (ContainsLocalReferences (context, lambda, lambda.Body))
					yield break;

				bool noReturn = false;
				BlockStatement body;
				if (lambda.Body is BlockStatement) {
					body = (BlockStatement)lambda.Body.Clone ();
				} else {
					body = new BlockStatement ();

					var type = LambdaHelper.GetLambdaReturnType (context, lambda);
					if (type == null || type.ReflectionName == "System.Void") {
						noReturn = true;
						body.Add (new ExpressionStatement ((Expression)lambda.Body.Clone ()));
					} else {
						body.Add (new ReturnStatement ((Expression)lambda.Body.Clone ()));
					}
				}
				var method = GetMethod (context, (LambdaResolveResult)context.Resolve (lambda), body, noReturn);
				yield return GetAction (context, lambda, method);
			}

			// anonymous method
			var anonymousMethod = context.GetNode<AnonymousMethodExpression> ();
			if (anonymousMethod != null && anonymousMethod.DelegateToken.Contains(context.Location)) {
				if (ContainsLocalReferences (context, anonymousMethod, anonymousMethod.Body))
					yield break;

				var method = GetMethod (context, (LambdaResolveResult)context.Resolve (anonymousMethod), 
										(BlockStatement)anonymousMethod.Body.Clone ());
				yield return GetAction (context, anonymousMethod, method);
			}
		}

		CodeAction GetAction (RefactoringContext context, AstNode node, MethodDeclaration method)
		{
			return new CodeAction (context.TranslateString ("Extract anonymous method"),
				script =>
				{
					var identifier = new IdentifierExpression ("Method");
					script.Replace (node, identifier);
					script.InsertBefore (node.GetParent<EntityDeclaration> (), method);
					script.Link (method.NameToken, identifier);
				});
		}

		static MethodDeclaration GetMethod (RefactoringContext context, LambdaResolveResult lambda, BlockStatement body,
			bool noReturnValue = false)
		{
			var method = new MethodDeclaration { Name = "Method" };

			if (noReturnValue) {
				method.ReturnType = new PrimitiveType ("void"); 
			} else {
				var type = lambda.GetInferredReturnType (lambda.Parameters.Select (p => p.Type).ToArray ());
				method.ReturnType = type.Name == "?" ? new PrimitiveType ("void") : context.CreateShortType (type);
			}

			foreach (var param in lambda.Parameters)
				method.Parameters.Add (new ParameterDeclaration (context.CreateShortType (param.Type), param.Name));

			method.Body = body;
			if (lambda.IsAsync)
				method.Modifiers |= Modifiers.Async;

			return method;
		}

		static bool ContainsLocalReferences (RefactoringContext context, AstNode expr, AstNode body)
		{
			var visitor = new ExtractMethod.VariableLookupVisitor (context);
			body.AcceptVisitor (visitor);
			return visitor.UsedVariables.Any (variable => !expr.Contains (variable.Region.Begin));
		}
	}
}
