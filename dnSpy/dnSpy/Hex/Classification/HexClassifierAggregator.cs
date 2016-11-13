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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	abstract class HexClassifierAggregator : HexClassifier {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly HexTagAggregator<HexClassificationTag> hexTagAggregator;
		readonly HexBuffer hexBuffer;

		public override event EventHandler<HexClassificationChangedEventArgs> ClassificationChanged;

		protected HexClassifierAggregator(HexTagAggregator<HexClassificationTag> hexTagAggregator, IClassificationTypeRegistryService classificationTypeRegistryService, HexBuffer hexBuffer) {
			if (hexTagAggregator == null)
				throw new ArgumentNullException(nameof(hexTagAggregator));
			if (classificationTypeRegistryService == null)
				throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			if (hexBuffer == null)
				throw new ArgumentNullException(nameof(hexBuffer));
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.hexTagAggregator = hexTagAggregator;
			this.hexBuffer = hexBuffer;
			hexTagAggregator.TagsChanged += HexTagAggregator_TagsChanged;
		}

		void HexTagAggregator_TagsChanged(object sender, HexTagsChangedEventArgs e) =>
			ClassificationChanged?.Invoke(this, new HexClassificationChangedEventArgs(e.Span));

		sealed class HexClassificationSpanComparer : IComparer<HexClassificationSpan> {
			public static readonly HexClassificationSpanComparer Instance = new HexClassificationSpanComparer();
			public int Compare(HexClassificationSpan x, HexClassificationSpan y) => x.Span.Start - y.Span.Start;
		}

		static HexCellSpanFlags GetCellSpanFlags(HexTagSpanFlags flags) {
			var res = HexCellSpanFlags.None;
			if ((flags & HexTagSpanFlags.Cell) != 0)
				res |= HexCellSpanFlags.Cell;
			if ((flags & HexTagSpanFlags.Separator) != 0)
				res |= HexCellSpanFlags.Separator;
			return res;
		}

		public override void GetClassificationSpans(List<HexClassificationSpan> result, HexClassificationContext context) =>
			GetClassificationSpansCore(result, context, null);

		public override void GetClassificationSpans(List<HexClassificationSpan> result, HexClassificationContext context, CancellationToken cancellationToken) =>
			GetClassificationSpansCore(result, context, cancellationToken);

		void GetClassificationSpansCore(List<HexClassificationSpan> result, HexClassificationContext context, CancellationToken? cancellationToken) {
			var span = context.Line.VisibleBytesSpan;
			var textSpan = new Span(0, context.Line.Text.Length);
			var list = new List<HexClassificationSpan>();

			var tags = cancellationToken != null ? hexTagAggregator.GetTags(span, cancellationToken.Value) : hexTagAggregator.GetTags(span);
			foreach (var tagSpan in tags) {
				var overlap = span.Overlap(tagSpan.Span);
				if (overlap == null)
					continue;

				var spanFlags = tagSpan.Flags;

				if ((spanFlags & HexTagSpanFlags.Offset) != 0) {
					var offsetSpan = context.Line.GetOffsetSpan();
					if (offsetSpan.Length != 0)
						list.Add(new HexClassificationSpan(offsetSpan, tagSpan.Tag.ClassificationType));
				}

				if ((spanFlags & HexTagSpanFlags.Values) != 0) {
					if ((spanFlags & HexTagSpanFlags.OneValue) != 0) {
						var flags = GetCellSpanFlags(spanFlags);
						foreach (var cell in context.Line.ValueCells.GetCells(overlap.Value)) {
							var cellSpan = cell.GetSpan(flags);
							if (cellSpan.Length != 0)
								list.Add(new HexClassificationSpan(cellSpan, tagSpan.Tag.ClassificationType));
						}
					}
					else {
						Span valuesSpan;
						if ((spanFlags & HexTagSpanFlags.AllCells) != 0)
							valuesSpan = context.Line.GetValuesSpan(onlyVisibleCells: false);
						else if ((spanFlags & HexTagSpanFlags.AllVisibleCells) != 0)
							valuesSpan = context.Line.GetValuesSpan(onlyVisibleCells: true);
						else
							valuesSpan = context.Line.GetValuesSpan(overlap.Value, GetCellSpanFlags(spanFlags)).TextSpan;
						if (valuesSpan.Length != 0)
							list.Add(new HexClassificationSpan(valuesSpan, tagSpan.Tag.ClassificationType));
					}
				}

				if ((spanFlags & HexTagSpanFlags.Ascii) != 0) {
					Span asciiSpan;
					if ((spanFlags & HexTagSpanFlags.AllCells) != 0)
						asciiSpan = context.Line.GetAsciiSpan(onlyVisibleCells: false);
					else if ((spanFlags & HexTagSpanFlags.AllVisibleCells) != 0)
						asciiSpan = context.Line.GetAsciiSpan(onlyVisibleCells: true);
					else
						asciiSpan = context.Line.GetAsciiSpan(overlap.Value, GetCellSpanFlags(spanFlags)).TextSpan;
					if (asciiSpan.Length != 0)
						list.Add(new HexClassificationSpan(asciiSpan, tagSpan.Tag.ClassificationType));
				}
			}

			var taggerContext = new HexTaggerContext(context.Line);
			var textTags = cancellationToken != null ? hexTagAggregator.GetTags(taggerContext, cancellationToken.Value) : hexTagAggregator.GetTags(taggerContext);
			foreach (var tagSpan in textTags) {
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
			var ctList = new List<IClassificationType>();
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
				newList.Add(new HexClassificationSpan(Span.FromBounds(minOffset, end), ct));
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
					list[write] = prev = new HexClassificationSpan(Span.FromBounds(prev.Span.Start, a.Span.End), prev.ClassificationType);
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
