//
// AstFormattingVisitor_Expressions.cs
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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	partial class FormattingVisitor : DepthFirstAstVisitor
	{
		public override void VisitComposedType(ComposedType composedType)
		{
			var spec = composedType.ArraySpecifiers.FirstOrDefault();
			if (spec != null)
				ForceSpacesBefore(spec.LBracketToken, policy.SpaceBeforeArrayDeclarationBrackets);

			if (composedType.HasNullableSpecifier)
				ForceSpacesBefore(composedType.NullableSpecifierToken, false);

			if (composedType.PointerRank > 0)
				foreach (var token in composedType.PointerTokens)
					ForceSpacesBefore(token, false);

			base.VisitComposedType(composedType);
		}

		public override void VisitAnonymousMethodExpression(AnonymousMethodExpression lambdaExpression)
		{
			FormatArguments(lambdaExpression);

			if (!lambdaExpression.Body.IsNull) {
				var old = curIndent;
				this.curIndent = curIndent.GetIndentWithoutSpace ();
				FixOpenBrace(policy.AnonymousMethodBraceStyle, lambdaExpression.Body.LBraceToken);
				VisitBlockWithoutFixingBraces(lambdaExpression.Body, policy.IndentBlocks);
				FixClosingBrace(policy.AnonymousMethodBraceStyle, lambdaExpression.Body.RBraceToken);
				curIndent = old;
			}

		}

		public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			ForceSpacesAround(assignmentExpression.OperatorToken, policy.SpaceAroundAssignment);
			base.VisitAssignmentExpression(assignmentExpression);
		}

		public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			bool forceSpaces = false;
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					forceSpaces = policy.SpaceAroundEqualityOperator;
					break;
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.LessThanOrEqual:
					forceSpaces = policy.SpaceAroundRelationalOperator;
					break;
				case BinaryOperatorType.ConditionalAnd:
				case BinaryOperatorType.ConditionalOr:
					forceSpaces = policy.SpaceAroundLogicalOperator;
					break;
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
				case BinaryOperatorType.ExclusiveOr:
					forceSpaces = policy.SpaceAroundBitwiseOperator;
					break;
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Subtract:
					forceSpaces = policy.SpaceAroundAdditiveOperator;
					break;
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.Modulus:
					forceSpaces = policy.SpaceAroundMultiplicativeOperator;
					break;
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					forceSpaces = policy.SpaceAroundShiftOperator;
					break;
				case BinaryOperatorType.NullCoalescing:
					forceSpaces = policy.SpaceAroundNullCoalescingOperator;
					break;
			}
			var opToken = binaryOperatorExpression.OperatorToken;
			if (opToken.PrevSibling != null && opToken.PrevSibling.Role != Roles.NewLine) {
				ForceSpacesBefore(opToken, forceSpaces);
			} else {
				ForceSpacesAfter(binaryOperatorExpression.Left, false);
				FixIndentation(opToken);
			}
			ForceSpacesAfter(opToken, opToken.NextSibling != null && opToken.NextSibling.Role != Roles.NewLine && forceSpaces);

			binaryOperatorExpression.Left.AcceptVisitor(this);
			// Handle line breaks in binary opeartor expression.
			if (binaryOperatorExpression.Left.EndLocation.Line != binaryOperatorExpression.Right.StartLocation.Line) {
				if (opToken.StartLocation.Line == binaryOperatorExpression.Right.StartLocation.Line) {
					FixStatementIndentation(opToken.StartLocation);
				} else {
					FixStatementIndentation(binaryOperatorExpression.Right.StartLocation);
				}
			}
			binaryOperatorExpression.Right.AcceptVisitor(this);
		}

		public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
		{
			ForceSpacesBefore(conditionalExpression.QuestionMarkToken, policy.SpaceBeforeConditionalOperatorCondition);
			ForceSpacesAfter(conditionalExpression.QuestionMarkToken, policy.SpaceAfterConditionalOperatorCondition);
			ForceSpacesBefore(conditionalExpression.ColonToken, policy.SpaceBeforeConditionalOperatorSeparator);
			ForceSpacesAfter(conditionalExpression.ColonToken, policy.SpaceAfterConditionalOperatorSeparator);
			base.VisitConditionalExpression(conditionalExpression);
		}

		public override void VisitCastExpression(CastExpression castExpression)
		{
			if (castExpression.RParToken != null) {
				ForceSpacesAfter(castExpression.LParToken, policy.SpacesWithinCastParentheses);
				ForceSpacesBefore(castExpression.RParToken, policy.SpacesWithinCastParentheses);

				ForceSpacesAfter(castExpression.RParToken, policy.SpaceAfterTypecast);
			}
			base.VisitCastExpression(castExpression);
		}

		void ForceSpacesAround(AstNode node, bool forceSpaces)
		{
			if (node.IsNull)
				return;
			ForceSpacesBefore(node, forceSpaces);
			ForceSpacesAfter(node, forceSpaces);
		}

		void FormatCommas(AstNode parent, bool before, bool after)
		{
			if (parent.IsNull) {
				return;
			}
			foreach (CSharpTokenNode comma in parent.Children.Where (node => node.Role == Roles.Comma)) {
				ForceSpacesAfter(comma, after);
				ForceSpacesBefore(comma, before);
			}
		}

		bool DoWrap(Wrapping wrapping, AstNode wrapNode, int argumentCount)
		{
			return wrapping == Wrapping.WrapAlways || 
				options.WrapLineLength > 0 && argumentCount > 1 && wrapping == Wrapping.WrapIfTooLong && wrapNode.StartLocation.Column >= options.WrapLineLength;
		}

		void FormatArguments(AstNode node)
		{
			Wrapping methodCallArgumentWrapping;
			NewLinePlacement newLineAferMethodCallOpenParentheses;
			bool doAlignToFirstArgument;
			NewLinePlacement methodClosingParenthesesOnNewLine;
			bool spaceWithinMethodCallParentheses;
			bool spaceWithinEmptyParentheses;
			bool spaceAfterMethodCallParameterComma;
			bool spaceBeforeMethodCallParameterComma;

			CSharpTokenNode rParToken, lParToken;
			List<AstNode> arguments;

			var constructorDeclaration = node as ConstructorDeclaration;
			if (constructorDeclaration != null) {
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinConstructorDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterConstructorDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeConstructorDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = constructorDeclaration.LParToken;
				rParToken = constructorDeclaration.RParToken;
				arguments = constructorDeclaration.Parameters.Cast<AstNode>().ToList();
			} else if (node is IndexerDeclaration) {
				var indexer = (IndexerDeclaration)node;
				methodCallArgumentWrapping = policy.IndexerDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferIndexerDeclarationOpenBracket;
				methodClosingParenthesesOnNewLine = policy.IndexerDeclarationClosingBracketOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstIndexerDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinIndexerDeclarationBracket;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterIndexerDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeIndexerDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = indexer.LBracketToken;
				rParToken = indexer.RBracketToken;
				arguments = indexer.Parameters.Cast<AstNode>().ToList();
			} else if (node is OperatorDeclaration) {
				var op = (OperatorDeclaration)node;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = op.LParToken;
				rParToken = op.RParToken;
				arguments = op.Parameters.Cast<AstNode>().ToList();
			} else if (node is MethodDeclaration) {
				var methodDeclaration = node as MethodDeclaration;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = methodDeclaration.LParToken;
				rParToken = methodDeclaration.RParToken;
				arguments = methodDeclaration.Parameters.Cast<AstNode>().ToList();
			} else if (node is IndexerExpression) {
				var indexer = (IndexerExpression)node;
				methodCallArgumentWrapping = policy.IndexerArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferIndexerOpenBracket;
				doAlignToFirstArgument = policy.AlignToFirstIndexerArgument;
				methodClosingParenthesesOnNewLine = policy.IndexerClosingBracketOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpacesWithinBrackets;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterBracketComma;
				spaceWithinEmptyParentheses = spaceWithinMethodCallParentheses;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeBracketComma;
				rParToken = indexer.RBracketToken;
				lParToken = indexer.LBracketToken;
				arguments = indexer.Arguments.Cast<AstNode>().ToList();
			} else if (node is ObjectCreateExpression) {
				var oce = node as ObjectCreateExpression;
				methodCallArgumentWrapping = policy.MethodCallArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodCallOpenParentheses;
				doAlignToFirstArgument = policy.AlignToFirstMethodCallArgument;
				methodClosingParenthesesOnNewLine = policy.MethodCallClosingParenthesesOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpacesWithinNewParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterNewParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeNewParameterComma;
				spaceWithinEmptyParentheses = policy.SpacesBetweenEmptyNewParentheses;

				rParToken = oce.RParToken;
				lParToken = oce.LParToken;
				arguments = oce.Arguments.Cast<AstNode>().ToList();
			} else if (node is Attribute) {
				var oce = node as Attribute;
				methodCallArgumentWrapping = policy.MethodCallArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodCallOpenParentheses;
				doAlignToFirstArgument = policy.AlignToFirstMethodCallArgument;
				methodClosingParenthesesOnNewLine = policy.MethodCallClosingParenthesesOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpacesWithinNewParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterNewParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeNewParameterComma;
				spaceWithinEmptyParentheses = policy.SpacesBetweenEmptyNewParentheses;

				rParToken = oce.RParToken;
				lParToken = oce.LParToken;
				arguments = oce.Arguments.Cast<AstNode>().ToList();
			} else if (node is LambdaExpression) {
				var methodDeclaration = node as LambdaExpression;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = methodDeclaration.LParToken;
				rParToken = methodDeclaration.RParToken;
				arguments = methodDeclaration.Parameters.Cast<AstNode>().ToList();
			} else if (node is AnonymousMethodExpression) {
				var methodDeclaration = node as AnonymousMethodExpression;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = methodDeclaration.LParToken;
				rParToken = methodDeclaration.RParToken;
				arguments = methodDeclaration.Parameters.Cast<AstNode>().ToList();
			}  else if (node is ConstructorInitializer) {
				var constructorInitializer = node as ConstructorInitializer;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodDeclarationParameter;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodDeclarationParentheses;
				lParToken = constructorInitializer.LParToken;
				rParToken = constructorInitializer.RParToken;
				arguments = constructorInitializer.Arguments.Cast<AstNode>().ToList();
			} else {
				InvocationExpression invocationExpression = node as InvocationExpression;
				methodCallArgumentWrapping = policy.MethodCallArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodCallOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodCallClosingParenthesesOnNewLine;
				doAlignToFirstArgument = policy.AlignToFirstMethodCallArgument;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodCallParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodCallParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodCallParameterComma;
				spaceWithinEmptyParentheses = policy.SpaceBetweenEmptyMethodCallParentheses;

				rParToken = invocationExpression.RParToken;
				lParToken = invocationExpression.LParToken;
				arguments = invocationExpression.Arguments.Cast<AstNode>().ToList();
			}

			if (formatter.FormattingMode == ICSharpCode.NRefactory.CSharp.FormattingMode.OnTheFly)
				methodCallArgumentWrapping = Wrapping.DoNotChange;
			int argumentStart = 1;
			var firstarg = arguments.FirstOrDefault();
			if (firstarg != null && firstarg.GetPrevNode().Role == Roles.NewLine) {
				doAlignToFirstArgument = false;
				argumentStart = 0;
			}
			bool wrapMethodCall = DoWrap(methodCallArgumentWrapping, rParToken, arguments.Count);
			if (wrapMethodCall && arguments.Any()) {
				if (ShouldBreakLine(newLineAferMethodCallOpenParentheses, lParToken)) {
					curIndent.Push(IndentType.Continuation);
					foreach (var arg in arguments) {
						FixStatementIndentation(arg.StartLocation);
						arg.AcceptVisitor(this);
					}
					curIndent.Pop();
				} else {
					if (!doAlignToFirstArgument) {
						curIndent.Push(IndentType.Continuation);
						foreach (var arg in arguments.Take (argumentStart)) {
							FixStatementIndentation(arg.StartLocation);
							arg.AcceptVisitor(this);
						}
						foreach (var arg in arguments.Skip (argumentStart)) {
							FixStatementIndentation(arg.StartLocation);
							arg.AcceptVisitor(this);
						}
						curIndent.Pop();
					} else {
						int extraSpaces = Math.Max(0, arguments.First().StartLocation.Column - 1 - curIndent.IndentString.Length);
						curIndent.ExtraSpaces += extraSpaces;
						foreach (var arg in arguments.Take (argumentStart)) {
							arg.AcceptVisitor(this);
						}
						foreach (var arg in arguments.Skip(argumentStart)) {
							FixStatementIndentation(arg.StartLocation);
							arg.AcceptVisitor(this);
						}
						curIndent.ExtraSpaces -= extraSpaces;
					}
				}

				if (!rParToken.IsNull) {
					if (ShouldBreakLine(methodClosingParenthesesOnNewLine, rParToken)) {
						FixStatementIndentation(rParToken.StartLocation);
					} else if (methodClosingParenthesesOnNewLine == NewLinePlacement.SameLine) {
						ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
					}
				}
			} else {

				foreach (var arg in arguments.Take (argumentStart)) {
					if (policy.IndentBlocksInsideExpressions)
						curIndent.Push(IndentType.Continuation);
					arg.AcceptVisitor(this);
					if (policy.IndentBlocksInsideExpressions)
						curIndent.Pop();
				}
				foreach (var arg in arguments.Skip(argumentStart)) {
					if (arg.GetPrevSibling(NoWhitespacePredicate) != null) {
						if (methodCallArgumentWrapping == Wrapping.DoNotWrap) {
							ForceSpacesBeforeRemoveNewLines(arg, spaceAfterMethodCallParameterComma && arg.GetPrevSibling(NoWhitespacePredicate).Role == Roles.Comma);
							if (policy.IndentBlocksInsideExpressions)
								curIndent.Push(IndentType.Continuation);
							arg.AcceptVisitor(this);
							if (policy.IndentBlocksInsideExpressions)
								curIndent.Pop();
						} else {
							if (!doAlignToFirstArgument && arg.PrevSibling.Role == Roles.NewLine) {
								curIndent.Push(IndentType.Continuation);
								FixStatementIndentation(arg.StartLocation);
								arg.AcceptVisitor(this);
								curIndent.Pop();
							} else {
								if (arg.PrevSibling.StartLocation.Line == arg.StartLocation.Line) {
									ForceSpacesBefore(arg, spaceAfterMethodCallParameterComma && arg.GetPrevSibling(NoWhitespacePredicate).Role == Roles.Comma);
									if (policy.IndentBlocksInsideExpressions)
										curIndent.Push(IndentType.Continuation);
									arg.AcceptVisitor(this);
									if (policy.IndentBlocksInsideExpressions)
										curIndent.Pop();
								} else {
									int extraSpaces = Math.Max(0, arguments.First().StartLocation.Column - 1 - curIndent.IndentString.Length);
									curIndent.ExtraSpaces += extraSpaces;
									FixStatementIndentation(arg.StartLocation);
									arg.AcceptVisitor(this);
									curIndent.ExtraSpaces -= extraSpaces;
								}
							}
						}
					} else {
						arg.AcceptVisitor(this);
					}
				}
				if (!rParToken.IsNull) {
					if (methodCallArgumentWrapping == Wrapping.DoNotWrap) {
						ForceSpacesBeforeRemoveNewLines(rParToken, arguments.Any() ? spaceWithinMethodCallParentheses : spaceWithinEmptyParentheses);
					} else {
						bool sameLine = rParToken.GetPrevNode(n => n.Role == Roles.Argument || n.Role == Roles.Parameter || n.Role == Roles.LPar || n.Role == Roles.Comma).EndLocation.Line == rParToken.StartLocation.Line;
						if (sameLine) {
							ForceSpacesBeforeRemoveNewLines(rParToken, arguments.Any() ? spaceWithinMethodCallParentheses : spaceWithinEmptyParentheses);
						} else {
							FixStatementIndentation(rParToken.StartLocation);
						}
					}
				}
			}
			if (!rParToken.IsNull) {
				foreach (CSharpTokenNode comma in rParToken.Parent.Children.Where(n => n.Role == Roles.Comma)) {
					ForceSpacesBefore(comma, spaceBeforeMethodCallParameterComma);
				}
			}
		}

		public override void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			if (!invocationExpression.Target.IsNull)
				invocationExpression.Target.AcceptVisitor(this);

			ForceSpacesBefore(invocationExpression.LParToken, policy.SpaceBeforeMethodCallParentheses);
			if (invocationExpression.Arguments.Any()) {
				ForceSpacesAfter(invocationExpression.LParToken, policy.SpaceWithinMethodCallParentheses);
			} else {
				ForceSpacesAfter(invocationExpression.LParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
				ForceSpacesBefore(invocationExpression.RParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
			}
			bool popIndent = false;
			if (invocationExpression.Target is MemberReferenceExpression) {
				var mt = (MemberReferenceExpression)invocationExpression.Target;
				if (mt.Target is InvocationExpression) {
					if (DoWrap(policy.ChainedMethodCallWrapping, mt.DotToken, 2)) {
						curIndent.Push(IndentType.Block);
						popIndent = true;
						FixStatementIndentation(mt.DotToken.StartLocation);
					} else {
						if (policy.ChainedMethodCallWrapping == Wrapping.DoNotWrap)
							ForceSpacesBeforeRemoveNewLines(mt.DotToken, false);
					}
				}
			}
			FormatArguments(invocationExpression);
			if (popIndent)
				curIndent.Pop();
		}

		public override void VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			ForceSpacesBeforeRemoveNewLines(indexerExpression.LBracketToken, policy.SpacesBeforeBrackets);
			ForceSpacesAfter(indexerExpression.LBracketToken, policy.SpacesWithinBrackets);

			if (!indexerExpression.Target.IsNull)
				indexerExpression.Target.AcceptVisitor(this);

			FormatArguments(indexerExpression);



		}

		public override void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			var lp = parenthesizedExpression.LParToken;
			var expr = parenthesizedExpression.Expression;
			int extraSpaces = 0;
			if (lp.StartLocation.Line == expr.StartLocation.Line) {
				ForceSpacesAfter(lp, policy.SpacesWithinParentheses);
			} else {
				extraSpaces += options.IndentSize;
				curIndent.ExtraSpaces += extraSpaces;
				FixIndentation(expr);
			}

			base.VisitParenthesizedExpression(parenthesizedExpression);

			var rp = parenthesizedExpression.RParToken;

			curIndent.ExtraSpaces -= extraSpaces;
			if (rp.StartLocation.Line == expr.EndLocation.Line) {
				ForceSpacesBefore(rp, policy.SpacesWithinParentheses);
			} else {
				FixIndentation(rp);
			}
		}

		public override void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			ForceSpacesBeforeRemoveNewLines(sizeOfExpression.LParToken, policy.SpaceBeforeSizeOfParentheses);
			ForceSpacesAfter(sizeOfExpression.LParToken, policy.SpacesWithinSizeOfParentheses);
			ForceSpacesBeforeRemoveNewLines(sizeOfExpression.RParToken, policy.SpacesWithinSizeOfParentheses);
			base.VisitSizeOfExpression(sizeOfExpression);
		}

		public override void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			ForceSpacesBeforeRemoveNewLines(typeOfExpression.LParToken, policy.SpaceBeforeTypeOfParentheses);
			ForceSpacesAfter(typeOfExpression.LParToken, policy.SpacesWithinTypeOfParentheses);
			ForceSpacesBeforeRemoveNewLines(typeOfExpression.RParToken, policy.SpacesWithinTypeOfParentheses);
			base.VisitTypeOfExpression(typeOfExpression);
		}

		public override void VisitCheckedExpression(CheckedExpression checkedExpression)
		{
			ForceSpacesAfter(checkedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBeforeRemoveNewLines(checkedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			base.VisitCheckedExpression(checkedExpression);
		}

		public override void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
		{
			ForceSpacesAfter(uncheckedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBeforeRemoveNewLines(uncheckedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			base.VisitUncheckedExpression(uncheckedExpression);
		}

		public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			ForceSpacesBeforeRemoveNewLines(objectCreateExpression.LParToken, policy.SpaceBeforeNewParentheses);

			if (objectCreateExpression.Arguments.Any()) {
				if (!objectCreateExpression.LParToken.IsNull)
					ForceSpacesAfter(objectCreateExpression.LParToken, policy.SpacesWithinNewParentheses);
			} else {
				if (!objectCreateExpression.LParToken.IsNull)
					ForceSpacesAfter(objectCreateExpression.LParToken, policy.SpacesBetweenEmptyNewParentheses);
			}

			if (!objectCreateExpression.Type.IsNull)
				objectCreateExpression.Type.AcceptVisitor(this);
			objectCreateExpression.Initializer.AcceptVisitor(this);
			FormatArguments(objectCreateExpression);
		}

		public override void VisitArrayCreateExpression(ArrayCreateExpression arrayObjectCreateExpression)
		{
			FormatCommas(arrayObjectCreateExpression, policy.SpaceBeforeMethodCallParameterComma, policy.SpaceAfterMethodCallParameterComma);
			base.VisitArrayCreateExpression(arrayObjectCreateExpression);
		}

		public override void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			var old = curIndent;
			curIndent = curIndent.Clone();
			curIndent.ExtraSpaces = 0;

			if (DoWrap(policy.ArrayInitializerWrapping, arrayInitializerExpression.RBraceToken, arrayInitializerExpression.Elements.Count)) {
				FixOpenBrace(policy.ArrayInitializerBraceStyle, arrayInitializerExpression.LBraceToken);
				curIndent.Push(IndentType.Block);
				foreach (var init in arrayInitializerExpression.Elements) {
					FixStatementIndentation(init.StartLocation);
					init.AcceptVisitor(this);
				}
				curIndent.Pop();
				FixClosingBrace(policy.ArrayInitializerBraceStyle, arrayInitializerExpression.RBraceToken);
			} else if (policy.ArrayInitializerWrapping == Wrapping.DoNotWrap) {
				ForceSpacesBeforeRemoveNewLines(arrayInitializerExpression.LBraceToken);
				foreach (var init in arrayInitializerExpression.Elements) {
					ForceSpacesBeforeRemoveNewLines(init);
					init.AcceptVisitor(this);
				}
				ForceSpacesBeforeRemoveNewLines(arrayInitializerExpression.RBraceToken);
			} else {
				var lBrace = arrayInitializerExpression.LBraceToken;
				var rBrace = arrayInitializerExpression.RBraceToken;

				foreach (var child in arrayInitializerExpression.Children) {
					if (child.Role == Roles.LBrace) {
						if (lBrace.StartLocation.Line == rBrace.StartLocation.Line && policy.AllowOneLinedArrayInitialziers) {
							ForceSpacesAfter(child, true);
						} else {
							FixOpenBrace(policy.ArrayInitializerBraceStyle, child);
						}
						curIndent.Push(IndentType.Block);
						continue;
					}
					if (child.Role == Roles.RBrace) {
						curIndent.Pop();
						if (lBrace.StartLocation.Line == rBrace.StartLocation.Line && policy.AllowOneLinedArrayInitialziers) {
							ForceSpaceBefore(child, true);

						} else {
							FixClosingBrace(policy.ArrayInitializerBraceStyle, child);
						}
						continue;
					}
					if (child.Role == Roles.Expression) {
						if (child.PrevSibling != null) {
							if (child.PrevSibling.Role == Roles.NewLine) {
								FixIndentation(child);
							}
							if (child.PrevSibling.Role == Roles.Comma) {
								ForceSpaceBefore(child, true);
							}
						}
						child.AcceptVisitor(this);
						if (child.NextSibling != null && child.NextSibling.Role == Roles.Comma)
							ForceSpacesAfter(child, false);
						continue;
					}

					child.AcceptVisitor(this);
				}
			}
			curIndent = old;
		}

		public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			var assignToken = parameterDeclaration.AssignToken;
			if (!assignToken.IsNull)
				ForceSpacesAround(assignToken, policy.SpaceAroundAssignment);
			base.VisitParameterDeclaration(parameterDeclaration);
		}

		public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			FormatArguments(lambdaExpression);
			ForceSpacesBeforeRemoveNewLines(lambdaExpression.ArrowToken, true);

			if (!lambdaExpression.Body.IsNull) {
				var body = lambdaExpression.Body as BlockStatement;
				if (body != null) {
					var old = curIndent;
					this.curIndent = curIndent.GetIndentWithoutSpace ();
					FixOpenBrace(policy.AnonymousMethodBraceStyle, body.LBraceToken);
					VisitBlockWithoutFixingBraces(body, policy.IndentMethodBody);
					FixClosingBrace(policy.AnonymousMethodBraceStyle, body.RBraceToken);
					curIndent = old;
				} else {
					ForceSpacesAfter(lambdaExpression.ArrowToken, true);
					lambdaExpression.Body.AcceptVisitor(this);
				}
			}
		}

		public override void VisitNamedExpression(NamedExpression namedExpression)
		{
			ForceSpacesAround(namedExpression.AssignToken, policy.SpaceAroundAssignment);
			base.VisitNamedExpression(namedExpression);
		}

		public override void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			ForceSpacesAfter(namedArgumentExpression.ColonToken, policy.SpaceInNamedArgumentAfterDoubleColon);

			base.VisitNamedArgumentExpression(namedArgumentExpression);
		}

		public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			var dot = memberReferenceExpression.DotToken;
			if (dot.PrevSibling.EndLocation.Line == dot.StartLocation.Line)
				ForceSpacesBefore(dot, false);
			ForceSpacesAfter(dot, false);
			base.VisitMemberReferenceExpression(memberReferenceExpression);
		}

		public override void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			ForceSpacesAround(pointerReferenceExpression.ArrowToken, policy.SpaceAroundUnsafeArrowOperator);
			base.VisitPointerReferenceExpression(pointerReferenceExpression);
		}

		public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			base.VisitUnaryOperatorExpression(unaryOperatorExpression);
			switch (unaryOperatorExpression.Operator) {
				case UnaryOperatorType.Any:
					break;
				case UnaryOperatorType.Not:
				case UnaryOperatorType.BitNot:
				case UnaryOperatorType.Minus:
				case UnaryOperatorType.Plus:
				case UnaryOperatorType.Increment:
				case UnaryOperatorType.Decrement:
					ForceSpacesBeforeRemoveNewLines(unaryOperatorExpression.Expression, false);
					break;
				case UnaryOperatorType.PostIncrement:
				case UnaryOperatorType.PostDecrement:
					ForceSpacesBeforeRemoveNewLines(unaryOperatorExpression.OperatorToken, false);
					break;
				case UnaryOperatorType.Dereference:
					ForceSpacesAfter(unaryOperatorExpression.OperatorToken, policy.SpaceAfterUnsafeAsteriskOfOperator);
					break;
				case UnaryOperatorType.AddressOf:
					ForceSpacesAfter(unaryOperatorExpression.OperatorToken, policy.SpaceAfterUnsafeAddressOfOperator);
					break;
				case UnaryOperatorType.Await:
					ForceSpacesBeforeRemoveNewLines(unaryOperatorExpression.Expression, true);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}

