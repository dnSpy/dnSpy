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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Normalized <see cref="HexSpan"/> collection
	/// </summary>
	public sealed class NormalizedHexSpanCollection : ReadOnlyCollection<HexSpan>, IEquatable<NormalizedHexSpanCollection> {
		/// <summary>
		/// An empty collection
		/// </summary>
		public static readonly NormalizedHexSpanCollection Empty = new NormalizedHexSpanCollection();

		/// <summary>
		/// Constructor
		/// </summary>
		public NormalizedHexSpanCollection()
			: base(Array.Empty<HexSpan>()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public NormalizedHexSpanCollection(HexSpan span)
			: base(new HexSpan[1] { span }) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="spans">Spans</param>
		public NormalizedHexSpanCollection(IEnumerable<HexSpan> spans)
			: base(Normalize(spans)) {
		}

		static HexSpan[] Normalize(IEnumerable<HexSpan> spans) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			var list = new List<HexSpan>(spans);
			if (list.Count <= 1)
				return list.ToArray();
			list.Sort(HexSpanComparer.Instance);
			int index = 0;
			var start = list[0].Start;
			var end = list[0].End;
			for (int i = 1; i < list.Count; i++) {
				var span = list[i];
				if (end < span.Start) {
					list[index++] = HexSpan.FromBounds(start, end);
					start = span.Start;
					end = span.End;
				}
				else
					end = HexPosition.Max(end, span.End);
			}
			list[index++] = HexSpan.FromBounds(start, end);
			var ary = new HexSpan[index];
			list.CopyTo(0, ary, 0, ary.Length);
			return ary;
		}

		sealed class HexSpanComparer : IComparer<HexSpan> {
			public static readonly HexSpanComparer Instance = new HexSpanComparer();
			public int Compare(HexSpan x, HexSpan y) => x.Start.CompareTo(y.Start);
		}

		/// <summary>
		/// Returns true if any of the spans in this instance overlaps with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool OverlapsWith(HexSpan span) {
			for (int i = 0; i < Count; i++) {
				if (this[i].OverlapsWith(span))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Returns true if any of the spans in this instance intersects with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(HexSpan span) {
			for (int i = 0; i < Count; i++) {
				if (this[i].IntersectsWith(span))
					return true;
			}
			return false;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(NormalizedHexSpanCollection left, NormalizedHexSpanCollection right) {
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
		public static bool operator !=(NormalizedHexSpanCollection left, NormalizedHexSpanCollection right) => !(left == right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(NormalizedHexSpanCollection other) {
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
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as NormalizedHexSpanCollection);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			int hc = 0;
			for (int i = 0; i < Count; i++)
				hc ^= this[i].GetHashCode();
			return hc;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			var sb = new StringBuilder();
			sb.Append('{');
			foreach (var span in this)
				sb.Append(span.ToString());
			sb.Append('}');
			return sb.ToString();
		}
	}
}
