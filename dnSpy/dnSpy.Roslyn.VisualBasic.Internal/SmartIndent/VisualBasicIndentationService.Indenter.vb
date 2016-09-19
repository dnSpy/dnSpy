' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports dnSpy.Roslyn.Internal
Imports dnSpy.Roslyn.Internal.SmartIndent
Imports dnSpy.Roslyn.Internal.SmartIndent.AbstractIndentationService
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.dnSpy.Roslyn.VisualBasic.Internal.SmartIndent
	Partial Friend Class VisualBasicIndentationService
		Private Class Indenter
			Inherits AbstractIndenter

			Public Sub New(syntaxFacts As ISyntaxFactsService,
						   syntaxTree As SyntaxTree,
						   rules As IEnumerable(Of IFormattingRule),
						   optionSet As OptionSet,
						   line As TextLine,
						   cancellationToken As CancellationToken)
				MyBase.New(syntaxFacts, syntaxTree, rules, optionSet, line, cancellationToken)
			End Sub

			Public Overrides Function GetDesiredIndentation() As IndentationResult?
				Dim indentStyle = OptionSet.GetOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic)
				If indentStyle = FormattingOptions.IndentStyle.None Then
					Return Nothing
				End If

				Dim previousLine = GetPreviousNonBlankOrPreprocessorLine()

				' first token in the file
				If previousLine Is Nothing Then
					Return IndentFromStartOfLine(0)
				End If

				Dim lastNonWhitespacePosition = previousLine.Value.GetLastNonWhitespacePosition()
				If Not lastNonWhitespacePosition.HasValue Then
					Return Nothing
				End If

				Dim token = Tree.GetRoot(CancellationToken).FindToken(lastNonWhitespacePosition.Value)
				If token.Kind = SyntaxKind.None OrElse
					indentStyle = FormattingOptions.IndentStyle.Block Then

					Return GetIndentationOfLine(previousLine.Value)
				End If

				If token.Span.End = lastNonWhitespacePosition.Value + 1 Then
					Return GetIndentationBasedOnToken(token)
				Else
					'Contract.Assert(token.FullSpan.Contains(lastNonWhitespacePosition.Value))

					Dim trivia = Tree.GetRoot(CancellationToken).FindTrivia(lastNonWhitespacePosition.Value)

					' preserve the indentation of the comment trivia before a case statement
					If trivia.Kind = SyntaxKind.CommentTrivia AndAlso trivia.Token.IsKind(SyntaxKind.CaseKeyword) AndAlso trivia.Token.Parent.IsKind(SyntaxKind.CaseStatement) Then
						Return GetIndentationOfLine(previousLine.Value)
					End If

					If trivia.Kind = SyntaxKind.LineContinuationTrivia OrElse trivia.Kind = SyntaxKind.CommentTrivia Then
						Return GetIndentationBasedOnToken(GetTokenOnLeft(trivia), trivia)
					End If

					' if we are at invalid token (skipped token) at the end of statement, treat it like we are after line continuation
					If trivia.Kind = SyntaxKind.SkippedTokensTrivia AndAlso trivia.Token.IsLastTokenOfStatement() Then
						Return GetIndentationBasedOnToken(GetTokenOnLeft(trivia), trivia)
					End If

					' okay, now check whether the trivia is at the beginning of the line
					Dim firstNonWhitespacePosition = previousLine.Value.GetFirstNonWhitespacePosition()
					If Not firstNonWhitespacePosition.HasValue Then
						Return IndentFromStartOfLine(0)
					End If

					Dim firstTokenOnLine = Tree.GetRoot(CancellationToken).FindToken(firstNonWhitespacePosition.Value, findInsideTrivia:=True)
					If firstTokenOnLine.Kind <> SyntaxKind.None AndAlso firstTokenOnLine.Span.Contains(firstNonWhitespacePosition.Value) Then
						'okay, beginning of the line is not trivia, use this token as the base token
						Return GetIndentationBasedOnToken(firstTokenOnLine)
					End If

					Return GetIndentationOfLine(previousLine.Value)
				End If
			End Function

			Private Function GetTokenOnLeft(trivia As SyntaxTrivia) As SyntaxToken
				Dim token = trivia.Token
				If token.Span.End <= trivia.SpanStart AndAlso Not token.IsMissing Then
					Return token
				End If

				Return token.GetPreviousToken()
			End Function

			Protected Overrides Function HasPreprocessorCharacter(currentLine As TextLine) As Boolean
				Dim text = currentLine.ToString()
				'Contract.Assert(String.IsNullOrWhiteSpace(text) = False)

				Dim trimmedText = text.Trim()

				'Contract.Assert(SyntaxFacts.GetText(SyntaxKind.HashToken).Length = 1)
				Return trimmedText(0) = SyntaxFacts.GetText(SyntaxKind.HashToken)(0)
			End Function

			Private Function GetIndentationBasedOnToken(token As SyntaxToken, Optional trivia As SyntaxTrivia = Nothing) As IndentationResult?
				Dim sourceText = LineToBeIndented.Text

				Dim position = GetCurrentPositionNotBelongToEndOfFileToken(LineToBeIndented.Start)

				' lines must be blank since we got the token from the first non blank line above current position
				If HasLinesBetween(Tree.GetText().Lines.IndexOf(token.Span.End), LineToBeIndented.LineNumber) Then
					' if there are blank lines between, return indentation of the owning statement
					Return GetIndentationOfCurrentPosition(token, position)
				End If

				Dim indentation = GetIndentationFromOperationService(token, position)
				If indentation.HasValue Then
					Return indentation.Value
				End If

				Dim queryNode = token.GetAncestor(Of QueryClauseSyntax)()
				If queryNode IsNot Nothing Then
					Dim subQuerySpaces = If(token.IsLastTokenOfStatement(), 0, Me.OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language))
					Return GetIndentationOfToken(queryNode.GetFirstToken(includeZeroWidth:=True), subQuerySpaces)
				End If

				' check one more time for query case
				If token.Kind = SyntaxKind.IdentifierToken AndAlso token.HasMatchingText(SyntaxKind.FromKeyword) Then
					Return GetIndentationOfToken(token)
				End If

				If FormattingHelpers.IsXmlTokenInXmlDeclaration(token) Then
					Dim xmlDocument = token.GetAncestor(Of XmlDocumentSyntax)()
					Return GetIndentationOfToken(xmlDocument.GetFirstToken(includeZeroWidth:=True))
				End If

				' implicit line continuation case
				If IsLineContinuable(token, trivia, position) Then
					Return GetIndentationFromTokenLineAfterLineContinuation(token, trivia)
				End If

				Return GetIndentationOfCurrentPosition(token, position)
			End Function

			Private Function GetIndentationOfCurrentPosition(token As SyntaxToken, position As Integer) As IndentationResult
				Return GetIndentationOfCurrentPosition(token, position, extraSpaces:=0)
			End Function

			Private Function GetIndentationOfCurrentPosition(token As SyntaxToken, position As Integer, extraSpaces As Integer) As IndentationResult
				' special case for multi-line string
				Dim containingToken = Tree.FindTokenOnLeftOfPosition(position, CancellationToken)
				If containingToken.IsKind(SyntaxKind.InterpolatedStringTextToken) OrElse
				   containingToken.IsKind(SyntaxKind.InterpolatedStringText) OrElse
					(containingToken.IsKind(SyntaxKind.CloseBraceToken) AndAlso token.Parent.IsKind(SyntaxKind.Interpolation)) Then
					Return IndentFromStartOfLine(0)
				End If
				If containingToken.Kind = SyntaxKind.StringLiteralToken AndAlso containingToken.FullSpan.Contains(position) Then
					Return IndentFromStartOfLine(0)
				End If

				Return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, extraSpaces, CancellationToken))
			End Function

			Private Function IsLineContinuable(lastVisibleTokenOnPreviousLine As SyntaxToken, trivia As SyntaxTrivia, position As Integer) As Boolean
				If trivia.Kind = SyntaxKind.LineContinuationTrivia OrElse
				   trivia.Kind = SyntaxKind.SkippedTokensTrivia Then
					Return True
				End If

				If lastVisibleTokenOnPreviousLine.IsLastTokenOfStatement() Then
					Return False
				End If

				Dim visibleTokenOnCurrentLine As SyntaxToken = lastVisibleTokenOnPreviousLine.GetNextToken()
				If Not lastVisibleTokenOnPreviousLine.IsKind(SyntaxKind.OpenBraceToken) AndAlso
					Not lastVisibleTokenOnPreviousLine.IsKind(SyntaxKind.CommaToken) Then
					If IsCloseBraceOfInitializerSyntax(visibleTokenOnCurrentLine) Then
						Return False
					End If
				Else
					If IsCloseBraceOfInitializerSyntax(visibleTokenOnCurrentLine) Then
						Return True
					End If
				End If

				If Not ContainingStatementHasDiagnostic(lastVisibleTokenOnPreviousLine.Parent) Then
					Return True
				End If

				If lastVisibleTokenOnPreviousLine.GetNextToken(includeZeroWidth:=True).IsMissing Then
					Return True
				End If

				Return False
			End Function

			Private Function IsCloseBraceOfInitializerSyntax(visibleTokenOnCurrentLine As SyntaxToken) As Boolean
				If visibleTokenOnCurrentLine.IsKind(SyntaxKind.CloseBraceToken) Then
					Dim visibleTokenOnCurrentLineParent = visibleTokenOnCurrentLine.Parent
					If TypeOf visibleTokenOnCurrentLineParent Is ObjectCreationInitializerSyntax OrElse
					TypeOf visibleTokenOnCurrentLineParent Is CollectionInitializerSyntax Then
						Return True
					End If
				End If

				Return False
			End Function

			Private Function ContainingStatementHasDiagnostic(node As SyntaxNode) As Boolean
				If node Is Nothing Then
					Return False
				End If

				If node.ContainsDiagnostics Then
					Return True
				End If

				Dim containingStatement = node.GetAncestorOrThis(Of StatementSyntax)()
				If containingStatement Is Nothing Then
					Return False
				End If

				Return containingStatement.ContainsDiagnostics()
			End Function

			Private Function GetIndentationFromOperationService(token As SyntaxToken, position As Integer) As IndentationResult?
				' check operation service to see whether we can determine indentation from it
				If token.Kind = SyntaxKind.None Then
					Return Nothing
				End If

				Dim indentation = Finder.FromIndentBlockOperations(Tree, token, position, CancellationToken)
				If indentation.HasValue Then
					Return IndentFromStartOfLine(indentation.Value)
				End If

				' special case xml text literal before checking alignment operation
				' VB has different behavior around missing alignment token. for query expression, VB prefers putting
				' caret aligned with previous query clause, but for xml literals, it prefer them to be ignored and indented
				' based on current indentation level.
				If token.Kind = SyntaxKind.XmlTextLiteralToken OrElse
				   token.Kind = SyntaxKind.XmlEntityLiteralToken Then
					Return GetIndentationOfLine(LineToBeIndented.Text.Lines.GetLineFromPosition(token.SpanStart))
				End If

				' check alignment token indentation
				Dim alignmentTokenIndentation = Finder.FromAlignTokensOperations(Tree, token)
				If alignmentTokenIndentation.HasValue Then
					Return IndentFromStartOfLine(alignmentTokenIndentation.Value)
				End If

				Return Nothing
			End Function

			Private Function GetIndentationFromTokenLineAfterLineContinuation(token As SyntaxToken, trivia As SyntaxTrivia) As IndentationResult
				Dim sourceText = LineToBeIndented.Text
				Dim position = LineToBeIndented.Start

				position = GetCurrentPositionNotBelongToEndOfFileToken(position)

				Dim currentTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart)

				' error case where the line continuation belongs to a meaningless token such as empty token for skipped text
				If token.Kind = SyntaxKind.EmptyToken Then
					Dim baseLine = sourceText.Lines.GetLineFromPosition(trivia.SpanStart)
					Return GetIndentationOfLine(baseLine)
				End If

				Dim xmlEmbeddedExpression = token.GetAncestor(Of XmlEmbeddedExpressionSyntax)()
				If xmlEmbeddedExpression IsNot Nothing Then
					Dim firstExpressionLine = sourceText.Lines.GetLineFromPosition(xmlEmbeddedExpression.GetFirstToken(includeZeroWidth:=True).SpanStart)
					Return GetIndentationFromTwoLines(firstExpressionLine, currentTokenLine, token, position)
				End If

				If FormattingHelpers.IsGreaterThanInAttribute(token) Then
					Dim attribute = token.GetAncestor(Of AttributeListSyntax)()
					Dim baseLine = sourceText.Lines.GetLineFromPosition(attribute.GetFirstToken(includeZeroWidth:=True).SpanStart)
					Return GetIndentationOfLine(baseLine)
				End If

				' if position is between "," and next token, consider the position to be belonged to the list that
				' owns the ","
				If IsCommaInParameters(token) AndAlso (token.Span.End <= position AndAlso position <= token.GetNextToken().SpanStart) Then
					Return GetIndentationOfCurrentPosition(token, token.SpanStart)
				End If

				Dim statement = token.GetAncestor(Of StatementSyntax)()

				' this can happen if only token in the file is End Of File Token
				If statement Is Nothing Then
					If trivia.Kind <> SyntaxKind.None Then
						Dim triviaLine = sourceText.Lines.GetLineFromPosition(trivia.SpanStart)
						Return GetIndentationOfLine(triviaLine, Me.OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language))
					End If

					' no base line to use to calculate the indentation
					Return IndentFromStartOfLine(0)
				End If

				' find line where first token of statement is starting on
				Dim firstTokenLine = sourceText.Lines.GetLineFromPosition(statement.GetFirstToken(includeZeroWidth:=True).SpanStart)
				Return GetIndentationFromTwoLines(firstTokenLine, currentTokenLine, token, position)
			End Function

			Private Function IsCommaInParameters(token As SyntaxToken) As Boolean
				Return token.Kind = SyntaxKind.CommaToken AndAlso
					(TypeOf token.Parent Is ParameterListSyntax OrElse
					 TypeOf token.Parent Is ArgumentListSyntax OrElse
					 TypeOf token.Parent Is TypeParameterListSyntax)
			End Function

			Private Function GetIndentationFromTwoLines(firstLine As TextLine, secondLine As TextLine, token As SyntaxToken, position As Integer) As IndentationResult
				If firstLine.LineNumber = secondLine.LineNumber Then
					' things are on same line, put the indentation size
					Return GetIndentationOfCurrentPosition(token, position, Me.OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language))
				End If

				' multiline
				Return GetIndentationOfLine(secondLine)
			End Function

			Private Function HasLinesBetween(lineNumber1 As Integer, lineNumber2 As Integer) As Boolean
				Return lineNumber1 + 1 < lineNumber2
			End Function

			Private Function GetCurrentPositionNotBelongToEndOfFileToken(position As Integer) As Integer
				If Not Tree.HasCompilationUnitRoot Then
					Return position
				End If

				Dim compilationUnit = DirectCast(Tree.GetRoot(CancellationToken), CompilationUnitSyntax)
				Debug.Assert(compilationUnit IsNot Nothing)

				Return Math.Min(compilationUnit.EndOfFileToken.FullSpan.Start, position)
			End Function
		End Class
	End Class
End Namespace
