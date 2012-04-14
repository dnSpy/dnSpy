// 
// CreateProperty.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create property", Description = "Creates a property for a undefined variable.")]
	public class CreatePropertyAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var identifier = context.GetNode(n => n is IdentifierExpression || n is MemberReferenceExpression) as Expression;
			if (identifier == null)
				yield break;
			if (CreateFieldAction.IsInvocationTarget(identifier))
				yield break;

			var propertyName = GetPropertyName(identifier);
			if (propertyName == null)
				yield break;

			var statement = context.GetNode<Statement>();
			if (statement == null)
				yield break;

			if (!(context.Resolve(identifier).IsError))
				yield break;

			var guessedType = CreateFieldAction.GuessAstType(context, identifier);
			if (guessedType == null)
				yield break;
			var state = context.GetResolverStateBefore(identifier);
			if (state.CurrentTypeDefinition == null)
				yield break;
			
			bool createInOtherType = false;
			ResolveResult targetResolveResult = null;
			if (identifier is MemberReferenceExpression) {
				targetResolveResult = context.Resolve(((MemberReferenceExpression)identifier).Target);
				createInOtherType = !state.CurrentTypeDefinition.Equals(targetResolveResult.Type.GetDefinition());
			}

			bool isStatic = targetResolveResult is TypeResolveResult;
			if (createInOtherType) {
				if (isStatic && targetResolveResult.Type.Kind == TypeKind.Interface)
					yield break;
			} else {
				if (state.CurrentMember == null)
					yield break;
				isStatic |= state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;
			}

//			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//			if (service != null && !service.IsValidName(propertyName, AffectedEntity.Property, Modifiers.Private, isStatic)) { 
//				yield break;
//			}

			yield return new CodeAction(context.TranslateString("Create property"), script => {
				var decl = new PropertyDeclaration() {
					ReturnType = guessedType,
					Name = propertyName,
					Getter = new Accessor(),
					Setter = new Accessor()
				};
				if (isStatic)
					decl.Modifiers |= Modifiers.Static;
				
				if (createInOtherType) {
					if (targetResolveResult.Type.Kind == TypeKind.Interface) {
						decl.Modifiers = Modifiers.None;
					} else {
						decl.Modifiers |= Modifiers.Public;
					}
					script.InsertWithCursor(context.TranslateString("Create property"), decl, targetResolveResult.Type.GetDefinition());
					return;
				}

				script.InsertWithCursor(context.TranslateString("Create property"), decl, Script.InsertPosition.Before);
			});
		}

		static string GetPropertyName(Expression expr)
		{
			if (expr is IdentifierExpression) 
				return ((IdentifierExpression)expr).Identifier;
			if (expr is MemberReferenceExpression) 
				return ((MemberReferenceExpression)expr).MemberName;

			return null;
		}
	}
}

