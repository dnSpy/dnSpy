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
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	sealed class NormalizedTextChangeCollection : INormalizedTextChangeCollection {
		public ITextChange this[int index] {
			get { return changes[index]; }
			set { throw new NotSupportedException(); }
		}

		public int Count => changes.Length;
		public bool IsReadOnly => true;
		readonly ITextChange[] changes;

		NormalizedTextChangeCollection(ITextChange[] changes) {
			this.changes = changes;
		}

		public static INormalizedTextChangeCollection Create(IList<ITextChange> changes) {
			if (changes.Count == 1)
				return new NormalizedTextChangeCollection(new ITextChange[1] { changes[0] });
			if (changes.Count == 0)
				return new NormalizedTextChangeCollection(Array.Empty<ITextChange>());

			var list = new List<ITextChange>(changes.Count);
			list.AddRange(changes);
			list.Sort(Comparer.Instance);
			for (int i = list.Count - 2; i >= 0; i--) {
				var a = list[i];
				var b = list[i + 1];
				if (a.OldSpan.OverlapsWith(b.OldSpan))
					throw new NotSupportedException("Overlapping ITextChanges is not supported");
				if (a.OldSpan.IntersectsWith(b.OldSpan)) {
					list[i] = new TextChange(a.OldPosition, a.OldText + b.OldText, a.NewText + b.NewText);
					list.RemoveAt(i + 1);
				}
			}
			return new NormalizedTextChangeCollection(list.ToArray());
		}

		sealed class Comparer : IComparer<ITextChange> {
			public static readonly Comparer Instance = new Comparer();
			public int Compare(ITextChange x, ITextChange y) => x.OldPosition - y.OldPosition;
		}

		public bool Contains(ITextChange item) => Array.IndexOf(changes, item) >= 0;
		public int IndexOf(ITextChange item) => Array.IndexOf(changes, item);
		public void CopyTo(ITextChange[] array, int arrayIndex) => Array.Copy(changes, 0, array, arrayIndex, changes.Length);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<ITextChange> GetEnumerator() {
			foreach (var c in changes)
				yield return c;
		}

		public void Add(ITextChange item) {
			throw new NotSupportedException();
		}

		public void Clear() {
			throw new NotSupportedException();
		}

		public void Insert(int index, ITextChange item) {
			throw new NotSupportedException();
		}

		public bool Remove(ITextChange item) {
			throw new NotSupportedException();
		}

		public void RemoveAt(int index) {
			throw new NotSupportedException();
		}
	}
}
