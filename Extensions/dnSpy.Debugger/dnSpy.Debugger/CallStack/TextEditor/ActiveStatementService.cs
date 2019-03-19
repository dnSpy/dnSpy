/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.CallStack.TextEditor;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.CallStack.TextEditor {
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Debuggable)]
	[TagType(typeof(ITextMarkerTag))]
	sealed class ActiveStatementTaggerProvider : IViewTaggerProvider {
		readonly ActiveStatementService activeStatementService;

		[ImportingConstructor]
		ActiveStatementTaggerProvider(ActiveStatementService activeStatementService) => this.activeStatementService = activeStatementService;

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (textView.TextBuffer != buffer)
				return null;
			return textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new ActiveStatementTagger(activeStatementService, textView)) as ITagger<T>;
		}
	}

	sealed class ActiveStatementTagger : ITagger<ITextMarkerTag> {
		public ITextView TextView { get; }
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly ActiveStatementService activeStatementService;

		public ActiveStatementTagger(ActiveStatementService activeStatementService, ITextView textView) {
			this.activeStatementService = activeStatementService;
			TextView = textView;
			TextView.Closed += TextView_Closed;
			activeStatementService.OnCreated(this);
		}

		public void RaiseTagsChanged(SnapshotSpan span) => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans) =>
			activeStatementService.GetTags(this, spans);

		void TextView_Closed(object sender, EventArgs e) {
			TextView.Closed -= TextView_Closed;
			activeStatementService.OnDisposed(this);
		}
	}

	[Export(typeof(ActiveStatementService))]
	sealed class ActiveStatementService {
		readonly HashSet<ActiveStatementTagger> taggers;
		readonly DbgStackFrameTextViewMarker[] dbgStackFrameTextViewMarkers;

		[ImportingConstructor]
		ActiveStatementService([ImportMany] IEnumerable<DbgStackFrameTextViewMarker> dbgStackFrameTextViewMarkers) {
			this.dbgStackFrameTextViewMarkers = dbgStackFrameTextViewMarkers.ToArray();
			taggers = new HashSet<ActiveStatementTagger>();
		}

		public void OnCreated(ActiveStatementTagger tagger) => taggers.Add(tagger);
		public void OnDisposed(ActiveStatementTagger tagger) => taggers.Remove(tagger);

		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(ActiveStatementTagger tagger, NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				yield break;

			var textView = tagger.TextView;
			foreach (var marker in dbgStackFrameTextViewMarkers) {
				foreach (var frameSpan in marker.GetFrameSpans(textView, spans))
					yield return new TagSpan<ITextMarkerTag>(frameSpan, activeStatementTextMarkerTag);
			}
		}
		static readonly TextMarkerTag activeStatementTextMarkerTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.ActiveStatementMarker);

		public void OnNewActiveStatements(ReadOnlyCollection<DbgStackFrame> frames) {
			foreach (var marker in dbgStackFrameTextViewMarkers)
				marker.OnNewFrames(frames);
			RaiseTagsChanged();
		}

		void RaiseTagsChanged() {
			foreach (var tagger in taggers) {
				//TODO: Optimize this by only raising tags-changed if needed
				var snapshot = tagger.TextView.TextSnapshot;
				tagger.RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
			}
		}
	}
}
