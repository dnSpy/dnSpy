//
// StaticMethodInvocationToExtensionMethodInvocationAction.cs
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	
	[ContextAction("Invoke using extension method syntax",
	               Description = "Converts the call into extension method call syntax.")]
	public class StaticMethodInvocationToExtensionMethodInvocationAction : ICodeActionProvider
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
			var firstArgument = invocation.Arguments.FirstOrDefault();
			if (firstArgument is NullReferenceExpression)
				yield break;
			var invocationRR = context.Resolve(invocation) as CSharpInvocationResolveResult;
			if (invocationRR == null)
				yield break;
			var method = invocationRR.Member as IMethod;
			if (method == null || !method.IsExtensionMethod || invocationRR.IsExtensionMethodInvocation)
				yield break;
			yield return new CodeAction(context.TranslateString("Convert to extension method call"), script => {
				var newArgumentList = invocation.Arguments.Skip(1).Select(arg => arg.Clone()).ToList();
				var newTarget = memberReference.Clone() as MemberReferenceExpression;
				newTarget.Target = firstArgument.Clone();
				script.Replace(invocation, new InvocationExpression(newTarget, newArgumentList));
			});
		}

		#endregion
	}
}

