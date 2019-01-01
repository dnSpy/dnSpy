// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using dnSpy.Roslyn.EditorFeatures.Extensions;
using dnSpy.Roslyn.EditorFeatures.Host;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.EditorFeatures.TextStructureNavigation
{
    internal abstract partial class AbstractTextStructureNavigatorProvider : ITextStructureNavigatorProvider
    {
        private readonly ITextStructureNavigatorSelectorService _selectorService;
        private readonly IContentTypeRegistryService _contentTypeService;
        private readonly IWaitIndicator _waitIndicator;

        protected AbstractTextStructureNavigatorProvider(
            ITextStructureNavigatorSelectorService selectorService,
            IContentTypeRegistryService contentTypeService,
            IWaitIndicator waitIndicator)
        {
            Contract.ThrowIfNull(selectorService);
            Contract.ThrowIfNull(contentTypeService);

            _selectorService = selectorService;
            _contentTypeService = contentTypeService;
            _waitIndicator = waitIndicator;
        }

        protected abstract bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia);
        protected abstract bool IsWithinNaturalLanguage(SyntaxToken token, int position);

        protected virtual TextExtent GetExtentOfWordFromToken(SyntaxToken token, SnapshotPoint position)
        {
            return new TextExtent(token.Span.ToSnapshotSpan(position.Snapshot), isSignificant: true);
        }

        public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer subjectBuffer)
        {
            var naturalLanguageNavigator = _selectorService.CreateTextStructureNavigator(
                subjectBuffer,
                _contentTypeService.GetContentType("any"));

            return new TextStructureNavigator(
                subjectBuffer,
                naturalLanguageNavigator,
                this,
                _waitIndicator);
        }
    }
}
