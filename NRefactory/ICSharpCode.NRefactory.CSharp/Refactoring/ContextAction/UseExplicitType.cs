// 
// UseExplicitType.cs
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
using ICSharpCode.NRefactory.TypeSystem;


namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class UseExplicitType: IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			var varDecl = GetVariableDeclarationStatement (context);
			if (varDecl == null)
				return false;
			var type = context.Resolve (varDecl.Variables.First ().Initializer).Type;
			return !type.Equals (SharedTypes.Null) && !type.Equals (SharedTypes.UnknownType);
		}
		
		public void Run (RefactoringContext context)
		{
			var varDecl = GetVariableDeclarationStatement (context);
			
			using (var script = context.StartScript ()) {
				var type = context.Resolve (varDecl.Variables.First ().Initializer).Type;
				script.Replace (varDecl.Type, context.CreateShortType (type));
			}
		}
		
		static VariableDeclarationStatement GetVariableDeclarationStatement (RefactoringContext context)
		{
			var result = context.GetNode<VariableDeclarationStatement> ();
			if (result != null && result.Variables.Count == 1 && !result.Variables.First ().Initializer.IsNull && result.Type.Contains (context.Location.Line, context.Location.Column) && result.Type.IsMatch (new SimpleType ("var"))) {
				if (context.Resolve (result.Variables.First ().Initializer) == null)
					return null;
				return result;
			}
			return null;
		}
	}
}

