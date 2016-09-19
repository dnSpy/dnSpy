// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.EditorFeatures.Extensions
{
    internal static class ITextSnapshotLineExtensions
    {
        /// <summary>
        /// Returns the first non-whitespace position on the given line, or null if 
        /// the line is empty or contains only whitespace.
        /// </summary>
        public static int? GetFirstNonWhitespacePosition(this ITextSnapshotLine line)
        {
            Contract.ThrowIfNull(line);

            var text = line.GetText();

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return line.Start + i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first non-whitespace position on the given line as an offset
        /// from the start of the line, or null if the line is empty or contains only
        /// whitespace.
        /// </summary>
        public static int? GetFirstNonWhitespaceOffset(this ITextSnapshotLine line)
        {
            Contract.ThrowIfNull(line);

            var text = line.GetText();

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the last non-whitespace position on the given line, or null if 
        /// the line is empty or contains only whitespace.
        /// </summary>
        public static int? GetLastNonWhitespacePosition(this ITextSnapshotLine line)
        {
            return line.AsTextLine().GetLastNonWhitespacePosition();
        }

        /// <summary>
        /// Determines whether the specified line is empty or contains whitespace only.
        /// </summary>
        public static bool IsEmptyOrWhitespace(this ITextSnapshotLine line)
        {
            Contract.ThrowIfNull("line");

            var text = line.GetText();

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static ITextSnapshotLine GetPreviousMatchingLine(this ITextSnapshotLine line, Func<ITextSnapshotLine, bool> predicate)
        {
            Contract.ThrowIfNull(line, @"line");
            Contract.ThrowIfNull(predicate, @"tree");

            if (line.LineNumber <= 0)
            {
                return null;
            }

            var snapshot = line.Snapshot;
            for (int lineNumber = line.LineNumber - 1; lineNumber >= 0; lineNumber--)
            {
                var currentLine = snapshot.GetLineFromLineNumber(lineNumber);
                if (!predicate(currentLine))
                {
                    continue;
                }

                return currentLine;
            }

            return null;
        }

        public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this ITextSnapshotLine line, IEditorOptions editorOptions)
        {
            return line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(editorOptions.GetTabSize());
        }

        public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this ITextSnapshotLine line, int tabSize)
        {
            return line.GetText().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);
        }

        public static int GetColumnFromLineOffset(this ITextSnapshotLine line, int lineOffset, IEditorOptions editorOptions)
        {
            return line.GetText().GetColumnFromLineOffset(lineOffset, editorOptions.GetTabSize());
        }

        public static int GetLineOffsetFromColumn(this ITextSnapshotLine line, int column, IEditorOptions editorOptions)
        {
            return line.GetText().GetLineOffsetFromColumn(column, editorOptions.GetTabSize());
        }

        /// <summary>
        /// Checks if the given line at the given snapshot index starts with the provided value.
        /// </summary>
        public static bool StartsWith(this ITextSnapshotLine line, int index, string value, bool ignoreCase)
        {
            var snapshot = line.Snapshot;
            if (index + value.Length > snapshot.Length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                var snapshotIndex = index + i;
                var actualCharacter = snapshot[snapshotIndex];
                var expectedCharacter = value[i];

                if (ignoreCase)
                {
                    actualCharacter = char.ToLowerInvariant(actualCharacter);
                    expectedCharacter = char.ToLowerInvariant(expectedCharacter);
                }

                if (actualCharacter != expectedCharacter)
                {
                    return false;
                }
            }

            return true;
        }
    }
}