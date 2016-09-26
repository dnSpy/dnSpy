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

using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Documents.Tabs.DocViewer {
	static class SpanDataCollectionUtilities {
		public static SpanData<TData>? GetCurrentSpanReference<TData>(SpanDataCollection<TData> spanReferenceCollection, ITextView textView) {
			var caretPos = textView.Caret.Position;
			// There are no refs in virtual space
			if (caretPos.VirtualSpaces > 0)
				return null;
			var pos = caretPos.BufferPosition;

			// If it's at the end of a word wrapped line, don't mark the reference that's
			// shown on the next line.
			if (caretPos.Affinity == PositionAffinity.Predecessor && pos.Position != 0) {
				pos = pos - 1;
				var prevSpanData = spanReferenceCollection.Find(pos.Position);
				if (prevSpanData == null || prevSpanData.Value.Span.End != pos.Position)
					return prevSpanData;
				else
					return null;
			}
			else
				return spanReferenceCollection.Find(pos.Position);
		}
	}
}
