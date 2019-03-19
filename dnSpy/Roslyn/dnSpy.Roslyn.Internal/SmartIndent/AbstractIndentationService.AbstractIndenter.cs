// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.SmartIndent
{
    internal abstract partial class AbstractIndentationService
    {
        internal abstract class AbstractIndenter
        {
            protected readonly OptionSet OptionSet;
            protected readonly TextLine LineToBeIndented;
            protected readonly int TabSize;
            protected readonly CancellationToken CancellationToken;

            protected readonly SyntaxTree Tree;
            protected readonly IEnumerable<IFormattingRule> Rules;
            protected readonly BottomUpBaseIndentationFinder Finder;

            private static readonly Func<SyntaxToken, bool> s_tokenHasDirective = tk => tk.ContainsDirectives &&
                                                  (tk.LeadingTrivia.Any(tr => tr.IsDirective) || tk.TrailingTrivia.Any(tr => tr.IsDirective));
            private readonly ISyntaxFactsService _syntaxFacts;

            public AbstractIndenter(
                ISyntaxFactsService syntaxFacts,
                SyntaxTree syntaxTree,
                IEnumerable<IFormattingRule> rules,
                OptionSet optionSet,
                TextLine lineToBeIndented,
                CancellationToken cancellationToken)
            {
                var syntaxRoot = syntaxTree.GetRoot(cancellationToken);

                this._syntaxFacts = syntaxFacts;
                this.OptionSet = optionSet;
                this.Tree = syntaxTree;
                this.LineToBeIndented = lineToBeIndented;
                this.TabSize = this.OptionSet.GetOption(FormattingOptions.TabSize, syntaxRoot.Language);
                this.CancellationToken = cancellationToken;

                this.Rules = rules;
                this.Finder = new BottomUpBaseIndentationFinder(
                         new ChainedFormattingRules(this.Rules, OptionSet),
                         this.TabSize,
                         this.OptionSet.GetOption(FormattingOptions.IndentationSize, syntaxRoot.Language),
                         tokenStream: null,
                         lastToken: default);
            }

            public IndentationResult? GetDesiredIndentation(Document document)
            {
                var indentStyle = OptionSet.GetOption(FormattingOptions.SmartIndent, document.Project.Language);
                if (indentStyle == FormattingOptions.IndentStyle.None)
                {
                    // If there is no indent style, then do nothing.
                    return null;
                }

                // find previous line that is not blank.  this will skip over things like preprocessor
                // regions and inactive code.
                var previousLineOpt = GetPreviousNonBlankOrPreprocessorLine();

                // it is beginning of the file, there is no previous line exists. 
                // in that case, indentation 0 is our base indentation.
                if (previousLineOpt == null)
                {
                    return IndentFromStartOfLine(0);
                }

                var previousNonWhitespaceOrPreprocessorLine = previousLineOpt.Value;

                // If the user wants block indentation, then we just return the indentation
                // of the last piece of real code.  
                //
                // TODO(cyrusn): It's not clear to me that this is correct.  Block indentation
                // should probably follow the indentation of hte last non-blank line *regardless
                // if it is inactive/preprocessor region.  By skipping over thse, we are essentially
                // being 'smart', and that seems to be overriding the user desire to have Block
                // indentation.
                if (indentStyle == FormattingOptions.IndentStyle.Block)
                {
                    // If it's block indentation, then just base 
                    return GetIndentationOfLine(previousNonWhitespaceOrPreprocessorLine);
                }

                Debug.Assert(indentStyle == FormattingOptions.IndentStyle.Smart);

                // Because we know that previousLine is not-whitespace, we know that we should be
                // able to get the last non-whitespace position.
                var lastNonWhitespacePosition = previousNonWhitespaceOrPreprocessorLine.GetLastNonWhitespacePosition().Value;

                var token = Tree.GetRoot(CancellationToken).FindToken(lastNonWhitespacePosition);
                Debug.Assert(token.RawKind != 0, "FindToken should always return a valid token");

                return GetDesiredIndentationWorker(
                    token, previousNonWhitespaceOrPreprocessorLine, lastNonWhitespacePosition);
            }

            protected abstract IndentationResult? GetDesiredIndentationWorker(
                SyntaxToken token, TextLine previousLine, int lastNonWhitespacePosition);

            protected IndentationResult IndentFromStartOfLine(int addedSpaces)
                => new IndentationResult(this.LineToBeIndented.Start, addedSpaces);

            protected IndentationResult GetIndentationOfToken(SyntaxToken token)
                => GetIndentationOfToken(token, addedSpaces: 0);

            protected IndentationResult GetIndentationOfToken(SyntaxToken token, int addedSpaces)
                => GetIndentationOfPosition(token.SpanStart, addedSpaces);

            protected IndentationResult GetIndentationOfLine(TextLine lineToMatch)
                => GetIndentationOfLine(lineToMatch, addedSpaces: 0);

            protected IndentationResult GetIndentationOfLine(TextLine lineToMatch, int addedSpaces)
            {
                var firstNonWhitespace = lineToMatch.GetFirstNonWhitespacePosition();
                firstNonWhitespace = firstNonWhitespace ?? lineToMatch.End;

                return GetIndentationOfPosition(firstNonWhitespace.Value, addedSpaces);
            }

            protected IndentationResult GetIndentationOfPosition(int position, int addedSpaces)
            {
                if (this.Tree.OverlapsHiddenPosition(GetNormalizedSpan(position), CancellationToken))
                {
                    // Oops, the line we want to line up to is either hidden, or is in a different
                    // visible region.
                    var root = this.Tree.GetRoot(CancellationToken.None);
                    var token = root.FindTokenFromEnd(LineToBeIndented.Start);
                    var indentation = Finder.GetIndentationOfCurrentPosition(this.Tree, token, LineToBeIndented.Start, CancellationToken.None);

                    return new IndentationResult(LineToBeIndented.Start, indentation);
                }

                return new IndentationResult(position, addedSpaces);
            }

            private TextSpan GetNormalizedSpan(int position)
            {
                if (LineToBeIndented.Start < position)
                {
                    return TextSpan.FromBounds(LineToBeIndented.Start, position);
                }

                return TextSpan.FromBounds(position, LineToBeIndented.Start);
            }

            protected TextLine? GetPreviousNonBlankOrPreprocessorLine()
            {
                if (LineToBeIndented.LineNumber <= 0)
                {
                    return null;
                }

                var sourceText = this.LineToBeIndented.Text;

                var lineNumber = this.LineToBeIndented.LineNumber - 1;
                while (lineNumber >= 0)
                {
                    var actualLine = sourceText.Lines[lineNumber];

                    // Empty line, no indentation to match.
                    if (string.IsNullOrWhiteSpace(actualLine.ToString()))
                    {
                        lineNumber--;
                        continue;
                    }

                    // No preprocessors in the entire tree, so this
                    // line definitely doesn't have one
                    var root = Tree.GetRoot(CancellationToken);
                    if (!root.ContainsDirectives)
                    {
                        return sourceText.Lines[lineNumber];
                    }

                    // This line is inside an inactive region. Examine the 
                    // first preceding line not in an inactive region.
                    var disabledSpan = _syntaxFacts.GetInactiveRegionSpanAroundPosition(this.Tree, actualLine.Span.Start, CancellationToken);
                    if (disabledSpan != default)
                    {
                        var targetLine = sourceText.Lines.GetLineFromPosition(disabledSpan.Start).LineNumber;
                        lineNumber = targetLine - 1;
                        continue;
                    }

                    // A preprocessor directive starts on this line.
                    if (HasPreprocessorCharacter(actualLine) &&
                        root.DescendantTokens(actualLine.Span, tk => tk.FullWidth() > 0).Any(s_tokenHasDirective))
                    {
                        lineNumber--;
                        continue;
                    }

                    return sourceText.Lines[lineNumber];
                }

                return null;
            }

            protected int GetCurrentPositionNotBelongToEndOfFileToken(int position)
            {
                var compilationUnit = Tree.GetRoot(CancellationToken) as ICompilationUnitSyntax;
                if (compilationUnit == null)
                {
                    return position;
                }

                return Math.Min(compilationUnit.EndOfFileToken.FullSpan.Start, position);
            }

            protected bool HasPreprocessorCharacter(TextLine currentLine)
            {
                var text = currentLine.ToString();
                //Contract.Requires(!string.IsNullOrWhiteSpace(text));

                var trimmedText = text.Trim();

                return trimmedText[0] == '#';
            }
        }
    }
}
