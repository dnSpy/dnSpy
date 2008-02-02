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
			    blockStatement.Parent is MethodDeclaration &&
			    ((ReturnStatement)lastStmt).Expression.IsNull)
			{
				lastStmt.Remove();
				return null;
			}
			// End of if body
			if (lastStmt is GotoStatement &&
			    blockStatement.Parent is IfElseStatement)
			{
				LabelStatement nextNodeAsLabel = blockStatement.Parent.Next() as LabelStatement;
				if (nextNodeAsLabel != null) {
					if (nextNodeAsLabel.Label == ((GotoStatement)lastStmt).Label) {
						lastStmt.Remove();
						return null;
					}
				}
			}
			
			return null;
		}
		
		// Gets a next fall though statement, entering and exiting code blocks
		// It does not matter what the current statement is
		// May return null
		public static Statement GetNextStatement(Statement statement)
		{
			if (statement == null) throw new ArgumentNullException();
			
			Statement next = (Statement)statement.Next();
			
			// Exit a block of code
			if (next == null) {
				// When an 'if' body is finished the execution continues with the
				// next statement after the 'if' statement
				if (statement.Parent is BlockStatement &&
				    statement.Parent.Parent is IfElseStatement) {
					return GetNextStatement((Statement)statement.Parent.Parent);
				} else {
					return null;
				}
			}
			
			// Enter a block of code
			while(true) {
				// If a 'for' loop does not have initializers and condition,
				// the next statement is the entry point
				ForStatement stmtAsForStmt = next as ForStatement;
				if (stmtAsForStmt != null &&
					stmtAsForStmt.Initializers.Count == 0 &&
					stmtAsForStmt.Condition.IsNull &&
					stmtAsForStmt.EmbeddedStatement is BlockStatement &&
					stmtAsForStmt.EmbeddedStatement.Children.Count > 0) {
					next = (Statement)stmtAsForStmt.EmbeddedStatement.Children.First;
					continue; // Restart
				}
				
				return next;
			}
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			// Remove redundant goto which goes to a label that imideately follows
			LabelStatement followingLabel = GetNextStatement(gotoStatement) as LabelStatement;
			if (followingLabel != null && followingLabel.Label == gotoStatement.Label) {
				RemoveCurrentNode();
				return null;
			}
			
			// Replace goto with 'break'
			// Break statement moves right outside the looop
			if (CurrentLoop != null &&
			    (CurrentLoop.Next() as LabelStatement) != null &&
			    (CurrentLoop.Next() as LabelStatement).Label == gotoStatement.Label) {
				ReplaceCurrentNode(new BreakStatement());
				return null;
			}
			
			// Replace goto with 'continue'
			// Continue statement which moves at the very end of loop
			if (CurrentLoop != null &&
			    (CurrentLoop.EmbeddedStatement as BlockStatement) != null &&
			    ((CurrentLoop.EmbeddedStatement as BlockStatement).Children.Last as LabelStatement) != null &&
			    ((CurrentLoop.EmbeddedStatement as BlockStatement).Children.Last as LabelStatement).Label == gotoStatement.Label) {
				ReplaceCurrentNode(new ContinueStatement());
				return null;
			}
			
			// Replace goto with 'continue'
			// Continue statement which moves at the very start of for loop if there is no contition and iteration
			if ((CurrentLoop as ForStatement) != null &&
			    (CurrentLoop as ForStatement).Condition.IsNull &&
			    (CurrentLoop as ForStatement).Iterator.Count == 0 &&
			    (CurrentLoop.EmbeddedStatement as BlockStatement) != null &&
			    ((CurrentLoop.EmbeddedStatement as BlockStatement).Children.First as LabelStatement) != null &&
			    ((CurrentLoop.EmbeddedStatement as BlockStatement).Children.First as LabelStatement).Label == gotoStatement.Label) {
				ReplaceCurrentNode(new ContinueStatement());
				return null;
			}
			
			return null;
		}
	}
}
