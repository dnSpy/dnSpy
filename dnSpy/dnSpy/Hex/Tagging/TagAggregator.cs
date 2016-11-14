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
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Tagging;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Hex.Tagging {
	abstract class TagAggregator<T> where T : HexTag {
		readonly Dispatcher dispatcher;
		readonly List<HexBufferSpan> batchedTagsChangedList;
		readonly object lockObj;
		readonly HexTagAggregatorProxy hexTagAggregatorProxy;
		HexTagger<T>[] taggers;

		internal HexTagAggregator<T> HexTagAggregator => hexTagAggregatorProxy;

		protected HexBuffer HexBuffer { get; }

		sealed class HexTagAggregatorProxy : HexTagAggregator<T> {
			public override HexBuffer Buffer => owner.HexBuffer;
			public override event EventHandler<HexBatchedTagsChangedEventArgs> BatchedTagsChanged;
			public override event EventHandler<HexTagsChangedEventArgs> TagsChanged;

			readonly TagAggregator<T> owner;

			public HexTagAggregatorProxy(TagAggregator<T> owner) {
				this.owner = owner;
			}

			public override IEnumerable<HexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans) =>
				owner.GetTags(spans);
			public override IEnumerable<HexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken) =>
				owner.GetTags(spans, cancellationToken);
			public override IEnumerable<HexTextTagSpan<T>> GetLineTags(HexTaggerContext context) =>
				owner.GetLineTags(context);
			public override IEnumerable<HexTextTagSpan<T>> GetLineTags(HexTaggerContext context, CancellationToken cancellationToken) =>
				owner.GetLineTags(context, cancellationToken);
			public override IEnumerable<HexTextTagSpan<T>> GetAllTags(HexTaggerContext context) =>
				owner.GetAllTags(context, null);
			public override IEnumerable<HexTextTagSpan<T>> GetAllTags(HexTaggerContext context, CancellationToken cancellationToken) =>
				owner.GetAllTags(context, cancellationToken);

			public bool IsBatchedTagsChangedHooked => BatchedTagsChanged != null;
			public void RaiseTagsChanged(object sender, HexTagsChangedEventArgs e) => TagsChanged?.Invoke(sender, e);
			public void RaiseBatchedTagsChanged(object sender, HexBatchedTagsChangedEventArgs e) => BatchedTagsChanged?.Invoke(sender, e);
			protected override void DisposeCore() => owner.Dispose();
		}

		protected TagAggregator(HexBuffer hexBuffer) {
			if (hexBuffer == null)
				throw new ArgumentNullException(nameof(hexBuffer));
			dispatcher = Dispatcher.CurrentDispatcher;
			batchedTagsChangedList = new List<HexBufferSpan>();
			lockObj = new object();
			HexBuffer = hexBuffer;
			taggers = Array.Empty<HexTagger<T>>();
			hexTagAggregatorProxy = new HexTagAggregatorProxy(this);
		}

		protected void Initialize() => RecreateTaggers();

		IEnumerable<HexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans) {
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

		IEnumerable<HexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken) {
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

		IEnumerable<HexTextTagSpan<T>> GetLineTags(HexTaggerContext context) {
			foreach (var tagger in taggers) {
				foreach (var tagSpan in tagger.GetTags(context))
					yield return tagSpan;
			}
		}

		IEnumerable<HexTextTagSpan<T>> GetLineTags(HexTaggerContext context, CancellationToken cancellationToken) {
			foreach (var tagger in taggers) {
				foreach (var tagSpan in tagger.GetTags(context, cancellationToken))
					yield return tagSpan;
			}
		}

		static HexCellSpanFlags GetCellSpanFlags(HexTagSpanFlags flags) {
			var res = HexCellSpanFlags.None;
			if ((flags & HexTagSpanFlags.Cell) != 0)
				res |= HexCellSpanFlags.Cell;
			if ((flags & HexTagSpanFlags.Separator) != 0)
				res |= HexCellSpanFlags.Separator;
			return res;
		}

		IEnumerable<HexTextTagSpan<T>> GetAllTags(HexTaggerContext context, CancellationToken? cancellationToken) {
			if (context.IsDefault)
				throw new ArgumentException();
			var span = context.Line.VisibleBytesSpan;
			var spans = new NormalizedHexBufferSpanCollection(span);
			var textSpan = context.LineSpan;
			foreach (var tagger in taggers) {
				var tags = cancellationToken != null ? tagger.GetTags(spans, cancellationToken.Value) : tagger.GetTags(spans);
				foreach (var tagSpan in tags) {
					var intersection = span.Intersection(tagSpan.Span);
					if (intersection == null)
						continue;

					var spanFlags = tagSpan.Flags;

					if (context.Line.IsOffsetColumnPresent && (spanFlags & HexTagSpanFlags.Offset) != 0) {
						var offsetSpan = textSpan.Intersection(context.Line.GetOffsetSpan());
						if (offsetSpan != null)
							yield return new HexTextTagSpan<T>(offsetSpan.Value, tagSpan.Tag);
					}

					if (context.Line.IsValuesColumnPresent && (spanFlags & HexTagSpanFlags.Values) != 0) {
						if ((spanFlags & HexTagSpanFlags.OneValue) != 0) {
							var flags = GetCellSpanFlags(spanFlags);
							foreach (var cell in context.Line.ValueCells.GetCells(intersection.Value)) {
								var cellSpan = textSpan.Intersection(cell.GetSpan(flags));
								if (cellSpan != null)
									yield return new HexTextTagSpan<T>(cellSpan.Value, tagSpan.Tag);
							}
						}
						else {
							Span valuesSpan;
							if ((spanFlags & HexTagSpanFlags.AllCells) != 0)
								valuesSpan = context.Line.GetValuesSpan(onlyVisibleCells: false);
							else if ((spanFlags & HexTagSpanFlags.AllVisibleCells) != 0)
								valuesSpan = context.Line.GetValuesSpan(onlyVisibleCells: true);
							else
								valuesSpan = context.Line.GetValuesSpan(intersection.Value, GetCellSpanFlags(spanFlags)).TextSpan;
							var vs = textSpan.Intersection(valuesSpan);
							if (vs != null)
								yield return new HexTextTagSpan<T>(vs.Value, tagSpan.Tag);
						}
					}

					if (context.Line.IsAsciiColumnPresent && (spanFlags & HexTagSpanFlags.Ascii) != 0) {
						Span asciiSpan;
						if ((spanFlags & HexTagSpanFlags.AllCells) != 0)
							asciiSpan = context.Line.GetAsciiSpan(onlyVisibleCells: false);
						else if ((spanFlags & HexTagSpanFlags.AllVisibleCells) != 0)
							asciiSpan = context.Line.GetAsciiSpan(onlyVisibleCells: true);
						else
							asciiSpan = context.Line.GetAsciiSpan(intersection.Value, GetCellSpanFlags(spanFlags)).TextSpan;
						var vs = textSpan.Intersection(asciiSpan);
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

		protected abstract IEnumerable<HexTagger<T>> CreateTaggers();

		void DisposeTaggers() {
			foreach (var t in taggers) {
				(t as IDisposable)?.Dispose();
				t.TagsChanged -= Tagger_TagsChanged;
			}
			taggers = Array.Empty<HexTagger<T>>();
		}

		void Tagger_TagsChanged(object sender, HexBufferSpanEventArgs e) {
			// Use original sender, not us
			RaiseTagsChanged(e.Span, sender);
		}

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
