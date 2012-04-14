// 
// RedundantThisInspector.cs
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;


namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant namespace usages.
	/// </summary>
	[IssueDescription("Remove redundant 'this.'",
	       Description= "Removes 'this.' references that are not required.",
	       Category = IssueCategories.Redundancies,
	       Severity = Severity.Hint,
	       IssueMarker = IssueMarker.GrayOut)]
	public class RedundantThisIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase
		{
			readonly RedundantThisIssue inspector;
			
			public GatherVisitor (BaseRefactoringContext ctx, RedundantThisIssue inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			static IMember GetMember (ResolveResult result)
			{
				if (result is MemberResolveResult) {
					return ((MemberResolveResult)result).Member;
				} else if (result is MethodGroupResolveResult) {
					return ((MethodGroupResolveResult)result).Methods.FirstOrDefault ();
				}

				return null;
			}

			public override void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
			{
				base.VisitThisReferenceExpression(thisReferenceExpression);
				var memberReference = thisReferenceExpression.Parent as MemberReferenceExpression;
				if (memberReference == null) {
					return;
				}

				var state = ctx.GetResolverStateAfter(thisReferenceExpression);
				var wholeResult = ctx.Resolve(memberReference);
			
				IMember member = GetMember(wholeResult);
				if (member == null) { 
					return;
				}

				var result = state.LookupSimpleNameOrTypeName(memberReference.MemberName, EmptyList<IType>.Instance, SimpleNameLookupMode.Expression);
			
				bool isRedundant;
				if (result is MemberResolveResult) {
					isRedundant = ((MemberResolveResult)result).Member.Region.Equals(member.Region);
				} else if (result is MethodGroupResolveResult) {
					isRedundant = ((MethodGroupResolveResult)result).Methods.Any(m => m.Region.Equals(member.Region));
				} else {
					return;
				}

				if (isRedundant) {
					AddIssue(thisReferenceExpression.StartLocation, memberReference.MemberNameToken.StartLocation, ctx.TranslateString("Remove redundant 'this.'"), script => {
						script.Replace(memberReference, RefactoringAstHelper.RemoveTarget(memberReference));
					}
					);
				}
			}
		}
	}
}