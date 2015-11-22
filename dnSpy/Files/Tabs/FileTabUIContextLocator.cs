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

using System;
using System.Collections.Generic;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Files.Tabs {
	sealed class FileTabUIContextLocator : IFileTabUIContextLocator {
		readonly IFileTabUIContextCreator[] creators;
		readonly Dictionary<object, WeakReference> cachedInstances;

		public FileTabUIContextLocator(IFileTabUIContextCreator[] creators) {
			this.creators = creators;
			this.cachedInstances = new Dictionary<object, WeakReference>();
		}

		T GetOrCreate<T>(object key, Func<T> creator) where T : class, IFileTabUIContext {
			WeakReference weakRef;
			if (cachedInstances.TryGetValue(key, out weakRef)) {
				var obj = weakRef.Target;
				if (obj != null)
					return (T)obj;
			}

			var res = creator();
			if (res == null)
				throw new InvalidOperationException();
			cachedInstances[key] = new WeakReference(res);
			return res;
		}

		T Create<T>() where T : class, IFileTabUIContext {
			foreach (var c in creators) {
				var t = c.Create<T>();
				if (t != null)
					return t;
			}
			throw new InvalidOperationException();
		}

		public T Get<T>() where T : class, IFileTabUIContext {
			return GetOrCreate(typeof(T), () => Create<T>());
		}

		public T Get<T>(object key, Func<T> creator) where T : class, IFileTabUIContext {
			if (key == null || creator == null)
				throw new ArgumentNullException();
			// System.Type keys are reserved by us so use a new Key instance instead of directly using key
			return GetOrCreate(new Key(key), () => creator());
		}

		sealed class Key : IEquatable<Key> {
            readonly object obj;

			public Key(object obj) {
				if (obj == null)
					throw new ArgumentNullException();
				this.obj = obj;
			}

			public bool Equals(Key other) {
				return other != null &&
						obj.Equals(other.obj);
			}

			public override bool Equals(object obj) {
				return Equals(obj as Key);
			}

			public override int GetHashCode() {
				return obj.GetHashCode();
			}
		}
	}
}
