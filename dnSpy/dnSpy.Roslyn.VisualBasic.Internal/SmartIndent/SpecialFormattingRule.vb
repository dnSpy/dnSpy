' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.dnSpy.Roslyn.VisualBasic.Internal.SmartIndent

	Friend Class SpecialFormattingRule
		Inherits AbstractFormattingRule

		Public Sub New()
		End Sub

		Public Overrides Sub AddSuppressOperations(list As List(Of SuppressOperation), node As SyntaxNode, lastToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextAction(Of SuppressOperation))
			' don't suppress anything
		End Sub

		Public Overrides Function GetAdjustNewLinesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation

			' unlike regular one. force position of attribute
			Dim attributeNode = TryCast(previousToken.Parent, AttributeListSyntax)
			If attributeNode IsNot Nothing AndAlso attributeNode.GreaterThanToken = previousToken Then
				Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
			End If

			' no line operation. no line changes what so ever
			Dim lineOperation = MyBase.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, nextOperation)
			If lineOperation IsNot Nothing Then
				' basically means don't ever put new line if there isn't already one, but do
				' indentation.
				Return FormattingOperations.CreateAdjustNewLinesOperation(line:=0, option:=AdjustNewLinesOption.PreserveLines)
			End If

			Return Nothing
		End Function

		Public Overrides Function GetAdjustSpacesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustSpacesOperation)) As AdjustSpacesOperation
			Dim spaceOperation = MyBase.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, nextOperation)

			' if there is force space operation, convert it to ForceSpaceIfSingleLine operation.
			' (force space basically means remove all line breaks)
			If spaceOperation IsNot Nothing AndAlso spaceOperation.Option = AdjustSpacesOption.ForceSpaces Then
				Return FormattingOperations.CreateAdjustSpacesOperation(spaceOperation.Space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
			End If

			Return spaceOperation
		End Function

		Public Overrides Sub AddIndentBlockOperations(list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of IndentBlockOperation))
			nextOperation.Invoke(list)

			Dim singleLineLambdaFunction = TryCast(node, SingleLineLambdaExpressionSyntax)
			If singleLineLambdaFunction IsNot Nothing Then
				Dim baseToken = node.GetFirstToken(includeZeroWidth:=True)
				Dim endToken = node.GetLastToken()
				Dim nextToken = endToken.GetNextToken(includeZeroWidth:=True)

				If SyntaxFacts.AllowsTrailingImplicitLineContinuation(endToken) OrElse
				   SyntaxFacts.AllowsLeadingImplicitLineContinuation(nextToken) OrElse
				   endToken.TrailingTrivia.Any(SyntaxKind.LineContinuationTrivia) Then
					Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)
					list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
								baseToken, startToken, endToken,
								TextSpan.FromBounds(startToken.FullSpan.Start, node.FullSpan.End), indentationDelta:=1, [option]:=IndentBlockOption.RelativePosition))
				End If

				Return
			End If

			AddIndentBlockOperations(Of ParameterListSyntax)(list, node, Function(n) Not n.OpenParenToken.IsMissing AndAlso n.Parameters.Count > 0)
			AddIndentBlockOperations(Of ArgumentListSyntax)(list, node, Function(n) Not n.OpenParenToken.IsMissing AndAlso n.Arguments.Count > 0 AndAlso n.Arguments.Any(Function(a) Not a.IsMissing))
			AddIndentBlockOperations(Of TypeParameterListSyntax)(list, node, Function(n) Not n.OpenParenToken.IsMissing AndAlso n.Parameters.Count > 0, indentationDelta:=1)
		End Sub

		Private Overloads Sub AddIndentBlockOperations(Of T As SyntaxNode)(list As List(Of IndentBlockOperation), node As SyntaxNode, predicate As Func(Of T, Boolean), Optional indentationDelta As Integer = 0)
			Dim parameterOrArgumentList = TryCast(node, T)
			If parameterOrArgumentList Is Nothing Then
				Return
			End If

			If Not predicate(parameterOrArgumentList) Then
				Return
			End If

			AddIndentBlockOperations(list, parameterOrArgumentList, indentationDelta)
		End Sub

		Private Overloads Sub AddIndentBlockOperations(list As List(Of IndentBlockOperation), parameterOrArgumentList As SyntaxNode, indentationDelta As Integer)
			Dim openBrace = parameterOrArgumentList.GetFirstToken(includeZeroWidth:=True)
			Dim closeBrace = parameterOrArgumentList.GetLastToken(includeZeroWidth:=True)

			' first token of first argument (first token of the node should be "open brace")
			Dim baseToken = openBrace.GetNextToken(includeZeroWidth:=True)

			' indent block start token
			Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)

			' last token of last argument (last token of the node should be "close brace")
			Dim endToken = closeBrace.GetPreviousToken(includeZeroWidth:=True)

			list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
					baseToken, startToken, endToken, TextSpan.FromBounds(baseToken.Span.End, closeBrace.Span.End), indentationDelta, IndentBlockOption.RelativePosition))
		End Sub

		Public Overrides Sub AddAlignTokensOperations(operations As List(Of AlignTokensOperation), node As SyntaxNode, optionSet As OptionSet, nextAction As NextAction(Of AlignTokensOperation))
			MyBase.AddAlignTokensOperations(operations, node, optionSet, nextAction)

			' Smart token formatting off: No token alignment
			If optionSet.GetOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic) <> FormattingOptions.IndentStyle.Smart Then
				Return
			End If

			AddAlignTokensOperations(Of ParameterListSyntax)(operations, node, Function(n) n.OpenParenToken)
			AddAlignTokensOperations(Of TypeParameterListSyntax)(operations, node, Function(n) n.OpenParenToken)
			AddAlignTokensOperations(Of ArrayRankSpecifierSyntax)(operations, node, Function(n) n.OpenParenToken)

			AddCaseClauseAlignTokensOperations(operations, node)
		End Sub

		Private Sub AddCaseClauseAlignTokensOperations(operations As List(Of AlignTokensOperation), node As SyntaxNode)
			Dim caseStatement = TryCast(node, CaseStatementSyntax)
			If caseStatement Is Nothing OrElse caseStatement.Cases.Count = 0 Then
				Return
			End If

			Dim cases = caseStatement.Cases.OfType(Of SimpleCaseClauseSyntax).ToList()
			If cases.Count < 2 Then
				Return
			End If

			operations.Add(FormattingOperations.CreateAlignTokensOperation(
						   cases(0).GetFirstToken(includeZeroWidth:=True),
						   cases.Skip(1).Select(Function(n) n.GetFirstToken(includeZeroWidth:=True)),
						   AlignTokensOption.AlignIndentationOfTokensToBaseToken))
		End Sub

		Private Overloads Sub AddAlignTokensOperations(Of T As SyntaxNode)(operations As List(Of AlignTokensOperation), node As SyntaxNode, baseTokenGetter As Func(Of T, SyntaxToken))
			Dim parameterList = TryCast(node, T)
			If parameterList Is Nothing Then
				Return
			End If

			Dim baseToken = baseTokenGetter(parameterList)
			If baseToken.IsMissing Then
				Return
			End If

			AddAlignTokensOperations(operations, baseToken)
		End Sub

		Private Overloads Sub AddAlignTokensOperations(operations As List(Of AlignTokensOperation), baseToken As SyntaxToken)
			operations.Add(FormattingOperations.CreateAlignTokensOperation(
						   baseToken,
						   New SyntaxToken() {baseToken.GetNextToken(includeZeroWidth:=True)},
						   AlignTokensOption.AlignIndentationOfTokensToBaseToken))
		End Sub
	End Class
End Namespace
