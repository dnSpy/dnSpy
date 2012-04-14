// 
// CheckIfParameterIsNull.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

using ICSharpCode.NRefactory.PatternMatching;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Creates a 'if (param == null) throw new System.ArgumentNullException ();' contruct for a parameter.
	/// </summary>
	[ContextAction("Check if parameter is null", Description = "Checks function parameter is not null.")]
	public class CheckIfParameterIsNullAction : SpecializedCodeAction<ParameterDeclaration>
	{
		protected override CodeAction GetAction(RefactoringContext context, ParameterDeclaration parameter)
		{
			var bodyStatement = parameter.Parent.GetChildByRole(Roles.Body);
			if (bodyStatement == null)
				return null;
			var type = context.ResolveType(parameter.Type);
			if (type.IsReferenceType == false || HasNullCheck(parameter)) 
				return null;
			
			return new CodeAction (context.TranslateString("Add null check for parameter"), script => {
				var statement = new IfElseStatement () {
					Condition = new BinaryOperatorExpression (new IdentifierExpression (parameter.Name), BinaryOperatorType.Equality, new NullReferenceExpression ()),
					TrueStatement = new ThrowStatement (new ObjectCreateExpression (context.CreateShortType("System", "ArgumentNullException"), new PrimitiveExpression (parameter.Name)))
				};
				script.AddTo(bodyStatement, statement);
			});
		}

		static bool HasNullCheck (ParameterDeclaration parameter)
		{
			var visitor = new CheckNullVisitor (parameter);
			parameter.Parent.AcceptVisitor (visitor, null);
			return visitor.ContainsNullCheck;
		}
		
		class CheckNullVisitor : DepthFirstAstVisitor<object, object>
		{
			readonly Expression pattern;
			
			internal bool ContainsNullCheck;
			
			public CheckNullVisitor (ParameterDeclaration parameter)
			{
				pattern = PatternHelper.CommutativeOperator(new IdentifierExpression(parameter.Name), BinaryOperatorType.Any, new NullReferenceExpression());
			}
			
			public override object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
			{
				if (ifElseStatement.Condition is BinaryOperatorExpression) {
					var binOp = ifElseStatement.Condition as BinaryOperatorExpression;
					if ((binOp.Operator == BinaryOperatorType.Equality || binOp.Operator == BinaryOperatorType.InEquality) && pattern.IsMatch(binOp)) {
						ContainsNullCheck = true;
					}
				}
				
				return base.VisitIfElseStatement (ifElseStatement, data);
			}
		}
	}
}
