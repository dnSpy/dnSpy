// 
// IntroduceConstantAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Introduce constant", Description = "Creates a constant for a non constant primitive expression.")]
	public class IntroduceConstantAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var pexpr = context.GetNode<PrimitiveExpression>();
			if (pexpr == null)
				yield break;
			var statement = context.GetNode<Statement>();
			if (statement == null) {
				yield break;
			}

			var resolveResult = context.Resolve(pexpr);

			yield return new CodeAction(context.TranslateString("Create local constant"), script => {
				string name = CreateMethodDeclarationAction.CreateBaseName(pexpr, resolveResult.Type);
				var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
				if (service != null)
					name = service.CheckName(context, name, AffectedEntity.LocalConstant);

				var initializer = new VariableInitializer(name, pexpr.Clone());
				var decl = new VariableDeclarationStatement() {
					Type = context.CreateShortType(resolveResult.Type),
					Modifiers = Modifiers.Const,
					Variables = { initializer }
				};

				script.InsertBefore(statement, decl);
				var variableUsage = new IdentifierExpression(name);
				script.Replace(pexpr, variableUsage);
				script.Link(initializer.NameToken, variableUsage);
			});

			yield return new CodeAction(context.TranslateString("Create constant field"), script => {
				string name = CreateMethodDeclarationAction.CreateBaseName(pexpr, resolveResult.Type);
				var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
				if (service != null)
					name = service.CheckName(context, name, AffectedEntity.ConstantField);

				var initializer = new VariableInitializer(name, pexpr.Clone());

				var decl = new FieldDeclaration() {
					ReturnType = context.CreateShortType(resolveResult.Type),
					Modifiers = Modifiers.Const,
					Variables = { initializer }
				};

				var variableUsage = new IdentifierExpression(name);
				script.Replace(pexpr, variableUsage);
//				script.Link(initializer.NameToken, variableUsage);
				script.InsertWithCursor(context.TranslateString("Create constant"), decl, Script.InsertPosition.Before);
			});
		}
	}
}

