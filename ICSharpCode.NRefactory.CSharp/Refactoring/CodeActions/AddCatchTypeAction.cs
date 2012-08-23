//
// AddCatchTypeAction.cs
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
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Add type",
	               Description = "Adds an exception type specifier to catch clauses.")]
	public class AddCatchTypeAction : ICodeActionProvider
	{
		#region ICodeActionProvider implementation

		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var catchClause = context.GetNode<CatchClause>();
			if (catchClause == null)
				yield break;
			if (!catchClause.Type.IsNull)
				yield break;
			yield return new CodeAction(context.TranslateString("Add type specifier"), script => {
				var newIdentifier = Identifier.Create("e");
				var newType = context.CreateShortType("System", "Exception");
				script.Replace(catchClause, new CatchClause() {
					Type = newType,
					VariableNameToken = newIdentifier,
					Body = catchClause.Body.Clone() as BlockStatement
				});
				script.Link(newType);
				script.Link(newIdentifier);
			});
		}

		#endregion
	}
}
