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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Tagging;

namespace dnSpy.Hex.Tagging {
	abstract class TagAggregator<T> where T : HexTag {
		readonly Dispatcher dispatcher;
		readonly List<HexBufferSpan> batchedTagsChangedList;
		readonly object lockObj;
		readonly HexTagAggregatorProxy hexTagAggregatorProxy;
		IHexTagger<T>[] taggers;

		internal HexTagAggregator<T> HexTagAggregator => hexTagAggregatorProxy;

		protected HexBuffer Buffer { get; }

		sealed class HexTagAggregatorProxy : HexTagAggregator<T> {
			public override HexBuffer Buffer => owner.Buffer;
			public override event EventHandler<HexBatchedTagsChangedEventArgs> BatchedTagsChanged;
			public override event EventHandler<HexTagsChangedEventArgs> TagsChanged;

			readonly TagAggregator<T> owner;

			public HexTagAggregatorProxy(TagAggregator<T> owner) => this.owner = owner;

			public override IEnumerable<IHexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans) =>
				owner.GetTags(spans);
			public override IEnumerable<IHexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken) =>
				owner.GetTags(spans, cancellationToken);
			public override IEnumerable<IHexTextTagSpan<T>> GetLineTags(HexTaggerContext context) =>
				owner.GetLineTags(context);
			public override IEnumerable<IHexTextTagSpan<T>> GetLineTags(HexTaggerContext context, CancellationToken cancellationToken) =>
				owner.GetLineTags(context, cancellationToken);
			public override IEnumerable<IHexTextTagSpan<T>> GetAllTags(HexTaggerContext context) =>
				owner.GetAllTags(context, null);
			public override IEnumerable<IHexTextTagSpan<T>> GetAllTags(HexTaggerContext context, CancellationToken cancellationToken) =>
				owner.GetAllTags(context, cancellationToken);

			public bool IsBatchedTagsChangedHooked => BatchedTagsChanged != null;
			public void RaiseTagsChanged(object sender, HexTagsChangedEventArgs e) => TagsChanged?.Invoke(sender, e);
			public void RaiseBatchedTagsChanged(object sender, HexBatchedTagsChangedEventArgs e) => BatchedTagsChanged?.Invoke(sender, e);
			protected override void DisposeCore() => owner.Dispose();
		}

		protected TagAggregator(HexBuffer buffer) {
			dispatcher = Dispatcher.CurrentDispatcher;
			batchedTagsChangedList = new List<HexBufferSpan>();
			lockObj = new object();
			Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			taggers = Array.Empty<IHexTagger<T>>();
			hexTagAggregatorProxy = new HexTagAggregatorProxy(this);
		}

		protected void Initialize() => RecreateTaggers();

		IEnumerable<IHexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (spans.Count == 0)
				yield break;
			foreach (var tagger in taggers) {
				foreach (var tagSpan in tagger.GetTags(spans)) {
					if (spans.IntersectsWith(tagSpan.Span))
						yield return tagSpan;
				}
			}
		}

		IEnumerable<IHexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (spans.Count == 0)
				yield break;
			foreach (var tagger in taggers) {
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var tagSpan in tagger.GetTags(spans, cancellationToken)) {
					if (spans.IntersectsWith(tagSpan.Span))
						yield return tagSpan;
				}
			}
		}

		IEnumerable<IHexTextTagSpan<T>> GetLineTags(HexTaggerContext context) {
			foreach (var tagger in taggers) {
				foreach (var tagSpan in tagger.GetTags(context))
					yield return tagSpan;
			}
		}

		IEnumerable<IHexTextTagSpan<T>> GetLineTags(HexTaggerContext context, CancellationToken cancellationToken) {
			foreach (var tagger in taggers) {
				foreach (var tagSpan in tagger.GetTags(context, cancellationToken))
					yield return tagSpan;
			}
		}

		IEnumerable<IHexTextTagSpan<T>> GetAllTags(HexTaggerContext context, CancellationToken? cancellationToken) {
			if (context.IsDefault)
				throw new ArgumentException();
			var span = context.Line.BufferSpan;
			var spans = new NormalizedHexBufferSpanCollection(span);
			var textSpan = context.LineSpan;
			foreach (var tagger in taggers) {
				var tags = cancellationToken != null ? tagger.GetTags(spans, cancellationToken.Value) : tagger.GetTags(spans);
				foreach (var tagSpan in tags) {
					var intersection = span.Intersection(tagSpan.Span);
					if (intersection == null)
						continue;

					foreach (var info in context.Line.GetSpans(intersection.Value, tagSpan.Flags)) {
						var vs = textSpan.Intersection(info.TextSpan);
						if (vs != null)
							yield return new HexTextTagSpan<T>(vs.Value, tagSpan.Tag);
					}
				}

				var textTags = cancellationToken != null ? tagger.GetTags(context, cancellationToken.Value) : tagger.GetTags(context);
				foreach (var tagSpan in textTags) {
					var intersection = textSpan.Intersection(tagSpan.Span);
					if (intersection != null)
						yield return new HexTextTagSpan<T>(intersection.Value, tagSpan.Tag);
				}
			}
		}

		void RecreateTaggers() {
			DisposeTaggers();

			taggers = CreateTaggers().ToArray();
			foreach (var t in taggers)
				t.TagsChanged += Tagger_TagsChanged;
		}

		protected abstract IEnumerable<IHexTagger<T>> CreateTaggers();

		void DisposeTaggers() {
			foreach (var t in taggers) {
				(t as IDisposable)?.Dispose();
				t.TagsChanged -= Tagger_TagsChanged;
			}
			taggers = Array.Empty<IHexTagger<T>>();
		}

		void Tagger_TagsChanged(object sender, HexBufferSpanEventArgs e) =>
			// Use original sender, not us
			RaiseTagsChanged(e.Span, sender);

		void RaiseTagsChanged(HexBufferSpan span, object sender = null) {
			if (IsDisposed)
				return;
			hexTagAggregatorProxy.RaiseTagsChanged(sender ?? taggers, new HexTagsChangedEventArgs(span));
			if (hexTagAggregatorProxy.IsBatchedTagsChangedHooked) {
				lock (lockObj) {
					batchedTagsChangedList.Add(span);
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
				List<HexBufferSpan> list;
				lock (lockObj) {
					list = new List<HexBufferSpan>(batchedTagsChangedList);
					batchedTagsChangedList.Clear();
				}
				hexTagAggregatorProxy.RaiseBatchedTagsChanged(hexTagAggregatorProxy, new HexBatchedTagsChangedEventArgs(list));
			}
		}

		protected bool IsDisposed { get; private set; }
		public virtual void Dispose() {
			IsDisposed = true;
			DisposeTaggers();
		}
	}
}
