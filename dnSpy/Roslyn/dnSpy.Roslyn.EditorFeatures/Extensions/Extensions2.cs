// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.EditorFeatures.Extensions
{
    static partial class Extensions2
    {
        public static SourceTextContainer AsTextContainer(this ITextBuffer buffer)
            => Text.Extensions.AsTextContainer(buffer);

        public static ITextBuffer GetTextBuffer(this SourceTextContainer textContainer)
            => TryGetTextBuffer(textContainer) ?? throw new ArgumentException();

        public static ITextBuffer TryGetTextBuffer(this SourceTextContainer textContainer)
            => Text.Extensions.TryFindEditorTextBuffer(textContainer);

        /// <summary>
        /// Returns the ITextSnapshot behind this SourceText, or null if it wasn't created from one.
        /// 
        /// Note that multiple ITextSnapshots may map to the same SourceText instance if
        /// ITextSnapshot.Version.ReiteratedVersionNumber doesn't change.
        /// </summary>
        /// <returns>The underlying ITextSnapshot.</returns>
        public static ITextSnapshot FindCorrespondingEditorTextSnapshot(this SourceText text)
            => Text.Extensions.TryFindEditorSnapshot(text);

        internal static ITextImage TryFindCorrespondingEditorTextImage(this SourceText text)
            => Text.Extensions.TryFindEditorTextImage(text);

        internal static TextLine AsTextLine(this ITextSnapshotLine line)
            => line.Snapshot.AsText().Lines[line.LineNumber];

        public static SourceText AsText(this ITextSnapshot textSnapshot)
            => Text.Extensions.AsText(textSnapshot);

        //internal static SourceText AsRoslynText(this ITextSnapshot textSnapshot, Encoding encoding)
        //    => new SnapshotSourceText.ClosedSnapshotSourceText(((ITextSnapshot2)textSnapshot).TextImage, encoding);

        /// <summary>
        /// Gets the workspace corresponding to the text buffer.
        /// </summary>
        public static Workspace GetWorkspace(this ITextBuffer buffer)
        {
            var container = buffer.AsTextContainer();
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                return workspace;
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="Document"/>s from the corresponding <see cref="Workspace.CurrentSolution"/> that are associated with the <see cref="ITextSnapshot"/>'s buffer,
        /// updated to contain the same text as the snapshot if necessary. There may be multiple <see cref="Document"/>s associated with the buffer
        /// if the file is linked into multiple projects or is part of a Shared Project.
        /// </summary>
        public static IEnumerable<Document> GetRelatedDocumentsWithChanges(this ITextSnapshot text)
            => text.AsText().GetRelatedDocumentsWithChanges();

        /// <summary>
        /// Gets the <see cref="Document"/> from the corresponding <see cref="Workspace.CurrentSolution"/> that is associated with the <see cref="ITextSnapshot"/>'s buffer
        /// in its current project context, updated to contain the same text as the snapshot if necessary. There may be multiple <see cref="Document"/>s
        /// associated with the buffer if it is linked into multiple projects or is part of a Shared Project. In this case, the <see cref="Workspace"/>
        /// is responsible for keeping track of which of these <see cref="Document"/>s is in the current project context.
        /// </summary>
        public static Document GetOpenDocumentInCurrentContextWithChanges(this ITextSnapshot text)
            => text.AsText().GetOpenDocumentInCurrentContextWithChanges();

        /// <summary>
        /// Gets the <see cref="Document"/>s from the corresponding <see cref="Workspace.CurrentSolution"/> that are associated with the <see cref="ITextBuffer"/>.
        /// There may be multiple <see cref="Document"/>s associated with the buffer if it is linked into multiple projects or is part of a Shared Project. 
        /// </summary>
        public static IEnumerable<Document> GetRelatedDocuments(this ITextBuffer buffer)
            => buffer.AsTextContainer().GetRelatedDocuments();

        /// <summary>
        /// Tries to get the document corresponding to the text from the current partial solution 
        /// associated with the text's container. If the document does not contain the exact text a document 
        /// from a new solution containing the specified text is constructed. If no document is associated
        /// with the specified text's container, or the text's container isn't associated with a workspace,
        /// then the method returns false.
        /// </summary>
        internal static Document GetDocumentWithFrozenPartialSemantics(this SourceText text, CancellationToken cancellationToken)
        {
            var document = text.GetOpenDocumentInCurrentContextWithChanges();
            return document?.WithFrozenPartialSemantics(cancellationToken);
        }

        internal static bool CanApplyChangeDocumentToWorkspace(this ITextBuffer buffer)
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out var workspace) &&
               workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);

        /// <summary>
        /// Get the encoding used to load this <see cref="ITextBuffer"/> if possible.
        /// <para>
        /// Note that this will return <see cref="Encoding.UTF8"/> if the <see cref="ITextBuffer"/>
        /// didn't come from an <see cref="ITextDocument"/>, or if the <see cref="ITextDocument"/>
        /// is already closed.
        /// </para>
        /// </summary>
        internal static Encoding GetEncodingOrUTF8(this ITextBuffer textBuffer)
            => textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument)
                ? textDocument.Encoding
                : Encoding.UTF8;
    }
}
