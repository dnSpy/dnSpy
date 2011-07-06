// 
// IntroduceFormatItem.cs
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Introduce format item. Works on strings that contain selections.
	/// "this is <some> string" => string.Format ("this is {0} string", <some>)
	/// </summary>
	public class IntroduceFormatItem : IContextAction
	{
		readonly static MemberReferenceExpression PrototypeFormatReference = new MemberReferenceExpression (new TypeReferenceExpression (new PrimitiveType ("string")), "Format");
		
		public bool IsValid (RefactoringContext context)
		{
			if (!context.IsSomethingSelected)
				return false;
			var pexpr = context.GetNode<PrimitiveExpression> ();
			if (pexpr == null || !(pexpr.Value is string))
				return false;
			if (pexpr.LiteralValue.StartsWith ("@"))
				return pexpr.StartLocation < new AstLocation (context.Location.Line, context.Location.Column - 1) &&
					new AstLocation (context.Location.Line, context.Location.Column + 1) < pexpr.EndLocation;
			return pexpr.StartLocation < context.Location && context.Location < pexpr.EndLocation;
		}
		
		public void Run (RefactoringContext context)
		{
			var pexpr = context.GetNode<PrimitiveExpression> ();
			var invocation = context.GetNode<InvocationExpression> ();
			if (invocation != null && invocation.Target.IsMatch (PrototypeFormatReference)) {
				AddFormatCallToInvocation (context, pexpr, invocation);
				return;
			}
			
			var arg = CreateFormatArgument (context);
			var newInvocation = new InvocationExpression (PrototypeFormatReference.Clone ()) {
				Arguments = { CreateFormatString (context, pexpr, 0), arg }
			};
			
			using (var script = context.StartScript ()) {
				script.Replace (pexpr, newInvocation);
				script.Select (arg);
			}
		}
		
		void AddFormatCallToInvocation (RefactoringContext context, PrimitiveExpression pExpr, InvocationExpression invocation)
		{
			var newInvocation = (InvocationExpression)invocation.Clone ();
			
			newInvocation.Arguments.First ().ReplaceWith (CreateFormatString (context, pExpr, newInvocation.Arguments.Count () - 1));
			newInvocation.Arguments.Add (CreateFormatArgument (context));
			
			using (var script = context.StartScript ()) {
				script.Replace (invocation, newInvocation);
			}
		}
		
		static PrimitiveExpression CreateFormatArgument (RefactoringContext context)
		{
			return new PrimitiveExpression (context.SelectedText);
		}
		
		static PrimitiveExpression CreateFormatString (RefactoringContext context, PrimitiveExpression pExpr, int argumentNumber)
		{
			var start = context.GetOffset (pExpr.StartLocation);
			var end = context.GetOffset (pExpr.EndLocation);
			
			return new PrimitiveExpression ("", context.GetText (start, context.SelectionStart - start) + "{" + argumentNumber + "}" + context.GetText (context.SelectionEnd, end - context.SelectionEnd));
		}
	}
}
