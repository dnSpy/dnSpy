// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal
{
    internal static class SourceTextExtensions
    {
        /// <summary>
        /// Returns the leading whitespace of the line located at the specified position in the given snapshot.
        /// </summary>
        public static string GetLeadingWhitespaceOfLineAtPosition(this SourceText text, int position)
        {
            Contract.ThrowIfNull(text);

            var line = text.Lines.GetLineFromPosition(position);
            var linePosition = line.GetFirstNonWhitespacePosition();
            if (!linePosition.HasValue)
            {
                return line.ToString();
            }

            var lineText = line.ToString();
            return lineText.Substring(0, linePosition.Value - line.Start);
        }

        public static void GetLineAndOffset(this SourceText text, int position, out int lineNumber, out int offset)
        {
            var line = text.Lines.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            offset = position - line.Start;
        }

        public static void GetLinesAndOffsets(
            this SourceText text,
            TextSpan textSpan,
            out int startLineNumber,
            out int startOffset,
            out int endLineNumber,
            out int endOffset)
        {
            text.GetLineAndOffset(textSpan.Start, out startLineNumber, out startOffset);
            text.GetLineAndOffset(textSpan.End, out endLineNumber, out endOffset);
        }

        public static bool OverlapsHiddenPosition(
            this SourceText text, TextSpan span, Func<int, CancellationToken, bool> isPositionHidden, CancellationToken cancellationToken)
        {
            var result = TryOverlapsHiddenPosition(text, span, isPositionHidden, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        /// <summary>
        /// Same as OverlapsHiddenPosition but doesn't throw on cancellation.  Instead, returns false
        /// in that case.
        /// </summary>
        public static bool TryOverlapsHiddenPosition(
            this SourceText text, TextSpan span, Func<int, CancellationToken, bool> isPositionHidden,
            CancellationToken cancellationToken)
        {
            var startLineNumber = text.Lines.IndexOf(span.Start);
            var endLineNumber = text.Lines.IndexOf(span.End);

            // NOTE(cyrusn): It's safe to examine the start of a line because you can't have a line
            // with both a pp directive and code on it.  so, for example, if a node crosses a region
            // then it must be the case that the start of some line from the start of the node to
            // the end is hidden.  i.e.:
#if false
'           class C
'           {
'#line hidden
'           }
'#line default
#endif
            // The start of the line with the } on it is hidden, and thus the node overlaps a hidden
            // region.

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var linePosition = text.Lines[lineNumber].Start;
                var isHidden = isPositionHidden(linePosition, cancellationToken);
                if (isHidden)
                {
                    return true;
                }
            }

            return false;
        }

        public static TextChangeRange GetEncompassingTextChangeRange(this SourceText newText, SourceText oldText)
        {
            var ranges = newText.GetChangeRanges(oldText);
            if (ranges.Count == 0)
            {
                return default(TextChangeRange);
            }

            // simple case.
            if (ranges.Count == 1)
            {
                return ranges[0];
            }

            return TextChangeRange.Collapse(ranges);
        }

        public static int IndexOf(this SourceText text, string value, int startIndex, bool caseSensitive)
        {
            var length = text.Length - value.Length;
            var normalized = caseSensitive ? value : CaseInsensitiveComparison.ToLower(value);

            for (var i = startIndex; i <= length; i++)
            {
                var match = true;
                for (var j = 0; j < normalized.Length; j++)
                {
                    // just use indexer of source text. perf of indexer depends on actual implementation of SourceText.
                    // * all of our implementation at editor layer should provide either O(1) or O(logn).
                    //
                    // only one implementation we have that could have bad indexer perf is CompositeText with heavily modified text
                    // at compiler layer but I believe that being used in find all reference will be very rare if not none.
                    if (!Match(normalized[j], text[i + j], caseSensitive))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf(this SourceText text, string value, int startIndex, bool caseSensitive)
        {
            var normalized = caseSensitive ? value : CaseInsensitiveComparison.ToLower(value);
            startIndex = startIndex + normalized.Length > text.Length
                ? text.Length - normalized.Length
                : startIndex;

            for (var i = startIndex; i >= 0; i--)
            {
                var match = true;
                for (var j = 0; j < normalized.Length; j++)
                {
                    // just use indexer of source text. perf of indexer depends on actual implementation of SourceText.
                    // * all of our implementation at editor layer should provide either O(1) or O(logn).
                    //
                    // only one implementation we have that could have bad indexer perf is CompositeText with heavily modified text
                    // at compiler layer but I believe that being used in find all reference will be very rare if not none.
                    if (!Match(normalized[j], text[i + j], caseSensitive))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool Match(char normalizedLeft, char right, bool caseSensitive)
        {
            return caseSensitive ? normalizedLeft == right : normalizedLeft == CaseInsensitiveComparison.ToLower(right);
        }

        public static bool AreOnSameLine(this SourceText text, SyntaxToken token1, SyntaxToken token2)
        {
            return token1.RawKind != 0 &&
                token2.RawKind != 0 &&
                text.Lines.IndexOf(token1.Span.End) == text.Lines.IndexOf(token2.SpanStart);
        }
    }
}
