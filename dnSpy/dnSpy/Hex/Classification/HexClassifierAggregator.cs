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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Tagging;
using VST = Microsoft.VisualStudio.Text;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	abstract class HexClassifierAggregator : HexClassifier {
		readonly VSTC.IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly HexTagAggregator<HexClassificationTag> hexTagAggregator;
		readonly HexBuffer buffer;

		public override event EventHandler<HexClassificationChangedEventArgs> ClassificationChanged;

		protected HexClassifierAggregator(HexTagAggregator<HexClassificationTag> hexTagAggregator, VSTC.IClassificationTypeRegistryService classificationTypeRegistryService, HexBuffer buffer) {
			if (hexTagAggregator == null)
				throw new ArgumentNullException(nameof(hexTagAggregator));
			if (classificationTypeRegistryService == null)
				throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.hexTagAggregator = hexTagAggregator;
			this.buffer = buffer;
			hexTagAggregator.TagsChanged += HexTagAggregator_TagsChanged;
		}

		void HexTagAggregator_TagsChanged(object sender, HexTagsChangedEventArgs e) =>
			ClassificationChanged?.Invoke(this, new HexClassificationChangedEventArgs(e.Span));

		sealed class HexClassificationSpanComparer : IComparer<HexClassificationSpan> {
			public static readonly HexClassificationSpanComparer Instance = new HexClassificationSpanComparer();
			public int Compare(HexClassificationSpan x, HexClassificationSpan y) => x.Span.Start - y.Span.Start;
		}

		public override void GetClassificationSpans(List<HexClassificationSpan> result, HexClassificationContext context) =>
			GetClassificationSpansCore(result, context, null);

		public override void GetClassificationSpans(List<HexClassificationSpan> result, HexClassificationContext context, CancellationToken cancellationToken) =>
			GetClassificationSpansCore(result, context, cancellationToken);

		void GetClassificationSpansCore(List<HexClassificationSpan> result, HexClassificationContext context, CancellationToken? cancellationToken) {
			if (context.IsDefault)
				throw new ArgumentException();
			var textSpan = context.LineSpan;
			var list = new List<HexClassificationSpan>();

			var taggerContext = new HexTaggerContext(context.Line, context.LineSpan);
			var tags = cancellationToken != null ? hexTagAggregator.GetAllTags(taggerContext, cancellationToken.Value) : hexTagAggregator.GetAllTags(taggerContext);
			foreach (var tagSpan in tags) {
				var overlap = textSpan.Overlap(tagSpan.Span);
				if (overlap != null)
					list.Add(new HexClassificationSpan(overlap.Value, tagSpan.Tag.ClassificationType));
			}

			if (list.Count <= 1) {
				if (list.Count == 1)
					result.Add(list[0]);
				return;
			}

			list.Sort(HexClassificationSpanComparer.Instance);

			// Common case
			if (!HasOverlaps(list)) {
				result.AddRange(Merge(list));
				return;
			}

			int min = 0;
			int minOffset = textSpan.Start;
			var newList = new List<HexClassificationSpan>();
			var ctList = new List<VSTC.IClassificationType>();
			while (min < list.Count) {
				while (min < list.Count && minOffset >= list[min].Span.End)
					min++;
				if (min >= list.Count)
					break;
				var cspan = list[min];
				minOffset = Math.Max(minOffset, cspan.Span.Start);
				int end = cspan.Span.End;
				ctList.Clear();
				ctList.Add(cspan.ClassificationType);
				for (int i = min + 1; i < list.Count; i++) {
					cspan = list[i];
					int cspanStart = cspan.Span.Start;
					if (cspanStart > minOffset) {
						if (cspanStart < end)
							end = cspanStart;
						break;
					}
					int cspanEnd = cspan.Span.End;
					if (minOffset >= cspanEnd)
						continue;
					if (cspanEnd < end)
						end = cspanEnd;
					if (!ctList.Contains(cspan.ClassificationType))
						ctList.Add(cspan.ClassificationType);
				}
				Debug.Assert(minOffset < end);
				var ct = ctList.Count == 1 ? ctList[0] : classificationTypeRegistryService.CreateTransientClassificationType(ctList);
				newList.Add(new HexClassificationSpan(VST.Span.FromBounds(minOffset, end), ct));
				minOffset = end;
			}

			Debug.Assert(!HasOverlaps(newList));
			result.AddRange(Merge(newList));
			return;
		}

		static List<HexClassificationSpan> Merge(List<HexClassificationSpan> list) {
			if (list.Count <= 1)
				return list;

			var prev = list[0];
			int read = 1, write = 0;
			for (; read < list.Count; read++) {
				var a = list[read];
				if (prev.ClassificationType == a.ClassificationType && prev.Span.End == a.Span.Start)
					list[write] = prev = new HexClassificationSpan(VST.Span.FromBounds(prev.Span.Start, a.Span.End), prev.ClassificationType);
				else {
					prev = a;
					list[++write] = a;
				}
			}
			write++;
			if (list.Count != write)
				list.RemoveRange(write, list.Count - write);

			return list;
		}

		static bool HasOverlaps(List<HexClassificationSpan> sortedList) {
			for (int i = 1; i < sortedList.Count; i++) {
				if (sortedList[i - 1].Span.End > sortedList[i].Span.Start)
					return true;
			}
			return false;
		}

		protected override void DisposeCore() {
			hexTagAggregator.TagsChanged -= HexTagAggregator_TagsChanged;
			hexTagAggregator.Dispose();
		}
	}
}
