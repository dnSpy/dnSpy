/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	public static class Extensions {
		// Pass in UTF8 (or any other valid encoding) so we can compile and get a PDB file
		static readonly ConditionalWeakTable<ITextSnapshot, SourceText>.CreateValueCallback createSourceText = a => new TextSnapshotSourceText(a, Encoding.UTF8);
		static readonly ConditionalWeakTable<ITextSnapshot, SourceText> snapshotToSourceText = new ConditionalWeakTable<ITextSnapshot, SourceText>();
		public static SourceText AsText(this ITextSnapshot textSnapshot) => snapshotToSourceText.GetValue(textSnapshot, createSourceText);

		static readonly ConditionalWeakTable<ITextBuffer, SourceTextContainer>.CreateValueCallback createSourceTextContainer = a => new TextBufferSourceTextContainer(a);
		static readonly ConditionalWeakTable<ITextBuffer, SourceTextContainer> textBufferToContainer = new ConditionalWeakTable<ITextBuffer, SourceTextContainer>();
		public static SourceTextContainer AsTextContainer(this ITextBuffer textBuffer) => textBufferToContainer.GetValue(textBuffer, createSourceTextContainer);

		public static ITextBuffer TryGetTextBuffer(this SourceTextContainer textContainer) =>
			(textContainer as TextBufferSourceTextContainer)?.TextBuffer;

		public static Workspace TryGetWorkspace(this ITextBuffer buffer) {
			Workspace ws;
			Workspace.TryGetWorkspace(buffer.AsTextContainer(), out ws);
			return ws;
		}

		public static ITextSnapshot TryGetTextSnapshot(this SourceText sourceText) =>
			(sourceText as TextSnapshotSourceText)?.TextSnapshot;

		internal static TextChangeEventArgs ToTextChangeEventArgs(this TextContentChangedEventArgs e) =>
			new TextChangeEventArgs(e.Before.AsText(), e.After.AsText(), e.Changes.ToTextChangeRange());
		public static TextSpan ToTextSpan(this Span span) => new TextSpan(span.Start, span.Length);
		public static Span ToSpan(this TextSpan textSpan) => new Span(textSpan.Start, textSpan.Length);
		internal static TextChangeRange ToTextChangeRange(this ITextChange textChange) =>
			new TextChangeRange(textChange.OldSpan.ToTextSpan(), textChange.NewLength);

		internal static TextChangeRange[] ToTextChangeRange(this INormalizedTextChangeCollection changes) {
			var res = new TextChangeRange[changes.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = changes[i].ToTextChangeRange();
			return res;
		}

		public static Document GetOpenDocumentInCurrentContextWithChanges(this ITextSnapshot text) =>
			text.AsText().GetOpenDocumentInCurrentContextWithChanges();

		// This internal Roslyn method was copied from roslyn/src/Workspaces/Core/Portable/Workspace/TextExtensions.cs
		public static Document GetOpenDocumentInCurrentContextWithChanges(this SourceText text) {
			Workspace workspace;
			if (Workspace.TryGetWorkspace(text.Container, out workspace)) {
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
