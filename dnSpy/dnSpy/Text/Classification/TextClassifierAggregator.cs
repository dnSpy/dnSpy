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
using System.Linq;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text.Classification {
	// The class is very similar to ClassifierAggregatorBase. In theory the code could be
	// re-used but that would probably slow down ClassifierAggregatorBase which is used by
	// the text editor code.
	sealed class TextClassifierAggregator : ITextClassifierAggregator {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly ITextClassifier[] textClassifiers;

		public TextClassifierAggregator(IClassificationTypeRegistryService classificationTypeRegistryService, IEnumerable<ITextClassifier> textClassifiers) {
			if (classificationTypeRegistryService == null)
				throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			if (textClassifiers == null)
				throw new ArgumentNullException(nameof(textClassifiers));
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.textClassifiers = textClassifiers.ToArray();
		}

		sealed class TextClassificationTagComparer : IComparer<TextClassificationTag> {
			public static readonly TextClassificationTagComparer Instance = new TextClassificationTagComparer();
			public int Compare(TextClassificationTag x, TextClassificationTag y) => x.Span.Start - y.Span.Start;
		}

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var list = new List<TextClassificationTag>();

			int textLength = context.Text.Length;
			foreach (var classifier in textClassifiers) {
				foreach (var tagTmp in classifier.GetTags(context)) {
					var tag = tagTmp;
					if (tag.Span.End > textLength)
						tag = new TextClassificationTag(Span.FromBounds(Math.Min(textLength, tag.Span.Start), Math.Min(textLength, tag.Span.End)), tag.ClassificationType);
					if (tag.Span.Length == 0)
						continue;
					list.Add(tag);
				}
			}

			if (list.Count <= 1)
				return list;

			list.Sort(TextClassificationTagComparer.Instance);

			// Common case
			if (!HasOverlaps(list))
				return Merge(list);

			int min = 0;
			int minOffset = 0;
			var newList = new List<TextClassificationTag>();
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
				var newSpan = new Span(minOffset, end - minOffset);
				var ct = ctList.Count == 1 ? ctList[0] : classificationTypeRegistryService.CreateTransientClassificationType(ctList);
				newList.Add(new TextClassificationTag(newSpan, ct));
				minOffset = end;
			}

			Debug.Assert(!HasOverlaps(newList));
			return Merge(newList);
		}

		static List<TextClassificationTag> Merge(List<TextClassificationTag> list) {
			if (list.Count <= 1)
				return list;

			var prev = list[0];
			int read = 1, write = 0;
			for (; read < list.Count; read++) {
				var a = list[read];
				if (prev.ClassificationType == a.ClassificationType && prev.Span.End == a.Span.Start)
					list[write] = prev = new TextClassificationTag(Span.FromBounds(prev.Span.Start, a.Span.End), prev.ClassificationType);
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

		static bool HasOverlaps(List<TextClassificationTag> sortedList) {
			for (int i = 1; i < sortedList.Count; i++) {
				if (sortedList[i - 1].Span.End > sortedList[i].Span.Start)
					return true;
			}
			return false;
		}

		public void Dispose() {
			foreach (var classifier in textClassifiers)
				(classifier as IDisposable)?.Dispose();
		}
	}
}
