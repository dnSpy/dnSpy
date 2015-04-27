// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Helper methods for constructing ASTs for refactoring.
	/// These helpers work with frozen ASTs, i.e. they clone input nodes.
	/// </summary>
	public class RefactoringAstHelper
	{
		/// <summary>
		/// Removes the target from a member reference while preserving the identifier and type arguments.
		/// </summary>
		public static IdentifierExpression RemoveTarget(MemberReferenceExpression mre)
		{
			IdentifierExpression ident = new IdentifierExpression(mre.MemberName);
			ident.TypeArguments.AddRange(mre.TypeArguments.Select(t => t.Clone()));
			return ident;
		}
		
		/// <summary>
		/// Removes the target from a member reference while preserving the identifier and type arguments.
		/// </summary>
		public static SimpleType RemoveTarget(MemberType memberType)
		{
			return new SimpleType(memberType.MemberName, memberType.TypeArguments.Select(t => t.Clone()));
		}
	}
}
