// 
// ImplementInterfaceExplicitAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Implement interface explicit", Description = "Creates an interface implementation.")]
	public class ImplementInterfaceExplicitAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var type = context.GetNode<AstType>();
			if (type == null || type.Role != Roles.BaseType)
				yield break;
			var state = context.GetResolverStateBefore(type);
			if (state.CurrentTypeDefinition == null)
				yield break;

			var resolveResult = context.Resolve(type);
			if (resolveResult.Type.Kind != TypeKind.Interface)
				yield break;

			var toImplement = ImplementInterfaceAction.CollectMembersToImplement(
				state.CurrentTypeDefinition,
				resolveResult.Type,
				false
			);
			if (toImplement.Count == 0)
				yield break;

			yield return new CodeAction(context.TranslateString("Implement interface explicit"), script => {
				script.InsertWithCursor(
					context.TranslateString("Implement Interface"),
					state.CurrentTypeDefinition,
					ImplementInterfaceAction.GenerateImplementation (context, toImplement.Select (t => Tuple.Create (t.Item1, true)))
				);
			});
		}
	}
}
