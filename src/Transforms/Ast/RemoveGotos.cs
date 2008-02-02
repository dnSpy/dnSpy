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
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			// Find what is the next statement following this one
			// It does fall though loop entries (the loop must not have initializer and condition)
			INode followingStmt = gotoStatement.Next();
			while(
				(followingStmt as ForStatement) != null &&
				(followingStmt as ForStatement).Initializers.Count == 0 &&
				(followingStmt as ForStatement).Condition.IsNull &&
				(followingStmt as ForStatement).EmbeddedStatement is BlockStatement) {
				followingStmt = (followingStmt as ForStatement).EmbeddedStatement.Children.First;
			}
			
			// Remove redundant goto which goes to a label that imideately follows
			LabelStatement followingLabel = followingStmt as LabelStatement;
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
