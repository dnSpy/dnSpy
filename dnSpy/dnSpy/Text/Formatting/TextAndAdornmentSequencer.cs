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

using System;
using System.Diagnostics;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Contracts.Text.Tagging;

namespace dnSpy.Text.Formatting {
	sealed class TextAndAdornmentSequencer : ITextAndAdornmentSequencer {
		public ITextBuffer SourceBuffer => textView.TextViewModel.EditBuffer;
		public ITextBuffer TopBuffer => textView.TextViewModel.VisualBuffer;

		readonly ITextView textView;
		readonly ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator;

		public TextAndAdornmentSequencer(ITextView textView, ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator) {
			if (textView == null)
				throw new System.ArgumentNullException(nameof(textView));
			if (tagAggregator == null)
				throw new System.ArgumentNullException(nameof(tagAggregator));
			this.textView = textView;
			this.tagAggregator = tagAggregator;
			textView.Closed += TextView_Closed;
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
		}

		public event EventHandler<TextAndAdornmentSequenceChangedEventArgs> SequenceChanged;

		void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e) =>
			SequenceChanged?.Invoke(this, new TextAndAdornmentSequenceChangedEventArgs(e.Span));

		public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(ITextSnapshotLine topLine, ITextSnapshot sourceTextSnapshot) {
			if (topLine == null)
				throw new ArgumentNullException(nameof(topLine));
			if (sourceTextSnapshot == null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (topLine.Snapshot.TextBuffer != TopBuffer)
				throw new InvalidOperationException();
			if (sourceTextSnapshot.TextBuffer != SourceBuffer)
				throw new InvalidOperationException();
			throw new NotImplementedException();//TODO:
		}

		public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(SnapshotSpan topSpan, ITextSnapshot sourceTextSnapshot) {
			if (topSpan.Snapshot == null)
				throw new ArgumentException();
			if (sourceTextSnapshot == null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (topSpan.Snapshot.TextBuffer != TopBuffer)
				throw new InvalidOperationException();
			if (sourceTextSnapshot.TextBuffer != SourceBuffer)
				throw new InvalidOperationException();
			throw new NotImplementedException();//TODO:
		}

		void TextView_Closed(object sender, EventArgs e) {
			Debug.Assert(textView.Properties.ContainsProperty(typeof(ITextAndAdornmentSequencer)));
			textView.Properties.RemoveProperty(typeof(ITextAndAdornmentSequencer));
			textView.Closed -= TextView_Closed;
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
		}
	}
}
