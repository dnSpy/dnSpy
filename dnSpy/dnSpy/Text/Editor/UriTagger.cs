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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IViewTaggerProvider))]
	[TagType(typeof(IUriTag))]
	[ContentType(ContentTypes.Text)]
	sealed class UriTaggerProvider : IViewTaggerProvider {
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag =>
			textView.Properties.GetOrCreateSingletonProperty(typeof(UriTagger), () => new UriTagger(textView)) as ITagger<T>;
	}

	sealed class UriTagger : ITagger<IUriTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly ITextView textView;
		readonly int maxLineLength;
		bool enableLinks;

		public UriTagger(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			this.maxLineLength = textView.Options.GetOptionValue(DefaultOptions.LongBufferLineThresholdId);
			textView.Closed += TextView_Closed;
			textView.Options.OptionChanged += Options_OptionChanged;
			UpdateOptions();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) => UpdateOptions();
		void UpdateOptions() {
			bool newValue = textView.Options.GetOptionValue(DefaultTextViewOptions.DisplayUrlsAsHyperlinksId);
			if (newValue == enableLinks)
				return;
			enableLinks = newValue;
			var snapshot = textView.TextSnapshot;
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
		}

		public IEnumerable<ITagSpan<IUriTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (!enableLinks)
				yield break;

			ITextSnapshotLine line = null;
			foreach (var span in spans) {
				int pos = span.Start;

				// We check full lines so make sure we don't re-check the same line again
				if (line != null && line.ExtentIncludingLineBreak.End.Position > pos)
					continue;

				for (;;) {
					if (line != null && line.ExtentIncludingLineBreak.End.Position == pos) {
						if (line.Snapshot.LineCount == line.LineNumber + 1)
							break;
						line = line.Snapshot.GetLineFromLineNumber(line.LineNumber + 1);
					}
					else {
						Debug.Assert(line == null || pos > line.ExtentIncludingLineBreak.End.Position);
						line = span.Snapshot.GetLineFromPosition(pos);
					}

					if (line.Length != 0 && line.Length <= maxLineLength) {
						var lineText = line.GetText();
						var uriFinder = new UriFinder(lineText);
						for (;;) {
							var res = uriFinder.GetNext();
							if (res == null)
								break;
							Debug.Assert(res.Value.Length != 0);
							if (res.Value.Length == 0)
								break;
							int start = line.Start.Position + res.Value.Start;
							int end = start + res.Value.Length;
							Debug.Assert(end <= line.Snapshot.Length);
							if (end > line.Snapshot.Length)
								break;
							yield return new TagSpan<IUriTag>(new SnapshotSpan(line.Snapshot, start, res.Value.Length), UriTag.Instance);
						}
					}

					pos = line.ExtentIncludingLineBreak.End;
					if (pos >= span.End)
						break;
				}
			}
		}

		void TextView_Closed(object sender, EventArgs e) {
			textView.Closed -= TextView_Closed;
			textView.Options.OptionChanged -= Options_OptionChanged;
		}
	}
}
