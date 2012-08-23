// 
// GenerateGetter.cs
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Generate getter", Description = "Generates a getter for a field.")]
	public class GenerateGetterAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var initializer = GetVariableInitializer(context);
			if (initializer == null || !initializer.NameToken.Contains(context.Location.Line, context.Location.Column)) {
				yield break;
			}
			var type = initializer.Parent.Parent as TypeDeclaration;
			if (type == null) {
				yield break;
			}
			foreach (var member in type.Members) {
				if (member is PropertyDeclaration && ContainsGetter((PropertyDeclaration)member, initializer)) {
					yield break;
				}
			}
			var field = initializer.Parent as FieldDeclaration;
			if (field == null) {
				yield break;
			}
			
			yield return new CodeAction(context.TranslateString("Create getter"), script => {
				script.InsertWithCursor(
					context.TranslateString("Create getter"),
					Script.InsertPosition.After,
					GeneratePropertyDeclaration(context, field, initializer));
			});
		}
		
		static PropertyDeclaration GeneratePropertyDeclaration (RefactoringContext context, FieldDeclaration field, VariableInitializer initializer)
		{
			var mod = ICSharpCode.NRefactory.CSharp.Modifiers.Public;
			if (field.HasModifier (ICSharpCode.NRefactory.CSharp.Modifiers.Static))
				mod |= ICSharpCode.NRefactory.CSharp.Modifiers.Static;
			
			return new PropertyDeclaration () {
				Modifiers = mod,
				Name = context.GetNameProposal (initializer.Name, false),
				ReturnType = field.ReturnType.Clone (),
				Getter = new Accessor () {
					Body = new BlockStatement () {
						new ReturnStatement (new IdentifierExpression (initializer.Name))
					}
				}
			};
		}
		
		bool ContainsGetter (PropertyDeclaration property, VariableInitializer initializer)
		{
			if (property.Getter.IsNull || property.Getter.Body.Statements.Count () != 1)
				return false;
			var ret = property.Getter.Body.Statements.Single () as ReturnStatement;
			if (ret == null)
				return false;
			return ret.Expression.IsMatch (new IdentifierExpression (initializer.Name)) || 
				ret.Expression.IsMatch (new MemberReferenceExpression (new ThisReferenceExpression (), initializer.Name));
		}
		
		VariableInitializer GetVariableInitializer (RefactoringContext context)
		{
			return context.GetNode<VariableInitializer> ();
		}
	}
}

