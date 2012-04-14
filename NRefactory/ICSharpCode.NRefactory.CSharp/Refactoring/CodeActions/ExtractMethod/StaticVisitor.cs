// 
// StaticVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.Semantics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring.ExtractMethod
{
	class StaticVisitor : DepthFirstAstVisitor
	{
		readonly RefactoringContext context;
		public bool UsesNonStaticMember = false;

		StaticVisitor(RefactoringContext context)
		{
			this.context = context;
		}
		
		
		public static bool UsesNotStaticMember(RefactoringContext context, AstNode node)
		{
			var visitor = new StaticVisitor(context);
			node.AcceptVisitor(visitor);
			return visitor.UsesNonStaticMember;
		}

		protected override void VisitChildren(AstNode node)
		{
			if (UsesNonStaticMember)
				return;
			base.VisitChildren(node);
		}

		public override void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			UsesNonStaticMember = true;
			base.VisitThisReferenceExpression(thisReferenceExpression);
		}

		public override void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
		{
			UsesNonStaticMember = true;
			base.VisitBaseReferenceExpression(baseReferenceExpression);
		}

		public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			var resolveResult = context.Resolve(identifierExpression);
			if (resolveResult is MemberResolveResult) {
				var memberResult = (MemberResolveResult)resolveResult;
				if (!memberResult.Member.IsStatic)
					UsesNonStaticMember = true;
			} else if (resolveResult is MethodGroupResolveResult) {
				var methodGroupResolveResult = (MethodGroupResolveResult)resolveResult;
				if (methodGroupResolveResult.Methods.Any(m => !m.IsStatic))
					UsesNonStaticMember = true;
			}
		}
	}
}
