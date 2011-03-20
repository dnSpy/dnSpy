// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Analysis;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Moves variable declarations to improved positions.
	/// </summary>
	public class DeclareVariables : IAstTransform
	{
		sealed class DeclaredVariableAnnotation {
			public readonly ExpressionStatement OriginalAssignmentStatement;
			
			public DeclaredVariableAnnotation(ExpressionStatement originalAssignmentStatement)
			{
				this.OriginalAssignmentStatement = originalAssignmentStatement;
			}
		}
		static readonly DeclaredVariableAnnotation declaredVariableAnnotation = new DeclaredVariableAnnotation(null);
		
		readonly CancellationToken cancellationToken;
		
		public DeclareVariables(DecompilerContext context)
		{
			this.cancellationToken = context.CancellationToken;
		}
		
		public void Run(AstNode node)
		{
			Run(node, null);
		}
		
		void Run(AstNode node, DefiniteAssignmentAnalysis daa)
		{
			BlockStatement block = node as BlockStatement;
			if (block != null) {
				var variables = block.Statements.TakeWhile(stmt => stmt is VariableDeclarationStatement
				                                           && stmt.Annotation<DeclaredVariableAnnotation>() == null)
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
						string variableName = varDecl.Variables.Single().Name;
						bool allowPassIntoLoops = varDecl.Variables.Single().Annotation<DelegateConstruction.CapturedVariableAnnotation>() == null;
						DeclareVariableInBlock(daa, block, varDecl.Type, variableName, allowPassIntoLoops);
					}
				}
			}
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				Run(child, daa);
			}
		}
		
		void DeclareVariableInBlock(DefiniteAssignmentAnalysis daa, BlockStatement block, AstType type, string variableName, bool allowPassIntoLoops)
		{
			// declarationPoint: The point where the variable would be declared, if we decide to declare it in this block
			Statement declarationPoint = null;
			// Check whether we can move down the variable into the sub-blocks
			bool ok = true;
			foreach (Statement stmt in block.Statements) {
				if (UsesVariable(stmt, variableName)) {
					if (declarationPoint == null)
						declarationPoint = stmt;
					if (!CanMoveVariableUseIntoSubBlock(stmt, variableName, allowPassIntoLoops)) {
						// If it's not possible to move the variable use into a nested block,
						// we need to declare the variable in this block
						ok = false;
						break;
					}
					// If we can move the variable into the sub-block, we need to ensure that the remaining code
					// does not use the value that was assigend by the first sub-block
					Statement nextStatement = stmt.NextStatement;
					// The next statement might be a variable declaration that we inserted, and thus does not exist
					// in the definite assignment graph. Thus we need to look up the corresponding instruction
					// prior to the introduction of the VariableDeclarationStatement.
					while (nextStatement is VariableDeclarationStatement) {
						DeclaredVariableAnnotation annotation = nextStatement.Annotation<DeclaredVariableAnnotation>();
						if (annotation == null)
							break;
						if (annotation.OriginalAssignmentStatement != null) {
							nextStatement = annotation.OriginalAssignmentStatement;
							break;
						}
						nextStatement = nextStatement.NextStatement;
					}
					if (nextStatement != null) {
						// Analyze the range from the next statement to the end of the block
						daa.SetAnalyzedRange(nextStatement, block);
						daa.Analyze(variableName);
						if (daa.UnassignedVariableUses.Count > 0) {
							ok = false;
							break;
						}
					}
				}
			}
			if (declarationPoint == null) {
				// The variable isn't used at all
				return;
			}
			if (ok) {
				// Declare the variable within the sub-blocks
				foreach (Statement stmt in block.Statements) {
					foreach (BlockStatement subBlock in stmt.Children.OfType<BlockStatement>()) {
						DeclareVariableInBlock(daa, subBlock, type, variableName, allowPassIntoLoops);
					}
				}
			} else {
				// Try converting an assignment expression into a VariableDeclarationStatement
				ExpressionStatement es = declarationPoint as ExpressionStatement;
				if (es != null) {
					AssignmentExpression ae = es.Expression as AssignmentExpression;
					if (ae != null && ae.Operator == AssignmentOperatorType.Assign) {
						IdentifierExpression ident = ae.Left as IdentifierExpression;
						if (ident != null && ident.Identifier == variableName) {
							// convert the declarationPoint into a VariableDeclarationStatement
							declarationPoint.ReplaceWith(
								new VariableDeclarationStatement {
									Type = (AstType)type.Clone(),
									Variables = {
										new VariableInitializer(variableName, ae.Right.Detach()).CopyAnnotationsFrom(ae)
									}
								}.CopyAnnotationsFrom(es).WithAnnotation(new DeclaredVariableAnnotation(es)));
							return;
						}
					}
				}
				// Declare the variable in front of declarationPoint
				block.Statements.InsertBefore(
					declarationPoint,
					new VariableDeclarationStatement((AstType)type.Clone(), variableName)
					.WithAnnotation(declaredVariableAnnotation));
			}
		}
		
		bool CanMoveVariableUseIntoSubBlock(Statement stmt, string variableName, bool allowPassIntoLoops)
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
			// We can move the variable into a sub-block only if the variable is used in only that sub-block
			for (AstNode child = stmt.FirstChild; child != null; child = child.NextSibling) {
				if (!(child is BlockStatement) && UsesVariable(child, variableName))
					return false;
			}
			return true;
		}
		
		bool UsesVariable(AstNode node, string variableName)
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
			
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				if (UsesVariable(child, variableName))
					return true;
			}
			return false;
		}
		
		#region FindInsertPos
		static AstNode FindInsertPos(AstNode node, string name, bool allowPassIntoLoops)
		{
			AstNode pos = null;
			AstNode withinPos = null;
			while (node != null) {
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null && ident.Identifier == name && ident.TypeArguments.Count == 0)
					return node;
				
				FixedStatement fixedStatement = node as FixedStatement;
				if (fixedStatement != null) {
					foreach (VariableInitializer v in fixedStatement.Variables) {
						if (v.Name == name)
							return null; // no need to introduce the variable here
					}
				}
				ForeachStatement foreachStatement = node as ForeachStatement;
				if (foreachStatement != null) {
					if (foreachStatement.VariableName == name)
						return null; // no need to introduce the variable here
				}
				
				AstNode withinCurrent = FindInsertPos(node.FirstChild, name, allowPassIntoLoops);
				if (withinCurrent != null) {
					if (pos == null) {
						pos = node;
						withinPos = withinCurrent;
					} else {
						return pos;
					}
				}
				node = node.NextSibling;
			}
			if (withinPos != null && withinPos.Role == BlockStatement.StatementRole && AllowPassInto(pos, allowPassIntoLoops))
				return withinPos;
			else
				return pos;
		}
		
		static bool AllowPassInto(AstNode node, bool allowPassIntoLoops)
		{
			if (node is AnonymousMethodExpression || node is LambdaExpression)
				return false;
			
			return allowPassIntoLoops;
			return true;
		}
		#endregion
	}
}
