// 
// SplitDeclarationAndAssignment.cs
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
using System.Linq;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class SplitDeclarationAndAssignment : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			AstType type;
			return GetVariableDeclarationStatement (context, out type) != null;
		}
		
		public void Run (RefactoringContext context)
		{
			AstType type;
			var varDecl = GetVariableDeclarationStatement (context, out type);
			
			var assign = new AssignmentExpression (new IdentifierExpression (varDecl.Variables.First ().Name), AssignmentOperatorType.Assign, varDecl.Variables.First ().Initializer.Clone ());
			
			var newVarDecl = (VariableDeclarationStatement)varDecl.Clone ();
			
			if (newVarDecl.Type.IsMatch (new SimpleType ("var")))
				newVarDecl.Type = type;
			
			newVarDecl.Variables.First ().Initializer = Expression.Null;
			
			using (var script = context.StartScript ()) {
				script.InsertBefore (varDecl, newVarDecl);
				script.Replace (varDecl, varDecl.Parent is ForStatement ? (AstNode)assign : new ExpressionStatement (assign));
			}
		}
		
		static VariableDeclarationStatement GetVariableDeclarationStatement (RefactoringContext context, out AstType resolvedType)
		{
			var result = context.GetNode<VariableDeclarationStatement> ();
			if (result != null && result.Variables.Count == 1 && !result.Variables.First ().Initializer.IsNull && result.Variables.First ().NameToken.Contains (context.Location.Line, context.Location.Column)) {
				resolvedType = context.Resolve (result.Variables.First ().Initializer).Type.ConvertToAstType ();
				if (resolvedType == null)
					return null;
				return result;
			}
			resolvedType = null;
			return null;
		}
	}
}

