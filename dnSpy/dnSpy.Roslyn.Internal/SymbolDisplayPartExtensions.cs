// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Internal
{
    internal static class SymbolDisplayPartExtensions
    {
        public static string GetFullText(this IEnumerable<SymbolDisplayPart> parts)
        {
            return string.Join(string.Empty, parts.Select(p => p.ToString()));
        }

        public static void AddLineBreak(this IList<SymbolDisplayPart> parts, string text = "\r\n")
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, text));
        }

        public static void AddMethodName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, null, text));
        }

        public static void AddPunctuation(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, text));
        }

        public static void AddSpace(this IList<SymbolDisplayPart> parts, string text = " ")
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, text));
        }

        public static void AddText(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text));
        }
    }
}