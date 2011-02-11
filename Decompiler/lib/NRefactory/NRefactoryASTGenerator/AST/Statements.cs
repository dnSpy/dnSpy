// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace NRefactoryASTGenerator.Ast
{
	[CustomImplementation]
	abstract class Statement : AbstractNode, INullable {}
	
	[CustomImplementation]
	abstract class StatementWithEmbeddedStatement : Statement {
		Statement embeddedStatement;
	}
	
	[CustomImplementation, HasChildren]
	class BlockStatement : Statement {}
	
	class BreakStatement : Statement {}
	
	enum ContinueType {}
	
	class ContinueStatement : Statement {
		ContinueType continueType;
		
		public ContinueStatement() {}
		public ContinueStatement(ContinueType continueType) {}
	}
	
	enum ConditionType {}
	enum ConditionPosition {}
	
	class DoLoopStatement : StatementWithEmbeddedStatement {
		Expression        condition;
		ConditionType     conditionType;
		ConditionPosition conditionPosition;
		
		public DoLoopStatement(Expression condition, Statement embeddedStatement, ConditionType conditionType, ConditionPosition conditionPosition) {}
	}
	
	class ForeachStatement : StatementWithEmbeddedStatement {
		TypeReference typeReference;
		string        variableName;
		Expression    expression;
		Expression    nextExpression;
		
		public ForeachStatement(TypeReference typeReference, string variableName, Expression expression, Statement embeddedStatement) {}
		public ForeachStatement(TypeReference typeReference, string variableName, Expression expression, Statement embeddedStatement, Expression nextExpression) {}
	}
	
	class ForStatement : StatementWithEmbeddedStatement {
		List<Statement> initializers;
		Expression      condition;
		List<Statement> iterator;
		
		public ForStatement(List<Statement> initializers, Expression condition, List<Statement> iterator, Statement embeddedStatement) {}
	}
	
	class GotoStatement : Statement {
		string label;
		
		public GotoStatement(string label) {}
	}
	
	[IncludeMember(@"
			public IfElseStatement(Expression condition, Statement trueStatement)
				: this(condition) {
				this.trueStatement.Add(Statement.CheckNull(trueStatement));
				if (trueStatement != null) trueStatement.Parent = this;
			}")]
	[IncludeMember(@"
			public IfElseStatement(Expression condition, Statement trueStatement, Statement falseStatement)
				: this(condition) {
				this.trueStatement.Add(Statement.CheckNull(trueStatement));
				this.falseStatement.Add(Statement.CheckNull(falseStatement));
				if (trueStatement != null) trueStatement.Parent = this;
				if (falseStatement != null) falseStatement.Parent = this;
			}")]
	[IncludeBoolProperty("HasElseStatements", "return falseStatement.Count > 0;")]
	[IncludeBoolProperty("HasElseIfSections", "return elseIfSections.Count > 0;")]
	class IfElseStatement : Statement {
		Expression condition;
		List<Statement> trueStatement; // List for stmt : stmt : stmt ... in VB.NET
		List<Statement> falseStatement;
		List<ElseIfSection> elseIfSections;
		
		public IfElseStatement(Expression condition) {}
	}
	
	class ElseIfSection : StatementWithEmbeddedStatement {
		Expression condition;
		
		public ElseIfSection(Expression condition, Statement embeddedStatement) {}
	}
	
	class LabelStatement : Statement {
		string label;
		
		public LabelStatement(string label) {}
	}
	
	[CustomImplementation]
	class LocalVariableDeclaration : Statement {
		TypeReference             typeReference;
		Modifiers                  modifier;
		List<VariableDeclaration> variables;
	}
	
	class LockStatement : StatementWithEmbeddedStatement
	{
		Expression lockExpression;
		
		public LockStatement(Expression lockExpression, Statement embeddedStatement) {}
	}
	
	class ReturnStatement : Statement
	{
		Expression expression;
		
		public ReturnStatement(Expression expression) { }
	}
	
	class ExpressionStatement : Statement {
		Expression expression;
		
		public ExpressionStatement(Expression expression) {}
	}
	
	class SwitchStatement : Statement {
		Expression          switchExpression;
		List<SwitchSection> switchSections;
		
		public SwitchStatement(Expression switchExpression, List<SwitchSection> switchSections) {}
	}
	
	class SwitchSection : BlockStatement {
		List<CaseLabel> switchLabels;
		
		public SwitchSection() { }
		public SwitchSection(List<CaseLabel> switchLabels) { }
	}
	
	[IncludeBoolProperty("IsDefault", "return label.IsNull;")]
	class CaseLabel : AbstractNode {
		Expression         label;
		BinaryOperatorType binaryOperatorType;
		Expression         toExpression;
		
		public CaseLabel() {}
		public CaseLabel(Expression label) {}
		public CaseLabel(Expression label, Expression toExpression) {}
		public CaseLabel(BinaryOperatorType binaryOperatorType, Expression label) {}
	}
	
	class ThrowStatement : Statement {
		Expression expression;
		
		public ThrowStatement(Expression expression) {}
	}
	
	class TryCatchStatement : Statement {
		Statement         statementBlock;
		List<CatchClause> catchClauses;
		Statement         finallyBlock;
		
		public TryCatchStatement(Statement statementBlock, List<CatchClause> catchClauses, Statement finallyBlock) {}
	}
	
	class CatchClause : AbstractNode {
		TypeReference typeReference;
		string     variableName;
		Statement  statementBlock;
		Expression condition;
		
		public CatchClause(TypeReference typeReference, string variableName, Statement statementBlock) {}
		public CatchClause(TypeReference typeReference, string variableName, Statement statementBlock, Expression condition) {}
		public CatchClause(Statement statementBlock) {}
	}
	
	class CheckedStatement : Statement {
		Statement block;
		
		public CheckedStatement(Statement block) {}
	}
	
	class EmptyStatement : Statement {}
	
	class FixedStatement : StatementWithEmbeddedStatement {
		Statement pointerDeclaration;
		
		public FixedStatement(Statement pointerDeclaration, Statement embeddedStatement) {}
	}
	
	[IncludeBoolProperty("IsDefaultCase", "return expression.IsNull;")]
	class GotoCaseStatement : Statement {
		Expression expression;
		
		public GotoCaseStatement(Expression expression) {}
	}
	
	class UncheckedStatement : Statement {
		Statement block;
		
		public UncheckedStatement(Statement block) {}
	}
	
	class UnsafeStatement : Statement {
		Statement block;
		
		public UnsafeStatement(Statement block) {}
	}
	
	class UsingStatement : StatementWithEmbeddedStatement {
		Statement resourceAcquisition;
		
		public UsingStatement(Statement resourceAcquisition, Statement embeddedStatement) {}
	}
	
	[IncludeBoolProperty("IsYieldReturn", "return statement is ReturnStatement;")]
	[IncludeBoolProperty("IsYieldBreak",  "return statement is BreakStatement;")]
	class YieldStatement : Statement {
		Statement statement;
		
		public YieldStatement(Statement statement) {}
	}
	
	class AddHandlerStatement : Statement {
		Expression eventExpression;
		Expression handlerExpression;
		
		public AddHandlerStatement(Expression eventExpression, Expression handlerExpression) {}
	}
	
	class EndStatement : Statement {}
	
	class EraseStatement : Statement {
		List<Expression> expressions;
		
		public EraseStatement() {}
		public EraseStatement(List<Expression> expressions) {}
	}
	
	class ErrorStatement : Statement {
		Expression expression;
		
		public ErrorStatement(Expression expression) {}
	}
	
	enum ExitType {}
	
	class ExitStatement : Statement {
		ExitType exitType;
		
		public ExitStatement(ExitType exitType) {}
	}
	
	class ForNextStatement : StatementWithEmbeddedStatement {
		Expression start;
		Expression end;
		Expression step;
		
		List<Expression> nextExpressions;
		// either use typeReference+variableName
		TypeReference typeReference;
		string        variableName;
		// or use loopVariableExpression:
		Expression loopVariableExpression;
	}
	
	class OnErrorStatement : StatementWithEmbeddedStatement {
		public OnErrorStatement(Statement embeddedStatement) {}
	}
	
	class RaiseEventStatement : Statement {
		string eventName;
		List<Expression> arguments;
		
		public RaiseEventStatement(string eventName, List<Expression> arguments) {}
	}
	
	class ReDimStatement : Statement {
		List<InvocationExpression> reDimClauses;
		bool isPreserve;
		
		public ReDimStatement(bool isPreserve) {}
	}
	
	class RemoveHandlerStatement : Statement {
		Expression eventExpression;
		Expression handlerExpression;
		
		public RemoveHandlerStatement(Expression eventExpression, Expression handlerExpression) {}
	}
	
	class ResumeStatement : Statement {
		string labelName;
		bool isResumeNext;
		
		public ResumeStatement(bool isResumeNext) {}
		
		public ResumeStatement(string labelName) {}
	}
	
	class StopStatement : Statement {}
	
	class WithStatement : Statement {
		Expression     expression;
		BlockStatement body;
		
		public WithStatement(Expression expression) {}
	}
}
