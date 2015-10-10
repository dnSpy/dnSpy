/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;

namespace dnSpy.MVVM {
	static class ListSorter {
		/// <summary>
		/// Add an item to an already sorted list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">List</param>
		/// <param name="index">Start of sorted range</param>
		/// <param name="count">Number of sorted items</param>
		/// <param name="item">Item to insert</param>
		public static void Insert<T>(IList<T> list, int index, int count, T item) {
			Insert(list, index, count, item, Comparer<T>.Default);
		}

		/// <summary>
		/// Add an item to an already sorted list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">List</param>
		/// <param name="index">Start of sorted range</param>
		/// <param name="count">Number of sorted items</param>
		/// <param name="item">Item to insert</param>
		/// <param name="comparer">Compares items</param>
		public static void Insert<T>(IList<T> list, int index, int count, T item, IComparer<T> comparer) {
			if (count == 0) {
				list.Insert(index, item);
				return;
			}

			int lo = index;
			int hi = index + count - 1;
			for (;;) {
				int i = (lo + hi) / 2;
				int res = comparer.Compare(item, list[i]);
				if (res < 0)
					hi = i - 1;
				else if (res > 0)
					lo = i + 1;
				else {
					// If there are identical elements, insert it last
					int end = index + count;
					while (++i < end) {
						res = comparer.Compare(item, list[i]);
						Debug.Assert(res <= 0);
						if (res != 0)
							break;
					}
					list.Insert(i, item);
					return;
				}
				if (lo > hi || hi == -1) {
					if (res < 0) {
						Debug.Assert(index <= i && i <= index + count);
						list.Insert(i, item);
					}
					else {
						Debug.Assert(index <= i && i + 1 <= index + count);
						list.Insert(i + 1, item);
					}
					return;
				}
			}
		}
	}
}
