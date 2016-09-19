' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports dnSpy.Roslyn.EditorFeatures.Editor
Imports dnSpy.Roslyn.EditorFeatures.Host
Imports dnSpy.Roslyn.EditorFeatures.TextStructureNavigation
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Global.Microsoft.CodeAnalysis.Editor.VisualBasic.TextStructureNavigation

	<Export(GetType(ITextStructureNavigatorProvider))>
	<ContentType(ContentTypeNames.VisualBasicContentType)>
	Friend Class TextStructureNavigatorProvider
		Inherits AbstractTextStructureNavigatorProvider

		<ImportingConstructor()>
		Friend Sub New(
			selectorService As ITextStructureNavigatorSelectorService,
			contentTypeService As IContentTypeRegistryService,
			waitIndicator As IWaitIndicator)
			MyBase.New(selectorService, contentTypeService, waitIndicator)
		End Sub

		Protected Overrides Function ShouldSelectEntireTriviaFromStart(trivia As SyntaxTrivia) As Boolean
			Return trivia.Kind() = SyntaxKind.CommentTrivia
		End Function

		Protected Overrides Function IsWithinNaturalLanguage(token As SyntaxToken, position As Integer) As Boolean
			Select Case token.Kind
				Case SyntaxKind.StringLiteralToken
					' This, in combination with the override of GetExtentOfWordFromToken() below, treats the closing
					' quote as a separate token.  This maintains behavior with VS2013.
					If position = token.Span.End - 1 AndAlso token.Text.EndsWith("""", StringComparison.Ordinal) Then
						Return False
					End If

					Return True

				Case SyntaxKind.CharacterLiteralToken
					' Before the opening quote is considered outside the character
					If position = token.SpanStart Then
						Return False
					End If

					Return True

				Case SyntaxKind.InterpolatedStringTextToken,
					 SyntaxKind.XmlTextLiteralToken
					Return True
			End Select

			Return False
		End Function

		Protected Overrides Function GetExtentOfWordFromToken(token As SyntaxToken, position As SnapshotPoint) As TextExtent
			If token.Kind() = SyntaxKind.StringLiteralToken AndAlso position.Position = token.Span.End - 1 AndAlso token.Text.EndsWith("""", StringComparison.Ordinal) Then
				' Special case to treat the closing quote of a string literal as a separate token.  This allows the
				' cursor to stop during word navigation (Ctrl+LeftArrow, etc.) immediately before AND after the
				' closing quote, just like it did in VS2013 and like it currently does for interpolated strings.
				Dim Span = New Span(position.Position, 1)
				Return New TextExtent(New SnapshotSpan(position.Snapshot, Span), isSignificant:=True)
			Else
				Return MyBase.GetExtentOfWordFromToken(token, position)
			End If
		End Function
	End Class
End Namespace
