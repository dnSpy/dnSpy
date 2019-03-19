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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Base class of types, members, assemblies, modules that allows you to attach data to instances
	/// </summary>
	public abstract class DmdObject {
		readonly object lockObj;
		List<(RuntimeTypeHandle key, object data)> dataList;

		/// <summary>
		/// Gets the lock object used by this instance
		/// </summary>
		protected object LockObject => lockObj;

		/// <summary>
		/// Constructor
		/// </summary>
		protected DmdObject() => lockObj = new object();

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
		/// Gets or creates data
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
				Debug.Assert(!(value is DmdObject));
				dataList.Add((type, value));
				return value;
			}
		}
	}
}
