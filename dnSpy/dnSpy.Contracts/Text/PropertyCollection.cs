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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Property collection
	/// </summary>
	public sealed class PropertyCollection {
		readonly object lockObj;
		readonly Dictionary<object, object> dict;

		/// <summary>
		/// Returns a property value
		/// </summary>
		/// <param name="key">Property key</param>
		/// <returns></returns>
		public object this[object key] {
			get { return GetProperty(key); }
			set { SetProperty(key, value); }
		}

		/// <summary>
		/// Gets all properties as a read only list. This list is created each time this
		/// property is called.
		/// </summary>
		public ReadOnlyCollection<KeyValuePair<object, object>> PropertyList {
			get {
				lock (lockObj) {
					var list = new List<KeyValuePair<object, object>>(dict.Count);
					list.AddRange(dict);
					return list.AsReadOnly();
				}
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PropertyCollection() {
			this.lockObj = new object();
			this.dict = new Dictionary<object, object>();
		}

		/// <summary>
		/// Adds a property. The property key must not already exist.
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="property">Property value</param>
		public void AddProperty(object key, object property) {
			lock (lockObj)
				dict.Add(key, property);
		}

		/// <summary>
		/// Returns true if <paramref name="key"/> is present in this collection
		/// </summary>
		/// <param name="key">Property key</param>
		/// <returns></returns>
		public bool ContainsProperty(object key) {
			lock (lockObj)
				return dict.ContainsKey(key);
		}

		/// <summary>
		/// Gets or creates a property. The key is the type of the property value (<typeparamref name="T"/>)
		/// </summary>
		/// <typeparam name="T">Property value type</typeparam>
		/// <param name="creator">Creates the property value</param>
		/// <returns></returns>
		public T GetOrCreateSingletonProperty<T>(Func<T> creator) where T : class => GetOrCreateSingletonProperty(typeof(T), creator);

		/// <summary>
		/// Gets or creates a property
		/// </summary>
		/// <typeparam name="T">Property value type</typeparam>
		/// <param name="key">Property key</param>
		/// <param name="creator">Creates the property value</param>
		/// <returns></returns>
		public T GetOrCreateSingletonProperty<T>(object key, Func<T> creator) where T : class {
			lock (lockObj) {
				object value;
				if (dict.TryGetValue(key, out value))
					return (T)value;
				var newValue = creator();
				// Check for race condition
				if (dict.TryGetValue(key, out value))
					return (T)value;
				dict.Add(key, newValue);
				return newValue;
			}
		}

		/// <summary>
		/// Returns the value of a property. The property must exist.
		/// </summary>
		/// <param name="key">Property key</param>
		/// <returns></returns>
		public object GetProperty(object key) {
			lock (lockObj)
				return dict[key];
		}

		/// <summary>
		/// Returns the value of a property. The property must exist.
		/// </summary>
		/// <typeparam name="TProperty">Property value type</typeparam>
		/// <param name="key">Property key</param>
		/// <returns></returns>
		public TProperty GetProperty<TProperty>(object key) {
			lock (lockObj)
				return (TProperty)dict[key];
		}

		/// <summary>
		/// Removes a property from the collection
		/// </summary>
		/// <param name="key">Property key</param>
		/// <returns></returns>
		public bool RemoveProperty(object key) {
			lock (lockObj)
				return dict.Remove(key);
		}

		/// <summary>
		/// Adds a property to the collection
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="property">Property value</param>
		void SetProperty(object key, object property) {
			lock (lockObj)
				dict[key] = property;
		}

		/// <summary>
		/// Tries to get a property value if it exists. Returns false if the property doesn't exist.
		/// </summary>
		/// <typeparam name="TProperty">Property value type</typeparam>
		/// <param name="key">Property key</param>
		/// <param name="property">Updated with the property value or the default value if the property key doesn't exist</param>
		/// <returns></returns>
		public bool TryGetProperty<TProperty>(object key, out TProperty property) {
			lock (lockObj) {
				object value;
				if (dict.TryGetValue(key, out value)) {
					property = (TProperty)value;
					return true;
				}
			}
			property = default(TProperty);
			return false;
		}
	}
}
