// 
// ConvertIfToSwitchAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Convert 'if' to 'switch'", Description = "Convert 'if' statement to 'switch' statement")]
	public class ConvertIfToSwitchAction : SpecializedCodeAction<IfElseStatement>
	{
		protected override CodeAction GetAction (RefactoringContext context, IfElseStatement node)
		{
			if (!node.IfToken.Contains (context.Location))
				return null;

			var switchExpr = GetSwitchExpression (context, node.Condition);
			if (switchExpr == null)
				return null;

			var switchSections = new List<SwitchSection> ();
			if (!CollectSwitchSections (switchSections, context, node, switchExpr))
				return null;

			return new CodeAction (context.TranslateString ("Convert 'if' to 'switch'"),
				script =>
				{
					var switchStatement = new SwitchStatement { Expression = switchExpr.Clone () };
					switchStatement.SwitchSections.AddRange (switchSections);
					script.Replace (node, switchStatement);
				});
		}

		static Expression GetSwitchExpression (RefactoringContext context, Expression expr)
		{
			var binaryOp = expr as BinaryOperatorExpression;
			if (binaryOp == null)
				return null;

			if (binaryOp.Operator == BinaryOperatorType.ConditionalOr)
				return GetSwitchExpression (context, binaryOp.Left);

			if (binaryOp.Operator == BinaryOperatorType.Equality) {
				Expression switchExpr = null;
				if (IsConstantExpression (context, binaryOp.Right))
					switchExpr = binaryOp.Left;
				if (IsConstantExpression (context, binaryOp.Left))
					switchExpr = binaryOp.Right;
				if (switchExpr != null && IsValidSwitchType (context.Resolve (switchExpr).Type))
					return switchExpr;
			}

			return null;
		}

		static bool IsConstantExpression (RefactoringContext context, Expression expr)
		{
			if (expr is PrimitiveExpression || expr is NullReferenceExpression)
				return true;
			return context.Resolve (expr).IsCompileTimeConstant;
		}

		static readonly KnownTypeCode [] validTypes = 
		{
			KnownTypeCode.String, KnownTypeCode.Boolean, KnownTypeCode.Char,
			KnownTypeCode.Byte, KnownTypeCode.SByte,
			KnownTypeCode.Int16, KnownTypeCode.Int32, KnownTypeCode.Int64,
			KnownTypeCode.UInt16, KnownTypeCode.UInt32, KnownTypeCode.UInt64
		};

		static bool IsValidSwitchType (IType type)
		{
			if (type.Kind == TypeKind.Enum)
				return true;
			var typeDefinition = type.GetDefinition ();
			if (typeDefinition == null)
				return false;

			if (typeDefinition.KnownTypeCode == KnownTypeCode.NullableOfT) {
				var nullableType = (ParameterizedType)type;
				typeDefinition = nullableType.TypeArguments [0].GetDefinition ();
				if (typeDefinition == null)
					return false;
			}
			return Array.IndexOf (validTypes, typeDefinition.KnownTypeCode) != -1;
		}

		static bool CollectSwitchSections (ICollection<SwitchSection> result, RefactoringContext context, 
										   IfElseStatement ifStatement, Expression switchExpr)
		{
			// if
			var section = new SwitchSection ();
			if (!CollectCaseLabels (section.CaseLabels, context, ifStatement.Condition, switchExpr))
				return false;
			CollectSwitchSectionStatements (section.Statements, context, ifStatement.TrueStatement);
			result.Add (section);

			if (ifStatement.FalseStatement.IsNull)
				return true;

			// else if
			var falseStatement = ifStatement.FalseStatement as IfElseStatement;
			if (falseStatement != null)
				return CollectSwitchSections (result, context, falseStatement, switchExpr);

			// else (default label)
			var defaultSection = new SwitchSection ();
			defaultSection.CaseLabels.Add (new CaseLabel ());
			CollectSwitchSectionStatements (defaultSection.Statements, context, ifStatement.FalseStatement);
			result.Add (defaultSection);

			return true;
		}

		static bool CollectCaseLabels (AstNodeCollection<CaseLabel> result, RefactoringContext context, 
									   Expression condition, Expression switchExpr)
		{
			if (condition is ParenthesizedExpression)
				return CollectCaseLabels (result, context, ((ParenthesizedExpression)condition).Expression, switchExpr);

			var binaryOp = condition as BinaryOperatorExpression;
			if (binaryOp == null)
				return false;

			if (binaryOp.Operator == BinaryOperatorType.ConditionalOr)
				return CollectCaseLabels (result, context, binaryOp.Left, switchExpr) &&
					   CollectCaseLabels (result, context, binaryOp.Right, switchExpr);

			if (binaryOp.Operator == BinaryOperatorType.Equality) {
				if (switchExpr.Match (binaryOp.Left).Success) {
					if (IsConstantExpression (context, binaryOp.Right)) {
						result.Add (new CaseLabel (binaryOp.Right.Clone ()));
						return true;
					}
				} else if (switchExpr.Match (binaryOp.Right).Success) {
					if (IsConstantExpression (context, binaryOp.Left)) {
						result.Add (new CaseLabel (binaryOp.Left.Clone ()));
						return true;
					}
				}
			}

			return false;
		}
		
		static void CollectSwitchSectionStatements (AstNodeCollection<Statement> result, RefactoringContext context, 
												    Statement statement)
		{
			BlockStatement blockStatement;
			if (statement is BlockStatement)
				blockStatement = (BlockStatement)statement.Clone ();
			else
				blockStatement = new BlockStatement { statement.Clone () };

			var breackStatement = new BreakStatement ();
			blockStatement.Add (breackStatement);
			// check if break is needed
			var reachabilityAnalysis = context.CreateReachabilityAnalysis (blockStatement);
			if (!reachabilityAnalysis.IsReachable (breackStatement))
				blockStatement.Statements.Remove (breackStatement);

			var statements = blockStatement.Statements.ToArray ();
			blockStatement.Statements.Clear ();
			result.AddRange (statements);
		}
	}
}
