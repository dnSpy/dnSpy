// 
// SplitString.cs
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
	public class SplitString: IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			if (context.IsSomethingSelected)
				return false;
			var pexpr = context.GetNode<PrimitiveExpression> ();
			if (pexpr == null || !(pexpr.Value is string))
				return false;
			if (pexpr.LiteralValue.StartsWith ("@"))
				return pexpr.StartLocation < new TextLocation (context.Location.Line, context.Location.Column - 2) &&
					new TextLocation (context.Location.Line, context.Location.Column + 2) < pexpr.EndLocation;
			return pexpr.StartLocation < new TextLocation (context.Location.Line, context.Location.Column - 1) &&
				new TextLocation (context.Location.Line, context.Location.Column + 1) < pexpr.EndLocation;
		}
		
		public void Run (RefactoringContext context)
		{
			var pexpr = context.GetNode<PrimitiveExpression> ();
			int offset = context.GetOffset (context.Location);
			using (var script = context.StartScript ()) {
				script.InsertText (offset, pexpr.LiteralValue.StartsWith ("@") ? "\" + @\"" : "\" + \"");
			}
		}	
	}
}
