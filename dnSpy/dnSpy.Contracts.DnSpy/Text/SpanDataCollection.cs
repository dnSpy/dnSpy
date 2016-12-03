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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Span and data collection, sorted by span, no overlaps, see also <see cref="SpanDataCollectionBuilder{TData}"/>
	/// </summary>
	/// <typeparam name="TData">Type of data</typeparam>
	public sealed class SpanDataCollection<TData> : IEnumerable<SpanData<TData>> {
		/// <summary>
		/// Gets the empty instance
		/// </summary>
		public static readonly SpanDataCollection<TData> Empty = new SpanDataCollection<TData>(Array.Empty<SpanData<TData>>());

		/// <summary>
		/// Gets the number of elements
		/// </summary>
		public int Count => spanDataArray.Length;

		/// <summary>
		/// Gets the element at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public SpanData<TData> this[int index] => spanDataArray[index];

		readonly SpanData<TData>[] spanDataArray;

		/// <summary>
		/// Constructor, see also <see cref="SpanDataCollectionBuilder{TData}"/>
		/// </summary>
		/// <param name="spanDataArray">Span and data collection</param>
		public SpanDataCollection(SpanData<TData>[] spanDataArray) {
			if (spanDataArray == null)
				throw new ArgumentNullException(nameof(spanDataArray));
#if DEBUG
			for (int i = 1; i < spanDataArray.Length; i++) {
				if (spanDataArray[i - 1].Span.Length == 0 || spanDataArray[i - 1].Span.End > spanDataArray[i].Span.Start)
					throw new ArgumentException("Input array must be sorted and must not contain any overlapping or empty elements", nameof(spanDataArray));
			}
			if (spanDataArray.Length > 0 && spanDataArray[spanDataArray.Length - 1].Span.Length == 0)
				throw new ArgumentException("Input array must be sorted and must not contain any overlapping or empty elements", nameof(spanDataArray));
#endif
			this.spanDataArray = spanDataArray;
		}

		/// <summary>
		/// Finds data or returns null if not found
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="allowIntersection">true if references whose <see cref="Span.End"/> equals <paramref name="position"/> can be returned</param>
		/// <returns></returns>
		public SpanData<TData>? Find(int position, bool allowIntersection = true) {
			var array = spanDataArray;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var spanData = array[index];
				if (position < spanData.Span.Start)
					hi = index - 1;
				else if (position >= spanData.Span.End)
					lo = index + 1;
				else
					return spanData;
			}
			if (allowIntersection && (uint)hi < (uint)array.Length && array[hi].Span.End == position)
				return array[hi];
			return null;
		}

		/// <summary>
		/// Finds data
		/// </summary>
		/// <param name="span">Span to search</param>
		/// <returns></returns>
		public IEnumerable<SpanData<TData>> Find(Span span) => Find(span.Start, span.Length);

		/// <summary>
		/// Finds data
		/// </summary>
		/// <param name="position">Start position</param>
		/// <returns></returns>
		public IEnumerable<SpanData<TData>> FindFrom(int position) {
			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));
			var array = spanDataArray;
			if (array.Length == 0)
				return Array.Empty<SpanData<TData>>();
			int lastPosition = array[array.Length - 1].Span.End;
			int length = lastPosition - position;
			if (length < 0)
				return Array.Empty<SpanData<TData>>();
			return Find(position, length);
		}

		/// <summary>
		/// Finds data
		/// </summary>
		/// <param name="position">Start position</param>
		/// <param name="length">Length</param>
		/// <returns></returns>
		public IEnumerable<SpanData<TData>> Find(int position, int length) {
			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			int end = position + length;
			if (end < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			int index = GetStartIndex(position);
			if (index < 0)
				yield break;
			var array = spanDataArray;
			while (index < array.Length) {
				var spanData = array[index++];
				if (end < spanData.Span.Start)
					break;
				Debug.Assert(spanData.Span.IntersectsWith(new Span(position, length)));
				yield return spanData;
			}
		}

		/// <summary>
		/// Gets the index of the first element whose span is greater than or equal to <paramref name="position"/>.
		/// -1 is returned if no such element exists.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public int GetStartIndex(int position) {
			var array = spanDataArray;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var spanData = array[index];
				if (position < spanData.Span.Start)
					hi = index - 1;
				else if (position >= spanData.Span.End)
					lo = index + 1;
				else {
					if (index > 0 && array[index - 1].Span.End == position)
						return index - 1;
					return index;
				}
			}
			if ((uint)hi < (uint)array.Length && array[hi].Span.End == position)
				return hi;
			return lo < array.Length ? lo : -1;
		}

		/// <summary>
		/// Returns the first <see cref="SpanData{TData}"/> in the collection that satisfies a condition
		/// or returns null if nothing was found
		/// </summary>
		/// <param name="predicate">Returns true if the element should be returned</param>
		/// <returns></returns>
		public SpanData<TData>? FirstOrNull(Func<SpanData<TData>, bool> predicate) {
			foreach (var info in spanDataArray) {
				if (predicate(info))
					return info;
			}
			return null;
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<SpanData<TData>>)this).GetEnumerator();
		IEnumerator<SpanData<TData>> IEnumerable<SpanData<TData>>.GetEnumerator() {
			foreach (var info in spanDataArray)
				yield return info;
		}
	}

	/// <summary>
	/// Span and data
	/// </summary>
	/// <typeparam name="TData">Type of data</typeparam>
	public struct SpanData<TData> {
		/// <summary>
		/// Gets the span
		/// </summary>
		public Span Span { get; }

		/// <summary>
		/// Gets the data
		/// </summary>
		public TData Data { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="data">Data</param>
		public SpanData(Span span, TData data) {
			Span = span;
			Data = data;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "[" + Span.ToString() + "]";
	}

	/// <summary>
	/// Builds a <see cref="SpanDataCollection{TData}"/>
	/// </summary>
	/// <typeparam name="TData">Type of data</typeparam>
	public struct SpanDataCollectionBuilder<TData> {
		readonly List<SpanData<TData>> list;

		/// <summary>
		/// Creates a <see cref="SpanDataCollectionBuilder{TData}"/>
		/// </summary>
		/// <returns></returns>
		public static SpanDataCollectionBuilder<TData> CreateBuilder() => new SpanDataCollectionBuilder<TData>(true);

		/// <summary>
		/// Creates a <see cref="SpanDataCollectionBuilder{TData}"/>
		/// </summary>
		/// <param name="capacity">Capacity</param>
		/// <returns></returns>
		public static SpanDataCollectionBuilder<TData> CreateBuilder(int capacity) => new SpanDataCollectionBuilder<TData>(capacity);

		SpanDataCollectionBuilder(bool unused) {
			list = new List<SpanData<TData>>();
		}

		SpanDataCollectionBuilder(int capacity) {
			list = new List<SpanData<TData>>(capacity);
		}

		/// <summary>
		/// Clears the created list
		/// </summary>
		public void Clear() => list.Clear();

		/// <summary>
		/// Adds span and data. The span must be located after the previously added span
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="data">Data</param>
		public void Add(Span span, TData data) {
			Debug.Assert(list.Count == 0 || list[list.Count - 1].Span.End <= span.Start);
			if (!span.IsEmpty)
				list.Add(new SpanData<TData>(span, data));
		}

		/// <summary>
		/// Creates a <see cref="SpanDataCollection{TData}"/>
		/// </summary>
		/// <returns></returns>
		public SpanDataCollection<TData> Create() => list.Count == 0 ? SpanDataCollection<TData>.Empty : new SpanDataCollection<TData>(list.ToArray());
	}
}
