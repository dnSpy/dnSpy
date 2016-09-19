// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal.SmartIndent.CSharp
{
    internal partial class CSharpIndentationService
    {
        internal class Indenter : AbstractIndenter
        {
            public Indenter(
                ISyntaxFactsService syntaxFacts,
                SyntaxTree syntaxTree,
                IEnumerable<IFormattingRule> rules,
                OptionSet optionSet,
                TextLine line,
                CancellationToken cancellationToken) :
                base(syntaxFacts, syntaxTree, rules, optionSet, line, cancellationToken)
            {
            }

            public override IndentationResult? GetDesiredIndentation()
            {
                var indentStyle = OptionSet.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);

                if (indentStyle == FormattingOptions.IndentStyle.None)
                {
                    return null;
                }

                // find previous line that is not blank
                var previousLine = GetPreviousNonBlankOrPreprocessorLine();

                // it is beginning of the file, there is no previous line exists. 
                // in that case, indentation 0 is our base indentation.
                if (previousLine == null)
                {
                    return IndentFromStartOfLine(0);
                }

                // okay, now see whether previous line has anything meaningful
                var lastNonWhitespacePosition = previousLine.Value.GetLastNonWhitespacePosition();
                if (!lastNonWhitespacePosition.HasValue)
                {
                    return null;
                }

                // there is known parameter list "," parse bug. if previous token is "," from parameter list,
                // FindToken will not be able to find them.
                var token = Tree.GetRoot(CancellationToken).FindToken(lastNonWhitespacePosition.Value);
                if (token.IsKind(SyntaxKind.None) || indentStyle == FormattingOptions.IndentStyle.Block)
                {
                    return GetIndentationOfLine(previousLine.Value);
                }

                // okay, now check whether the text we found is trivia or actual token.
                if (token.Span.Contains(lastNonWhitespacePosition.Value))
                {
                    // okay, it is a token case, do special work based on type of last token on previous line
                    return GetIndentationBasedOnToken(token);
                }
                else
                {
                    // there must be trivia that contains or touch this position
                    Contract.Assert(token.FullSpan.Contains(lastNonWhitespacePosition.Value));

                    // okay, now check whether the trivia is at the beginning of the line
                    var firstNonWhitespacePosition = previousLine.Value.GetFirstNonWhitespacePosition();
                    if (!firstNonWhitespacePosition.HasValue)
                    {
                        return IndentFromStartOfLine(0);
                    }

                    var trivia = Tree.GetRoot(CancellationToken).FindTrivia(firstNonWhitespacePosition.Value, findInsideTrivia: true);
                    if (trivia.Kind() == SyntaxKind.None || this.LineToBeIndented.LineNumber > previousLine.Value.LineNumber + 1)
                    {
                        // If the token belongs to the next statement and is also the first token of the statement, then it means the user wants
                        // to start type a new statement. So get indentation from the start of the line but not based on the token.
                        // Case:
                        // static void Main(string[] args)
                        // {
                        //     // A
                        //     // B
                        //     
                        //     $$
                        //     return;
                        // }

                        var containingStatement = token.GetAncestor<StatementSyntax>();
                        if (containingStatement != null && containingStatement.GetFirstToken() == token)
                        {
                            var position = GetCurrentPositionNotBelongToEndOfFileToken(LineToBeIndented.Start);
                            return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, CancellationToken));
                        }

                        // If the token previous of the base token happens to be a Comma from a separation list then we need to handle it different
                        // Case:
                        // var s = new List<string>
                        //                 {
                        //                     """",
                        //                             """",/*sdfsdfsdfsdf*/
                        //                                  // dfsdfsdfsdfsdf
                        //                                  
                        //                             $$
                        //                 };
                        var previousToken = token.GetPreviousToken();
                        if (previousToken.IsKind(SyntaxKind.CommaToken))
                        {
                            return GetIndentationFromCommaSeparatedList(previousToken);
                        }
                        else if (!previousToken.IsKind(SyntaxKind.None))
                        {
                            // okay, beginning of the line is not trivia, use the last token on the line as base token
                            return GetIndentationBasedOnToken(token);
                        }
                    }

                    // this case we will keep the indentation of this trivia line
                    // this trivia can't be preprocessor by the way.
                    return GetIndentationOfLine(previousLine.Value);
                }
            }

            private IndentationResult? GetIndentationBasedOnToken(SyntaxToken token)
            {
                Contract.ThrowIfNull(Tree);
                Contract.ThrowIfTrue(token.Kind() == SyntaxKind.None);

                // special cases
                // case 1: token belongs to verbatim token literal
                // case 2: $@"$${0}"
                // case 3: $@"Comment$$ inbetween{0}"
                // case 4: $@"{0}$$"
                if (token.IsVerbatimStringLiteral() ||
                    token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken) ||
                    token.IsKind(SyntaxKind.InterpolatedStringTextToken) ||
                    (token.IsKind(SyntaxKind.CloseBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation)))
                {
                    return IndentFromStartOfLine(0);
                }

                // if previous statement belong to labeled statement, don't follow label's indentation
                // but its previous one.
                if (token.Parent is LabeledStatementSyntax || token.IsLastTokenInLabelStatement())
                {
                    token = token.GetAncestor<LabeledStatementSyntax>().GetFirstToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true);
                }

                var position = GetCurrentPositionNotBelongToEndOfFileToken(LineToBeIndented.Start);

                // first check operation service to see whether we can determine indentation from it
                var indentation = Finder.FromIndentBlockOperations(Tree, token, position, CancellationToken);
                if (indentation.HasValue)
                {
                    return IndentFromStartOfLine(indentation.Value);
                }

                var alignmentTokenIndentation = Finder.FromAlignTokensOperations(Tree, token);
                if (alignmentTokenIndentation.HasValue)
                {
                    return IndentFromStartOfLine(alignmentTokenIndentation.Value);
                }

                // if we couldn't determine indentation from the service, use heuristic to find indentation.
                var sourceText = LineToBeIndented.Text;

                // If this is the last token of an embedded statement, walk up to the top-most parenting embedded
                // statement owner and use its indentation.
                //
                // cases:
                //   if (true)
                //     if (false)
                //       Foo();
                //
                //   if (true)
                //     { }

                if (token.IsSemicolonOfEmbeddedStatement() ||
                    token.IsCloseBraceOfEmbeddedBlock())
                {
                    Contract.Requires(
                        token.Parent != null &&
                        (token.Parent.Parent is StatementSyntax || token.Parent.Parent is ElseClauseSyntax));

                    var embeddedStatementOwner = token.Parent.Parent;
                    while (embeddedStatementOwner.IsEmbeddedStatement())
                    {
                        embeddedStatementOwner = embeddedStatementOwner.Parent;
                    }

                    return GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(embeddedStatementOwner.GetFirstToken(includeZeroWidth: true).SpanStart));
                }

                switch (token.Kind())
                {
                    case SyntaxKind.SemicolonToken:
                        {
                            // special cases
                            if (token.IsSemicolonInForStatement())
                            {
                                return GetDefaultIndentationFromToken(token);
                            }

                            return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, CancellationToken));
                        }

                    case SyntaxKind.CloseBraceToken:
                        {
                            if (token.Parent.IsKind(SyntaxKind.AccessorList) &&
                                token.Parent.Parent.IsKind(SyntaxKind.PropertyDeclaration))
                            {
                                if (token.GetNextToken().IsEqualsTokenInAutoPropertyInitializers())
                                {
                                    return GetDefaultIndentationFromToken(token);
                                }
                            }

                            return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, CancellationToken));
                        }

                    case SyntaxKind.OpenBraceToken:
                        {
                            return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, CancellationToken));
                        }

                    case SyntaxKind.ColonToken:
                        {
                            var nonTerminalNode = token.Parent;
                            Contract.ThrowIfNull(nonTerminalNode, @"Malformed code or bug in parser???");

                            if (nonTerminalNode is SwitchLabelSyntax)
                            {
                                return GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(nonTerminalNode.GetFirstToken(includeZeroWidth: true).SpanStart), OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language));
                            }

                            // default case
                            return GetDefaultIndentationFromToken(token);
                        }

                    case SyntaxKind.CloseBracketToken:
                        {
                            var nonTerminalNode = token.Parent;
                            Contract.ThrowIfNull(nonTerminalNode, @"Malformed code or bug in parser???");

                            // if this is closing an attribute, we shouldn't indent.
                            if (nonTerminalNode is AttributeListSyntax)
                            {
                                return GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(nonTerminalNode.GetFirstToken(includeZeroWidth: true).SpanStart));
                            }

                            // default case
                            return GetDefaultIndentationFromToken(token);
                        }

                    case SyntaxKind.XmlTextLiteralToken:
                        {
                            return GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(token.SpanStart));
                        }

                    case SyntaxKind.CommaToken:
                        {
                            return GetIndentationFromCommaSeparatedList(token);
                        }

                    default:
                        {
                            return GetDefaultIndentationFromToken(token);
                        }
                }
            }

            private IndentationResult? GetIndentationFromCommaSeparatedList(SyntaxToken token)
            {
                var node = token.Parent;

                var argument = node as BaseArgumentListSyntax;
                if (argument != null)
                {
                    return GetIndentationFromCommaSeparatedList(argument.Arguments, token);
                }

                var parameter = node as BaseParameterListSyntax;
                if (parameter != null)
                {
                    return GetIndentationFromCommaSeparatedList(parameter.Parameters, token);
                }

                var typeArgument = node as TypeArgumentListSyntax;
                if (typeArgument != null)
                {
                    return GetIndentationFromCommaSeparatedList(typeArgument.Arguments, token);
                }

                var typeParameter = node as TypeParameterListSyntax;
                if (typeParameter != null)
                {
                    return GetIndentationFromCommaSeparatedList(typeParameter.Parameters, token);
                }

                var enumDeclaration = node as EnumDeclarationSyntax;
                if (enumDeclaration != null)
                {
                    return GetIndentationFromCommaSeparatedList(enumDeclaration.Members, token);
                }

                var initializerSyntax = node as InitializerExpressionSyntax;
                if (initializerSyntax != null)
                {
                    return GetIndentationFromCommaSeparatedList(initializerSyntax.Expressions, token);
                }

                return GetDefaultIndentationFromToken(token);
            }

            private IndentationResult? GetIndentationFromCommaSeparatedList<T>(SeparatedSyntaxList<T> list, SyntaxToken token) where T : SyntaxNode
            {
                var index = list.GetWithSeparators().IndexOf(token);
                if (index < 0)
                {
                    return GetDefaultIndentationFromToken(token);
                }

                // find node that starts at the beginning of a line
                var sourceText = LineToBeIndented.Text;
                for (int i = (index - 1) / 2; i >= 0; i--)
                {
                    var node = list[i];
                    var firstToken = node.GetFirstToken(includeZeroWidth: true);

                    if (firstToken.IsFirstTokenOnLine(sourceText))
                    {
                        return GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(firstToken.SpanStart));
                    }
                }

                // smart indenter has a special indent block rule for comma separated list, so don't
                // need to add default additional space for multiline expressions
                return GetDefaultIndentationFromTokenLine(token, additionalSpace: 0);
            }

            private IndentationResult? GetDefaultIndentationFromToken(SyntaxToken token)
            {
                if (IsPartOfQueryExpression(token))
                {
                    return GetIndentationForQueryExpression(token);
                }

                return GetDefaultIndentationFromTokenLine(token);
            }

            private IndentationResult? GetIndentationForQueryExpression(SyntaxToken token)
            {
                // find containing non terminal node
                var queryExpressionClause = GetQueryExpressionClause(token);
                if (queryExpressionClause == null)
                {
                    return GetDefaultIndentationFromTokenLine(token);
                }

                // find line where first token of the node is
                var sourceText = LineToBeIndented.Text;
                var firstToken = queryExpressionClause.GetFirstToken(includeZeroWidth: true);
                var firstTokenLine = sourceText.Lines.GetLineFromPosition(firstToken.SpanStart);

                // find line where given token is
                var givenTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);

                if (firstTokenLine.LineNumber != givenTokenLine.LineNumber)
                {
                    // do default behavior
                    return GetDefaultIndentationFromTokenLine(token);
                }

                // okay, we are right under the query expression.
                // align caret to query expression
                if (firstToken.IsFirstTokenOnLine(sourceText))
                {
                    return GetIndentationOfToken(firstToken);
                }

                // find query body that has a token that is a first token on the line
                var queryBody = queryExpressionClause.Parent as QueryBodySyntax;
                if (queryBody == null)
                {
                    return GetIndentationOfToken(firstToken);
                }

                // find preceding clause that starts on its own.
                var clauses = queryBody.Clauses;
                for (int i = clauses.Count - 1; i >= 0; i--)
                {
                    var clause = clauses[i];
                    if (firstToken.SpanStart <= clause.SpanStart)
                    {
                        continue;
                    }

                    var clauseToken = clause.GetFirstToken(includeZeroWidth: true);
                    if (clauseToken.IsFirstTokenOnLine(sourceText))
                    {
                        return GetIndentationOfToken(clauseToken);
                    }
                }

                // no query clause start a line. use the first token of the query expression
                return GetIndentationOfToken(queryBody.Parent.GetFirstToken(includeZeroWidth: true));
            }

            private SyntaxNode GetQueryExpressionClause(SyntaxToken token)
            {
                var clause = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => n is QueryClauseSyntax || n is SelectOrGroupClauseSyntax);

                if (clause != null)
                {
                    return clause;
                }

                // If this is a query continuation, use the last clause of its parenting query.
                var body = token.GetAncestor<QueryBodySyntax>();
                if (body != null)
                {
                    if (body.SelectOrGroup.IsMissing)
                    {
                        return body.Clauses.LastOrDefault();
                    }
                    else
                    {
                        return body.SelectOrGroup;
                    }
                }

                return null;
            }

            private bool IsPartOfQueryExpression(SyntaxToken token)
            {
                var queryExpression = token.GetAncestor<QueryExpressionSyntax>();
                return queryExpression != null;
            }

            private IndentationResult? GetDefaultIndentationFromTokenLine(SyntaxToken token, int? additionalSpace = null)
            {
                var spaceToAdd = additionalSpace ?? this.OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language);

                var sourceText = LineToBeIndented.Text;

                // find line where given token is
                var givenTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);

                // find right position
                var position = GetCurrentPositionNotBelongToEndOfFileToken(LineToBeIndented.Start);

                // find containing non expression node
                var nonExpressionNode = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => n is StatementSyntax);
                if (nonExpressionNode == null)
                {
                    // well, I can't find any non expression node. use default behavior
                    return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, spaceToAdd, CancellationToken));
                }

                // find line where first token of the node is
                var firstTokenLine = sourceText.Lines.GetLineFromPosition(nonExpressionNode.GetFirstToken(includeZeroWidth: true).SpanStart);

                // single line expression
                if (firstTokenLine.LineNumber == givenTokenLine.LineNumber)
                {
                    return IndentFromStartOfLine(Finder.GetIndentationOfCurrentPosition(Tree, token, position, spaceToAdd, CancellationToken));
                }

                // okay, looks like containing node is written over multiple lines, in that case, give same indentation as given token
                return GetIndentationOfLine(givenTokenLine);
            }

            protected override bool HasPreprocessorCharacter(TextLine currentLine)
            {
                var text = currentLine.ToString();
                Contract.Requires(!string.IsNullOrWhiteSpace(text));

                var trimmedText = text.Trim();

                Contract.Assert(SyntaxFacts.GetText(SyntaxKind.HashToken).Length == 1);
                return trimmedText[0] == SyntaxFacts.GetText(SyntaxKind.HashToken)[0];
            }

            private int GetCurrentPositionNotBelongToEndOfFileToken(int position)
            {
                var compilationUnit = Tree.GetRoot(CancellationToken) as CompilationUnitSyntax;
                if (compilationUnit == null)
                {
                    return position;
                }

                return Math.Min(compilationUnit.EndOfFileToken.FullSpan.Start, position);
            }
        }
    }
}
