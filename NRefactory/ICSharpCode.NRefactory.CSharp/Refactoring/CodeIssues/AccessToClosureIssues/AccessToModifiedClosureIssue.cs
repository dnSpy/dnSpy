// 
// AccessToModifiedClosureIssue.cs
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Access to modified closure variable",
					   Description = "Access to closure variable from anonymous method when the variable is modified " +
									 "externally",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]	
	public class AccessToModifiedClosureIssue : AccessToClosureIssue
	{
		public AccessToModifiedClosureIssue ()
			: base ("Access to modified closure")
		{
		}

		protected override NodeKind GetNodeKind (AstNode node)
		{
			var assignment = node.GetParent<AssignmentExpression> ();
			if (assignment != null && assignment.Left == node) {
				if (assignment.Operator == AssignmentOperatorType.Assign) 
					return NodeKind.Modification;
				return NodeKind.ReferenceAndModification;
			}
			var unaryExpr = node.GetParent<UnaryOperatorExpression> ();
			if (unaryExpr != null && unaryExpr.Expression == node &&
				(unaryExpr.Operator == UnaryOperatorType.Increment ||
				 unaryExpr.Operator == UnaryOperatorType.PostIncrement ||
				 unaryExpr.Operator == UnaryOperatorType.Decrement ||
				 unaryExpr.Operator == UnaryOperatorType.PostDecrement)) {
				return NodeKind.ReferenceAndModification;
			}
			if (node.Parent is ForeachStatement)
				return NodeKind.Modification;

			return NodeKind.Reference;
		}

		protected override IEnumerable<CodeAction> GetFixes (BaseRefactoringContext context, Node env,
															 string variableName)
		{
			var containingStatement = env.ContainingStatement;

			// we don't give a fix for these cases since the general fix may not work
			// lambda in while/do-while/for condition
			if (containingStatement is WhileStatement || containingStatement is DoWhileStatement ||
				containingStatement is ForStatement)
				yield break;
			// lambda in for initializer/iterator
			if (containingStatement.Parent is ForStatement &&
				((ForStatement)containingStatement.Parent).EmbeddedStatement != containingStatement)
				yield break;

			Action<Script> action = script =>
			{
				var newName = LocalVariableNamePicker.PickSafeName (
					containingStatement.GetParent<EntityDeclaration> (),
					Enumerable.Range (1, 100).Select (i => variableName + i));

				var variableDecl = new VariableDeclarationStatement (new SimpleType("var"), newName, 
																	 new IdentifierExpression (variableName));
				
				if (containingStatement.Parent is BlockStatement || containingStatement.Parent is SwitchSection) {
					script.InsertBefore (containingStatement, variableDecl);
				} else {
					var offset = script.GetCurrentOffset (containingStatement.StartLocation);
					script.InsertBefore (containingStatement, variableDecl);
					script.InsertText (offset, "{");
					script.InsertText (script.GetCurrentOffset (containingStatement.EndLocation), "}");
					script.FormatText (containingStatement.Parent);
				}

				var textNodes = new List<AstNode> ();
				textNodes.Add (variableDecl.Variables.First ().NameToken);

				foreach (var reference in env.GetAllReferences ()) {
					var identifier = new IdentifierExpression (newName);
					script.Replace (reference.AstNode, identifier);
					textNodes.Add (identifier);
				}
				script.Link (textNodes.ToArray ());
			};
			yield return new CodeAction (context.TranslateString ("Copy to local variable"), action);
		}
	}
}
