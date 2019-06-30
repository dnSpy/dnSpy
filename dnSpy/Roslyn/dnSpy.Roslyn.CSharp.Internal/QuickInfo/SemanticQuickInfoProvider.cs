// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace dnSpy.Roslyn.Internal.QuickInfo.CSharp
{
    [ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Semantic, LanguageNames.CSharp)]
    internal class SemanticQuickInfoProvider : AbstractSemanticQuickInfoProvider
    {
        [ImportingConstructor]
        public SemanticQuickInfoProvider()
        {
        }

        protected override bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }
    }
}
