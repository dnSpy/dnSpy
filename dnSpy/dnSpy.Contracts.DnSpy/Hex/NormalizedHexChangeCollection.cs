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
using System.Linq;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// A normalized read-only <see cref="HexChange"/> collection
	/// </summary>
	public sealed class NormalizedHexChangeCollection : IList<HexChange> {
		/// <summary>
		/// Gets 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public HexChange this[int index] {
			get => changes[index];
			set => throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the number of elements in this collection
		/// </summary>
		public int Count => changes.Length;

		bool ICollection<HexChange>.IsReadOnly => true;

		readonly HexChange[] changes;

		NormalizedHexChangeCollection(HexChange[] changes) => this.changes = changes;

		/// <summary>
		/// Creates an instance
		/// </summary>
		/// <param name="change">Change</param>
		/// <returns></returns>
		public static NormalizedHexChangeCollection Create(HexChange change) {
			if (change == null)
				throw new ArgumentNullException(nameof(change));
			return new NormalizedHexChangeCollection(new[] { change });
		}

		/// <summary>
		/// Creates an instance
		/// </summary>
		/// <param name="changes">Changes</param>
		/// <returns></returns>
		public static NormalizedHexChangeCollection Create(IList<HexChange> changes) {
			if (changes == null)
				throw new ArgumentNullException(nameof(changes));
			if (changes.Count == 0)
				return new NormalizedHexChangeCollection(Array.Empty<HexChange>());
			if (changes.Count == 1)
				return new NormalizedHexChangeCollection(new[] { changes[0] });
			return new NormalizedHexChangeCollection(CreateNormalizedList(changes).ToArray());
		}

		static IList<HexChange> CreateNormalizedList(IList<HexChange> changes) {
			if (changes.Count == 0)
				return Array.Empty<HexChange>();

			var list = new List<HexChange>(changes.Count);
			list.AddRange(changes);
			list.Sort(Comparer.Instance);
			for (int i = list.Count - 2; i >= 0; i--) {
				var a = list[i];
				var b = list[i + 1];
				// We'll fix these in the next loop, and they must not have been normalized yet
				Debug.Assert(a.OldPosition == a.NewPosition && b.OldPosition == b.NewPosition);
				if (a.OldSpan.OverlapsWith(b.OldSpan))
					throw new NotSupportedException($"Overlapping {nameof(HexChange)}s is not supported");
				if (a.OldSpan.IntersectsWith(b.OldSpan)) {
					list[i] = new HexChangeImpl(a.OldPosition, Add(a.OldData, b.OldData), Add(a.NewData, b.NewData));
					list.RemoveAt(i + 1);
				}
			}

			long deletedBytes = 0;
			for (int i = 0; i < list.Count; i++) {
				var change = list[i];
				if (deletedBytes != 0) {
					var newChange = new HexChangeImpl(change.OldPosition, change.OldData, change.NewPosition - deletedBytes, change.NewData);
					list[i] = newChange;
				}
				deletedBytes += -change.Delta;
			}
			return new NormalizedHexChangeCollection(list.ToArray());
		}

		static byte[] Add(byte[] a, byte[] b) {
			if (a.Length == 0)
				return b;
			if (b.Length == 0)
				return a;
			var res = new byte[a.Length + b.Length];
			Array.Copy(a, 0, res, 0, a.Length);
			Array.Copy(b, 0, res, a.Length, b.Length);
			return res;
		}

		sealed class Comparer : IComparer<HexChange> {
			public static readonly Comparer Instance = new Comparer();
			public int Compare(HexChange x, HexChange y) => x.OldPosition.CompareTo(y.OldPosition);
		}

		/// <summary>
		/// Returns true if <paramref name="item"/> is a part of this collection
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns></returns>
		public bool Contains(HexChange item) => Array.IndexOf(changes, item) >= 0;

		/// <summary>
		/// Returns the index of <paramref name="item"/> in this collection or a value less than 0 if it's not a part of this collection
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns></returns>
		public int IndexOf(HexChange item) => Array.IndexOf(changes, item);

		/// <summary>
		/// Copies this collection to an array
		/// </summary>
		/// <param name="array">Destination array</param>
		/// <param name="arrayIndex">Destination array index</param>
		public void CopyTo(HexChange[] array, int arrayIndex) => Array.Copy(changes, 0, array, arrayIndex, changes.Length);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Returns an enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator<HexChange> GetEnumerator() {
			foreach (var c in changes)
				yield return c;
		}

		void ICollection<HexChange>.Add(HexChange item) => throw new NotSupportedException();

		void ICollection<HexChange>.Clear() => throw new NotSupportedException();

		void IList<HexChange>.Insert(int index, HexChange item) => throw new NotSupportedException();

		bool ICollection<HexChange>.Remove(HexChange item) => throw new NotSupportedException();

		void IList<HexChange>.RemoveAt(int index) => throw new NotSupportedException();
	}
}
