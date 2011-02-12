using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveGotos: AbstractAstTransformer
	{
		Stack<StatementWithEmbeddedStatement> enteredLoops = new Stack<StatementWithEmbeddedStatement>();
		
		StatementWithEmbeddedStatement CurrentLoop {
			get {
				if (enteredLoops.Count > 0) {
					return enteredLoops.Peek();
				} else {
					return null;
				}
			}
		}
		
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			enteredLoops.Push(forStatement);
			base.VisitForStatement(forStatement, data);
			enteredLoops.Pop();
			return null;
		}
		
		public override object VisitDoLoopStatement(DoLoopStatement doLoopStatement, object data)
		{
			enteredLoops.Push(doLoopStatement);
			base.VisitDoLoopStatement(doLoopStatement, data);
			enteredLoops.Pop();
			return null;
		}
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			base.VisitBlockStatement(blockStatement, data);
			
			// Remove redundant jump at the end of block
			INode lastStmt = blockStatement.Children.Last;
			// End of while loop
			if (lastStmt is ContinueStatement && 
			    blockStatement.Parent is DoLoopStatement)
			{
				lastStmt.Remove();
				return null;
			}
			// End of for loop
			if (lastStmt is ContinueStatement && 
			    blockStatement.Parent is ForStatement)
			{
				lastStmt.Remove();
				return null;
			}
			// End of method
			if (lastStmt is ReturnStatement && 
			    (blockStatement.Parent is MethodDeclaration || blockStatement.Parent is ConstructorDeclaration) &&
			    ((ReturnStatement)lastStmt).Expression.IsNull)
			{
				lastStmt.Remove();
				return null;
			}
			
			return null;
		}
		
		// Get the next statement that will be executed after this one 
		// May return null
		public static INode GetNextStatement(Statement statement)
		{
			if (statement == null) throw new ArgumentNullException();
			
			Statement next = (Statement)statement.Next();
			
			if (next != null) {
				return EnterBlockStatement(next);
			} else {
				if (statement.Parent is BlockStatement &&
				    statement.Parent.Parent is Statement) {
					return ExitBlockStatement((Statement)statement.Parent.Parent);
				} else {
					return null;
				}
			}
		}
		
		// Get the statement that will be executed once the given block exits by the end brace
		// May return null
		public static INode ExitBlockStatement(Statement statement)
		{
			if (statement == null) throw new ArgumentNullException();
			
			// When an 'if' body is finished the execution continues with the
			// next statement after the 'if' statement
			if (statement is IfElseStatement) {
				return GetNextStatement((IfElseStatement)statement);
			}
			
			// When a 'for' body is finished the execution continues by:
			// Iterator; Condition; Body
			if (statement is ForStatement) {
				ForStatement forLoop = statement as ForStatement;
				if (forLoop.Iterator.Count > 0) {
					return forLoop.Iterator[0];
				} else if (!forLoop.Condition.IsNull) {
					return forLoop.Condition;
				} else {
					return EnterBlockStatement((Statement)forLoop.EmbeddedStatement.Children.First);
				}
			}
			
			return null;
		}
		
		// Get the first statement that will be executed in the given block
		public static INode EnterBlockStatement(Statement statement)
		{
			if (statement == null) throw new ArgumentNullException();
			
			// For loop starts as follows: Initializers; Condition; Body
			if (statement is ForStatement) {
				ForStatement forLoop = statement as ForStatement;
				if (forLoop.Initializers.Count > 0) {
					return forLoop.Initializers[0];
				} else if (!forLoop.Condition.IsNull) {
					return forLoop.Condition;
				} else if (forLoop.EmbeddedStatement is BlockStatement &&
					       forLoop.EmbeddedStatement.Children.Count > 0) {
					statement = (Statement)forLoop.EmbeddedStatement.Children.First;
					return EnterBlockStatement(statement);  // Simplify again
				}
			}
			
			return statement; // Can not simplify
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			// Remove redundant goto which goes to a label that imideately follows
			INode fallthoughTarget = GetNextStatement(gotoStatement);
			while(true) {
				if (fallthoughTarget is LabelStatement) {
				    if ((fallthoughTarget as LabelStatement).Label == gotoStatement.Label) {
						RemoveCurrentNode();
						return null;
					} else {
						fallthoughTarget = GetNextStatement((LabelStatement)fallthoughTarget);
						continue;
					}
				}
				break;
			}
			
			// Replace goto with 'break'
			// Break statement moves right outside the looop
			if (CurrentLoop != null) {
				INode breakTarget = GetNextStatement(CurrentLoop);
				if ((breakTarget is LabelStatement) &&
				    (breakTarget as LabelStatement).Label == gotoStatement.Label) {
					ReplaceCurrentNode(new BreakStatement());
					return null;
				}
			}
			
			// Replace goto with 'continue'
			// Continue statement which moves at the very end of loop
			if (CurrentLoop != null &&
			    (CurrentLoop.EmbeddedStatement is BlockStatement) &&
			    ((CurrentLoop.EmbeddedStatement as BlockStatement).Children.Last as LabelStatement) != null &&
			    ((CurrentLoop.EmbeddedStatement as BlockStatement).Children.Last as LabelStatement).Label == gotoStatement.Label) {
				ReplaceCurrentNode(new ContinueStatement());
				return null;
			}
			
			// Replace goto with 'continue'
			// Continue statement which moves at the very start of for loop
			if (CurrentLoop != null) {
				INode continueTarget = ExitBlockStatement(CurrentLoop); // The start of the loop
				if ((continueTarget is LabelStatement) &&
				    (continueTarget as LabelStatement).Label == gotoStatement.Label) {
					ReplaceCurrentNode(new ContinueStatement());
					return null;
				}
			}
			
			return null;
		}
	}
}
