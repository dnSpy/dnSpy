using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms.Ast
{
	public class RemoveGotos: DepthFirstAstVisitor<object, object>
	{
		Stack<Statement> enteredLoops = new Stack<Statement>();
		
		Statement CurrentLoop {
			get {
				if (enteredLoops.Count > 0) {
					return enteredLoops.Peek();
				} else {
					return null;
				}
			}
		}
		
		Statement CurrentLoopBody {
			get {
				if (this.CurrentLoop == null) {
					return null;
				} else if (this.CurrentLoop is ForStatement) {
					return ((ForStatement)this.CurrentLoop).EmbeddedStatement;
				} else if (this.CurrentLoop is WhileStatement) {
					return ((WhileStatement)this.CurrentLoop).EmbeddedStatement;
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
		
		public override object VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			enteredLoops.Push(whileStatement);
			base.VisitWhileStatement(whileStatement, data);
			enteredLoops.Pop();
			return null;
		}
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			base.VisitBlockStatement(blockStatement, data);
			
			// Remove redundant jump at the end of block
			AstNode lastStmt = blockStatement.Children.LastOrDefault();
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
		public static AstNode GetNextStatement(Statement statement)
		{
			if (statement == null) throw new ArgumentNullException();
			
			Statement next = (Statement)statement.NextSibling;
			
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
		public static AstNode ExitBlockStatement(Statement statement)
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
				if (forLoop.Iterators.Any()) {
					return forLoop.Iterators.First();
				} else if (!forLoop.Condition.IsNull) {
					return forLoop.Condition;
				} else {
					return EnterBlockStatement((Statement)forLoop.EmbeddedStatement.FirstChild);
				}
			}
			
			return null;
		}
		
		// Get the first statement that will be executed in the given block
		public static AstNode EnterBlockStatement(Statement statement)
		{
			if (statement == null) throw new ArgumentNullException();
			
			// For loop starts as follows: Initializers; Condition; Body
			if (statement is ForStatement) {
				ForStatement forLoop = statement as ForStatement;
				if (forLoop.Initializers.Any()) {
				    	return forLoop.Initializers.First();
				} else if (!forLoop.Condition.IsNull) {
					return forLoop.Condition;
				} else if (forLoop.EmbeddedStatement.Children.FirstOrDefault() is Statement) {
					return EnterBlockStatement((Statement)forLoop.EmbeddedStatement.FirstChild);  // Simplify again
				}
			}
			
			return statement; // Can not simplify
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			// Remove redundant goto which goes to a label that imideately follows
			AstNode fallthoughTarget = GetNextStatement(gotoStatement);
			while(true) {
				if (fallthoughTarget is LabelStatement) {
				    if ((fallthoughTarget as LabelStatement).Label == gotoStatement.Label) {
						gotoStatement.Remove();
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
				AstNode breakTarget = GetNextStatement(CurrentLoop);
				if ((breakTarget is LabelStatement) &&
				    (breakTarget as LabelStatement).Label == gotoStatement.Label) {
					gotoStatement.ReplaceWith(new BreakStatement());
					return null;
				}
			}
			
			// Replace goto with 'continue'
			// Continue statement which moves at the very end of loop
			if (CurrentLoop != null &&
			    (CurrentLoopBody is BlockStatement) &&
			    ((CurrentLoopBody as BlockStatement).LastChild as LabelStatement) != null &&
			    ((CurrentLoopBody as BlockStatement).LastChild as LabelStatement).Label == gotoStatement.Label) {
				gotoStatement.ReplaceWith(new ContinueStatement());
				return null;
			}
			
			// Replace goto with 'continue'
			// Jump before while
			if (CurrentLoop is WhileStatement &&
			    CurrentLoop.PrevSibling != null &&
			    CurrentLoop.PrevSibling is LabelStatement &&
			    (CurrentLoop.PrevSibling as LabelStatement).Label == gotoStatement.Label) {
				gotoStatement.ReplaceWith(new ContinueStatement());
				return null;
			}
			
			// Replace goto with 'continue'
			// Continue statement which moves at the very start of for loop
			if (CurrentLoop != null) {
				AstNode continueTarget = ExitBlockStatement(CurrentLoop); // The start of the loop
				if ((continueTarget is LabelStatement) &&
				    (continueTarget as LabelStatement).Label == gotoStatement.Label) {
					gotoStatement.ReplaceWith(new ContinueStatement());
					return null;
				}
			}
			
			return null;
		}
	}
}
