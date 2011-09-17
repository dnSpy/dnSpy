// 
// AddAnotherAccessor.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Add another accessor to a property declaration that has only one.
	/// </summary>
	public class AddAnotherAccessor : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			var pdecl = GetPropertyDeclaration (context);
			if (pdecl == null)
				return false;
			var type = pdecl.Parent as TypeDeclaration;
			if (type != null && type.ClassType == ClassType.Interface)
				return false;
			
			return pdecl.Setter.IsNull || pdecl.Getter.IsNull;
		}
		
		public void Run (RefactoringContext context)
		{
			var pdecl = GetPropertyDeclaration (context);
			var accessorStatement = BuildAccessorStatement (context, pdecl);
			
			Accessor accessor = new Accessor () {
				Body = new BlockStatement { accessorStatement }
			};
			
			pdecl.AddChild (accessor, pdecl.Setter.IsNull ? PropertyDeclaration.SetterRole : PropertyDeclaration.GetterRole);
			
			using (var script = context.StartScript ()) {
				script.InsertBefore (pdecl.RBraceToken, accessor);
				script.Select (accessorStatement);
				script.FormatText (ctx => GetPropertyDeclaration (context));
			}
		}

		static Statement BuildAccessorStatement (RefactoringContext context, PropertyDeclaration pdecl)
		{
			if (pdecl.Setter.IsNull && !pdecl.Getter.IsNull) {
				var field = RemoveBackingStore.ScanGetter (context, pdecl);
				if (field != null) 
					return new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (field.Name), AssignmentOperatorType.Assign, new IdentifierExpression ("value")));
			}
			
			if (!pdecl.Setter.IsNull && pdecl.Getter.IsNull) {
				var field = RemoveBackingStore.ScanSetter (context, pdecl);
				if (field != null) 
					return new ReturnStatement (new IdentifierExpression (field.Name));
			}
			
			return new ThrowStatement (new ObjectCreateExpression (context.CreateShortType ("System", "NotImplementedException")));
		}
		
		static PropertyDeclaration GetPropertyDeclaration (RefactoringContext context)
		{
			var node = context.GetNode ();
			if (node == null)
				return null;
			return node.Parent as PropertyDeclaration;
		}
	}
}
