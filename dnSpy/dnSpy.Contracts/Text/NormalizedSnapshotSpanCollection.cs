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
using System.Linq;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// A normalized read-only <see cref="SnapshotSpan"/> collection
	/// </summary>
	public sealed class NormalizedSnapshotSpanCollection : IList<SnapshotSpan>, IEquatable<NormalizedSnapshotSpanCollection> {
		/// <summary>
		/// An empty instance
		/// </summary>
		public static readonly NormalizedSnapshotSpanCollection Empty = new NormalizedSnapshotSpanCollection();

		readonly NormalizedSpanCollection spans;
		readonly ITextSnapshot snapshot;

		bool ICollection<SnapshotSpan>.IsReadOnly => true;

		/// <summary>
		/// Gets the number of elements stored in this collection
		/// </summary>
		public int Count => spans.Count;

		/// <summary>
		/// Gets an item
		/// </summary>
		/// <param name="index">Index of item</param>
		/// <returns></returns>
		public SnapshotSpan this[int index] {
			get { return new SnapshotSpan(snapshot, spans[index]); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public NormalizedSnapshotSpanCollection() {
			this.spans = new NormalizedSpanCollection();
			this.snapshot = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public NormalizedSnapshotSpanCollection(SnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			this.spans = new NormalizedSpanCollection(span.Span);
			this.snapshot = span.Snapshot;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshotSpans">Spans</param>
		public NormalizedSnapshotSpanCollection(IEnumerable<SnapshotSpan> snapshotSpans) {
			if (snapshotSpans == null)
				throw new ArgumentNullException(nameof(snapshotSpans));
			var array = snapshotSpans.ToArray();
			this.spans = new NormalizedSpanCollection(array.Select(a => a.Span));
			this.snapshot = array.Length == 0 ? null : array[0].Snapshot;
			foreach (var s in array) {
				if (s.Snapshot != snapshot)
					throw new ArgumentException();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshotSpans">Spans</param>
		public NormalizedSnapshotSpanCollection(IList<SnapshotSpan> snapshotSpans) {
			if (snapshotSpans == null)
				throw new ArgumentNullException(nameof(snapshotSpans));
			this.spans = new NormalizedSpanCollection(snapshotSpans.Select(a => a.Span));
			this.snapshot = snapshotSpans.Count == 0 ? null : snapshotSpans[0].Snapshot;
			foreach (var s in snapshotSpans) {
				if (s.Snapshot != snapshot)
					throw new ArgumentException();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="spans">Spans</param>
		public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, NormalizedSpanCollection spans) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if ((object)spans == null)
				throw new ArgumentNullException(nameof(spans));
			this.spans = spans;
			this.snapshot = snapshot;
			foreach (var s in this.spans) {
				if (s.End > snapshot.Length)
					throw new ArgumentOutOfRangeException(nameof(spans));
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="spans">Spans</param>
		public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, IEnumerable<Span> spans) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			this.spans = new NormalizedSpanCollection(spans);
			this.snapshot = snapshot;
			foreach (var s in this.spans) {
				if (s.End > snapshot.Length)
					throw new ArgumentOutOfRangeException(nameof(spans));
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="spans">Spans</param>
		public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, IList<Span> spans) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			this.spans = new NormalizedSpanCollection(spans);
			this.snapshot = snapshot;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="span">Span</param>
		public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, Span span) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (span.End > snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			this.spans = new NormalizedSpanCollection(span);
			this.snapshot = snapshot;
		}

		/// <summary>
		/// Checks whether this instance intersects with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(SnapshotSpan span) {
			if (span.Snapshot != snapshot)
				throw new ArgumentException();
			return spans.IntersectsWith(span.Span);
		}

		/// <summary>
		/// Checks whether this instance overlapsWith with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool OverlapsWith(SnapshotSpan span) {
			if (span.Snapshot != snapshot)
				throw new ArgumentException();
			return spans.OverlapsWith(span.Span);
		}

		/// <summary>
		/// implicit operator <see cref="NormalizedSpanCollection"/>
		/// </summary>
		/// <param name="spans"></param>
		public static implicit operator NormalizedSpanCollection(NormalizedSnapshotSpanCollection spans) => spans.spans;

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right) {
			if ((object)left == right)
				return true;
			if ((object)left == null || (object)right == null)
				return false;
			return left.Equals(right);
		}

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right) => !(left == right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(NormalizedSnapshotSpanCollection other) => other != null && snapshot == other.snapshot && spans.Equals(other.spans);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as NormalizedSnapshotSpanCollection);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (snapshot?.GetHashCode() ?? 0) ^ spans.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => spans.Count == 1 ? spans[0].ToString() : spans.ToString();

		/// <summary>
		/// Gets the index of <paramref name="item"/> in this collection or -1
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns></returns>
		public int IndexOf(SnapshotSpan item) {
			if (item.Snapshot != snapshot)
				return -1;
			return spans.IndexOf(item.Span);
		}

		/// <summary>
		/// Checks whether <paramref name="item"/> exists in this collection
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(SnapshotSpan item) => IndexOf(item) >= 0;

		/// <summary>
		/// Copies this collection to <paramref name="array"/>
		/// </summary>
		/// <param name="array">Destination array</param>
		/// <param name="arrayIndex">Index in <paramref name="array"/></param>
		public void CopyTo(SnapshotSpan[] array, int arrayIndex) {
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			for (int i = 0; i < spans.Count; i++)
				array[arrayIndex + i] = new SnapshotSpan(snapshot, spans[i]);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator<SnapshotSpan> GetEnumerator() {
			foreach (var s in spans)
				yield return new SnapshotSpan(snapshot, s);
		}

		void IList<SnapshotSpan>.Insert(int index, SnapshotSpan item) {
			throw new NotSupportedException();
		}

		void IList<SnapshotSpan>.RemoveAt(int index) {
			throw new NotSupportedException();
		}

		void ICollection<SnapshotSpan>.Add(SnapshotSpan item) {
			throw new NotSupportedException();
		}

		void ICollection<SnapshotSpan>.Clear() {
			throw new NotSupportedException();
		}

		bool ICollection<SnapshotSpan>.Remove(SnapshotSpan item) {
			throw new NotSupportedException();
		}
	}
}
