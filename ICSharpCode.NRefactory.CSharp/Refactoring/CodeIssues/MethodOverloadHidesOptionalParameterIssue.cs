// 
// MethodOverloadHidesOptionalParameterIssue.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Method with optional parameter is hidden by overload",
					   Description = "Method with optional parameter is hidden by overload",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class MethodOverloadHidesOptionalParameterIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
			{
				base.VisitMethodDeclaration (methodDeclaration);

				var resolveResult = ctx.Resolve (methodDeclaration) as MemberResolveResult;
				if (resolveResult == null)
					return;
				var method = resolveResult.Member as IMethod;
				if (method == null)
					return;

				if (method.Parameters.Count == 0 || !method.Parameters.Last ().IsOptional)
					return;

				var overloads = method.DeclaringType.GetMembers (m => m.Name == method.Name).OfType<IMethod> ()
					.ToArray ();

				var parameterNodes = methodDeclaration.Parameters.ToArray();
				var parameters = new List<IParameter> ();
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (method.Parameters [i].IsOptional && 
						overloads.Any (m => ParameterListComparer.Instance.Equals (parameters, m.Parameters))) {
						AddIssue (parameterNodes [i].StartLocation, parameterNodes.Last ().EndLocation,
							ctx.TranslateString ("Method with optional parameter is hidden by overload"));
						break;
					}
					parameters.Add (method.Parameters [i]);
				}
			}
		}
	}
}
