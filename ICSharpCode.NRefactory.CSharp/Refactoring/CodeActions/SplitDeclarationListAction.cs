// 
// SplitDeclarationListAction.cs
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Split declaration list", 
		Description = "Split variable declaration with multiple variables into declarations with a single variable")] 
	public class SplitDeclarationListAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions (RefactoringContext context)
		{
			// field, local var, event, fixed var, fixed field

			// local variable
			var variableDecl = context.GetNode<VariableDeclarationStatement> ();
			if (variableDecl != null && variableDecl.Parent is BlockStatement) {
				return GetAction (context, variableDecl, v => v.Variables);
			}

			// field
			var fieldDecl = context.GetNode<FieldDeclaration> ();
			if (fieldDecl != null)
				return GetAction (context, fieldDecl, f => f.Variables);

			// event
			var eventDecl = context.GetNode<EventDeclaration> ();
			if (eventDecl != null)
				return GetAction (context, eventDecl, e => e.Variables);

			// fixed field
			var fixedFieldDecl = context.GetNode<FixedFieldDeclaration> ();
			if (fixedFieldDecl != null)
				return GetAction (context, fixedFieldDecl, f => f.Variables);

			return Enumerable.Empty<CodeAction> ();
		}

		IEnumerable<CodeAction> GetAction<T, S> (RefactoringContext context, T decl, 
												 Func<T, AstNodeCollection<S>> getInitializers)
			where T : AstNode
			where S : AstNode
		{
			var initializers = getInitializers(decl);
			if (initializers.Count < 2)
				yield break;

			yield return new CodeAction (context.TranslateString ("Split declaration list"),
				script =>
				{
					var emptyDecl = (T)decl.Clone ();
					getInitializers (emptyDecl).Clear ();

					var declList = initializers.Select (v =>
					{
						var singleDecl = (T)emptyDecl.Clone ();
						getInitializers(singleDecl).Add ((S)v.Clone ());
						return singleDecl;
					});

					foreach (var singleDecl in declList)
						script.InsertBefore (decl, singleDecl);
					script.Remove (decl);
				});
		}
	}
}
