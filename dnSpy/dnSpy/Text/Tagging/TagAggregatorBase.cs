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
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging {
	abstract class TagAggregatorBase<T> : ITagAggregator<T> where T : ITag {
		readonly Dispatcher dispatcher;
		readonly List<IMappingSpan> batchedTagsChangedList;
		readonly object lockObj;
		protected ITextBuffer TextBuffer { get; }
		ITagger<T>[] taggers;

		public IBufferGraph BufferGraph {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		protected TagAggregatorBase(ITextBuffer textBuffer, TagAggregatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.batchedTagsChangedList = new List<IMappingSpan>();
			this.lockObj = new object();
			TextBuffer = textBuffer;
			TextBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
			this.taggers = Array.Empty<ITagger<T>>();
		}

		protected void Initialize() => RecreateTaggers();

		public event EventHandler<BatchedTagsChangedEventArgs> BatchedTagsChanged;
		public event EventHandler<TagsChangedEventArgs> TagsChanged;

		public IEnumerable<IMappingTagSpan<T>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans) {
			if (snapshotSpans == null)
				throw new ArgumentNullException(nameof(snapshotSpans));
			if (snapshotSpans.Count == 0)
				yield break;
			var snapshotSpansSnapshot = snapshotSpans[0].Snapshot;
			foreach (var tagger in taggers) {
				foreach (var tagSpan in tagger.GetTags(snapshotSpans)) {
					var newSpan = tagSpan.Span.TranslateTo(snapshotSpansSnapshot, SpanTrackingMode.EdgeExclusive);
					if (snapshotSpans.IntersectsWith(newSpan))
						yield return new MappingTagSpan<T>(new MappingSpan(tagSpan.Span, SpanTrackingMode.EdgeExclusive), tagSpan.Tag);
				}
			}
		}

		public IEnumerable<IMappingTagSpan<T>> GetTags(IMappingSpan span) {
			if (span == null)
				throw new ArgumentNullException(nameof(span));
			return GetTags(span.GetSpans(TextBuffer));
		}

		public IEnumerable<IMappingTagSpan<T>> GetTags(SnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			return GetTags(new NormalizedSnapshotSpanCollection(span));
		}

		void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			RecreateTaggers();
			RaiseTagsChanged(new SnapshotSpan(TextBuffer.CurrentSnapshot, 0, TextBuffer.CurrentSnapshot.Length));
		}

		void RecreateTaggers() {
			DisposeTaggers();

			taggers = CreateTaggers().ToArray();
			foreach (var t in taggers)
				t.TagsChanged += Tagger_TagsChanged;
		}

		protected abstract IEnumerable<ITagger<T>> CreateTaggers();

		void DisposeTaggers() {
			foreach (var t in taggers) {
				(t as IDisposable)?.Dispose();
				t.TagsChanged -= Tagger_TagsChanged;
			}
			taggers = Array.Empty<ITagger<T>>();
		}

		void Tagger_TagsChanged(object sender, SnapshotSpanEventArgs e) {
			// Use original sender, not us
			RaiseTagsChanged(e.Span, sender);
		}

		void RaiseTagsChanged(SnapshotSpan span, object sender = null) {
			if (IsDisposed)
				return;
			IMappingSpan mappingSpan = null;
			TagsChanged?.Invoke(sender ?? this, new TagsChangedEventArgs(mappingSpan = new MappingSpan(span, SpanTrackingMode.EdgeExclusive)));
			if (BatchedTagsChanged != null) {
				lock (lockObj) {
					batchedTagsChangedList.Add(mappingSpan ?? new MappingSpan(span, SpanTrackingMode.EdgeExclusive));
					if (batchedTagsChangedList.Count == 1)
						dispatcher.BeginInvoke(new Action(RaiseOnUIThread), DispatcherPriority.Normal);
				}
			}
		}

		protected virtual bool CanRaiseBatchedTagsChanged => true;

		void RaiseOnUIThread() {
			if (IsDisposed)
				return;
			if (!CanRaiseBatchedTagsChanged)
				dispatcher.BeginInvoke(new Action(RaiseOnUIThread), DispatcherPriority.Normal);
			else {
				List<IMappingSpan> list;
				lock (lockObj) {
					list = new List<IMappingSpan>(batchedTagsChangedList);
					batchedTagsChangedList.Clear();
				}
				BatchedTagsChanged?.Invoke(this, new BatchedTagsChangedEventArgs(list));
			}
		}

		protected bool IsDisposed { get; private set; }
		public virtual void Dispose() {
			IsDisposed = true;
			TextBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			DisposeTaggers();
		}
	}
}
