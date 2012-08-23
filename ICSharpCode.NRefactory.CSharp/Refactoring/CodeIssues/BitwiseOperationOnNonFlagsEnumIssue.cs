// 
// BitwiseOperationOnNonFlagsEnumIssue.cs
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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Bitwise Operations on enum without [Flags] attribute",
					   Description = "Bitwise Operations on enum not marked with [Flags] attribute",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class BitwiseOperationOnNonFlagsEnumIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base(ctx)
			{
			}
			
			static bool IsBitwiseOperator (UnaryOperatorType op)
			{
				return op == UnaryOperatorType.BitNot;
			}

			static bool IsBitwiseOperator (AssignmentOperatorType op)
			{
				return op == AssignmentOperatorType.BitwiseAnd || op == AssignmentOperatorType.BitwiseOr ||
					op == AssignmentOperatorType.ExclusiveOr;
			}

			static bool IsBitwiseOperator (BinaryOperatorType op)
			{
				return op == BinaryOperatorType.BitwiseAnd || op == BinaryOperatorType.BitwiseOr || 
					op == BinaryOperatorType.ExclusiveOr;
			}

			bool IsNonFlagsEnum (Expression expr)
			{
				var resolveResult = ctx.Resolve (expr);
				if (resolveResult == null || resolveResult.Type.Kind != TypeKind.Enum)
					return false;

				// check [Flags]
				var typeDef = resolveResult.Type.GetDefinition ();
				return typeDef != null &&
					typeDef.Attributes.All (attr => attr.AttributeType.FullName != "System.FlagsAttribute");
			}

			private void AddIssue (CSharpTokenNode operatorToken)
			{
				AddIssue (operatorToken, 
					ctx.TranslateString ("Bitwise Operations on enum not marked with Flags attribute"));
			}

			public override void VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression)
			{
				base.VisitUnaryOperatorExpression (unaryOperatorExpression);

				if (!IsBitwiseOperator (unaryOperatorExpression.Operator))
					return;
				if (IsNonFlagsEnum (unaryOperatorExpression.Expression))
					AddIssue (unaryOperatorExpression.OperatorToken);
			}

			public override void VisitAssignmentExpression (AssignmentExpression assignmentExpression)
			{
				base.VisitAssignmentExpression (assignmentExpression);

				if (!IsBitwiseOperator (assignmentExpression.Operator))
					return;
				if (IsNonFlagsEnum (assignmentExpression.Right))
					AddIssue (assignmentExpression.OperatorToken);
			}

			public override void VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression)
			{
				base.VisitBinaryOperatorExpression (binaryOperatorExpression);

				if (!IsBitwiseOperator (binaryOperatorExpression.Operator))
					return;
				if (IsNonFlagsEnum (binaryOperatorExpression.Left) || IsNonFlagsEnum (binaryOperatorExpression.Right))
					AddIssue (binaryOperatorExpression.OperatorToken);
			}

		}
	}
}
