// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal.SmartIndent.CSharp
{
    [ExportLanguageService(typeof(ISynchronousIndentationService), LanguageNames.CSharp), Shared]
    internal partial class CSharpIndentationService : AbstractIndentationService
    {
        private static readonly IFormattingRule s_instance = new FormattingRule();

        protected override IFormattingRule GetSpecializedIndentationFormattingRule()
        {
            return s_instance;
        }

        protected override AbstractIndenter GetIndenter(
            ISyntaxFactsService syntaxFacts, SyntaxTree syntaxTree, TextLine lineToBeIndented, IEnumerable<IFormattingRule> formattingRules, OptionSet optionSet, CancellationToken cancellationToken)
        {
            return new Indenter(
                syntaxFacts, syntaxTree, formattingRules,
                optionSet, lineToBeIndented, cancellationToken);
        }

        protected override bool ShouldUseSmartTokenFormatterInsteadOfIndenter(
            IEnumerable<IFormattingRule> formattingRules,
            SyntaxNode root,
            TextLine line,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            return ShouldUseSmartTokenFormatterInsteadOfIndenter(
                formattingRules, (CompilationUnitSyntax)root, line, optionSet, cancellationToken);
        }

        public static bool ShouldUseSmartTokenFormatterInsteadOfIndenter(
            IEnumerable<IFormattingRule> formattingRules,
            CompilationUnitSyntax root,
            TextLine line,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(root);

            if (optionSet.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp) != FormattingOptions.IndentStyle.Smart)
            {
                return false;
            }

            var firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition();
            if (!firstNonWhitespacePosition.HasValue)
            {
                return false;
            }

            var token = root.FindToken(firstNonWhitespacePosition.Value);
            if (token.IsKind(SyntaxKind.None) ||
                token.SpanStart != firstNonWhitespacePosition)
            {
                return false;
            }

            // first see whether there is a line operation for current token
            var previousToken = token.GetPreviousToken(includeZeroWidth: true);

            // only use smart token formatter when we have two visible tokens.
            if (previousToken.Kind() == SyntaxKind.None || previousToken.IsMissing)
            {
                return false;
            }

            var lineOperation = FormattingOperations.GetAdjustNewLinesOperation(formattingRules, previousToken, token, optionSet);
            if (lineOperation != null && lineOperation.Option != AdjustNewLinesOption.ForceLinesIfOnSingleLine)
            {
                return true;
            }

            // no indentation operation, nothing to do for smart token formatter
            return false;
        }

        private class FormattingRule : AbstractFormattingRule
        {
            public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
            {
                // these nodes should be from syntax tree from ITextSnapshot.
                Contract.Requires(node.SyntaxTree != null);
                Contract.Requires(node.SyntaxTree.GetText() != null);

                nextOperation.Invoke(list);

                ReplaceCaseIndentationRules(list, node);

                if (node is BaseParameterListSyntax ||
                    node is TypeArgumentListSyntax ||
                    node is TypeParameterListSyntax ||
                    node.IsKind(SyntaxKind.Interpolation))
                {
                    AddIndentBlockOperations(list, node);
                    return;
                }

                var argument = node as BaseArgumentListSyntax;
                if (argument != null &&
                    argument.Parent.Kind() != SyntaxKind.ThisConstructorInitializer &&
                    !IsBracketedArgumentListMissingBrackets(argument as BracketedArgumentListSyntax))
                {
                    AddIndentBlockOperations(list, argument);
                    return;
                }

                // only valid if the user has started to actually type a constructor initializer
                var constructorInitializer = node as ConstructorInitializerSyntax;
                if (constructorInitializer != null &&
                    constructorInitializer.ArgumentList.OpenParenToken.Kind() != SyntaxKind.None &&
                    !constructorInitializer.ThisOrBaseKeyword.IsMissing)
                {
                    var text = node.SyntaxTree.GetText();

                    // 3 different cases
                    // first case : this or base is the first token on line
                    // second case : colon is the first token on line
                    var colonIsFirstTokenOnLine = !constructorInitializer.ColonToken.IsMissing && constructorInitializer.ColonToken.IsFirstTokenOnLine(text);
                    var thisOrBaseIsFirstTokenOnLine = !constructorInitializer.ThisOrBaseKeyword.IsMissing && constructorInitializer.ThisOrBaseKeyword.IsFirstTokenOnLine(text);

                    if (colonIsFirstTokenOnLine || thisOrBaseIsFirstTokenOnLine)
                    {
                        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            constructorInitializer.ThisOrBaseKeyword,
                            constructorInitializer.ArgumentList.OpenParenToken.GetNextToken(includeZeroWidth: true),
                            constructorInitializer.ArgumentList.CloseParenToken.GetPreviousToken(includeZeroWidth: true),
                            indentationDelta: 1,
                            option: IndentBlockOption.RelativePosition));
                    }
                    else
                    {
                        // third case : none of them are the first token on the line
                        AddIndentBlockOperations(list, constructorInitializer.ArgumentList);
                    }
                }
            }

            private bool IsBracketedArgumentListMissingBrackets(BracketedArgumentListSyntax node)
            {
                return node != null && node.OpenBracketToken.IsMissing && node.CloseBracketToken.IsMissing;
            }

            private void ReplaceCaseIndentationRules(List<IndentBlockOperation> list, SyntaxNode node)
            {
                var section = node as SwitchSectionSyntax;
                if (section == null || section.Statements.Count == 0)
                {
                    return;
                }

                var startToken = section.Statements.First().GetFirstToken(includeZeroWidth: true);
                var endToken = section.Statements.Last().GetLastToken(includeZeroWidth: true);

                for (int i = 0; i < list.Count; i++)
                {
                    var operation = list[i];
                    if (operation.StartToken == startToken && operation.EndToken == endToken)
                    {
                        // replace operation
                        list[i] = FormattingOperations.CreateIndentBlockOperation(startToken, endToken, indentationDelta: 1, option: IndentBlockOption.RelativePosition);
                    }
                }
            }

            private static void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node)
            {
                // only add indent block operation if the base token is the first token on line
                var text = node.SyntaxTree.GetText();
                var baseToken = node.Parent.GetFirstToken(includeZeroWidth: true);

                list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                    baseToken,
                    node.GetFirstToken(includeZeroWidth: true).GetNextToken(includeZeroWidth: true),
                    node.GetLastToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true),
                    indentationDelta: 1,
                    option: IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine));
            }
        }
    }
}
