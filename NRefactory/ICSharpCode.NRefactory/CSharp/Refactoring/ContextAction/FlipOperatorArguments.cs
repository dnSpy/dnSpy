// 
// FlipOperatorArguments.cs
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
	public class FlipOperatorArguments : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			return GetBinaryOperatorExpression (context) != null;
		}
		
		public void Run (RefactoringContext context)
		{
			var binop = GetBinaryOperatorExpression (context);
			
			using (var script = context.StartScript ()) {
				script.Replace (binop.Left, binop.Right);
				script.Replace (binop.Right, binop.Left);
			}
		}
		
		public static BinaryOperatorExpression GetBinaryOperatorExpression (RefactoringContext context)
		{
			var node = context.GetNode<BinaryOperatorExpression> ();
			
			if (node == null || !node.OperatorToken.Contains (context.Location))
				return null;
			var result = node as BinaryOperatorExpression;
			if (result == null || (result.Operator != BinaryOperatorType.Equality && result.Operator != BinaryOperatorType.InEquality))
				return null;
			return result;
		}
	}
}
