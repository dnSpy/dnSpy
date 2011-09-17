// 
// InsertAnonymousMethodSignature.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class InsertAnonymousMethodSignature : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			IType type;
			return GetAnonymousMethodExpression (context, out type) != null;
		}
		
		public void Run (RefactoringContext context)
		{
			IType type;
			var anonymousMethodExpression = GetAnonymousMethodExpression (context, out type);
			
			var delegateMethod = type.GetDelegateInvokeMethod ();
			
			var sb = new StringBuilder ("(");
			for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
				if (k > 0)
					sb.Append (", ");
				
				var paramType = delegateMethod.Parameters [k].Type;
				
				sb.Append (paramType.ToString ());
				sb.Append (" ");
				sb.Append (delegateMethod.Parameters [k].Name);
			}
			sb.Append (")");
			
			using (var script = context.StartScript ()) {
				script.InsertText (context.GetOffset (anonymousMethodExpression.DelegateToken.EndLocation), sb.ToString ());
			}
		}
		
		static AnonymousMethodExpression GetAnonymousMethodExpression (RefactoringContext context, out IType delegateType)
		{
			delegateType = null;
			
			var anonymousMethodExpression = context.GetNode<AnonymousMethodExpression> ();
			if (anonymousMethodExpression == null || !anonymousMethodExpression.DelegateToken.Contains (context.Location.Line, context.Location.Column) || anonymousMethodExpression.HasParameterList)
				return null;
			
			IType resolvedType = null;
			var parent = anonymousMethodExpression.Parent;
			if (parent is AssignmentExpression) {
				resolvedType = context.Resolve (((AssignmentExpression)parent).Left).Type;
			} else if (parent is VariableDeclarationStatement) {
				resolvedType = context.Resolve (((VariableDeclarationStatement)parent).Type).Type;
			} else if (parent is InvocationExpression) {
				// TODO: handle invocations
			}
			
			if (resolvedType == null)
				return null;
			delegateType = resolvedType;
			if (delegateType.Kind != TypeKind.Delegate) 
				return null;
			
			return anonymousMethodExpression;
		}
	}
}

