// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp {
	public class QueryExpressionExpansionResult {
		public AstNode AstNode { get; private set; }

		/// <summary>
		/// Maps original range variables to some node in the new tree that represents them.
		/// </summary>
		public IDictionary<Identifier, AstNode> RangeVariables { get; private set; }

		/// <summary>
		/// Maps clauses to method calls. The keys will always be either a <see cref="QueryClause"/> or a <see cref="QueryOrdering"/>
		/// </summary>
		public IDictionary<AstNode, Expression> Expressions { get; private set; }

		public QueryExpressionExpansionResult(AstNode astNode, IDictionary<Identifier, AstNode> rangeVariables, IDictionary<AstNode, Expression> expressions) {
			AstNode = astNode;
			RangeVariables = rangeVariables;
			Expressions = expressions;
		}
	}

	public class QueryExpressionExpander {
		class Visitor : DepthFirstAstVisitor<AstNode> {
			internal IEnumerator<string> TransparentIdentifierNamePicker;

			protected override AstNode VisitChildren(AstNode node) {
				List<AstNode> newChildren = null;
				
				int i = 0;
				foreach (var child in node.Children) {
					var newChild = child.AcceptVisitor(this);
					if (newChild != null) {
						newChildren = newChildren ?? Enumerable.Repeat((AstNode)null, i).ToList();
						newChildren.Add(newChild);
					}
					else if (newChildren != null) {
						newChildren.Add(null);
					}
					i++;
				}

				if (newChildren == null)
					return null;

				var result = node.Clone();

				i = 0;
				foreach (var children in result.Children) {
					if (newChildren[i] != null)
						children.ReplaceWith(newChildren[i]);
					i++;
				}

				return result;
			}

			Expression MakeNestedMemberAccess(Expression target, IEnumerable<string> members) {
				return members.Aggregate(target, (current, m) => current.Member(m));
			}

			Expression VisitNested(Expression node, ParameterDeclaration transparentParameter) {
				var oldRangeVariableSubstitutions = activeRangeVariableSubstitutions;
				try {
					if (transparentParameter != null && currentTransparentType.Count > 1) {
						activeRangeVariableSubstitutions = new Dictionary<string, Expression>(activeRangeVariableSubstitutions);
						foreach (var t in currentTransparentType)
							activeRangeVariableSubstitutions[t.Item1.Name] = MakeNestedMemberAccess(new IdentifierExpression(transparentParameter.Name), t.Item2);
					}
					var result = node.AcceptVisitor(this);
					return (Expression)(result ?? node.Clone());
				}
				finally {
					activeRangeVariableSubstitutions = oldRangeVariableSubstitutions;
				}
			}

			QueryClause GetNextQueryClause(QueryClause clause) {
				for (AstNode node = clause.NextSibling; node != null; node = node.NextSibling) {
					if (node.Role == QueryExpression.ClauseRole)
						return (QueryClause)node;
				}
				return null;
			}

			public IDictionary<Identifier, AstNode> rangeVariables = new Dictionary<Identifier, AstNode>();
			public IDictionary<AstNode, Expression> expressions = new Dictionary<AstNode, Expression>();

			Dictionary<string, Expression> activeRangeVariableSubstitutions = new Dictionary<string, Expression>();
			List<Tuple<Identifier, List<string>>> currentTransparentType = new List<Tuple<Identifier, List<string>>>();
			Expression currentResult;
			bool eatSelect;

			void MapExpression(AstNode orig, Expression newExpr) {
				Debug.Assert(orig is QueryClause || orig is QueryOrdering);
				expressions[orig] = newExpr;
			}

			internal static IEnumerable<string> FallbackTransparentIdentifierNamePicker()
			{
				const string TransparentParameterNameTemplate = "x{0}";
				int currentTransparentParameter = 0;
				for (;;) {
					yield return string.Format(CultureInfo.InvariantCulture, TransparentParameterNameTemplate, currentTransparentParameter++);
				}
			}

			ParameterDeclaration CreateParameterForCurrentRangeVariable() {
				var param = new ParameterDeclaration();

				if (currentTransparentType.Count == 1) {
					var clonedRangeVariable = (Identifier)currentTransparentType[0].Item1.Clone();
					if (!rangeVariables.ContainsKey(currentTransparentType[0].Item1))
						rangeVariables[currentTransparentType[0].Item1] = param;
					param.AddChild(clonedRangeVariable, Roles.Identifier);
				}
				else {
					if (!TransparentIdentifierNamePicker.MoveNext()) {
						TransparentIdentifierNamePicker = FallbackTransparentIdentifierNamePicker().GetEnumerator();
						TransparentIdentifierNamePicker.MoveNext();
					}
					string name = TransparentIdentifierNamePicker.Current;
					param.AddChild(Identifier.Create(name), Roles.Identifier);
				}
				return param;
			}

			LambdaExpression CreateLambda(IList<ParameterDeclaration> parameters, Expression body) {
				var result = new LambdaExpression();
				if (parameters.Count > 1)
					result.AddChild(new CSharpTokenNode(TextLocation.Empty, Roles.LPar), Roles.LPar);
				result.AddChild(parameters[0], Roles.Parameter);
				for (int i = 1; i < parameters.Count; i++) {
					result.AddChild(new CSharpTokenNode(TextLocation.Empty, Roles.Comma), Roles.Comma);
					result.AddChild(parameters[i], Roles.Parameter);
				}
				if (parameters.Count > 1)
					result.AddChild(new CSharpTokenNode(TextLocation.Empty, Roles.RPar), Roles.RPar);
				result.AddChild(body, LambdaExpression.BodyRole);

				return result;
			}

			ParameterDeclaration CreateParameter(Identifier identifier) {
				var result = new ParameterDeclaration();
				result.AddChild(identifier, Roles.Identifier);
				return result;
			}

			Expression AddMemberToCurrentTransparentType(ParameterDeclaration param, Identifier name, Expression value, bool namedExpression) {
				Expression newAssignment = VisitNested(value, param);
				if (namedExpression) {
					newAssignment = new NamedExpression(name.Name, VisitNested(value, param));
					if (!rangeVariables.ContainsKey(name) )
						rangeVariables[name] = ((NamedExpression)newAssignment).NameToken;
				}

				foreach (var t in currentTransparentType)
					t.Item2.Insert(0, param.Name);

				currentTransparentType.Add(Tuple.Create(name, new List<string> { name.Name }));
				return new AnonymousTypeCreateExpression(new[] { new IdentifierExpression(param.Name), newAssignment });
			}

			void AddFirstMemberToCurrentTransparentType(Identifier identifier) {
				Debug.Assert(currentTransparentType.Count == 0);
				currentTransparentType.Add(Tuple.Create(identifier, new List<string>()));
			}

			public override AstNode VisitQueryExpression(QueryExpression queryExpression) {
				var oldTransparentType = currentTransparentType;
				var oldResult = currentResult;
				var oldEatSelect = eatSelect;
				try {
					currentTransparentType = new List<Tuple<Identifier, List<string>>>();
					currentResult = null;
					eatSelect = false;

					foreach (var clause in queryExpression.Clauses) {
						var result = (Expression)clause.AcceptVisitor(this);
						MapExpression(clause, result ?? currentResult);
						currentResult = result;
					}

					return currentResult; 
				}
				finally {
					currentTransparentType = oldTransparentType;
					currentResult = oldResult;
					eatSelect = oldEatSelect;
				}
			}

			public override AstNode VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause) {
				var prev = VisitNested(queryContinuationClause.PrecedingQuery, null);
				AddFirstMemberToCurrentTransparentType(queryContinuationClause.IdentifierToken);
				return prev;
			}

			static bool NeedsToBeParenthesized(Expression expr)
			{
				UnaryOperatorExpression unary = expr as UnaryOperatorExpression;
				if (unary != null) {
					if (unary.Operator == UnaryOperatorType.PostIncrement || unary.Operator == UnaryOperatorType.PostDecrement) {
						return false;
					}
					return true;
				}
			
				if (expr is BinaryOperatorExpression || expr is ConditionalExpression || expr is AssignmentExpression) {
					return true;
				}

				return false;
			}

			static Expression ParenthesizeIfNeeded(Expression expr)
			{
				return NeedsToBeParenthesized(expr) ? new ParenthesizedExpression(expr.Clone()) : expr;
			}

			public override AstNode VisitQueryFromClause(QueryFromClause queryFromClause) {
				if (currentResult == null) {
					AddFirstMemberToCurrentTransparentType(queryFromClause.IdentifierToken);
					if (queryFromClause.Type.IsNull) {
						return VisitNested(ParenthesizeIfNeeded(queryFromClause.Expression), null);
					}
					else {
						return VisitNested(ParenthesizeIfNeeded(queryFromClause.Expression), null).Invoke("Cast", new[] { queryFromClause.Type.Clone() }, new Expression[0]);
					}
				}
				else {
					var innerSelectorParam = CreateParameterForCurrentRangeVariable();
					var lambdaContent = VisitNested(queryFromClause.Expression, innerSelectorParam);
					if (!queryFromClause.Type.IsNull) {
						lambdaContent = lambdaContent.Invoke("Cast", new[] { queryFromClause.Type.Clone() }, new Expression[0]);
					}
					var innerSelector = CreateLambda(new[] { innerSelectorParam }, lambdaContent);

					var clonedIdentifier = (Identifier)queryFromClause.IdentifierToken.Clone();

					var resultParam = CreateParameterForCurrentRangeVariable();
					Expression body;
					// Second from clause - SelectMany
					var select = GetNextQueryClause(queryFromClause) as QuerySelectClause;
					if (select != null) {
						body = VisitNested(select.Expression, resultParam);
						eatSelect = true;
					}
					else {
						body = AddMemberToCurrentTransparentType(resultParam, queryFromClause.IdentifierToken, new IdentifierExpression(queryFromClause.Identifier), false);
					}

					var resultSelectorParam2 = CreateParameter(clonedIdentifier);
					var resultSelector = CreateLambda(new[] { resultParam, resultSelectorParam2 }, body);
					rangeVariables[queryFromClause.IdentifierToken] = resultSelectorParam2;

					return currentResult.Invoke("SelectMany", innerSelector, resultSelector);
				}
			}

			public override AstNode VisitQueryLetClause(QueryLetClause queryLetClause) {
				var param = CreateParameterForCurrentRangeVariable();
				var body = AddMemberToCurrentTransparentType(param, queryLetClause.IdentifierToken, queryLetClause.Expression, true);
				var lambda = CreateLambda(new[] { param }, body);

				return currentResult.Invoke("Select", lambda);
			}

			public override AstNode VisitQueryWhereClause(QueryWhereClause queryWhereClause) {
				var param = CreateParameterForCurrentRangeVariable();
				return currentResult.Invoke("Where", CreateLambda(new[] { param }, VisitNested(queryWhereClause.Condition, param)));
			}

			public override AstNode VisitQueryJoinClause(QueryJoinClause queryJoinClause) {
				Expression resultSelectorBody = null;
				var inExpression = VisitNested(queryJoinClause.InExpression, null);
				if (!queryJoinClause.Type.IsNull) {
					inExpression = inExpression.Invoke("Cast", new[] { queryJoinClause.Type.Clone() }, EmptyList<Expression>.Instance);
				}
				var key1SelectorFirstParam = CreateParameterForCurrentRangeVariable();
				var key1Selector = CreateLambda(new[] { key1SelectorFirstParam }, VisitNested(queryJoinClause.OnExpression, key1SelectorFirstParam));
				var key2Param = CreateParameter(Identifier.Create(queryJoinClause.JoinIdentifier));
				var key2Selector = CreateLambda(new[] { key2Param }, VisitNested(queryJoinClause.EqualsExpression, null));

				var resultSelectorFirstParam = CreateParameterForCurrentRangeVariable();

				var select = GetNextQueryClause(queryJoinClause) as QuerySelectClause;
				if (select != null) {
					resultSelectorBody = VisitNested(select.Expression, resultSelectorFirstParam);
					eatSelect = true;
				}

				if (queryJoinClause.IntoKeyword.IsNull) {
					// Normal join
					if (resultSelectorBody == null)
						resultSelectorBody = AddMemberToCurrentTransparentType(resultSelectorFirstParam, queryJoinClause.JoinIdentifierToken, new IdentifierExpression(queryJoinClause.JoinIdentifier), false);

					var resultSelector = CreateLambda(new[] { resultSelectorFirstParam, CreateParameter(Identifier.Create(queryJoinClause.JoinIdentifier)) }, resultSelectorBody);
					rangeVariables[queryJoinClause.JoinIdentifierToken] = key2Param;
					return currentResult.Invoke("Join", inExpression, key1Selector, key2Selector, resultSelector);
				}
				else {
					// Group join
					if (resultSelectorBody == null)
						resultSelectorBody = AddMemberToCurrentTransparentType(resultSelectorFirstParam, queryJoinClause.IntoIdentifierToken, new IdentifierExpression(queryJoinClause.IntoIdentifier), false);

					var intoParam = CreateParameter(Identifier.Create(queryJoinClause.IntoIdentifier));
					var resultSelector = CreateLambda(new[] { resultSelectorFirstParam, intoParam }, resultSelectorBody);
					rangeVariables[queryJoinClause.IntoIdentifierToken] = intoParam;

					return currentResult.Invoke("GroupJoin", inExpression, key1Selector, key2Selector, resultSelector);
				}
			}

			public override AstNode VisitQueryOrderClause(QueryOrderClause queryOrderClause) {
				var current = currentResult;
				bool first = true;
				foreach (var o in queryOrderClause.Orderings) {
					string methodName = first ? (o.Direction == QueryOrderingDirection.Descending ? "OrderByDescending" : "OrderBy")
					                          : (o.Direction == QueryOrderingDirection.Descending ? "ThenByDescending" : "ThenBy");

					var param = CreateParameterForCurrentRangeVariable();
					current = current.Invoke(methodName, CreateLambda(new[] { param }, VisitNested(o.Expression, param)));
					MapExpression(o, current);
					first = false;
				}
				return current;
			}

			bool IsSingleRangeVariable(Expression expr) {
				if (currentTransparentType.Count > 1)
					return false;
				var unpacked = ParenthesizedExpression.UnpackParenthesizedExpression(expr);
				return unpacked is IdentifierExpression && ((IdentifierExpression)unpacked).Identifier == currentTransparentType[0].Item1.Name;
			}

			public override AstNode VisitQuerySelectClause(QuerySelectClause querySelectClause) {
				if (eatSelect) {
					eatSelect = false;
					return currentResult;
				}
				else if (((QueryExpression)querySelectClause.Parent).Clauses.Count > 2 && IsSingleRangeVariable(querySelectClause.Expression)) {
					// A simple query that ends with a trivial select should be removed.
					return currentResult;
				}

				var param = CreateParameterForCurrentRangeVariable();
				var lambda = CreateLambda(new[] { param }, VisitNested(querySelectClause.Expression, param));
				return currentResult.Invoke("Select", lambda);
			}

			public override AstNode VisitQueryGroupClause(QueryGroupClause queryGroupClause) {
				var param = CreateParameterForCurrentRangeVariable();
				var keyLambda = CreateLambda(new[] { param }, VisitNested(queryGroupClause.Key, param));

				if (IsSingleRangeVariable(queryGroupClause.Projection)) {
					// We are grouping by the single active range variable, so we can use the single argument form of GroupBy
					return currentResult.Invoke("GroupBy", keyLambda);
				}
				else {
					var projectionParam = CreateParameterForCurrentRangeVariable();
					var projectionLambda = CreateLambda(new[] { projectionParam }, VisitNested(queryGroupClause.Projection, projectionParam));
					return currentResult.Invoke("GroupBy", keyLambda, projectionLambda);
				}
			}

			public override AstNode VisitIdentifierExpression(IdentifierExpression identifierExpression) {
				Expression subst;
				activeRangeVariableSubstitutions.TryGetValue(identifierExpression.Identifier, out subst);
				return subst != null ? subst.Clone() : null;
			}
		}

		/// <summary>
		/// Expands all occurances of query patterns in the specified node. Returns a clone of the node with all query patterns expanded, or null if there was no query pattern to expand.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="transparentIdentifierNamePicker">A sequence of names to use for transparent identifiers. Once the sequence is over, a fallback name generator is used</param> 
		/// <returns></returns>
		public QueryExpressionExpansionResult ExpandQueryExpressions(AstNode node, IEnumerable<string> transparentIdentifierNamePicker) {
			var visitor = new Visitor();
			visitor.TransparentIdentifierNamePicker = transparentIdentifierNamePicker.GetEnumerator();
			var astNode = node.AcceptVisitor(visitor);
			if (astNode != null) {
				astNode.Freeze();
				return new QueryExpressionExpansionResult(astNode, visitor.rangeVariables, visitor.expressions);
			}
			else {
				return null;
			}
		}

		
		/// <summary>
		/// Expands all occurances of query patterns in the specified node. Returns a clone of the node with all query patterns expanded, or null if there was no query pattern to expand.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public QueryExpressionExpansionResult ExpandQueryExpressions(AstNode node)
		{
			return ExpandQueryExpressions(node, Enumerable.Empty<string>());
		}
	}
}
