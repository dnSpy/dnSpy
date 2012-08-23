//
// CriteriaCollector.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class TypeCriteriaCollector : DepthFirstAstVisitor
	{
		BaseRefactoringContext context;
		
		public TypeCriteriaCollector(BaseRefactoringContext context)
		{
			this.context = context;
			TypeCriteria = new Dictionary<IVariable, IList<ITypeCriterion>>();
			UsedVariables = new HashSet<IVariable>();
		}

		IDictionary<IVariable, IList<ITypeCriterion>> TypeCriteria { get; set; }

		public HashSet<IVariable> UsedVariables { get; private set; }

		public ITypeCriterion GetCriterion(IVariable variable)
		{
			if (!TypeCriteria.ContainsKey(variable))
				return new ConjunctionCriteria(new List<ITypeCriterion>());
			return new ConjunctionCriteria(TypeCriteria[variable]);
		}

		void AddCriterion(IVariable variable, ITypeCriterion criterion)
		{
			if (!TypeCriteria.ContainsKey(variable))
				TypeCriteria[variable] = new List<ITypeCriterion>();
			TypeCriteria[variable].Add(criterion);
		}

		public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			base.VisitMemberReferenceExpression(memberReferenceExpression);

			var targetResolveResult = context.Resolve(memberReferenceExpression.Target) as LocalResolveResult;
			if (targetResolveResult == null)
				return;
			var variable = targetResolveResult.Variable;
			var conversion = context.GetConversion(memberReferenceExpression);
			if (conversion.IsMethodGroupConversion) {
				AddCriterion(variable, new HasMemberCriterion(conversion.Method));
			} else {
				var resolveResult = context.Resolve(memberReferenceExpression);
				var memberResolveResult = resolveResult as MemberResolveResult;
				if (memberResolveResult != null)
					AddCriterion(variable, new HasMemberCriterion(memberResolveResult.Member));
			}
		}

		public override void VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			base.VisitIndexerExpression(indexerExpression);

			var localResolveResult = context.Resolve(indexerExpression.Target) as LocalResolveResult;
			if (localResolveResult == null)
				return;
			var resolveResult = context.Resolve(indexerExpression);
			if (localResolveResult == null)
				return;
			var parent = indexerExpression.Parent;
			while (parent is ParenthesizedExpression)
				parent = parent.Parent;
			if (parent is DirectionExpression) {
				// The only types which are indexable and where the indexing expression
				// results in a variable is an actual array type
				AddCriterion(localResolveResult.Variable, new IsArrayTypeCriterion());
			} else if (resolveResult is ArrayAccessResolveResult) {
				var arrayResolveResult = (ArrayAccessResolveResult)resolveResult;
				var arrayType = arrayResolveResult.Array.Type as ArrayType;
				if (arrayType != null) {
					var parameterTypes = arrayResolveResult.Indexes.Select(index => index.Type);
					var criterion = new SupportsIndexingCriterion(arrayType.ElementType, parameterTypes, CSharpConversions.Get(context.Compilation));
					AddCriterion(localResolveResult.Variable, criterion);
				}
			} else if (resolveResult is CSharpInvocationResolveResult) {
				var invocationResolveResult = (CSharpInvocationResolveResult)resolveResult;
				var parameterTypes = invocationResolveResult.Arguments.Select(arg => arg.Type);
				var criterion = new SupportsIndexingCriterion(invocationResolveResult.Member.ReturnType, parameterTypes, CSharpConversions.Get(context.Compilation));
				AddCriterion(localResolveResult.Variable, criterion);
			}
		}
		
		public override void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			base.VisitInvocationExpression(invocationExpression);
			
			var resolveResult = context.Resolve(invocationExpression);
			var invocationResolveResult = resolveResult as InvocationResolveResult;
			if (invocationResolveResult == null)
				return;
			
			// invocationExpression.Target resolves to a method group and VisitMemberReferenceExpression
			// only handles members, so handle method groups here
			var targetResolveResult = invocationResolveResult.TargetResult as LocalResolveResult;
			if (targetResolveResult != null) {
				var variable = targetResolveResult.Variable;
				AddCriterion(variable, new HasMemberCriterion(invocationResolveResult.Member));
			}
		}

		Role[] roles = new [] {
			Roles.Expression,
			Roles.Argument,
			Roles.Condition,
			BinaryOperatorExpression.RightRole,
			BinaryOperatorExpression.LeftRole
		};

		public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			var resolveResult = context.Resolve(identifierExpression);
			var localResolveResult = resolveResult as LocalResolveResult;
			if (localResolveResult == null)
				return;
			
			var variable = localResolveResult.Variable;
			if (!UsedVariables.Contains(variable))
				UsedVariables.Add(variable);

			// Assignment expressions are checked separately, see VisitAssignmentExpression
			if (!roles.Contains(identifierExpression.Role) || identifierExpression.Parent is AssignmentExpression)
				return;

			CheckForCriterion(identifierExpression, variable);
		}
		
		public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			base.VisitAssignmentExpression(assignmentExpression);
			
			// Only check the right side; The left side always has the type of
			// the variable, which is not what we want to check

			var rightResolveResult = context.Resolve(assignmentExpression.Right) as LocalResolveResult;
			if (rightResolveResult != null) {
				CheckForCriterion(assignmentExpression.Right, rightResolveResult.Variable);
			}
		}

		void CheckForCriterion(Expression expression, IVariable variable)
		{
			if (ConstructHasLocalIndependentTyping(expression)) {
				AddCriterion(variable, new IsTypeCriterion(context.GetExpectedType(expression)));
			}
		}

		bool ConstructHasLocalIndependentTyping(AstNode astNode)
		{
			// TODO: Implement this thing correctly
			var parent = astNode.Parent;
			while (!(parent is InvocationExpression || parent is ObjectCreateExpression || parent is Statement))
				parent = parent.Parent;
			if (parent is InvocationExpression || parent is ObjectCreateExpression) {
				var resolveResult = context.Resolve(parent) as InvocationResolveResult;
				if (resolveResult == null)
					return true;
				var specializedMember = resolveResult.Member as SpecializedMethod;
				return specializedMember == null || specializedMember.TypeParameters.Count == 0;
			}
			var initializer = parent as VariableDeclarationStatement;
			if (initializer != null) {
				return initializer.Type.GetText() != "var";
			}

			return true;
		}

		class ConjunctionCriteria : ITypeCriterion
		{
			IList<ITypeCriterion> criteria;

			public ConjunctionCriteria(IList<ITypeCriterion> criteria)
			{
				this.criteria = criteria;
			}
			
			public bool SatisfiedBy(IType type)
			{
				foreach (var criterion in criteria) {
					if (!criterion.SatisfiedBy(type)) {
						return false;
					}
				}
				return true;
			}
		}
	}
}

