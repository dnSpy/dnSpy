//
// RemoveField.cs
//
// Author:
//       Ciprian Khlud <ciprian.mustiata@yahoo.com>
//
// Copyright (c) 2013 Ciprian Khlud
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp
{
//	[ContextAction("Removes a field from a class", Description = "It removes also the empty assingments and the usages")]
	public class RemoveFieldRefactoryAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var fieldDeclaration = GetFieldDeclaration(context);
            if(fieldDeclaration==null)
			    yield break;

			
			yield return new CodeAction(string.Format(context.TranslateString("Remove field '{0}'"), fieldDeclaration.Name)
			                            , script => GenerateNewScript(
                script, fieldDeclaration, context), fieldDeclaration);
		}
		
		
		void GenerateNewScript(Script script, FieldDeclaration fieldDeclaration, RefactoringContext context)
        {
            var firstOrNullObject = fieldDeclaration.Variables.FirstOrNullObject();
		    if(firstOrNullObject==null)
                return;
			var matchedNodes = ComputeMatchNodes(context, firstOrNullObject);

		    foreach (var matchNode in matchedNodes)
            {
                var parent = matchNode.Parent;
                if (matchNode is VariableInitializer)
                {
                    script.Remove(parent);
                }
                else
                if (matchNode is IdentifierExpression)
                {
                    if(parent is AssignmentExpression)
                    {
                        script.Remove(parent.Parent);
                    }
                    else
                    {
                        var clone = (IdentifierExpression)matchNode.Clone();
                        clone.Identifier = "TODO";
                        script.Replace(matchNode, clone);
                    }
                }
            }
        }

	    private static List<AstNode> ComputeMatchNodes(RefactoringContext context, VariableInitializer firstOrNullObject)
	    {
	        var referenceFinder = new FindReferences();
	        var matchedNodes = new List<AstNode>();

	        var resolveResult = context.Resolver.Resolve(firstOrNullObject);
	        var member = resolveResult as MemberResolveResult;
            if (member == null)//not a member is unexpected case, so is better to return no match than to break the code
                return matchedNodes;

	        FoundReferenceCallback callback = (matchNode, result) => matchedNodes.Add(matchNode);

	        var searchScopes = referenceFinder.GetSearchScopes(member.Member);
	        referenceFinder.FindReferencesInFile(searchScopes,
	                                             context.UnresolvedFile,
	                                             context.RootNode as SyntaxTree,
	                                             context.Compilation, callback,
	                                             context.CancellationToken);
	        return matchedNodes;
	    }

	    FieldDeclaration GetFieldDeclaration(RefactoringContext context)
		{
			var result = context.GetNode<FieldDeclaration>();

			return result;
		}
	}
}

