// 
// ImplementAbstractMembersAction.cs
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
	[ContextAction("Implement abstract members", Description = "Implements abstract members from an abstract class.")]
	public class ImplementAbstractMembersAction : ICodeActionProvider
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
			if (resolveResult.Type.Kind != TypeKind.Class || resolveResult.Type.GetDefinition() == null || !resolveResult.Type.GetDefinition().IsAbstract)
				yield break;

			var toImplement = CollectMembersToImplement(state.CurrentTypeDefinition, resolveResult.Type);
			if (toImplement.Count == 0)
				yield break;

			yield return new CodeAction(context.TranslateString("Implement abstract members"), script => {
				script.InsertWithCursor(
					context.TranslateString("Implement abstract members"),
					state.CurrentTypeDefinition,
					ImplementInterfaceAction.GenerateImplementation (context, toImplement.Select (m => Tuple.Create (m, false))).Select (entity => {
						var decl = entity as EntityDeclaration;
						if (decl != null)
							decl.Modifiers |= Modifiers.Override;
						return entity;
					})
				);
			});
		}

		public static List<IMember> CollectMembersToImplement(ITypeDefinition implementingType, IType abstractType)
		{
			var def = abstractType.GetDefinition();
			var toImplement = new List<IMember>();
			bool alreadyImplemented;
			
			// Stub out non-implemented events defined by @iface
			foreach (var ev in abstractType.GetEvents (e => !e.IsSynthetic && e.IsAbstract)) {
				alreadyImplemented = implementingType.GetAllBaseTypeDefinitions().Any(
					x => x.Kind != TypeKind.Interface && x.Events.Any (y => y.Name == ev.Name)
					);
				
				if (!alreadyImplemented)
					toImplement.Add(ev);
			}
			
			// Stub out non-implemented methods defined by @iface
			foreach (var method in abstractType.GetMethods (d => !d.IsSynthetic  && d.IsAbstract)) {
				alreadyImplemented = false;
				
				foreach (var cmet in implementingType.GetMethods ()) {
					if (!cmet.IsAbstract && ImplementInterfaceAction.CompareMethods(method, cmet)) {
						alreadyImplemented = true;
					}
				}
				if (!alreadyImplemented) 
					toImplement.Add(method);
			}
			
			// Stub out non-implemented properties defined by @iface
			foreach (var prop in abstractType.GetProperties (p => !p.IsSynthetic && p.IsAbstract)) {
				alreadyImplemented = false;
				foreach (var t in implementingType.GetAllBaseTypeDefinitions ()) {
					if (t.Kind == TypeKind.Interface)
						continue;
					foreach (IProperty cprop in t.Properties) {
						if (!cprop.IsAbstract && cprop.Name == prop.Name) {
							alreadyImplemented = true;
						}
					}
				}
				if (!alreadyImplemented)
					toImplement.Add(prop);
			}
			return toImplement;
		}

	}
}
