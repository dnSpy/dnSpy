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
using System.Threading;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Base class of debugger objects that only exist while debugging
	/// </summary>
	public abstract class DbgObject {
		readonly object lockObj;
		List<(Type key, object data)> dataList;

		/// <summary>
		/// Constructor
		/// </summary>
		protected DbgObject() => lockObj = new object();

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
		/// Closes the instance. This method must only be executed in the dispatcher thread
		/// 
		/// This method must only be called by the owner object.
		/// </summary>
		/// <param name="dispatcherThread">Dispatcher thread</param>
		public void Close(DispatcherThread dispatcherThread) {
			if (dispatcherThread == null)
				throw new ArgumentNullException(nameof(dispatcherThread));
			dispatcherThread.VerifyAccess();
			Debug.Assert(!IsClosed);
			if (Interlocked.Increment(ref isClosed) != 1)
				return;
			Closed?.Invoke(this, EventArgs.Empty);

			CloseCore();

			(Type key, object data)[] data;
			lock (lockObj) {
				data = dataList == null || dataList.Count == 0 ? Array.Empty<(Type, object)>() : dataList.ToArray();
				dataList?.Clear();
			}
			foreach (var kv in data)
				(kv.data as IDisposable)?.Dispose();
		}

		/// <summary>
		/// Called by <see cref="Close"/> after it has raised <see cref="Closed"/> and before it disposes
		/// of all data. This method is called in the dispatcher thread (see <see cref="DbgManager.DispatcherThread"/>)
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
					var type = typeof(T);
					foreach (var kv in dataList) {
						if (kv.key == type) {
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
		/// Gets or creates data. If it implements <see cref="IDisposable"/>, it will get disposed
		/// when this object gets closed.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="create">Creates the data if it doesn't exist</param>
		/// <returns></returns>
		public T GetOrCreateData<T>(Func<T> create) where T : class {
			lock (lockObj) {
				if (dataList == null)
					dataList = new List<(Type, object)>();
				var type = typeof(T);
				foreach (var kv in dataList) {
					if (kv.key == type)
						return (T)kv.data;
				}
				var value = create();
				dataList.Add((type, value));
				return value;
			}
		}
	}
}
