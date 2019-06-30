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

using System.Collections;
using System.Collections.Generic;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class TList<T> : IEnumerable<T> {
		readonly object lockObj;
		readonly List<T> list;

		/// <summary>
		/// 
		/// </summary>
		public object SyncRoot => lockObj;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index] {
			get {
				lock (lockObj)
					return list[index];
			}
			set {
				lock (lockObj)
					list[index] = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public int Count {
			get {
				lock (lockObj)
					return list.Count;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public T[] GetElements() {
			lock (lockObj)
				return list.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		public TList() {
			lockObj = new object();
			list = new List<T>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="capacity"></param>
		public TList(int capacity) {
			lockObj = new object();
			list = new List<T>(capacity);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		public void AddRange(IEnumerable<T> collection) {
			lock (lockObj)
				list.AddRange(collection);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void Add(T item) {
			lock (lockObj)
				list.Add(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		public void Insert(int index, T item) {
			lock (lockObj)
				list.Insert(index, item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(T item) {
			lock (lockObj)
				return list.Remove(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index) {
			lock (lockObj)
				list.RemoveAt(index);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int IndexOf(T item) {
			lock (lockObj)
				return list.IndexOf(item);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Clear() {
			lock (lockObj)
				list.Clear();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)GetElements()).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetElements().GetEnumerator();
	}
}
