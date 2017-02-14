/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Base class of debugger objects that only exist while debugging
	/// </summary>
	public abstract class DbgObject {
		readonly object lockObj;
		List<KeyValuePair<Type, object>> dataList;

		/// <summary>
		/// Constructor
		/// </summary>
		protected DbgObject() => lockObj = new object();

		/// <summary>
		/// true if the instance has been closed
		/// </summary>
		public bool IsClosed { get; private set; }

		/// <summary>
		/// Raised when it's closed
		/// </summary>
		public event EventHandler Closed;

		/// <summary>
		/// Closes the instance
		/// </summary>
		protected void Close() {
			Debug.Assert(!IsClosed);
			if (IsClosed)
				return;
			IsClosed = true;
			OnClosed();
			Closed?.Invoke(this, EventArgs.Empty);

			KeyValuePair<Type, object>[] data;
			lock (lockObj) {
				data = dataList.Count == 0 ? Array.Empty<KeyValuePair<Type, object>>() : dataList.ToArray();
				dataList.Clear();
			}
			foreach (var kv in data)
				(kv.Value as IDisposable)?.Dispose();
		}

		/// <summary>
		/// Called when it gets closed
		/// </summary>
		protected virtual void OnClosed() { }

		/// <summary>
		/// Checks if the data exists or is null
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <returns></returns>
		public bool HasData<T>() where T : class => TryGetData<T>(out var value);

		/// <summary>
		/// Gets or creates data
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <returns></returns>
		public T GetOrCreateData<T>() where T : class, new() => GetOrCreateData(() => new T());

		/// <summary>
		/// Gets data
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="value">Result</param>
		/// <returns></returns>
		public bool TryGetData<T>(out T value) where T : class {
			lock (lockObj) {
				if (dataList != null) {
					var type = typeof(T);
					foreach (var kv in dataList) {
						if (kv.Key == type) {
							value = (T)kv.Value;
							return true;
						}
					}
				}
			}
			value = null;
			return false;
		}

		/// <summary>
		/// Gets or creates data
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="create">Creates the data if it doesn't exist</param>
		/// <returns></returns>
		public T GetOrCreateData<T>(Func<T> create) where T : class {
			lock (lockObj) {
				if (dataList == null)
					dataList = new List<KeyValuePair<Type, object>>();
				var type = typeof(T);
				foreach (var kv in dataList) {
					if (kv.Key == type)
						return (T)kv.Value;
				}
				var value = create();
				dataList.Add(new KeyValuePair<Type, object>(type, value));
				return value;
			}
		}
	}
}
