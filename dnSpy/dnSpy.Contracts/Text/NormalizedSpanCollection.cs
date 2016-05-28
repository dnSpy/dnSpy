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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// A normalized read-only <see cref="Span"/> collection
	/// </summary>
	public sealed class NormalizedSpanCollection : ReadOnlyCollection<Span>, IEquatable<NormalizedSpanCollection> {
		static readonly List<Span> emptyList = new List<Span>();

		/// <summary>
		/// Constructor
		/// </summary>
		public NormalizedSpanCollection()
			: base(emptyList) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public NormalizedSpanCollection(Span span)
			: base(new List<Span> { span }) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="spans">Spans</param>
		public NormalizedSpanCollection(IEnumerable<Span> spans)
			: base(CreateNormalizedList(spans)) {
		}

		static List<Span> CreateNormalizedList(IEnumerable<Span> spans) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			var list = spans.ToList();
			if (list.Count <= 1)
				return list;
			list.Sort(Comparer.Instance);

			int lastIndex = 0;
			for (int i = 1; i < list.Count; i++) {
				var last = list[lastIndex];
				var elem = list[i];
				if (last.IntersectsWith(elem))
					list[lastIndex] = Span.FromBounds(last.Start, Math.Max(last.End, elem.End));
				else {
					lastIndex++;
					list[lastIndex] = elem;
				}
			}
			list.RemoveRange(lastIndex + 1, list.Count - lastIndex - 1);

			return list;
		}

		sealed class Comparer : IComparer<Span> {
			public static readonly Comparer Instance = new Comparer();
			public int Compare(Span x, Span y) => x.Start - y.Start;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(NormalizedSpanCollection left, NormalizedSpanCollection right) {
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
		public static bool operator !=(NormalizedSpanCollection left, NormalizedSpanCollection right) => !(left == right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(NormalizedSpanCollection other) {
			if ((object)other == null)
				return false;
			if (Count != other.Count)
				return false;
			for (int i = 0; i < Count; i++) {
				if (this[i] != other[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as NormalizedSpanCollection);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			int hc = 0;
			foreach (var s in this)
				hc ^= s.GetHashCode();
			return hc;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			var sb = new StringBuilder();
			sb.Append('{');
			foreach (var s in this)
				sb.Append(s.ToString());
			sb.Append('}');
			return sb.ToString();
		}
	}
}
