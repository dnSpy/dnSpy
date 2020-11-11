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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Normalized <see cref="HexBufferSpan"/> collection
	/// </summary>
	public sealed class NormalizedHexBufferSpanCollection : IList<HexBufferSpan>, IList, IEquatable<NormalizedHexBufferSpanCollection?> {
		/// <summary>
		/// An empty collection
		/// </summary>
		public static readonly NormalizedHexBufferSpanCollection Empty = new NormalizedHexBufferSpanCollection();

		readonly NormalizedHexSpanCollection coll;
		readonly HexBuffer? buffer;

		/// <summary>
		/// Constructor
		/// </summary>
		public NormalizedHexBufferSpanCollection() {
			coll = NormalizedHexSpanCollection.Empty;
			buffer = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="spans">Spans</param>
		public NormalizedHexBufferSpanCollection(HexBuffer buffer, NormalizedHexSpanCollection spans) {
			coll = spans ?? throw new ArgumentNullException(nameof(spans));
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="spans">Spans</param>
		public NormalizedHexBufferSpanCollection(HexBuffer buffer, IEnumerable<HexSpan> spans) {
			if (spans is null)
				throw new ArgumentNullException(nameof(spans));
			coll = new NormalizedHexSpanCollection(spans);
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="span">Span</param>
		public NormalizedHexBufferSpanCollection(HexBuffer buffer, HexSpan span) {
			coll = new NormalizedHexSpanCollection(span);
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public NormalizedHexBufferSpanCollection(HexBufferSpan span) {
			if (span.IsDefault)
				throw new ArgumentException();
			coll = new NormalizedHexSpanCollection(span.Span);
			buffer = span.Buffer;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="spans">Spans</param>
		public NormalizedHexBufferSpanCollection(IEnumerable<HexBufferSpan> spans) {
			if (spans is null)
				throw new ArgumentNullException(nameof(spans));
			var list = new List<HexSpan>();
			HexBuffer? buffer = null;
			foreach (var span in spans) {
				if (span.IsDefault)
					throw new ArgumentException();
				if (buffer is not null && buffer != span.Buffer)
					throw new ArgumentException();
				buffer = span.Buffer;
				list.Add(span.Span);
			}
			this.buffer = buffer;
			coll = new NormalizedHexSpanCollection(list);
		}

		/// <summary>
		/// implicit operator NormalizedHexSpanCollection
		/// </summary>
		/// <param name="spans"></param>
		public static implicit operator NormalizedHexSpanCollection(NormalizedHexBufferSpanCollection spans) {
			if (spans is null)
				throw new ArgumentNullException(nameof(spans));
			return spans.coll;
		}

		/// <summary>
		/// Returns true if any of the spans in this instance overlaps with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool OverlapsWith(HexBufferSpan span) {
			if (span.IsDefault)
				throw new ArgumentException();
			// buffer could be null if Count is 0
			if (Count == 0)
				return false;
			if (span.Buffer != buffer)
				throw new ArgumentException();
			return coll.OverlapsWith(span.Span);
		}

		/// <summary>
		/// Returns true if any of the spans in this instance intersects with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(HexBufferSpan span) {
			if (span.IsDefault)
				throw new ArgumentException();
			// buffer could be null if Count is 0
			if (Count == 0)
				return false;
			if (span.Buffer != buffer)
				throw new ArgumentException();
			return coll.IntersectsWith(span.Span);
		}

		/// <summary>
		/// Gets the span at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public HexBufferSpan this[int index] {
			get => new HexBufferSpan(buffer!, coll[index]);// if buffer's null, coll is empty and throws
			set => throw new NotSupportedException();
		}

		object? IList.this[int index] {
			get => this[index];
			set => throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the number of elements in the collection
		/// </summary>
		public int Count => coll.Count;

		bool IList.IsFixedSize => true;
		bool ICollection<HexBufferSpan>.IsReadOnly => true;
		bool IList.IsReadOnly => true;
		bool ICollection.IsSynchronized => false;
		object ICollection.SyncRoot => ((IList)coll).SyncRoot;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator<HexBufferSpan> GetEnumerator() {
			foreach (var span in coll) {
				Debug2.Assert(buffer is not null);// Can't be null if coll is non-empty
				yield return new HexBufferSpan(buffer, span);
			}
		}

		// These don't seem very useful
		bool ICollection<HexBufferSpan>.Contains(HexBufferSpan item) => throw new NotImplementedException();
		void ICollection<HexBufferSpan>.CopyTo(HexBufferSpan[] array, int arrayIndex) => throw new NotImplementedException();
		int IList<HexBufferSpan>.IndexOf(HexBufferSpan item) => throw new NotImplementedException();
		void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();
		bool IList.Contains(object? value) => throw new NotImplementedException();
		int IList.IndexOf(object? value) => throw new NotImplementedException();

		// It's a read-only collection
		int IList.Add(object? value) => throw new NotSupportedException();
		void ICollection<HexBufferSpan>.Add(HexBufferSpan item) => throw new NotSupportedException();
		void IList.Clear() => throw new NotSupportedException();
		void ICollection<HexBufferSpan>.Clear() => throw new NotSupportedException();
		void IList.Insert(int index, object? value) => throw new NotSupportedException();
		void IList<HexBufferSpan>.Insert(int index, HexBufferSpan item) => throw new NotSupportedException();
		void IList.Remove(object? value) => throw new NotSupportedException();
		bool ICollection<HexBufferSpan>.Remove(HexBufferSpan item) => throw new NotSupportedException();
		void IList.RemoveAt(int index) => throw new NotSupportedException();
		void IList<HexBufferSpan>.RemoveAt(int index) => throw new NotSupportedException();

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(NormalizedHexBufferSpanCollection? left, NormalizedHexBufferSpanCollection? right) {
			if ((object?)left == right)
				return true;
			if (left is null || right is null)
				return false;
			return left.Equals(right);
		}

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(NormalizedHexBufferSpanCollection? left, NormalizedHexBufferSpanCollection? right) => !(left == right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(NormalizedHexBufferSpanCollection? other) {
			if (other is null)
				return false;
			if (Count != other.Count)
				return false;
			if (buffer != other.buffer)
				return false;
			for (int i = 0; i < Count; i++) {
				if (coll[i] != other.coll[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as NormalizedHexBufferSpanCollection);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			int hc = buffer?.GetHashCode() ?? 0;
			for (int i = 0; i < Count; i++)
				hc ^= this[i].GetHashCode();
			return hc;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (Count == 1)
				return coll[0].ToString();
			return coll.ToString();
		}
	}
}
