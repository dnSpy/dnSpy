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
using System.Linq;

namespace dndbg.Engine {
	sealed class DebuggerCollection<TKey, TValue> where TKey : class where TValue : class {
		readonly Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
		readonly Func<TKey, TValue> createValue;

		public int Count {
			get { return dict.Count; }
		}

		public DebuggerCollection(Func<TKey, TValue> createValue) {
			this.createValue = createValue;
		}

		/// <summary>
		/// Tries to get an existing item. Returns null if it doesn't exist.
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		public TValue TryGet(TKey key) {
			if (key == null)
				return null;

			TValue value;
			dict.TryGetValue(key, out value);

			return value;
		}

		/// <summary>
		/// Adds a new item. If it already exists, the old one is returned.
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		public TValue Add(TKey key) {
			if (key == null)
				return null;

			var value = TryGet(key);
			if (value != null)
				return value;

			var createdValue = createValue(key);
			dict.Add(key, createdValue);
			return createdValue;
		}

		/// <summary>
		/// Removes the item
		/// </summary>
		/// <param name="key">Key</param>
		public bool Remove(TKey key) {
			if (key == null)
				return false;

			return dict.Remove(key);
		}

		/// <summary>
		/// Gets all items
		/// </summary>
		/// <returns></returns>
		public TValue[] GetAll() {
			return dict.Values.ToArray();
		}
	}
}
