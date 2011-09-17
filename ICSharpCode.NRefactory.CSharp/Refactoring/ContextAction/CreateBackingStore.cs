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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class CreateBackingStore : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			var propertyDeclaration = context.GetNode<PropertyDeclaration> ();
			return propertyDeclaration != null && 
				!propertyDeclaration.Getter.IsNull && !propertyDeclaration.Setter.IsNull && // automatic properties always need getter & setter
				propertyDeclaration.Getter.Body.IsNull &&
				propertyDeclaration.Setter.Body.IsNull;
		}
		
		public void Run (RefactoringContext context)
		{
			var property = context.GetNode<PropertyDeclaration> ();
			
			string backingStoreName = context.GetNameProposal (property.Name);
			
			// create field
			var backingStore = new FieldDeclaration ();
			backingStore.ReturnType = property.ReturnType.Clone ();
			
			var initializer = new VariableInitializer (backingStoreName);
			backingStore.Variables.Add (initializer);
			
			// create new property & implement the get/set bodies
			var newProperty = (PropertyDeclaration)property.Clone ();
			var id1 = new IdentifierExpression (backingStoreName);
			var id2 = new IdentifierExpression (backingStoreName);
			newProperty.Getter.Body = new BlockStatement () {
				new ReturnStatement (id1)
			};
			newProperty.Setter.Body = new BlockStatement () {
				new ExpressionStatement (new AssignmentExpression (id2, AssignmentOperatorType.Assign, new IdentifierExpression ("value")))
			};
			
			using (var script = context.StartScript ()) {
				script.Replace (property, newProperty);
				script.InsertBefore (property, backingStore);
				script.Link (initializer, id1, id2);
			}
		}
	}
}

