// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal.SmartIndent
{
    internal abstract partial class AbstractIndentationService : ISynchronousIndentationService
    {
        protected abstract IFormattingRule GetSpecializedIndentationFormattingRule();

        private IEnumerable<IFormattingRule> GetFormattingRules(Document document, int position)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, position);

            var formattingRules = new[] { baseIndentationRule, this.GetSpecializedIndentationFormattingRule() }.Concat(Formatter.GetDefaultFormattingRules(document));
            return formattingRules;
        }

        public IndentationResult? GetDesiredIndentation(
            Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var sourceText = root.SyntaxTree.GetText(cancellationToken);
            var documentOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var lineToBeIndented = sourceText.Lines[lineNumber];

            var formattingRules = GetFormattingRules(document, lineToBeIndented.Start);

            // There are two important cases for indentation.  The first is when we're simply
            // trying to figure out the appropriate indentation on a blank line (i.e. after
            // hitting enter at the end of a line, or after moving to a blank line).  The 
            // second is when we're trying to figure out indentation for a non-blank line
            // (i.e. after hitting enter in the middle of a line, causing tokens to move to
            // the next line).  If we're in the latter case, we defer to the Formatting engine
            // as we need it to use all its rules to determine where the appropriate location is
            // for the following tokens to go.
            if (ShouldUseSmartTokenFormatterInsteadOfIndenter(formattingRules, root, lineToBeIndented, documentOptions, cancellationToken))
            {
                return null;
            }

            var indenter = GetIndenter(
                document.GetLanguageService<ISyntaxFactsService>(),
                root.SyntaxTree, lineToBeIndented, formattingRules,
                documentOptions, cancellationToken);

            return indenter.GetDesiredIndentation(document);
        }

        protected abstract AbstractIndenter GetIndenter(
            ISyntaxFactsService syntaxFacts, SyntaxTree syntaxTree, TextLine lineToBeIndented, IEnumerable<IFormattingRule> formattingRules, OptionSet optionSet, CancellationToken cancellationToken);

        protected abstract bool ShouldUseSmartTokenFormatterInsteadOfIndenter(
            IEnumerable<IFormattingRule> formattingRules, SyntaxNode root, TextLine line, OptionSet optionSet, CancellationToken cancellationToken);
    }
}
