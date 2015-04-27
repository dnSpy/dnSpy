//
// AstFormattingVisitor_Statements.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System;
using System.Linq;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.CSharp
{
	partial class FormattingVisitor : DepthFirstAstVisitor
	{
		public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			base.VisitExpressionStatement(expressionStatement);
			FixSemicolon(expressionStatement.SemicolonToken);
		}

		void VisitBlockWithoutFixingBraces(BlockStatement blockStatement, bool indent)
		{
			if (indent) {
				curIndent.Push(IndentType.Block);
			}

			VisitChildrenToFormat (blockStatement, child => {
				if (child.Role == Roles.LBrace || child.Role == Roles.RBrace) {
					return;
				}

				if (child is Statement) {
					FixStatementIndentation(child.StartLocation);
					child.AcceptVisitor(this);
				} else if (child is Comment) {
					child.AcceptVisitor(this);
				} else if (child is NewLineNode) {
					// ignore
				} else {
					// pre processor directives at line start, if they are there.
					if (child.StartLocation.Column > 1)
						FixStatementIndentation(child.StartLocation);
				}
			});

			if (indent) {
				curIndent.Pop ();
			}
		}

		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			FixIndentation(blockStatement);
			VisitBlockWithoutFixingBraces(blockStatement, policy.IndentBlocks);
			FixIndentation(blockStatement.RBraceToken);
		}

		public override void VisitBreakStatement(BreakStatement breakStatement)
		{
			FixSemicolon(breakStatement.SemicolonToken);
		}

		public override void VisitCheckedStatement(CheckedStatement checkedStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, checkedStatement.Body);
		}

		public override void VisitContinueStatement(ContinueStatement continueStatement)
		{
			FixSemicolon(continueStatement.SemicolonToken);
		}

		public override void VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			// Empty
		}

		public override void VisitFixedStatement(FixedStatement fixedStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, fixedStatement.EmbeddedStatement);
		}

		public override void VisitForeachStatement(ForeachStatement foreachStatement)
		{
			ForceSpacesBeforeRemoveNewLines(foreachStatement.LParToken, policy.SpaceBeforeForeachParentheses);

			ForceSpacesAfter(foreachStatement.LParToken, policy.SpacesWithinForeachParentheses);
			ForceSpacesBeforeRemoveNewLines(foreachStatement.RParToken, policy.SpacesWithinForeachParentheses);
			
			FixEmbeddedStatment(policy.StatementBraceStyle, foreachStatement.EmbeddedStatement);
		}

		void FixEmbeddedStatment(BraceStyle braceStyle, AstNode node)
		{
			FixEmbeddedStatment(braceStyle, null, false, node);
		}

		void FixEmbeddedStatment(BraceStyle braceStyle, CSharpTokenNode token, bool allowInLine, AstNode node, bool statementAlreadyIndented = false)
		{
			if (node == null)
				return;
			bool isBlock = node is BlockStatement;
			FormattingChanges.TextReplaceAction beginBraceAction = null;
			FormattingChanges.TextReplaceAction endBraceAction = null;
			BlockStatement closeBlockToBeFixed = null;
			if (isBlock) {
				BlockStatement block = node as BlockStatement;
				if (allowInLine && block.StartLocation.Line == block.EndLocation.Line && block.Statements.Count() <= 1) {
					if (block.Statements.Count() == 1)
						nextStatementIndent = " ";
				} else {
					if (!statementAlreadyIndented)
						FixOpenBrace(braceStyle, block.LBraceToken);
					closeBlockToBeFixed = block;
				}

				if (braceStyle == BraceStyle.NextLineShifted2)
					curIndent.Push(IndentType.Block);
			} else {
				if (allowInLine && token.StartLocation.Line == node.EndLocation.Line) {
					nextStatementIndent = " ";
				}
			}
			bool pushed = false;

			if (policy.IndentBlocks && !(
				policy.AlignEmbeddedStatements && node is IfElseStatement && node.Parent is IfElseStatement || 
				policy.AlignEmbeddedStatements && node is UsingStatement && node.Parent is UsingStatement || 
				policy.AlignEmbeddedStatements && node is LockStatement && node.Parent is LockStatement)) { 
				curIndent.Push(IndentType.Block);
				pushed = true;
			}
			if (isBlock) {
				VisitBlockWithoutFixingBraces((BlockStatement)node, false);
			} else {
				if (!statementAlreadyIndented) {
					PlaceOnNewLine(policy.EmbeddedStatementPlacement, node);
					nextStatementIndent = null;
				}
				node.AcceptVisitor(this);
			}
			nextStatementIndent = null;
			if (pushed)
				curIndent.Pop();
			if (beginBraceAction != null && endBraceAction != null) {
				beginBraceAction.DependsOn = endBraceAction;
				endBraceAction.DependsOn = beginBraceAction;
			}

			if (isBlock && braceStyle == BraceStyle.NextLineShifted2)
				curIndent.Pop();
			if (closeBlockToBeFixed != null) {
				FixClosingBrace(braceStyle, closeBlockToBeFixed.RBraceToken);
			}

		}

		public bool IsLineIsEmptyUpToEol(TextLocation startLocation)
		{
			return IsLineIsEmptyUpToEol(document.GetOffset(startLocation) - 1);
		}

		bool IsLineIsEmptyUpToEol(int startOffset)
		{
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = document.GetCharAt(offset);
				if (ch != ' ' && ch != '\t') {
					return NewLine.IsNewLine (ch);
				}
			}
			return true;
		}

		int SearchWhitespaceStart(int startOffset)
		{
			if (startOffset < 0) {
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			}
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = document.GetCharAt(offset);
				if (!Char.IsWhiteSpace(ch)) {
					return offset + 1;
				}
			}
			return 0;
		}

		int SearchWhitespaceEnd(int startOffset)
		{
			if (startOffset > document.TextLength) {
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			}
			for (int offset = startOffset + 1; offset < document.TextLength; offset++) {
				char ch = document.GetCharAt(offset);
				if (!Char.IsWhiteSpace(ch)) {
					return offset + 1;
				}
			}
			return document.TextLength - 1;
		}

		int SearchWhitespaceLineStart(int startOffset)
		{
			if (startOffset < 0) {
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			}
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = document.GetCharAt(offset);
				if (ch != ' ' && ch != '\t') {
					return offset + 1;
				}
			}
			return 0;
		}

		public override void VisitForStatement(ForStatement forStatement)
		{
			foreach (AstNode node in forStatement.Children) {
				if (node.Role == Roles.Semicolon) {
					if (node.GetNextSibling(NoWhitespacePredicate) is CSharpTokenNode || node.GetNextSibling(NoWhitespacePredicate) is EmptyStatement) {
						continue;
					}
					ForceSpacesBefore(node, policy.SpaceBeforeForSemicolon);
					ForceSpacesAfter(node, policy.SpaceAfterForSemicolon);
				} else if (node.Role == Roles.LPar) {
					ForceSpacesBeforeRemoveNewLines(node, policy.SpaceBeforeForParentheses);
					ForceSpacesAfter(node, policy.SpacesWithinForParentheses);
				} else if (node.Role == Roles.RPar) {
					ForceSpacesBeforeRemoveNewLines(node, policy.SpacesWithinForParentheses);
				} else if (node.Role == Roles.EmbeddedStatement) {
					FixEmbeddedStatment(policy.StatementBraceStyle, node);
				} else {
					node.AcceptVisitor(this); 
				}
			}
		}

		public override void VisitGotoStatement(GotoStatement gotoStatement)
		{
			VisitChildren(gotoStatement);
			FixSemicolon(gotoStatement.SemicolonToken);
		}

		public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			ForceSpacesBeforeRemoveNewLines(ifElseStatement.LParToken, policy.SpaceBeforeIfParentheses);
			Align(ifElseStatement.LParToken, ifElseStatement.Condition, policy.SpacesWithinIfParentheses);
			ForceSpacesBeforeRemoveNewLines(ifElseStatement.RParToken, policy.SpacesWithinIfParentheses);


			if (!ifElseStatement.TrueStatement.IsNull) {
				FixEmbeddedStatment(policy.StatementBraceStyle, ifElseStatement.IfToken, policy.AllowIfBlockInline, ifElseStatement.TrueStatement);
			}

			if (!ifElseStatement.FalseStatement.IsNull) {
				var placeElseOnNewLine = policy.ElseNewLinePlacement;
				if (!(ifElseStatement.TrueStatement is BlockStatement)) {
					placeElseOnNewLine = NewLinePlacement.NewLine;
				}
				PlaceOnNewLine(placeElseOnNewLine, ifElseStatement.ElseToken);
				if (ifElseStatement.FalseStatement is IfElseStatement) {
					PlaceOnNewLine(policy.ElseIfNewLinePlacement, ((IfElseStatement)ifElseStatement.FalseStatement).IfToken);
				}
				FixEmbeddedStatment(policy.StatementBraceStyle, ifElseStatement.ElseToken, policy.AllowIfBlockInline, ifElseStatement.FalseStatement, ifElseStatement.FalseStatement is IfElseStatement);
			}
		}

		public override void VisitLabelStatement(LabelStatement labelStatement)
		{
			// TODO
			VisitChildren(labelStatement);
		}

		public override void VisitLockStatement(LockStatement lockStatement)
		{
			ForceSpacesBeforeRemoveNewLines(lockStatement.LParToken, policy.SpaceBeforeLockParentheses);

			ForceSpacesAfter(lockStatement.LParToken, policy.SpacesWithinLockParentheses);
			ForceSpacesBeforeRemoveNewLines(lockStatement.RParToken, policy.SpacesWithinLockParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, lockStatement.EmbeddedStatement);
		}

		public override void VisitReturnStatement(ReturnStatement returnStatement)
		{
			VisitChildren(returnStatement);
			FixSemicolon(returnStatement.SemicolonToken);
		}

		public override void VisitSwitchStatement(SwitchStatement switchStatement)
		{
			ForceSpacesBeforeRemoveNewLines(switchStatement.LParToken, policy.SpaceBeforeSwitchParentheses);

			ForceSpacesAfter(switchStatement.LParToken, policy.SpacesWithinSwitchParentheses);
			ForceSpacesBeforeRemoveNewLines(switchStatement.RParToken, policy.SpacesWithinSwitchParentheses);

			FixOpenBrace(policy.StatementBraceStyle, switchStatement.LBraceToken);
			VisitChildren(switchStatement);
			FixClosingBrace(policy.StatementBraceStyle, switchStatement.RBraceToken);
		}

		public override void VisitSwitchSection(SwitchSection switchSection)
		{
			if (policy.IndentSwitchBody) {
				curIndent.Push(IndentType.Block);
			}

			foreach (CaseLabel label in switchSection.CaseLabels) {
				FixStatementIndentation(label.StartLocation);
				label.AcceptVisitor(this);
			}
			if (policy.IndentCaseBody) {
				curIndent.Push(IndentType.Block);
			}

			foreach (var stmt in switchSection.Statements) {
				if (stmt is BreakStatement && !policy.IndentBreakStatements && policy.IndentCaseBody) {
					curIndent.Pop();
					FixStatementIndentation(stmt.StartLocation);
					stmt.AcceptVisitor(this);
					curIndent.Push(IndentType.Block);
					continue;
				}
				FixStatementIndentation(stmt.StartLocation);
				stmt.AcceptVisitor(this);
			}
			if (policy.IndentCaseBody) {
				curIndent.Pop ();
			}

			if (policy.IndentSwitchBody) {
				curIndent.Pop ();
			}
		}

		public override void VisitCaseLabel(CaseLabel caseLabel)
		{
			ForceSpacesBefore(caseLabel.ColonToken, false);
		}

		public override void VisitThrowStatement(ThrowStatement throwStatement)
		{
			VisitChildren(throwStatement);
			FixSemicolon(throwStatement.SemicolonToken);
		}

		public override void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
		{
			if (!tryCatchStatement.TryBlock.IsNull) {
				FixEmbeddedStatment(policy.StatementBraceStyle, tryCatchStatement.TryBlock);
			}

			foreach (CatchClause clause in tryCatchStatement.CatchClauses) {
				PlaceOnNewLine(policy.CatchNewLinePlacement, clause.CatchToken);
				if (!clause.LParToken.IsNull) {
					ForceSpacesBeforeRemoveNewLines(clause.LParToken, policy.SpaceBeforeCatchParentheses);

					ForceSpacesAfter(clause.LParToken, policy.SpacesWithinCatchParentheses);
					ForceSpacesBeforeRemoveNewLines(clause.RParToken, policy.SpacesWithinCatchParentheses);
				}
				FixEmbeddedStatment(policy.StatementBraceStyle, clause.Body);
			}

			if (!tryCatchStatement.FinallyBlock.IsNull) {
				PlaceOnNewLine(policy.FinallyNewLinePlacement, tryCatchStatement.FinallyToken);

				FixEmbeddedStatment(policy.StatementBraceStyle, tryCatchStatement.FinallyBlock);
			}

		}

		public override void VisitCatchClause(CatchClause catchClause)
		{
			// Handled in TryCatchStatement
		}

		public override void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, uncheckedStatement.Body);
		}

		public override void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, unsafeStatement.Body);
		}

		public override void VisitUsingStatement(UsingStatement usingStatement)
		{
			ForceSpacesBeforeRemoveNewLines(usingStatement.LParToken, policy.SpaceBeforeUsingParentheses);

			Align(usingStatement.LParToken, usingStatement.ResourceAcquisition, policy.SpacesWithinUsingParentheses);

			ForceSpacesBeforeRemoveNewLines(usingStatement.RParToken, policy.SpacesWithinUsingParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, usingStatement.EmbeddedStatement);
		}

		public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			var returnType = variableDeclarationStatement.Type;
			returnType.AcceptVisitor(this);
			if ((variableDeclarationStatement.Modifiers & Modifiers.Const) == Modifiers.Const) {
				ForceSpacesAround(returnType, true);
			} else {
				ForceSpacesAfter(returnType, true);
			}
			var lastLoc = variableDeclarationStatement.StartLocation;
			foreach (var initializer in variableDeclarationStatement.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					FixStatementIndentation(initializer.StartLocation);
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}

			FormatCommas(variableDeclarationStatement, policy.SpaceBeforeLocalVariableDeclarationComma, policy.SpaceAfterLocalVariableDeclarationComma);
			FixSemicolon(variableDeclarationStatement.SemicolonToken);
		}

		public override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, doWhileStatement.EmbeddedStatement);
			PlaceOnNewLine(doWhileStatement.EmbeddedStatement is BlockStatement ? policy.WhileNewLinePlacement : NewLinePlacement.NewLine, doWhileStatement.WhileToken);

			Align(doWhileStatement.LParToken, doWhileStatement.Condition, policy.SpacesWithinWhileParentheses);
			ForceSpacesBeforeRemoveNewLines(doWhileStatement.RParToken, policy.SpacesWithinWhileParentheses);
		}

		void Align(AstNode lPar, AstNode alignNode, bool space)
		{
			int extraSpaces = 0;
			var useExtraSpaces = lPar.StartLocation.Line == alignNode.StartLocation.Line;
			if (useExtraSpaces) {
				extraSpaces = Math.Max(0, lPar.StartLocation.Column + (space ? 1 : 0) - curIndent.IndentString.Length);
				curIndent.ExtraSpaces += extraSpaces;
				ForceSpacesAfter(lPar, space);
			} else {
				curIndent.Push(IndentType.Continuation); 
				FixIndentation(alignNode);
			}
			alignNode.AcceptVisitor(this);

			if (useExtraSpaces) {
				curIndent.ExtraSpaces -= extraSpaces;
			} else {
				curIndent.Pop();
			}

		}

		public override void VisitWhileStatement(WhileStatement whileStatement)
		{
			ForceSpacesBeforeRemoveNewLines(whileStatement.LParToken, policy.SpaceBeforeWhileParentheses);
			Align(whileStatement.LParToken, whileStatement.Condition, policy.SpacesWithinWhileParentheses);
			ForceSpacesBeforeRemoveNewLines(whileStatement.RParToken, policy.SpacesWithinWhileParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, whileStatement.EmbeddedStatement);
		}

		public override void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			FixSemicolon(yieldBreakStatement.SemicolonToken);
		}

		public override void VisitYieldReturnStatement(YieldReturnStatement yieldStatement)
		{
			yieldStatement.Expression.AcceptVisitor(this);
			FixSemicolon(yieldStatement.SemicolonToken);
		}

		public override void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			if (!variableInitializer.AssignToken.IsNull) {
				ForceSpacesAround(variableInitializer.AssignToken, policy.SpaceAroundAssignment);
			}
			if (!variableInitializer.Initializer.IsNull) {
				int extraSpaces = 0;
				var useExtraSpaces = variableInitializer.AssignToken.StartLocation.Line == variableInitializer.Initializer.StartLocation.Line;
				if (useExtraSpaces) {
					extraSpaces = Math.Max(0, variableInitializer.AssignToken.StartLocation.Column + 1 - curIndent.IndentString.Length);
					curIndent.ExtraSpaces += extraSpaces;
				} else {
					curIndent.Push(IndentType.Continuation); 
					FixIndentation(variableInitializer.Initializer);
				}
				variableInitializer.Initializer.AcceptVisitor(this);

				if (useExtraSpaces) {
					curIndent.ExtraSpaces -= extraSpaces;
				} else {
					curIndent.Pop();
				}
			}

		}
	}
}

