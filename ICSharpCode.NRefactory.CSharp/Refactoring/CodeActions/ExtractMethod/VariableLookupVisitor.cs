// 
// VariableLookupVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring.ExtractMethod
{
	class VariableLookupVisitor : DepthFirstAstVisitor
	{
		readonly RefactoringContext context;

		public List<IVariable> UsedVariables = new List<IVariable> ();

		TextLocation startLocation = TextLocation.Empty;
		TextLocation endLocation = TextLocation.Empty;


		public VariableLookupVisitor (RefactoringContext context)
		{
			this.context = context;
		}
		
		public void SetAnalyzedRange(AstNode start, AstNode end, bool startInclusive = true, bool endInclusive = true)
		{
			if (start == null)
				throw new ArgumentNullException("start");
			if (end == null)
				throw new ArgumentNullException("end");
			startLocation = startInclusive ? start.StartLocation : start.EndLocation;
			endLocation = endInclusive ? end.EndLocation : end.StartLocation;
		}

		public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			if (startLocation.IsEmpty || startLocation <= identifierExpression.StartLocation && identifierExpression.EndLocation <= endLocation) {
				var result = context.Resolve(identifierExpression);
				var local = result as LocalResolveResult;
				if (local != null && !UsedVariables.Contains(local.Variable))
					UsedVariables.Add(local.Variable);
			}
		}

		public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			base.VisitVariableDeclarationStatement(variableDeclarationStatement);
			foreach (var varDecl in variableDeclarationStatement.Variables) {
				if (startLocation.IsEmpty || startLocation <= varDecl.StartLocation && varDecl.EndLocation <= endLocation) {
					var result = context.Resolve(varDecl);
					var local = result as LocalResolveResult;
					if (local != null && !UsedVariables.Contains(local.Variable))
						UsedVariables.Add(local.Variable);
				}
			}
		}
		

		public static List<IVariable> Analyze(RefactoringContext context, Expression expression)
		{
			var visitor = new VariableLookupVisitor(context);
			expression.AcceptVisitor(visitor);
			return visitor.UsedVariables;
		}

		public static List<IVariable> Analyze(RefactoringContext context, List<Statement> statements)
		{
			var visitor = new VariableLookupVisitor(context);
			statements.ForEach(stmt => stmt.AcceptVisitor(visitor));
			return visitor.UsedVariables;
		}
	}
}

