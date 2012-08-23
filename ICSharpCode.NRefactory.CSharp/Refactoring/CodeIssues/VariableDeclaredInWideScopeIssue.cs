//
// VariableDeclaredWideScopeIssue.cs
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
using System.Collections.Generic;
using System.Linq;
using System;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("The variable can be declared in a nested scope",
	                   Description = "Highlights variables that can be declared in a nested scope.",
	                   Category = IssueCategories.Opportunities,
	                   Severity = Severity.Suggestion)]
	public class VariableDeclaredInWideScopeIssue : ICodeIssueProvider
	{
		#region ICodeIssueProvider implementation
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}
		#endregion

		class GatherVisitor : GatherVisitorBase
		{
			readonly BaseRefactoringContext context;
			
			public GatherVisitor(BaseRefactoringContext context, VariableDeclaredInWideScopeIssue inspector) : base (context)
			{
				this.context = context;
			}

			static IList<Type> moveTargetBlacklist = new List<Type>() {
				typeof(WhileStatement),
				typeof(ForeachStatement),
				typeof(ForStatement),
				typeof(DoWhileStatement),
				typeof(TryCatchStatement),
				typeof(AnonymousMethodExpression),
				typeof(LambdaExpression)
			};
		
			public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
			{
				base.VisitVariableDeclarationStatement(variableDeclarationStatement);

				if (!(variableDeclarationStatement.Parent is BlockStatement))
					// We are somewhere weird, like a the ResourceAquisition of a using statement
					return;

				if (variableDeclarationStatement.Variables.Count > 1)
					return;

				// Start at the parent node. Presumably this is a BlockStatement
				var rootNode = variableDeclarationStatement.Parent;
				var variableInitializer = variableDeclarationStatement.Variables.First();
				var identifiers = GetIdentifiers(rootNode.Descendants, variableInitializer.Name).ToList();

				if (identifiers.Count == 0)
					// variable is not used
					return;

				AstNode deepestCommonAncestor = GetDeepestCommonAncestor(rootNode, identifiers);
				var path = GetPath(rootNode, deepestCommonAncestor);

				// Restrict path to only those where the initializer has not changed
				var firstInitializerChange = GetFirstInitializerChange(variableDeclarationStatement, path, variableInitializer.Initializer);
				if (firstInitializerChange != null) {
					path = GetPath(rootNode, firstInitializerChange);
				}

				// Restict to locations outside of blacklisted node types
				var firstBlackListedNode = (from node in path
				                            where moveTargetBlacklist.Contains(node.GetType())
				                            select node).FirstOrDefault();
				if (firstBlackListedNode != null) {
					path = GetPath(rootNode, firstBlackListedNode);
				}

				// Get the most nested possible target for the move
				Statement mostNestedFollowingStatement = null;
				for (int i = path.Count - 1; i >= 0; i--) {
					var statement = path[i] as Statement;
					if (statement != null && (IsScopeContainer(statement.Parent) || IsScopeContainer(statement))) {
						mostNestedFollowingStatement = statement;
						break;
					}
				}

				if (mostNestedFollowingStatement != null && mostNestedFollowingStatement != rootNode && mostNestedFollowingStatement.Parent != rootNode) {
					AddIssue(variableDeclarationStatement, context.TranslateString("Variable could be moved to a nested scope"),
					         GetActions(variableDeclarationStatement, mostNestedFollowingStatement));
				}
			}

			static IEnumerable<IdentifierExpression> GetIdentifiers(IEnumerable<AstNode> candidates, string name = null)
			{
				return 
					from node in candidates
					let identifier = node as IdentifierExpression
					where identifier != null && (name == null || identifier.Identifier == name)
					select identifier;
			}

			AstNode GetFirstInitializerChange(AstNode variableDeclarationStatement, IList<AstNode> path, Expression initializer)
			{
				var identifiers = GetIdentifiers(initializer.DescendantsAndSelf).ToList();
				var mayChangeInitializer = GetChecker (initializer, identifiers);
				AstNode lastChange = null;
				for (int i = path.Count - 1; i >= 0; i--) {
					for (AstNode node = path[i].PrevSibling; node != null && node != variableDeclarationStatement; node = node.PrevSibling) {
						// Special case for IfElseStatement: The AST nesting does not match the scope nesting, so
						// don't handle branches here: The correct one has already been checked anyway.
						// This also works to our advantage: No special checking is needed for the condition since
						// it is a the same level in the tree as the false branch
						if (node.Role == IfElseStatement.TrueRole || node.Role == IfElseStatement.FalseRole)
							continue;
						foreach (var expression in node.DescendantsAndSelf.Where(n => n is Expression).Cast<Expression>()) {
							if (mayChangeInitializer(expression)) {
								lastChange = expression;
							}
						}
					}
				}
				return lastChange;
			}

			Func<Expression, bool> GetChecker(Expression expression, IList<IdentifierExpression> identifiers)
			{
				// TODO: This only works for simple cases.
				IList<IMember> members;
				IList<IVariable> locals;
				var identifierResolveResults = identifiers.Select(identifier => context.Resolve(identifier)).ToList();
				SplitResolveResults(identifierResolveResults, out members, out locals);
				
				if (expression is InvocationExpression || expression is ObjectCreateExpression) {
					return node => {
						if (node is InvocationExpression || expression is ObjectCreateExpression)
							// We don't know what these might do, so assume it will change the initializer
							return true;
						var binaryOperator = node as BinaryOperatorExpression;
						if (binaryOperator != null) {
							var resolveResult = context.Resolve(binaryOperator) as OperatorResolveResult;
							// Built-in operators are ok, user defined ones not so much
							return resolveResult.UserDefinedOperatorMethod != null;
						}
						return IsConflictingAssignment(node, identifiers, members, locals);
					};
				} else if (expression is IdentifierExpression) {
					var initializerDependsOnMembers = identifierResolveResults.Any(result => result is MemberResolveResult);
					var initializerDependsOnReferenceType = identifierResolveResults.Any(result => result.Type.IsReferenceType == true);
					return node => {
						if ((node is InvocationExpression || node is ObjectCreateExpression) &&
						    (initializerDependsOnMembers || initializerDependsOnReferenceType))
							// Anything can happen...
							return true;
						var binaryOperator = node as BinaryOperatorExpression;
						if (binaryOperator != null) {
							var resolveResult = context.Resolve(binaryOperator) as OperatorResolveResult;
							return resolveResult.UserDefinedOperatorMethod != null;
						}
						return IsConflictingAssignment(node, identifiers, members, locals);
					};
				}

				return node => false;
			}

			bool IsConflictingAssignment (Expression node, IList<IdentifierExpression> identifiers, IList<IMember> members, IList<IVariable> locals)
			{
				var assignmentExpression = node as AssignmentExpression;
				if (assignmentExpression != null) {
					IList<IMember> targetMembers;
					IList<IVariable> targetLocals;
					var identifierResolveResults = identifiers.Select(identifier => context.Resolve(identifier)).ToList();
					SplitResolveResults(identifierResolveResults, out targetMembers, out targetLocals);

					return members.Any(member => targetMembers.Contains(member)) ||
						locals.Any(local => targetLocals.Contains(local));
				}
				return false;
			}

			static void SplitResolveResults(List<ResolveResult> identifierResolveResults, out IList<IMember> members, out IList<IVariable> locals)
			{
				members = new List<IMember>();
				locals = new List<IVariable>();
				foreach (var resolveResult in identifierResolveResults) {
					var memberResolveResult = resolveResult as MemberResolveResult;
					if (memberResolveResult != null) {
						members.Add(memberResolveResult.Member);
					}
					var localResolveResult = resolveResult as LocalResolveResult;
					if (localResolveResult != null) {
						locals.Add(localResolveResult.Variable);
					}
				}
			}

			bool IsScopeContainer(AstNode node)
			{
				if (node == null)
					return false;

				var blockStatement = node as BlockStatement;
				if (blockStatement != null)
					return true;

				var statement = node as Statement;
				if (statement == null)
					return false;

				var role = node.Role;
				if (role == Roles.EmbeddedStatement ||
					role == IfElseStatement.TrueRole ||
					role == IfElseStatement.FalseRole) {
					return true;
				}
				return false;
			}

			IEnumerable<CodeAction> GetActions(Statement oldStatement, Statement followingStatement)
			{
				yield return new CodeAction(context.TranslateString("Move to nested scope"), script => {
					if (!(followingStatement.Parent is BlockStatement)) {
						var newBlockStatement = new BlockStatement {
							Statements = {
								oldStatement.Clone(),
								followingStatement.Clone()
							}
						};
						script.Replace(followingStatement, newBlockStatement);
						script.FormatText(followingStatement.Parent);
					} else {
						script.InsertBefore(followingStatement, oldStatement.Clone());
					}
					script.Remove(oldStatement);
				});
			}

			AstNode GetDeepestCommonAncestor(AstNode assumedRoot, IEnumerable<AstNode> leaves)
			{
				var previousPath = GetPath(assumedRoot, leaves.First());
				int lowestIndex = previousPath.Count - 1;
				foreach (var leaf in leaves.Skip(1)) {
					var currentPath = GetPath(assumedRoot, leaf);
					lowestIndex = GetLowestCommonAncestorIndex(previousPath, currentPath, lowestIndex);
					previousPath = currentPath;
				}
				return previousPath [lowestIndex];
			}
			
			int GetLowestCommonAncestorIndex(IList<AstNode> path1, IList<AstNode> path2, int maxIndex)
			{
				var max = Math.Min(Math.Min(path1.Count, path2.Count), maxIndex);
				for (int i = 0; i <= max; i++) {
					if (path1 [i] != path2 [i])
						return i - 1;
				}
				return max;
			}

			IList<AstNode> GetPath(AstNode from, AstNode to)
			{
				var reversePath = new List<AstNode>();
				do {
					reversePath.Add(to);
					to = to.Parent;
				} while (to != from.Parent);
				reversePath.Reverse();
				return reversePath;
			}
		}
	}
}

