//
// ConvertLambdaToDelegateAction.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Convert lambda to anonymous delegate",
	               Description = "Converts a lambda to an anonymous delegate.")]
	public class ConvertLambdaToAnonymousDelegateAction : SpecializedCodeAction<LambdaExpression>
	{
		#region implemented abstract members of SpecializedCodeAction
		protected override CodeAction GetAction(RefactoringContext context, LambdaExpression node)
		{
			if (context.Location < node.StartLocation || context.Location >= node.Body.StartLocation)
				return null;

			var lambdaResolveResult = context.Resolve(node) as LambdaResolveResult;
			if (lambdaResolveResult == null)
				return null;

			return new CodeAction(context.TranslateString("Convert to anonymous delegate"), script => {
				BlockStatement newBody;
				if (node.Body is BlockStatement) {
					newBody = (BlockStatement)node.Body.Clone();
				} else {
					newBody = new BlockStatement {
						Statements = {
							new ExpressionStatement((Expression)node.Body.Clone())
						}
					};
				}
				var method = new AnonymousMethodExpression (newBody, GetParameters(lambdaResolveResult.Parameters, context));
				method.HasParameterList = true;
				script.Replace(node, method);
			});
		}
		#endregion

		IEnumerable<ParameterDeclaration> GetParameters(IList<IParameter> parameters, RefactoringContext context)
		{
			foreach (var parameter in parameters) {
				var type = context.CreateShortType(parameter.Type);
				var name = parameter.Name;
				ParameterModifier modifier = ParameterModifier.None;
				if (parameter.IsRef) 
					modifier |= ParameterModifier.Ref;
				if (parameter.IsOut)
					modifier |= ParameterModifier.Out;
				yield return new ParameterDeclaration(type, name, modifier);
			}
		}
	}
}

