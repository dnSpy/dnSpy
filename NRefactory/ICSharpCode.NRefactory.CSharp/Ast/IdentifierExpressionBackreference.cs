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
using System.Linq;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Matches identifier expressions that have the same identifier as the referenced variable/type definition/method definition.
	/// </summary>
	public class IdentifierExpressionBackreference : Pattern
	{
		readonly string referencedGroupName;
		
		public string ReferencedGroupName {
			get { return referencedGroupName; }
		}
		
		public IdentifierExpressionBackreference(string referencedGroupName)
		{
			if (referencedGroupName == null)
				throw new ArgumentNullException("referencedGroupName");
			this.referencedGroupName = referencedGroupName;
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			CSharp.IdentifierExpression ident = other as CSharp.IdentifierExpression;
			if (ident == null || ident.TypeArguments.Any())
				return false;
			CSharp.AstNode referenced = (CSharp.AstNode)match.Get(referencedGroupName).Last();
			if (referenced == null)
				return false;
			return ident.Identifier == referenced.GetChildByRole(CSharp.Roles.Identifier).Name;
		}
	}
}
