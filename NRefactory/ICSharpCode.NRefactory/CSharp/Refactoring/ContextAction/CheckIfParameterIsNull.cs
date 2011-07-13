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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Creates a 'if (param == null) throw new System.ArgumentNullException ();' contruct for a parameter.
	/// </summary>
	public class CheckIfParameterIsNull : IContextAction
	{
		//TODO: Create 'multiple' null checks when more than 1 parameter is selected.
		public bool IsValid (RefactoringContext context)
		{
			var parameter = GetParameterDeclaration (context);
			if (parameter == null)
				return false;
			
			var bodyStatement = parameter.Parent.GetChildByRole (AstNode.Roles.Body);
			
			if (bodyStatement == null)
				return false;
			
			if (parameter.Type is PrimitiveType)
				return (((PrimitiveType)parameter.Type).Keyword == "object" || ((PrimitiveType)parameter.Type).Keyword == "string") && !HasNullCheck (parameter);
			
			// TODO: check for structs
			return !HasNullCheck (parameter);
		}
		
		public void Run (RefactoringContext context)
		{
			var parameter = GetParameterDeclaration (context);
			
			var bodyStatement = parameter.Parent.GetChildByRole (AstNode.Roles.Body);
			
			var statement = new IfElseStatement () {
				Condition = new BinaryOperatorExpression (new IdentifierExpression (parameter.Name), BinaryOperatorType.Equality, new NullReferenceExpression ()),
				TrueStatement = new ThrowStatement (new ObjectCreateExpression (context.CreateShortType ("System", "ArgumentNullException"), new PrimitiveExpression (parameter.Name)))
			};
			
			using (var script = context.StartScript ()) {
				script.AddTo (bodyStatement, statement);
			}
		}
		
		static ParameterDeclaration GetParameterDeclaration (RefactoringContext context)
		{
			return context.GetNode<ICSharpCode.NRefactory.CSharp.ParameterDeclaration> ();
		}

		static bool HasNullCheck (ParameterDeclaration parameter)
		{
			var visitor = new CheckNullVisitor (parameter);
			parameter.Parent.AcceptVisitor (visitor, null);
			return visitor.ContainsNullCheck;
		}
		
		class CheckNullVisitor : DepthFirstAstVisitor<object, object>
		{
			ParameterDeclaration parameter;
			
			public bool ContainsNullCheck { 
				get;
				set;
			}
			
			public CheckNullVisitor (ParameterDeclaration parameter)
			{
				this.parameter = parameter;
			}
			
			public object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
			{
				if (ifElseStatement.Condition is BinaryOperatorExpression) {
					var binOp = ifElseStatement.Condition as BinaryOperatorExpression;
					if ((binOp.Operator == BinaryOperatorType.Equality || binOp.Operator == BinaryOperatorType.InEquality) &&
						binOp.Left.IsMatch (new IdentifierExpression (parameter.Name)) && binOp.Right.IsMatch (new NullReferenceExpression ()) || 
						binOp.Right.IsMatch (new IdentifierExpression (parameter.Name)) && binOp.Left.IsMatch (new NullReferenceExpression ())) {
						ContainsNullCheck = true;
					}
				}
				
				return base.VisitIfElseStatement (ifElseStatement, data);
			}
		}
	}
}
