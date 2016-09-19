// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;

namespace dnSpy.Roslyn.Internal
{
    internal static class ISyntaxFactsServiceExtensions
    {
        public static bool IsWord(this ISyntaxFactsService syntaxFacts, SyntaxToken token)
        {
            return syntaxFacts.IsIdentifier(token)
                || syntaxFacts.IsKeyword(token)
                || syntaxFacts.IsContextualKeyword(token)
                || syntaxFacts.IsPreprocessorKeyword(token);
        }

        public static bool IsAnyMemberAccessExpression(
            this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            return syntaxFacts.IsMemberAccessExpression(node) || syntaxFacts.IsPointerMemberAccessExpression(node);
        }
    }
}
