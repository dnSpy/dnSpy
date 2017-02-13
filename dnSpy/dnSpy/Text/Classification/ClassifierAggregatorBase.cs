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
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Classification {
	abstract class ClassifierAggregatorBase : ISynchronousClassifier, IDisposable {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly ISynchronousTagAggregator<IClassificationTag> tagAggregator;
		readonly ITextBuffer textBuffer;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		protected ClassifierAggregatorBase(ISynchronousTagAggregator<IClassificationTag> tagAggregator, IClassificationTypeRegistryService classificationTypeRegistryService, ITextBuffer textBuffer) {
			this.classificationTypeRegistryService = classificationTypeRegistryService ?? throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			this.tagAggregator = tagAggregator ?? throw new ArgumentNullException(nameof(tagAggregator));
			this.textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
		}

		void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e) {
			if (ClassificationChanged == null)
				return;
			foreach (var span in e.Span.GetSpans(textBuffer))
				ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
		}

		sealed class ClassificationSpanComparer : IComparer<ClassificationSpan> {
			public static readonly ClassificationSpanComparer Instance = new ClassificationSpanComparer();
			public int Compare(ClassificationSpan x, ClassificationSpan y) => x.Span.Start.Position - y.Span.Start.Position;
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) =>
			GetClassificationSpansCore(span, null);

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span, CancellationToken cancellationToken) =>
			GetClassificationSpansCore(span, cancellationToken);

		IList<ClassificationSpan> GetClassificationSpansCore(SnapshotSpan span, CancellationToken? cancellationToken) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (span.Length == 0)
				return Array.Empty<ClassificationSpan>();

			var list = new List<ClassificationSpan>();
			var targetSnapshot = span.Snapshot;
			var tags = cancellationToken != null ? tagAggregator.GetTags(span, cancellationToken.Value) : tagAggregator.GetTags(span);
			foreach (var mspan in tags) {
				foreach (var s in mspan.Span.GetSpans(textBuffer)) {
					var overlap = span.Overlap(s.TranslateTo(targetSnapshot, SpanTrackingMode.EdgeExclusive));
					if (overlap != null)
						list.Add(new ClassificationSpan(overlap.Value, mspan.Tag.ClassificationType));
				}
			}

			if (list.Count <= 1)
				return list;

			list.Sort(ClassificationSpanComparer.Instance);

			// Common case
			if (!HasOverlaps(list))
				return Merge(list);

			int min = 0;
			int minOffset = span.Start.Position;
			var newList = new List<ClassificationSpan>();
			var ctList = new List<IClassificationType>();
			while (min < list.Count) {
				while (min < list.Count && minOffset >= list[min].Span.End)
					min++;
				if (min >= list.Count)
					break;
				var cspan = list[min];
				minOffset = Math.Max(minOffset, cspan.Span.Start.Position);
				int end = cspan.Span.End.Position;
				ctList.Clear();
				ctList.Add(cspan.ClassificationType);
				for (int i = min + 1; i < list.Count; i++) {
					cspan = list[i];
					int cspanStart = cspan.Span.Start.Position;
					if (cspanStart > minOffset) {
						if (cspanStart < end)
							end = cspanStart;
						break;
					}
					int cspanEnd = cspan.Span.End.Position;
					if (minOffset >= cspanEnd)
						continue;
					if (cspanEnd < end)
						end = cspanEnd;
					if (!ctList.Contains(cspan.ClassificationType))
						ctList.Add(cspan.ClassificationType);
				}
				Debug.Assert(minOffset < end);
				var newSnapshotSpan = new SnapshotSpan(targetSnapshot, minOffset, end - minOffset);
				var ct = ctList.Count == 1 ? ctList[0] : classificationTypeRegistryService.CreateTransientClassificationType(ctList);
				newList.Add(new ClassificationSpan(newSnapshotSpan, ct));
				minOffset = end;
			}

			Debug.Assert(!HasOverlaps(newList));
			return Merge(newList);
		}

		static List<ClassificationSpan> Merge(List<ClassificationSpan> list) {
			if (list.Count <= 1)
				return list;

			var prev = list[0];
			int read = 1, write = 0;
			for (; read < list.Count; read++) {
				var a = list[read];
				if (prev.ClassificationType == a.ClassificationType && prev.Span.End == a.Span.Start)
					list[write] = prev = new ClassificationSpan(new SnapshotSpan(prev.Span.Start, a.Span.End), prev.ClassificationType);
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

		static bool HasOverlaps(List<ClassificationSpan> sortedList) {
			for (int i = 1; i < sortedList.Count; i++) {
				if (sortedList[i - 1].Span.End > sortedList[i].Span.Start)
					return true;
			}
			return false;
		}

		public void Dispose() {
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
		}
	}
}
