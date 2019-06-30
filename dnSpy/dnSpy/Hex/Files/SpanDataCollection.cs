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

using System.Collections.Generic;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex.Files {
	sealed class SpanDataCollection<TData> where TData : class {
		public int Count => spanDataList.Count;
		public SpanData<TData> this[int index] => spanDataList[index];
		readonly List<SpanData<TData>> spanDataList;

		sealed class SpanDataComparer : IComparer<SpanData<TData>> {
			public static readonly SpanDataComparer Instance = new SpanDataComparer();
			public int Compare(SpanData<TData> x, SpanData<TData> y) {
				var c = x.Span.Start.CompareTo(y.Span.Start);
				if (c != 0)
					return c;
				return x.Span.Length.CompareTo(y.Span.Length);
			}
		}

		public SpanDataCollection() => spanDataList = new List<SpanData<TData>>();

		public TData? FindData(HexPosition position) {
			int index = GetStartIndex(position);
			var list = spanDataList;
			if (index < 0 || !list[index].Span.Contains(position))
				return null;
			return list[index].Data;
		}

		int GetStartIndex(HexPosition position) {
			var list = spanDataList;
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var spanData = list[index];
				if (position < spanData.Span.Start)
					hi = index - 1;
				else if (position >= spanData.Span.End)
					lo = index + 1;
				else
					return index;
			}
			return lo < list.Count ? lo : -1;
		}

		public void Add(SpanData<TData> data) {
			spanDataList.Add(data);
			SortList();
		}

		public void Add(IEnumerable<SpanData<TData>> data) {
			spanDataList.AddRange(data);
			SortList();
		}

		public SpanData<TData>[] Remove(IEnumerable<HexSpan> spans) {
			var list = spanDataList;

			var removedFiles = new List<SpanData<TData>>();
			foreach (var span in spans) {
				if (list.Count == 0)
					break;
				if (span.Start < list[0].Span.End && list[list.Count - 1].Span.Start < span.End) {
					removedFiles.AddRange(list);
					list.Clear();
					break;
				}

				int index = GetStartIndex(span.Start);
				if (index < 0)
					continue;
				int startIndex = index;
				while (index < list.Count) {
					var info = list[index];
					if (!info.Span.OverlapsWith(span))
						break;
					removedFiles.Add(info);
					index++;
				}
				list.RemoveRange(startIndex, index - startIndex);
			}

			return removedFiles.ToArray();
		}

		void SortList() => spanDataList.Sort(SpanDataComparer.Instance);
	}

	readonly struct SpanData<TData> {
		public HexSpan Span { get; }
		public TData Data { get; }
		public SpanData(HexSpan span, TData data) {
			Span = span;
			Data = data;
		}
		public override string ToString() => "[" + Span.ToString() + "]";
	}
}
