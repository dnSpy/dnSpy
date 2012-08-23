//
// SetterDoesNotUseValueParameterTests.cs
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Threading;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("The value parameter is not used in a context where is should be",
	       Description = "Warns about property or indexer setters and event adders or removers that do not use the value parameter.",
	       Category = IssueCategories.CodeQualityIssues,
	       Severity = Severity.Warning)]
	public class ValueParameterUnusedIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase
		{
			readonly BaseRefactoringContext context;
			
			public GatherVisitor(BaseRefactoringContext context, ValueParameterUnusedIssue inspector) : base (context)
			{
				this.context = context;
			}

			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
			{
				FindIssuesInNode(indexerDeclaration.Setter, indexerDeclaration.Setter.Body);
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
				FindIssuesInNode(propertyDeclaration.Setter, propertyDeclaration.Setter.Body);
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
				var addAccessor = eventDeclaration.AddAccessor;
				FindIssuesInNode(addAccessor, addAccessor.Body, "add accessor");
				var removeAccessor = eventDeclaration.RemoveAccessor;
				FindIssuesInNode(removeAccessor, removeAccessor.Body, "remove accessor");
			}

			void FindIssuesInNode(AstNode anchor, AstNode node, string accessorName = "setter")
			{
				if (node == null || node.IsNull)
					return;
				var localResolveResult = context.GetResolverStateBefore(node)
					.LookupSimpleNameOrTypeName("value", new List<IType>(), NameLookupMode.Expression) as LocalResolveResult; 
				if (localResolveResult == null)
					return;

				var variable = localResolveResult.Variable;
				bool referenceFound = false;
				var findRef = new FindReferences();
				var syntaxTree = (SyntaxTree)context.RootNode;
				findRef.FindLocalReferences(variable, context.UnresolvedFile, syntaxTree, context.Compilation, (n, entity) => {
					if (n.StartLocation >= node.StartLocation && n.EndLocation <= node.EndLocation) {
						referenceFound = true;
					}
				}, CancellationToken.None);

				if(!referenceFound)
					AddIssue(anchor, context.TranslateString("The " + accessorName + " does not use the 'value' parameter"));
			}
		}
	}
}
