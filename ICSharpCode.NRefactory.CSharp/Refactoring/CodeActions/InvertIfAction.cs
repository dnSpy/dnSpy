// 
// InvertIf.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Invert if", Description = "Inverts an 'if ... else' expression.")]
	public class InvertIfAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			// TODO: Invert if without else
			// ex. if (cond) DoSomething () == if (!cond) return; DoSomething ()
			// beware of loop contexts return should be continue then.
			
			var ifStatement = GetIfElseStatement(context);
			if (!(ifStatement != null && !ifStatement.TrueStatement.IsNull && !ifStatement.FalseStatement.IsNull)) {
				yield break;

			}
			yield return new CodeAction (context.TranslateString("Invert if"), script => {
				script.Replace(ifStatement.Condition, CSharpUtil.InvertCondition(ifStatement.Condition.Clone()));
				script.Replace(ifStatement.TrueStatement, ifStatement.FalseStatement.Clone());
				script.Replace(ifStatement.FalseStatement, ifStatement.TrueStatement.Clone());
				script.FormatText(ifStatement);
			});
		}
		
		static IfElseStatement GetIfElseStatement (RefactoringContext context)
		{
			var result = context.GetNode<IfElseStatement> ();
			if (result != null && result.IfToken.Contains (context.Location))
				return result;
			return null;
		}
	}
}
