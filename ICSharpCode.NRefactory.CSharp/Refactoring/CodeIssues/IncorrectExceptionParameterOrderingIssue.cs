//
// IncorrectExceptionParametersOrderingIssue.cs
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Incorrect ordering of exception constructor parameters",
	       Description = "Warns about the constructor parameter ordering of some confusing exception types.",
	       Category = IssueCategories.CodeQualityIssues,
	       Severity = Severity.Warning)]
	public class IncorrectExceptionParameterOrderingIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase
		{
			readonly BaseRefactoringContext context;
			Dictionary<string, Func<int, int, bool>> rules;

			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
				this.context = context;
				rules = new Dictionary<string, Func<int, int, bool>>();
				rules [typeof(ArgumentException).FullName] = (left, right) => left > right;
				rules [typeof(ArgumentNullException).FullName] = (left, right) => left < right;
				rules [typeof(ArgumentOutOfRangeException).FullName] = (left, right) => left < right;
				rules [typeof(DuplicateWaitObjectException).FullName] = (left, right) => left < right;
			}

			public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
			{
				var type = context.Resolve(objectCreateExpression.Type) as TypeResolveResult;
				if (type == null)
					return;
				var parameters = objectCreateExpression.Arguments;
				if (parameters.Count != 2)
					return;
				var firstParam = parameters.FirstOrNullObject() as PrimitiveExpression;
				var secondParam = parameters.LastOrNullObject() as PrimitiveExpression;
				if (firstParam == null || firstParam.Value.GetType() != typeof(string) ||
					secondParam == null || firstParam.Value.GetType() != typeof(string))
					return;
				var leftLength = (firstParam.Value as string).Length;
				var rightLength = (secondParam.Value as string).Length;

				Func<int, int, bool> func;
				if (!rules.TryGetValue(type.Type.FullName, out func))
					return;
				if (!func(leftLength, rightLength))
					AddIssue(objectCreateExpression,
					         context.TranslateString("The parameters are in the wrong order"),
					         GetActions(objectCreateExpression, firstParam, secondParam));
			}

			IEnumerable<CodeAction> GetActions(ObjectCreateExpression objectCreateExpression,
			                                   PrimitiveExpression firstParam, PrimitiveExpression secondParam)
			{
				yield return new CodeAction(context.TranslateString("Swap parameters"), script =>  {
					var newOCE = objectCreateExpression.Clone() as ObjectCreateExpression;
					newOCE.Arguments.Clear();
					newOCE.Arguments.Add(secondParam.Clone());
					newOCE.Arguments.Add(firstParam.Clone());
					script.Replace(objectCreateExpression, newOCE);
				});
			}
		}
	}
}
