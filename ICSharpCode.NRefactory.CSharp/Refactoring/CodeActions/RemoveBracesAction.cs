// 
// RemoveBraces.cs
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Remove braces", Description = "Removes redundant braces around a statement.")]
	public class RemoveBracesAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var block = GetBlockStatement(context);
			if (block == null) {
				yield break;
			}

			yield return new CodeAction (context.TranslateString("Remove braces"), script => {
				script.Remove(block.LBraceToken);
				script.Remove(block.RBraceToken);
				script.FormatText(block.Parent);
			});
		}
		
		static BlockStatement GetBlockStatement(RefactoringContext context)
		{
			var block = context.GetNode<BlockStatement>();
			if (block == null || block.LBraceToken.IsNull || block.RBraceToken.IsNull)
				return null;
			if (!(block.LBraceToken.IsInside(context.Location) || block.RBraceToken.IsInside(context.Location)))
				return null;
			if (!(block.Parent is Statement) || block.Parent is TryCatchStatement) 
				return null;
			if (block.Statements.Count != 1 || block.Statements.First () is VariableDeclarationStatement) 
				return null;
			return block;
		}
	}
}
