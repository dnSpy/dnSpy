' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports dnSpy.Roslyn.Internal.SmartIndent
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.dnSpy.Roslyn.VisualBasic.Internal.SmartIndent
	<ExportLanguageService(GetType(ISynchronousIndentationService), LanguageNames.VisualBasic), [Shared]>
	Partial Friend Class VisualBasicIndentationService
		Inherits AbstractIndentationService

		Private Shared ReadOnly s_instance As IFormattingRule = New SpecialFormattingRule()

		Protected Overrides Function GetSpecializedIndentationFormattingRule() As IFormattingRule
			Return s_instance
		End Function

		Protected Overrides Function GetIndenter(syntaxFacts As ISyntaxFactsService,
												 syntaxTree As SyntaxTree,
												 lineToBeIndented As TextLine,
												 formattingRules As IEnumerable(Of IFormattingRule),
												 optionSet As OptionSet,
												 cancellationToken As CancellationToken) As AbstractIndenter
			Return New Indenter(syntaxFacts, syntaxTree, formattingRules, optionSet, lineToBeIndented, cancellationToken)
		End Function

		Protected Overrides Function ShouldUseSmartTokenFormatterInsteadOfIndenter(formattingRules As IEnumerable(Of IFormattingRule),
																				   root As SyntaxNode,
																				   line As TextLine,
																				   optionSet As OptionSet,
																				   cancellationToken As CancellationToken) As Boolean
			Return ShouldUseSmartTokenFormatterInsteadOfIndenter(formattingRules, DirectCast(root, CompilationUnitSyntax), line, optionSet, cancellationToken)
		End Function

		Public Overloads Shared Function ShouldUseSmartTokenFormatterInsteadOfIndenter(
				formattingRules As IEnumerable(Of IFormattingRule),
				root As CompilationUnitSyntax,
				line As TextLine,
				optionSet As OptionSet,
				CancellationToken As CancellationToken,
				Optional neverUseWhenHavingMissingToken As Boolean = True) As Boolean

			' find first text on line
			Dim firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition()
			If Not firstNonWhitespacePosition.HasValue Then
				Return False
			End If

			' enter on token only works when first token on line is first text on line
			Dim token = root.FindToken(firstNonWhitespacePosition.Value)
			If token.Kind = SyntaxKind.None OrElse token.SpanStart <> firstNonWhitespacePosition Then
				Return False
			End If

			' now try to gather various token information to see whether we are at an applicable position.
			' all these are heuristic based
			' 
			' we need at least current and previous tokens to ask about existing line break formatting rules 
			Dim previousToken = token.GetPreviousToken(includeZeroWidth:=True)

			' only use smart token formatter when we have at least two visible tokens.
			If previousToken.Kind = SyntaxKind.None Then
				Return False
			End If

			' check special case 
			' if previous token (or one before previous token if the previous token is statement terminator token) is missing, make sure
			' we are a first token of a statement
			If previousToken.IsMissing AndAlso neverUseWhenHavingMissingToken Then
				Return False
			ElseIf previousToken.IsMissing Then
				Dim statement = token.GetAncestor(Of StatementSyntax)()
				If statement Is Nothing Then
					Return False
				End If

				' check whether current token is first token of a statement
				Return statement.GetFirstToken() = token
			End If

			' now, regular case. ask formatting rule to see whether we should use token formatter or not
			Dim lineOperation = FormattingOperations.GetAdjustNewLinesOperation(formattingRules, previousToken, token, optionSet)
			If lineOperation IsNot Nothing AndAlso lineOperation.Option <> AdjustNewLinesOption.ForceLinesIfOnSingleLine Then
				Return True
			End If

			' check whether there is an alignment operation
			Dim startNode = token.Parent

			Dim currentNode = startNode
			Do While currentNode IsNot Nothing
				Dim operations = FormattingOperations.GetAlignTokensOperations(
					formattingRules, currentNode, lastToken:=Nothing, optionSet:=optionSet)

				If Not operations.Any() Then
					currentNode = currentNode.Parent
					Continue Do
				End If

				' make sure we have the given token as one of tokens to be aligned to the base token
				Dim match = operations.FirstOrDefault(Function(o) o.Tokens.Contains(token))
				If match IsNot Nothing Then
					Return True
				End If

				currentNode = currentNode.Parent
			Loop

			' no indentation operation, nothing to do for smart token formatter
			Return False
		End Function
	End Class
End Namespace
