// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Analysis;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Moves variable declarations to improved positions.
	/// </summary>
	public class DeclareVariables : IAstTransform
	{
		sealed class VariableToDeclare
		{
			public AstType Type;
			public string Name;
			public ILVariable ILVariable;
			
			public AssignmentExpression ReplacedAssignment;
			public Statement InsertionPoint;
		}
		
		readonly CancellationToken cancellationToken;
		List<VariableToDeclare> variablesToDeclare = new List<VariableToDeclare>();
		
		public DeclareVariables(DecompilerContext context)
		{
			this.cancellationToken = context.CancellationToken;
		}
		
		public void Run(AstNode node)
		{
			Run(node, null);
			// Declare all the variables at the end, after all the logic has run.
			// This is done so that definite assignment analysis can work on a single representation and doesn't have to be updated
			// when we change the AST.
			foreach (var v in variablesToDeclare) {
				if (v.ReplacedAssignment == null) {
					BlockStatement block = (BlockStatement)v.InsertionPoint.Parent;
					var decl = new VariableDeclarationStatement((AstType)v.Type.Clone(), v.Name);
					if (v.ILVariable != null)
						decl.Variables.Single().AddAnnotation(v.ILVariable);
					block.Statements.InsertBefore(
						v.InsertionPoint,
						decl);
				}
			}
			// First do all the insertions, then do all the replacements. This is necessary because a replacement might remove our reference point from the AST.
			foreach (var v in variablesToDeclare) {
				if (v.ReplacedAssignment != null) {
					// We clone the right expression so that it doesn't get removed from the old ExpressionStatement,
					// which might be still in use by the definite assignment graph.
					VariableInitializer initializer = new VariableInitializer(v.Name, v.ReplacedAssignment.Right.Detach()).CopyAnnotationsFrom(v.ReplacedAssignment).WithAnnotation(v.ILVariable);
					VariableDeclarationStatement varDecl = new VariableDeclarationStatement {
						Type = (AstType)v.Type.Clone(),
						Variables = { initializer }
					};
					ExpressionStatement es = v.ReplacedAssignment.Parent as ExpressionStatement;
					if (es != null) {
						// Note: if this crashes with 'Cannot replace the root node', check whether two variables were assigned the same name
						es.ReplaceWith(varDecl.CopyAnnotationsFrom(es));
					} else {
						v.ReplacedAssignment.ReplaceWith(varDecl);
					}
				}
			}
			variablesToDeclare = null;
		}
		
		void Run(AstNode node, DefiniteAssignmentAnalysis daa)
		{
			BlockStatement block = node as BlockStatement;
			if (block != null) {
				var variables = block.Statements.TakeWhile(stmt => stmt is VariableDeclarationStatement)
					.Cast<VariableDeclarationStatement>().ToList();
				if (variables.Count > 0) {
					// remove old variable declarations:
					foreach (VariableDeclarationStatement varDecl in variables) {
						Debug.Assert(varDecl.Variables.Single().Initializer.IsNull);
						varDecl.Remove();
					}
					if (daa == null) {
						// If possible, reuse the DefiniteAssignmentAnalysis that was created for the parent block
						daa = new DefiniteAssignmentAnalysis(block, cancellationToken);
					}
					foreach (VariableDeclarationStatement varDecl in variables) {
						VariableInitializer initializer = varDecl.Variables.Single();
						string variableName = initializer.Name;
						ILVariable v = initializer.Annotation<ILVariable>();
						bool allowPassIntoLoops = initializer.Annotation<DelegateConstruction.CapturedVariableAnnotation>() == null;
						DeclareVariableInBlock(daa, block, varDecl.Type, variableName, v, allowPassIntoLoops);
					}
				}
			}
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				Run(child, daa);
			}
		}
		
		void DeclareVariableInBlock(DefiniteAssignmentAnalysis daa, BlockStatement block, AstType type, string variableName, ILVariable v, bool allowPassIntoLoops)
		{
			// declarationPoint: The point where the variable would be declared, if we decide to declare it in this block
			Statement declarationPoint = null;
			// Check whether we can move down the variable into the sub-blocks
			bool canMoveVariableIntoSubBlocks = FindDeclarationPoint(daa, variableName, allowPassIntoLoops, block, out declarationPoint);
			if (declarationPoint == null) {
				// The variable isn't used at all
				return;
			}
			if (canMoveVariableIntoSubBlocks) {
				// Declare the variable within the sub-blocks
				foreach (Statement stmt in block.Statements) {
					ForStatement forStmt = stmt as ForStatement;
					if (forStmt != null && forStmt.Initializers.Count == 1) {
						// handle the special case of moving a variable into the for initializer
						if (TryConvertAssignmentExpressionIntoVariableDeclaration(forStmt.Initializers.Single(), type, variableName))
							continue;
					}
					UsingStatement usingStmt = stmt as UsingStatement;
					if (usingStmt != null && usingStmt.ResourceAcquisition is AssignmentExpression) {
						// handle the special case of moving a variable into a using statement
						if (TryConvertAssignmentExpressionIntoVariableDeclaration((Expression)usingStmt.ResourceAcquisition, type, variableName))
							continue;
					}
					IfElseStatement ies = stmt as IfElseStatement;
					if (ies != null) {
						foreach (var child in IfElseChainChildren(ies)) {
							BlockStatement subBlock = child as BlockStatement;
							if (subBlock != null)
								DeclareVariableInBlock(daa, subBlock, type, variableName, v, allowPassIntoLoops);
						}
						continue;
					}
					foreach (AstNode child in stmt.Children) {
						BlockStatement subBlock = child as BlockStatement;
						if (subBlock != null) {
							DeclareVariableInBlock(daa, subBlock, type, variableName, v, allowPassIntoLoops);
						} else if (HasNestedBlocks(child)) {
							foreach (BlockStatement nestedSubBlock in child.Children.OfType<BlockStatement>()) {
								DeclareVariableInBlock(daa, nestedSubBlock, type, variableName, v, allowPassIntoLoops);
							}
						}
					}
				}
			} else {
				// Try converting an assignment expression into a VariableDeclarationStatement
				if (!TryConvertAssignmentExpressionIntoVariableDeclaration(declarationPoint, type, variableName)) {
					// Declare the variable in front of declarationPoint
					variablesToDeclare.Add(new VariableToDeclare { Type = type, Name = variableName, ILVariable = v, InsertionPoint = declarationPoint });
				}
			}
		}

		bool TryConvertAssignmentExpressionIntoVariableDeclaration(Statement declarationPoint, AstType type, string variableName)
		{
			// convert the declarationPoint into a VariableDeclarationStatement
			ExpressionStatement es = declarationPoint as ExpressionStatement;
			if (es != null) {
				return TryConvertAssignmentExpressionIntoVariableDeclaration(es.Expression, type, variableName);
			}
			return false;
		}
		
		bool TryConvertAssignmentExpressionIntoVariableDeclaration(Expression expression, AstType type, string variableName)
		{
			AssignmentExpression ae = expression as AssignmentExpression;
			if (ae != null && ae.Operator == AssignmentOperatorType.Assign) {
				IdentifierExpression ident = ae.Left as IdentifierExpression;
				if (ident != null && ident.Identifier == variableName) {
					variablesToDeclare.Add(new VariableToDeclare { Type = type, Name = variableName, ILVariable = ident.Annotation<ILVariable>(), ReplacedAssignment = ae });
					return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Finds the declaration point for the variable within the specified block.
		/// </summary>
		/// <param name="daa">
		/// Definite assignment analysis, must be prepared for 'block' or one of its parents.
		/// </param>
		/// <param name="varDecl">The variable to declare</param>
		/// <param name="block">The block in which the variable should be declared</param>
		/// <param name="declarationPoint">
		/// Output parameter: the first statement within 'block' where the variable needs to be declared.
		/// </param>
		/// <returns>
		/// Returns whether it is possible to move the variable declaration into sub-blocks.
		/// </returns>
		public static bool FindDeclarationPoint(DefiniteAssignmentAnalysis daa, VariableDeclarationStatement varDecl, BlockStatement block, out Statement declarationPoint)
		{
			string variableName = varDecl.Variables.Single().Name;
			bool allowPassIntoLoops = varDecl.Variables.Single().Annotation<DelegateConstruction.CapturedVariableAnnotation>() == null;
			return FindDeclarationPoint(daa, variableName, allowPassIntoLoops, block, out declarationPoint);
		}
		
		static bool FindDeclarationPoint(DefiniteAssignmentAnalysis daa, string variableName, bool allowPassIntoLoops, BlockStatement block, out Statement declarationPoint)
		{
			// declarationPoint: The point where the variable would be declared, if we decide to declare it in this block
			declarationPoint = null;
			foreach (Statement stmt in block.Statements) {
				if (UsesVariable(stmt, variableName)) {
					if (declarationPoint == null)
						declarationPoint = stmt;
					if (!CanMoveVariableUseIntoSubBlock(stmt, variableName, allowPassIntoLoops)) {
						// If it's not possible to move the variable use into a nested block,
						// we need to declare the variable in this block
						return false;
					}
					// If we can move the variable into the sub-block, we need to ensure that the remaining code
					// does not use the value that was assigned by the first sub-block
					Statement nextStatement = stmt.GetNextStatement();
					if (nextStatement != null) {
						// Analyze the range from the next statement to the end of the block
						daa.SetAnalyzedRange(nextStatement, block);
						daa.Analyze(variableName);
						if (daa.UnassignedVariableUses.Count > 0) {
							return false;
						}
					}
				}
			}
			return true;
		}
		
		static bool CanMoveVariableUseIntoSubBlock(Statement stmt, string variableName, bool allowPassIntoLoops)
		{
			if (!allowPassIntoLoops && (stmt is ForStatement || stmt is ForeachStatement || stmt is DoWhileStatement || stmt is WhileStatement))
				return false;
			
			ForStatement forStatement = stmt as ForStatement;
			if (forStatement != null && forStatement.Initializers.Count == 1) {
				// for-statement is special case: we can move variable declarations into the initializer
				ExpressionStatement es = forStatement.Initializers.Single() as ExpressionStatement;
				if (es != null) {
					AssignmentExpression ae = es.Expression as AssignmentExpression;
					if (ae != null && ae.Operator == AssignmentOperatorType.Assign) {
						IdentifierExpression ident = ae.Left as IdentifierExpression;
						if (ident != null && ident.Identifier == variableName) {
							return !UsesVariable(ae.Right, variableName);
						}
					}
				}
			}
			
			UsingStatement usingStatement = stmt as UsingStatement;
			if (usingStatement != null) {
				// using-statement is special case: we can move variable declarations into the initializer
				AssignmentExpression ae = usingStatement.ResourceAcquisition as AssignmentExpression;
				if (ae != null && ae.Operator == AssignmentOperatorType.Assign) {
					IdentifierExpression ident = ae.Left as IdentifierExpression;
					if (ident != null && ident.Identifier == variableName) {
						return !UsesVariable(ae.Right, variableName);
					}
				}
			}

			IfElseStatement ies = stmt as IfElseStatement;
			if (ies != null) {
				foreach (var child in IfElseChainChildren(ies)) {
					if (!(child is BlockStatement) && UsesVariable(child, variableName))
						return false;
				}
				return true;
			}
			
			// We can move the variable into a sub-block only if the variable is used in only that sub-block (and not in expressions such as the loop condition)
			for (AstNode child = stmt.FirstChild; child != null; child = child.NextSibling) {
				if (!(child is BlockStatement) && UsesVariable(child, variableName)) {
					if (HasNestedBlocks(child)) {
						// catch clauses/switch sections can contain nested blocks
						for (AstNode grandchild = child.FirstChild; grandchild != null; grandchild = grandchild.NextSibling) {
							if (!(grandchild is BlockStatement) && UsesVariable(grandchild, variableName))
								return false;
						}
					} else {
						return false;
					}
				}
			}
			return true;
		}

		static IEnumerable<AstNode> IfElseChainChildren(IfElseStatement ies)
		{
			IfElseStatement prev;
			do {
				yield return ies.Condition;
				yield return ies.TrueStatement;
				prev = ies;
				ies = ies.FalseStatement as IfElseStatement;
			} while (ies != null);
			if (!prev.FalseStatement.IsNull)
				yield return prev.FalseStatement;
		}
		
		static bool HasNestedBlocks(AstNode node)
		{
			return node is CatchClause || node is SwitchSection;
		}
		
		static bool UsesVariable(AstNode node, string variableName)
		{
			IdentifierExpression ie = node as IdentifierExpression;
			if (ie != null && ie.Identifier == variableName)
				return true;
			
			FixedStatement fixedStatement = node as FixedStatement;
			if (fixedStatement != null) {
				foreach (VariableInitializer v in fixedStatement.Variables) {
					if (v.Name == variableName)
						return false; // no need to introduce the variable here
				}
			}
			
			ForeachStatement foreachStatement = node as ForeachStatement;
			if (foreachStatement != null) {
				if (foreachStatement.VariableName == variableName)
					return false; // no need to introduce the variable here
			}
			
			UsingStatement usingStatement = node as UsingStatement;
			if (usingStatement != null) {
				VariableDeclarationStatement varDecl = usingStatement.ResourceAcquisition as VariableDeclarationStatement;
				if (varDecl != null) {
					foreach (VariableInitializer v in varDecl.Variables) {
						if (v.Name == variableName)
							return false; // no need to introduce the variable here
					}
				}
			}
			
			CatchClause catchClause = node as CatchClause;
			if (catchClause != null && catchClause.VariableName == variableName) {
				return false; // no need to introduce the variable here
			}
			
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				if (UsesVariable(child, variableName))
					return true;
			}
			return false;
		}
	}
}
