// 
// RedundantNamespaceUsageInspector.cs
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
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant namespace usages.
	/// </summary>
	[IssueDescription("Remove redundant namespace usages",
	                  Description = "Removes namespace usages that are obsolete.",
	                  Category = IssueCategories.Redundancies,
	                  Severity = Severity.Hint,
	                  IssueMarker = IssueMarker.GrayOut)]
	public class RedundantNamespaceUsageIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase
		{
			readonly RedundantNamespaceUsageIssue inspector;
			
			public GatherVisitor (BaseRefactoringContext ctx, RedundantNamespaceUsageIssue inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
			{
				base.VisitMemberReferenceExpression(memberReferenceExpression);
				HandleMemberReference(
					memberReferenceExpression, memberReferenceExpression.Target, memberReferenceExpression.MemberNameToken, memberReferenceExpression.TypeArguments, SimpleNameLookupMode.Expression,
					script => {
						script.Replace(memberReferenceExpression, RefactoringAstHelper.RemoveTarget(memberReferenceExpression));
					});
			}
			
			public override void VisitMemberType(MemberType memberType)
			{
				base.VisitMemberType(memberType);
				HandleMemberReference(
					memberType, memberType.Target, memberType.MemberNameToken, memberType.TypeArguments, SimpleNameLookupMode.Type,
					script => {
						script.Replace(memberType, RefactoringAstHelper.RemoveTarget(memberType));
					});
			}
			
			void HandleMemberReference(AstNode wholeNode, AstNode targetNode, Identifier memberName, IEnumerable<AstType> typeArguments, SimpleNameLookupMode mode, Action<Script> action)
			{
				var result = ctx.Resolve(targetNode);
				if (!(result is NamespaceResolveResult)) {
					return;
				}
				var wholeResult = ctx.Resolve(wholeNode);
				if (!(wholeResult is TypeResolveResult)) {
					return;
				}

				var state = ctx.GetResolverStateBefore(wholeNode);
				var resolvedTypeArguments = typeArguments.Select(ctx.ResolveType).ToList();
				var lookupName = state.LookupSimpleNameOrTypeName(memberName.Name, resolvedTypeArguments, mode);
				
				if (lookupName is TypeResolveResult && !lookupName.IsError && wholeResult.Type.Equals(lookupName.Type)) {
					AddIssue(wholeNode.StartLocation, memberName.StartLocation, ctx.TranslateString("Remove redundant namespace usage"), action);
				}
			}
		}
	}
}
