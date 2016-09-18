// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal.QuickInfo
{
    internal abstract partial class AbstractSemanticQuickInfoProvider : AbstractQuickInfoProvider
    {
        public AbstractSemanticQuickInfoProvider()
        {
        }

        protected override async Task<QuickInfoContent> BuildContentAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var linkedDocumentIds = document.GetLinkedDocumentIds();

            var modelAndSymbols = await this.BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);
            if ((modelAndSymbols.Item2 == null || modelAndSymbols.Item2.Count == 0) && !linkedDocumentIds.Any())
            {
                return null;
            }

            if (!linkedDocumentIds.Any())
            {
                return await CreateContentAsync(document.Project.Solution.Workspace,
                    token,
                    modelAndSymbols.Item1,
                    modelAndSymbols.Item2,
                    supportedPlatforms: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Linked files/shared projects: imagine the following when FOO is false
            // #if FOO
            // int x = 3;
            // #endif 
            // var y = x$$;
            //
            // 'x' will bind as an error type, so we'll show incorrect information.
            // Instead, we need to find the head in which we get the best binding, 
            // which in this case is the one with no errors.

            var candidateProjects = new List<ProjectId>() { document.Project.Id };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<Tuple<DocumentId, SemanticModel, IList<ISymbol>>>();
            candidateResults.Add(Tuple.Create(document.Id, modelAndSymbols.Item1, modelAndSymbols.Item2));

            foreach (var link in linkedDocumentIds)
            {
                var linkedDocument = document.Project.Solution.GetDocument(link);
                var linkedToken = await FindTokenInLinkedDocument(token, linkedDocument, cancellationToken).ConfigureAwait(false);

                if (linkedToken != default(SyntaxToken))
                {
                    // Not in an inactive region, so this file is a candidate.
                    candidateProjects.Add(link.ProjectId);
                    var linkedModelAndSymbols = await this.BindTokenAsync(linkedDocument, linkedToken, cancellationToken).ConfigureAwait(false);
                    candidateResults.Add(Tuple.Create(link, linkedModelAndSymbols.Item1, linkedModelAndSymbols.Item2));
                }
            }

            // Take the first result with no errors.
            var bestBinding = candidateResults.FirstOrDefault(c => c.Item3.Count > 0 && !ErrorVisitor.ContainsError(c.Item3.FirstOrDefault()));

            // Every file binds with errors. Take the first candidate, which is from the current file.
            if (bestBinding == null)
            {
                bestBinding = candidateResults.First();
            }

            if (bestBinding.Item3 == null || !bestBinding.Item3.Any())
            {
                return null;
            }

            // We calculate the set of supported projects
            candidateResults.Remove(bestBinding);
            foreach (var candidate in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!candidate.Item3.Intersect(bestBinding.Item3, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                {
                    invalidProjects.Add(candidate.Item1.ProjectId);
                }
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, document.Project.Solution.Workspace);
            return await CreateContentAsync(document.Project.Solution.Workspace, token, bestBinding.Item2, bestBinding.Item3, supportedPlatforms, cancellationToken).ConfigureAwait(false);
        }

        private async Task<SyntaxToken> FindTokenInLinkedDocument(SyntaxToken token, Document linkedDocument, CancellationToken cancellationToken)
        {
            if (!linkedDocument.SupportsSyntaxTree)
            {
                return default(SyntaxToken);
            }

            var root = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Don't search trivia because we want to ignore inactive regions
            var linkedToken = root.FindToken(token.SpanStart);

            // The new and old tokens should have the same span?
            if (token.Span == linkedToken.Span)
            {
                return linkedToken;
            }

            return default(SyntaxToken);
        }

        protected async Task<QuickInfoContent> CreateContentAsync(
            Workspace workspace,
            SyntaxToken token,
            SemanticModel semanticModel,
            IEnumerable<ISymbol> symbols,
            SupportedPlatformData supportedPlatforms,
            CancellationToken cancellationToken)
        {
            var descriptionService = workspace.Services.GetLanguageServices(token.Language).GetService<ISymbolDisplayService>();

            var sections = await descriptionService.ToDescriptionGroupsAsync(workspace, semanticModel, token.SpanStart, symbols.AsImmutable(), cancellationToken).ConfigureAwait(false);

            var mainDescriptionBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.MainDescription))
            {
                mainDescriptionBuilder.AddRange(sections[SymbolDescriptionGroups.MainDescription].ToTaggedText());
            }

            var typeParameterMapBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.TypeParameterMap))
            {
                var parts = sections[SymbolDescriptionGroups.TypeParameterMap];
                if (!parts.IsDefaultOrEmpty)
                {
                    typeParameterMapBuilder.AddLineBreak();
                    typeParameterMapBuilder.AddRange(parts.ToTaggedText());
                }
            }

            var anonymousTypesBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.AnonymousTypes))
            {
                var parts = sections[SymbolDescriptionGroups.AnonymousTypes];
                if (!parts.IsDefaultOrEmpty)
                {
                    anonymousTypesBuilder.AddLineBreak();
                    anonymousTypesBuilder.AddRange(parts.ToTaggedText());
                }
            }

            var usageTextBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.AwaitableUsageText))
            {
                var parts = sections[SymbolDescriptionGroups.AwaitableUsageText];
                if (!parts.IsDefaultOrEmpty)
                {
                    usageTextBuilder.AddRange(parts.ToTaggedText());
                }
            }

            if (supportedPlatforms != null)
            {
                usageTextBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());
            }

            var exceptionsTextBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.Exceptions))
            {
                var parts = sections[SymbolDescriptionGroups.Exceptions];
                if (!parts.IsDefaultOrEmpty)
                {
                    exceptionsTextBuilder.AddRange(parts.ToTaggedText());
                }
            }

            var formatter = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IDocumentationCommentFormattingService>();
            var syntaxFactsService = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>();
            var documentationContent = GetDocumentationContent(symbols, sections, semanticModel, token, formatter, syntaxFactsService, cancellationToken);
            var showWarningGlyph = supportedPlatforms != null && supportedPlatforms.HasValidAndInvalidProjects();
            var showSymbolGlyph = true;

            if (workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>().IsAwaitKeyword(token) &&
                (symbols.First() as INamedTypeSymbol)?.SpecialType == SpecialType.System_Void)
            {
                documentationContent = CreateDocumentationCommentDeferredContent(null);
                showSymbolGlyph = false;
            }

            return this.CreateQuickInfoDisplayDeferredContent(
                symbol: symbols.First(),
                showWarningGlyph: showWarningGlyph,
                showSymbolGlyph: showSymbolGlyph,
                mainDescription: mainDescriptionBuilder,
                documentation: documentationContent,
                typeParameterMap: typeParameterMapBuilder,
                anonymousTypes: anonymousTypesBuilder,
                usageText: usageTextBuilder,
                exceptionText: exceptionsTextBuilder);
        }

        private ImmutableArray<TaggedText> GetDocumentationContent(
            IEnumerable<ISymbol> symbols,
            IDictionary<SymbolDescriptionGroups, ImmutableArray<SymbolDisplayPart>> sections,
            SemanticModel semanticModel,
            SyntaxToken token,
            IDocumentationCommentFormattingService formatter,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            if (sections.ContainsKey(SymbolDescriptionGroups.Documentation))
            {
                var documentationBuilder = new List<TaggedText>();
                documentationBuilder.AddRange(sections[SymbolDescriptionGroups.Documentation].ToTaggedText());
                return CreateClassifiableDeferredContent(documentationBuilder);
            }
            else if (symbols.Any())
            {
                var symbol = symbols.First().OriginalDefinition;

                // if generating quick info for an attribute, bind to the class instead of the constructor
                if (syntaxFactsService.IsAttributeName(token.Parent) &&
                    symbol.ContainingType?.IsAttribute() == true)
                {
                    symbol = symbol.ContainingType;
                }

                var documentation = symbol.GetDocumentationParts(semanticModel, token.SpanStart, formatter, cancellationToken);

                if (documentation != null)
                {
                    return CreateClassifiableDeferredContent(documentation.ToTaggedText());
                }
            }

            return CreateDocumentationCommentDeferredContent(null);
        }

        private async Task<ValueTuple<SemanticModel, IList<ISymbol>>> BindTokenAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(false);
            var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

            var symbols = semanticModel.GetSymbols(token, document.Project.Solution.Workspace, bindLiteralsToUnderlyingType: true, cancellationToken: cancellationToken);

            var bindableParent = document.GetLanguageService<ISyntaxFactsService>().GetBindableParent(token);
            var overloads = semanticModel.GetMemberGroup(bindableParent, cancellationToken);

            symbols = symbols.Where(IsOk)
                             .Where(s => IsAccessible(s, enclosingType))
                             .Concat(overloads)
                             .Distinct(SymbolEquivalenceComparer.Instance);

            if (symbols.Any())
            {
                var typeParameter = symbols.First() as ITypeParameterSymbol;
                return new ValueTuple<SemanticModel, IList<ISymbol>>(
                    semanticModel,
                    typeParameter != null && typeParameter.TypeParameterKind == TypeParameterKind.Cref
                        ? SpecializedCollections.EmptyList<ISymbol>()
                        : symbols.ToList());
            }

            // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
            // least bind it to a type.
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (syntaxFacts.IsOperator(token))
            {
                var typeInfo = semanticModel.GetTypeInfo(token.Parent, cancellationToken);
                if (IsOk(typeInfo.Type))
                {
                    return new ValueTuple<SemanticModel, IList<ISymbol>>(semanticModel, new List<ISymbol>(1) { typeInfo.Type });
                }
            }

            return ValueTuple.Create(semanticModel, SpecializedCollections.EmptyList<ISymbol>());
        }

        private static bool IsOk(ISymbol symbol)
        {
            return symbol != null && !symbol.IsErrorType() && !symbol.IsAnonymousFunction();
        }

        private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol within)
        {
            return within == null || symbol.IsAccessibleWithin(within);
        }
    }
}
