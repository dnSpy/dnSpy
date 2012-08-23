//
// VariableUsageAnalyzation.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring.ExtractMethod
{
	
	public enum VariableState {
		None,
		Used,
		Changed
	}
	
	
	public class VariableUsageAnalyzation : DepthFirstAstVisitor
	{
		readonly RefactoringContext context;
		readonly List<IVariable> usedVariables;
		
		Dictionary<IVariable, VariableState> states = new Dictionary<IVariable, VariableState> ();
		
		TextLocation startLocation = TextLocation.Empty;
		TextLocation endLocation = TextLocation.Empty;
		
		public VariableUsageAnalyzation (RefactoringContext context, List<IVariable> usedVariables)
		{
			this.context = context;
			this.usedVariables = usedVariables;
		}

		public bool Has(IVariable variable)
		{
			return states.ContainsKey (variable);
		}
		
		public void SetAnalyzedRange(AstNode start, AstNode end, bool startInclusive = true, bool endInclusive = true)
		{
			if (start == null)
				throw new ArgumentNullException("start");
			if (end == null)
				throw new ArgumentNullException("end");
			startLocation = startInclusive ? start.StartLocation : start.EndLocation;
			endLocation = endInclusive ? end.EndLocation : end.StartLocation;
			states.Clear ();
		}
		
		public VariableState GetStatus (IVariable variable)
		{
			VariableState state;
			if (!states.TryGetValue (variable, out state))
				return VariableState.None;
			return state;
		}
		
		void SetState (string identifier, VariableState state)
		{
			var variable = usedVariables.FirstOrDefault (v => v.Name == identifier);
			if (variable == null)
				return;
			VariableState oldState;
			if (states.TryGetValue (variable, out oldState)) {
				if (oldState < state)
					states [variable] = state;
			} else {
				states [variable] = state;
			}
		}
		
		public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			if (startLocation.IsEmpty || startLocation <= identifierExpression.StartLocation && identifierExpression.EndLocation <= endLocation) {
				SetState (identifierExpression.Identifier, VariableState.Used);
			}
		}
		
		public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			if (startLocation.IsEmpty || startLocation <= assignmentExpression.StartLocation && assignmentExpression.EndLocation <= endLocation) {
				var left = assignmentExpression.Left as IdentifierExpression;
				if (left != null)
					SetState(left.Identifier, VariableState.Changed);
			}
			base.VisitAssignmentExpression (assignmentExpression);
		}
		
		public override void VisitDirectionExpression(DirectionExpression directionExpression)
		{
			if (startLocation.IsEmpty || startLocation <= directionExpression.StartLocation && directionExpression.EndLocation <= endLocation) {
				var expr = directionExpression.Expression as IdentifierExpression;
				if (expr != null)
					SetState(expr.Identifier, VariableState.Changed);
			}
			base.VisitDirectionExpression (directionExpression);
		}

		public override void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			if (startLocation.IsEmpty || startLocation <= variableInitializer.StartLocation && variableInitializer.EndLocation <= endLocation) {
				SetState(variableInitializer.Name, variableInitializer.Initializer.IsNull ?  VariableState.None : VariableState.Changed);
			}

			base.VisitVariableInitializer(variableInitializer);
		}

		public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			if (startLocation.IsEmpty || startLocation <= unaryOperatorExpression.StartLocation && unaryOperatorExpression.EndLocation <= endLocation) {
				if (unaryOperatorExpression.Operator == UnaryOperatorType.Increment || unaryOperatorExpression.Operator == UnaryOperatorType.Decrement ||
					unaryOperatorExpression.Operator == UnaryOperatorType.PostIncrement || unaryOperatorExpression.Operator == UnaryOperatorType.PostDecrement) {
					var expr = unaryOperatorExpression.Expression as IdentifierExpression;
					if (expr != null)
						SetState(expr.Identifier, VariableState.Changed);
				}
			}
			base.VisitUnaryOperatorExpression (unaryOperatorExpression);
		}
		
	}
}

