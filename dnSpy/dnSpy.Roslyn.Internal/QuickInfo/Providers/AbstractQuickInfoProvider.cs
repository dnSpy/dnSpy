// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.QuickInfo
{
    internal abstract partial class AbstractQuickInfoProvider : IQuickInfoProvider
    {
        protected AbstractQuickInfoProvider()
        {
        }

        public async Task<QuickInfoItem> GetItemAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            var state = await GetQuickInfoItemAsync(document, token, position, cancellationToken).ConfigureAwait(false);
            if (state != null)
            {
                return state;
            }

            if (ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();

                if ((state = await GetQuickInfoItemAsync(document, previousToken, position, cancellationToken).ConfigureAwait(false)) != null)
                {
                    return state;
                }
            }

            return null;
        }

        protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return true;
        }

        private async Task<QuickInfoItem> GetQuickInfoItemAsync(
            Document document,
            SyntaxToken token,
            int position,
            CancellationToken cancellationToken)
        {
            if (token != default(SyntaxToken) &&
                token.Span.IntersectsWith(position))
            {
                var deferredContent = await BuildContentAsync(document, token, cancellationToken).ConfigureAwait(false);
                if (deferredContent != null)
                {
                    return new QuickInfoItem(token.Span, deferredContent);
                }
            }

            return null;
        }

        protected abstract Task<QuickInfoContent> BuildContentAsync(Document document, SyntaxToken token, CancellationToken cancellationToken);

        protected QuickInfoContent CreateQuickInfoDisplayDeferredContent(
            ISymbol symbol,
            bool showWarningGlyph,
            bool showSymbolGlyph,
            IList<TaggedText> mainDescription,
            ImmutableArray<TaggedText> documentation,
            IList<TaggedText> typeParameterMap,
            IList<TaggedText> anonymousTypes,
            IList<TaggedText> usageText,
            IList<TaggedText> exceptionText)
        {
            return new InformationQuickInfoContent(
                symbolGlyph: showSymbolGlyph ? CreateGlyphDeferredContent(symbol) : (Glyph?)null,
                warningGlyph: showWarningGlyph ? CreateWarningGlyph() : (Glyph?)null,
                mainDescription: CreateClassifiableDeferredContent(mainDescription),
                documentation: documentation,
                typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
                anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
                usageText: CreateClassifiableDeferredContent(usageText),
                exceptionText: CreateClassifiableDeferredContent(exceptionText));
        }

        private Glyph CreateWarningGlyph()
        {
            return Glyph.CompletionWarning;
        }

        protected QuickInfoContent CreateQuickInfoDisplayDeferredContent(
            Glyph glyph,
            IList<TaggedText> mainDescription,
            ImmutableArray<TaggedText> documentation,
            IList<TaggedText> typeParameterMap,
            IList<TaggedText> anonymousTypes,
            IList<TaggedText> usageText,
            IList<TaggedText> exceptionText)
        {
            return new InformationQuickInfoContent(
                symbolGlyph: glyph,
                warningGlyph: null,
                mainDescription: CreateClassifiableDeferredContent(mainDescription),
                documentation: documentation,
                typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
                anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
                usageText: CreateClassifiableDeferredContent(usageText),
                exceptionText: CreateClassifiableDeferredContent(exceptionText));
        }

        protected Glyph CreateGlyphDeferredContent(ISymbol symbol)
        {
            return symbol.GetGlyph().ToOurGlyph();
        }

        protected ImmutableArray<TaggedText> CreateClassifiableDeferredContent(
            IList<TaggedText> content)
        {
            return content.AsImmutable();
        }

        protected ImmutableArray<TaggedText> CreateDocumentationCommentDeferredContent(
            string documentationComment)
        {
            return string.IsNullOrEmpty(documentationComment) ? ImmutableArray<TaggedText>.Empty : ImmutableArray.Create(new TaggedText(TextTags.Text, documentationComment));
        }

        protected QuickInfoContent CreateElisionBufferDeferredContent(TextSpan span)
        {
            return new CodeSpanQuickInfoContent(span);
        }
    }
}
