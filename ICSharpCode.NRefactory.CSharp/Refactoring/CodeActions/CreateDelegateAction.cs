// 
// CreateDelegateAction.cs
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

using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create delegate", Description = "Creates a delegate declaration out of an event declaration.")]
	public class CreateDelegateAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var simpleType = context.GetNode<SimpleType>();
			if (simpleType != null && (simpleType.Parent is EventDeclaration || simpleType.Parent is CustomEventDeclaration)) 
				return GetActions(context, simpleType);

			return Enumerable.Empty<CodeAction>();
		}

		static IEnumerable<CodeAction> GetActions(RefactoringContext context, SimpleType node)
		{
			var resolveResult = context.Resolve(node) as UnknownIdentifierResolveResult;
			if (resolveResult == null)
				yield break;

			yield return new CodeAction(context.TranslateString("Create delegate"), script => {
				script.CreateNewType(CreateType(context,  node));
			});

		}

		static DelegateDeclaration CreateType(RefactoringContext context, SimpleType simpleType)
		{
			var result = new DelegateDeclaration() {
				Name = simpleType.Identifier,
				Modifiers = ((EntityDeclaration)simpleType.Parent).Modifiers,
				ReturnType = new PrimitiveType("void"),
				Parameters = {
					new ParameterDeclaration(new PrimitiveType("object"), "sender"),
					new ParameterDeclaration(context.CreateShortType("System", "EventArgs"), "e")
				}
			};
			return result;
		}
	}
}