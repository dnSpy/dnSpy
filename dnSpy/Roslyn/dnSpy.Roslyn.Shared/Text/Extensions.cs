/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Shared.Text {
	/// <summary>
	/// Extension methods
	/// </summary>
	static class Extensions {
		/// <summary>
		/// Converts <paramref name="textSnapshot"/> to a <see cref="SourceText"/>
		/// </summary>
		/// <param name="textSnapshot">Snapshot</param>
		/// <returns></returns>
		public static SourceText AsText(this ITextSnapshot textSnapshot) => snapshotToSourceText.GetValue(textSnapshot, createSourceText);
		// Pass in UTF8 (or any other valid encoding) so we can compile and get a PDB file
		static readonly ConditionalWeakTable<ITextSnapshot, SourceText>.CreateValueCallback createSourceText = a => new TextSnapshotSourceText(a, Encoding.UTF8);
		static readonly ConditionalWeakTable<ITextSnapshot, SourceText> snapshotToSourceText = new ConditionalWeakTable<ITextSnapshot, SourceText>();

		/// <summary>
		/// Converts <paramref name="textBuffer"/> to a <see cref="SourceTextContainer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <returns></returns>
		public static SourceTextContainer AsTextContainer(this ITextBuffer textBuffer) => textBufferToContainer.GetValue(textBuffer, createSourceTextContainer);
		static readonly ConditionalWeakTable<ITextBuffer, SourceTextContainer>.CreateValueCallback createSourceTextContainer = a => new TextBufferSourceTextContainer(a);
		static readonly ConditionalWeakTable<ITextBuffer, SourceTextContainer> textBufferToContainer = new ConditionalWeakTable<ITextBuffer, SourceTextContainer>();

		/// <summary>
		/// Returns a <see cref="ITextBuffer"/> or null
		/// </summary>
		/// <param name="textContainer">Text container</param>
		/// <returns></returns>
		public static ITextBuffer TryGetTextBuffer(this SourceTextContainer textContainer) =>
			(textContainer as TextBufferSourceTextContainer)?.TextBuffer;

		/// <summary>
		/// Returns the workspace or null
		/// </summary>
		/// <param name="buffer">Text buffer</param>
		/// <returns></returns>
		public static Workspace TryGetWorkspace(this ITextBuffer buffer) {
			Workspace.TryGetWorkspace(buffer.AsTextContainer(), out var ws);
			return ws;
		}

		/// <summary>
		/// Gets the snapshot or null
		/// </summary>
		/// <param name="sourceText">Source text</param>
		/// <returns></returns>
		public static ITextSnapshot TryGetTextSnapshot(this SourceText sourceText) =>
			(sourceText as TextSnapshotSourceText)?.TextSnapshot;

		internal static TextChangeEventArgs ToTextChangeEventArgs(this TextContentChangedEventArgs e) =>
			new TextChangeEventArgs(e.Before.AsText(), e.After.AsText(), e.Changes.ToTextChangeRange());

		/// <summary>
		/// Converts <paramref name="span"/> to a <see cref="TextSpan"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public static TextSpan ToTextSpan(this Span span) => new TextSpan(span.Start, span.Length);

		/// <summary>
		/// Converts <paramref name="textSpan"/> to a <see cref="Span"/>
		/// </summary>
		/// <param name="textSpan"></param>
		/// <returns></returns>
		public static Span ToSpan(this TextSpan textSpan) => new Span(textSpan.Start, textSpan.Length);
		internal static TextChangeRange ToTextChangeRange(this ITextChange textChange) =>
			new TextChangeRange(textChange.OldSpan.ToTextSpan(), textChange.NewLength);

		internal static TextChangeRange[] ToTextChangeRange(this INormalizedTextChangeCollection changes) {
			var res = new TextChangeRange[changes.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = changes[i].ToTextChangeRange();
			return res;
		}

		/// <summary>
		/// Gets the document or null
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		public static Document GetOpenDocumentInCurrentContextWithChanges(this ITextSnapshot snapshot) =>
			snapshot.AsText().GetOpenDocumentInCurrentContextWithChanges();

		/// <summary>
		/// Gets the document or null
		/// </summary>
		/// <param name="text">Source text</param>
		/// <returns></returns>
		public static Document GetOpenDocumentInCurrentContextWithChanges(this SourceText text) {
			// This internal Roslyn method was copied from roslyn/src/Workspaces/Core/Portable/Workspace/TextExtensions.cs
			if (Workspace.TryGetWorkspace(text.Container, out var workspace)) {
				var id = workspace.GetDocumentIdInCurrentContext(text.Container);
				if (id == null || !workspace.CurrentSolution.ContainsDocument(id))
					return null;

				var sol = workspace.CurrentSolution.WithDocumentText(id, text, PreservationMode.PreserveIdentity);
				return sol.GetDocument(id);
			}

			return null;
		}
	}
}
