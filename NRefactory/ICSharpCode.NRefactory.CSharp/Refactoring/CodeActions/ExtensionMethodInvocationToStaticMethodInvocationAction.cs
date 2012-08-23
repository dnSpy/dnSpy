//
// ExtensionMethodInvocationToStaticMethodInvocationAction.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Invoke using static method syntax",
	               Description = "Converts the call into static method call syntax.")]
	public class ExtensionMethodInvocationToStaticMethodInvocationAction : ICodeActionProvider
	{
		#region ICodeActionProvider implementation

		public IEnumerable<CodeAction> GetActions (RefactoringContext context)
		{
			var invocation = context.GetNode<InvocationExpression>();
			if (invocation == null)
				yield break;
			var memberReference = invocation.Target as MemberReferenceExpression;
			if (memberReference == null)
				yield break;
			var invocationRR = context.Resolve(invocation) as CSharpInvocationResolveResult;
			if (invocationRR == null)
				yield break;
			if (invocationRR.IsExtensionMethodInvocation)
				yield return new CodeAction(context.TranslateString("Convert to call to static method"), script => {
					script.Replace(invocation, ToStaticMethodInvocation(invocation, memberReference, invocationRR));
				});
		}

		#endregion

		AstNode ToStaticMethodInvocation(InvocationExpression invocation, MemberReferenceExpression memberReference,
		                                 CSharpInvocationResolveResult invocationRR)
		{
			var newArgumentList = invocation.Arguments.Select(arg => arg.Clone()).ToList();
			newArgumentList.Insert(0, memberReference.Target.Clone());
			var newTarget = memberReference.Clone() as MemberReferenceExpression;
			newTarget.Target = new IdentifierExpression(invocationRR.Member.DeclaringType.Name);
			return new InvocationExpression(newTarget, newArgumentList);
		}

	}
}

