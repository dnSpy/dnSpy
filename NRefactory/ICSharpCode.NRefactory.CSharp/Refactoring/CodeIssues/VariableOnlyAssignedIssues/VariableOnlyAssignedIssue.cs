// 
// VariableOnlyAssignedIssue.cs
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class VariableOnlyAssignedIssue : ICodeIssueProvider
	{
		static FindReferences refFinder = new FindReferences ();
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return Enumerable.Empty<CodeIssue> ();
			return GetGatherVisitor (context, unit).GetIssues ();
		}

		protected static bool TestOnlyAssigned (BaseRefactoringContext ctx, SyntaxTree unit, IVariable variable)
		{
			var assignment = false;
			var nonAssignment = false;
			refFinder.FindLocalReferences (variable, ctx.UnresolvedFile, unit, ctx.Compilation,
				(node, resolveResult) =>
				{
					if (node is ParameterDeclaration)
						return;

					if (node is VariableInitializer) {
						if (!(node as VariableInitializer).Initializer.IsNull)
							assignment = true;
						return;
					}

					if (node is IdentifierExpression) {
						var parent = node.Parent;
						if (parent is AssignmentExpression) {
							if (((AssignmentExpression)parent).Left == node) {
								assignment = true;
								return;
							}
						} else if (parent is UnaryOperatorExpression) {
							var op = ((UnaryOperatorExpression)parent).Operator;
							switch (op) {
							case UnaryOperatorType.Increment:
							case UnaryOperatorType.PostIncrement:
							case UnaryOperatorType.Decrement:
							case UnaryOperatorType.PostDecrement:
								assignment = true;
								return;
							}
						} else if (parent is DirectionExpression) {
							if (((DirectionExpression)parent).FieldDirection == FieldDirection.Out) {
								assignment = true;
								return;
							}
						}
					}
					nonAssignment = true;
				}, ctx.CancellationToken);
			return assignment && !nonAssignment;
		}

		internal abstract GatherVisitorBase GetGatherVisitor (BaseRefactoringContext ctx, SyntaxTree unit);
	}
}
