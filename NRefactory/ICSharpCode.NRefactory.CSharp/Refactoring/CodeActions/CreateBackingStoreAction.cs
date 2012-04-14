// 
// CreateBackingStore.cs
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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create backing store for auto property", Description = "Creates a backing field for an auto property.")]
	public class CreateBackingStoreAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var property = context.GetNode<PropertyDeclaration>();
			if (!(property != null &&
				!property.Getter.IsNull && !property.Setter.IsNull && // automatic properties always need getter & setter
				property.Getter.Body.IsNull &&
				property.Setter.Body.IsNull)) {
				yield break;
			}
			
			yield return new CodeAction(context.TranslateString("Create backing store"), script => {
				string backingStoreName = context.GetNameProposal (property.Name);
				
				// create field
				var backingStore = new FieldDeclaration ();
				backingStore.ReturnType = property.ReturnType.Clone ();
				
				var initializer = new VariableInitializer (backingStoreName);
				backingStore.Variables.Add (initializer);
				
				// create new property & implement the get/set bodies
				var newProperty = (PropertyDeclaration)property.Clone ();
				Expression id1;
				if (backingStoreName == "value")
					id1 = new ThisReferenceExpression().Member("value");
				else
					id1 = new IdentifierExpression (backingStoreName);
				Expression id2 = id1.Clone();
				newProperty.Getter.Body = new BlockStatement () {
					new ReturnStatement (id1)
				};
				newProperty.Setter.Body = new BlockStatement () {
					new AssignmentExpression (id2, AssignmentOperatorType.Assign, new IdentifierExpression ("value"))
				};
				
				script.Replace (property, newProperty);
				script.InsertBefore (property, backingStore);
				script.Link (initializer, id1, id2);
			});
		}
	}
}

