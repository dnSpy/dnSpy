//
// CallToVirtualFunctionFromConstructorIssue.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Constructors should not call virtual members",
	       Description = "Warns about calls to virtual member functions occuring in the constructor.",
	       Category = IssueCategories.CodeQualityIssues,
	       Severity = Severity.Warning)]
	public class CallToVirtualFunctionFromConstructorIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase
		{
			readonly BaseRefactoringContext context;
			
			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
				this.context = context;
			}

			bool isSealedType;

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				bool oldIsSealedType = isSealedType;
				isSealedType = typeDeclaration.Modifiers.HasFlag(Modifiers.Sealed);
				base.VisitTypeDeclaration(typeDeclaration);
				isSealedType = oldIsSealedType;
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				if (isSealedType)
					return;
				var body = constructorDeclaration.Body;
				if (body == null || body.IsNull)
					return;
				var callFinder = new VirtualCallFinderVisitor(context);
				body.AcceptVisitor(callFinder);
				FoundIssues.AddRange(callFinder.FoundIssues);
			}
		}

		class VirtualCallFinderVisitor: GatherVisitorBase
		{
			readonly BaseRefactoringContext context;

			public VirtualCallFinderVisitor(BaseRefactoringContext context) : base(context)
			{
				this.context = context;
			}

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);
				if (!IsCallDependentOnCurrentInstance(invocationExpression))
					// Call within current class scope without 'this' or 'base'
					return;
				var targetMethod = context.Resolve(invocationExpression) as InvocationResolveResult;
				if (targetMethod == null)
					return;
				if (targetMethod.IsVirtualCall) {
					AddIssue(invocationExpression,
					         context.TranslateString("Constructors should not call virtual members"));
				}
			}

			bool IsCallDependentOnCurrentInstance(InvocationExpression invocationExpression)
			{
				if (invocationExpression.Target is IdentifierExpression)
					// Call within current class scope without 'this' or 'base'
					return true;
				var expression = invocationExpression.Target as MemberReferenceExpression;
				if (expression == null || expression.Target is ThisReferenceExpression)
					// Call within current class scope using 'this' or 'base'
					return true;
				return false;
			}
		}
	}
}

