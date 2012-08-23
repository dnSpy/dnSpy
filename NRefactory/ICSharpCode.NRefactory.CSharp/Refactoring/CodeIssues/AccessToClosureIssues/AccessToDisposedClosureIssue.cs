// 
// AccessToDisposedClosureIssue.cs
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
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{

	[IssueDescription ("Access to disposed closure variable",
					   Description = "Access to closure variable from anonymous method when the variable is" + 
									 " disposed externally",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class AccessToDisposedClosureIssue : AccessToClosureIssue
	{
		public AccessToDisposedClosureIssue ()
			: base ("Access to disposed closure")
		{
		}

		protected override bool IsTargetVariable (IVariable variable)
		{
			return variable.Type.GetAllBaseTypeDefinitions ().Any (t => t.KnownTypeCode == KnownTypeCode.IDisposable);
		}

		protected override NodeKind GetNodeKind (AstNode node)
		{
			if (node.Parent is UsingStatement)
				return NodeKind.ReferenceAndModification;

			if (node.Parent is VariableDeclarationStatement && node.Parent.Parent is UsingStatement)
				return NodeKind.Modification;

			var memberRef = node.Parent as MemberReferenceExpression;
			if (memberRef != null && memberRef.Parent is InvocationExpression && memberRef.MemberName == "Dispose")
				return NodeKind.ReferenceAndModification;

			return NodeKind.Reference;
		}

		protected override bool CanReachModification (ControlFlowNode node, Statement start,
												   IDictionary<Statement, IList<Node>> modifications)
		{
			if (base.CanReachModification (node, start, modifications))
				return true;

			if (node.NextStatement != start) {
				var usingStatement = node.PreviousStatement as UsingStatement;
				if (usingStatement != null) {
					if (modifications.ContainsKey(usingStatement))
						return true;
					if (usingStatement.ResourceAcquisition is Statement &&
						modifications.ContainsKey ((Statement)usingStatement.ResourceAcquisition))
						return true;
				}
			}
			return false;
		}

		protected override IEnumerable<CodeAction> GetFixes (BaseRefactoringContext context, Node env,
															 string variableName)
		{
			yield break;
		}
	}
}