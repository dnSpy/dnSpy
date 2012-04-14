// 
// GatherVisitorBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// A base class for writing issue provider visitor implementations.
	/// </summary>
	class GatherVisitorBase : DepthFirstAstVisitor
	{
		protected readonly BaseRefactoringContext ctx;

		public readonly List<CodeIssue> FoundIssues = new List<CodeIssue> ();

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.GatherVisitorBase"/> class.
		/// </summary>
		/// <param name='ctx'>
		/// The refactoring context.
		/// </param>
		public GatherVisitorBase (BaseRefactoringContext ctx)
		{
			this.ctx = ctx;
		}

		/// <summary>
		/// Gets all the issues using the context root node as base.
		/// </summary>
		/// <returns>
		/// The issues.
		/// </returns>
		public IEnumerable<CodeIssue> GetIssues()
		{
			ctx.RootNode.AcceptVisitor(this);
			return FoundIssues;
		}

		protected override void VisitChildren (AstNode node)
		{
			if (ctx.CancellationToken.IsCancellationRequested)
				return;
			base.VisitChildren (node);
		}
		
		protected void AddIssue(AstNode node, string title, System.Action<Script> fix = null)
		{
			FoundIssues.Add(new CodeIssue (title, node.StartLocation, node.EndLocation, fix != null ? new CodeAction (title, fix) : null));
		}

		protected void AddIssue(TextLocation start, TextLocation end, string title, System.Action<Script> fix = null)
		{
			FoundIssues.Add(new CodeIssue(title, start, end, fix != null ? new CodeAction(title, fix) : null));
		}

		protected void AddIssue(AstNode node, string title, IEnumerable<CodeAction> fixes)
		{
			FoundIssues.Add(new CodeIssue(title, node.StartLocation, node.EndLocation, fixes));
		}

		protected void AddIssue(TextLocation start, TextLocation end, string title, IEnumerable<CodeAction> fixes)
		{
			FoundIssues.Add(new CodeIssue (title, start, end, fixes));
		}


	}
		
	
}

