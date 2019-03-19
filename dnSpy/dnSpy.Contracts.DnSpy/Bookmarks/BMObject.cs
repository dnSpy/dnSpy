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
using System.Diagnostics;
using System.Threading;

namespace dnSpy.Contracts.Bookmarks {
	/// <summary>
	/// Base class of bookmark objects
	/// </summary>
	public abstract class BMObject {
		readonly object lockObj;
		List<(RuntimeTypeHandle key, object data)> dataList;

		/// <summary>
		/// Constructor
		/// </summary>
		protected BMObject() => lockObj = new object();

#if DEBUG
		/// <summary>
		/// Destructor
		/// </summary>
		~BMObject() {
			Debug.Assert(Environment.HasShutdownStarted, nameof(BMObject) + " dtor called! Type: " + GetType().FullName);
		}
#endif

		/// <summary>
		/// true if the instance has been closed
		/// </summary>
		public bool IsClosed => isClosed != 0;
		volatile int isClosed;

		/// <summary>
		/// Raised when it's closed. Data methods eg. <see cref="TryGetData{T}(out T)"/> can be called
		/// but some other methods could throw or can't be called. After all handlers have been notified,
		/// all data get disposed (if they implement <see cref="IDisposable"/>).
		/// </summary>
		public event EventHandler Closed;

		/// <summary>
		/// Closes the instance. This method must only be executed on the dispatcher thread
		/// 
		/// This method must only be called by the owner object.
		/// </summary>
		public void Close() {
			Debug.Assert(!IsClosed);
			if (Interlocked.Exchange(ref isClosed, 1) != 0)
				return;
			Closed?.Invoke(this, EventArgs.Empty);
			Closed = null;

			CloseCore();

			(RuntimeTypeHandle key, object data)[] data;
			lock (lockObj) {
				data = dataList == null || dataList.Count == 0 ? Array.Empty<(RuntimeTypeHandle, object)>() : dataList.ToArray();
				dataList?.Clear();
			}
			foreach (var kv in data)
				(kv.data as IDisposable)?.Dispose();

#if DEBUG
			GC.SuppressFinalize(this);
#endif
		}

		/// <summary>
		/// Called by <see cref="Close"/> after it has raised <see cref="Closed"/> and before it disposes
		/// of all data.
		/// </summary>
		protected abstract void CloseCore();

		/// <summary>
		/// Checks if the data exists or is null
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <returns></returns>
		public bool HasData<T>() where T : class => TryGetData<T>(out var value);

		/// <summary>
		/// Gets or creates data. If it implements <see cref="IDisposable"/>, it will get disposed
		/// when this object gets closed.
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
					var type = typeof(T).TypeHandle;
					foreach (var kv in dataList) {
						if (kv.key.Equals(type)) {
							value = (T)kv.data;
							return true;
						}
					}
				}
			}
			value = null;
			return false;
		}

		/// <summary>
		/// Gets existing data or throws if the data doesn't exist
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <returns></returns>
		public T GetData<T>() where T : class {
			if (TryGetData<T>(out var data))
				return data;
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Gets or creates data. If it implements <see cref="IDisposable"/>, it will get disposed
		/// when this object gets closed.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="create">Creates the data if it doesn't exist</param>
		/// <returns></returns>
		public T GetOrCreateData<T>(Func<T> create) where T : class {
			if (create == null)
				throw new ArgumentNullException(nameof(create));
			lock (lockObj) {
				if (dataList == null)
					dataList = new List<(RuntimeTypeHandle, object)>();
				var type = typeof(T).TypeHandle;
				foreach (var kv in dataList) {
					if (kv.key.Equals(type))
						return (T)kv.data;
				}
				var value = create();
				dataList.Add((type, value));
				return value;
			}
		}
	}
}
