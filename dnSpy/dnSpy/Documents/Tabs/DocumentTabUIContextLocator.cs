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
using dnSpy.Contracts.Documents.Tabs;

namespace dnSpy.Documents.Tabs {
	sealed class DocumentTabUIContextLocator : IDocumentTabUIContextLocator, IDisposable {
		readonly Lazy<IDocumentTabUIContextProvider, IDocumentTabUIContextProviderMetadata>[] documentTabUIContextProviders;
		readonly Dictionary<object, WeakReference> weakCachedInstances;
		readonly Dictionary<object, object> strongCachedInstances;

		public DocumentTabUIContextLocator(Lazy<IDocumentTabUIContextProvider, IDocumentTabUIContextProviderMetadata>[] documentTabUIContextProviders) {
			this.documentTabUIContextProviders = documentTabUIContextProviders;
			weakCachedInstances = new Dictionary<object, WeakReference>();
			strongCachedInstances = new Dictionary<object, object>();
		}

		readonly struct ReferenceResult<T> where T : class {
			public T Reference { get; }
			public bool UseStrongReference { get; }
			public ReferenceResult(T reference, bool useStrongReference) {
				Reference = reference;
				UseStrongReference = useStrongReference;
			}
		}

		T GetOrCreate<T>(object key, Func<ReferenceResult<T>> creator) where T : class {
			if (strongCachedInstances.TryGetValue(key, out var obj))
				return (T)obj;
			if (weakCachedInstances.TryGetValue(key, out var weakRef)) {
				obj = weakRef.Target;
				if (obj is not null)
					return (T)obj;
			}

			var res = creator();
			if (res.Reference is null)
				throw new InvalidOperationException();
			if (res.UseStrongReference)
				strongCachedInstances[key] = res.Reference;
			else
				weakCachedInstances[key] = new WeakReference(res.Reference);
			return res.Reference;
		}

		ReferenceResult<T> Create<T>() where T : class {
			foreach (var c in documentTabUIContextProviders) {
				var t = c.Value.Create<T>() as T;
				if (t is not null)
					return new ReferenceResult<T>(t, c.Metadata.UseStrongReference);
			}
			throw new InvalidOperationException();
		}

		public T Get<T>() where T : class => GetOrCreate(typeof(T), () => Create<T>());
		public T Get<T>(object key, Func<T> creator) where T : class => Get(key, false, creator);
		public T Get<T>(object key, bool useStrongReference, Func<T> creator) where T : class {
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			if (creator is null)
				throw new ArgumentNullException(nameof(creator));
			// System.Type keys are reserved by us so use a new Key instance instead of directly using key
			return GetOrCreate(new Key(key), () => new ReferenceResult<T>(creator(), useStrongReference));
		}

		sealed class Key : IEquatable<Key?> {
			readonly object obj;

			public Key(object obj) => this.obj = obj ?? throw new ArgumentNullException(nameof(obj));

			public bool Equals(Key? other) => other is not null && obj.Equals(other.obj);
			public override bool Equals(object? obj) => Equals(obj as Key);
			public override int GetHashCode() => obj.GetHashCode();
		}

		public void Dispose() {
			foreach (var v in weakCachedInstances.Values)
				(v.Target as IDisposable)?.Dispose();
			foreach (var v in strongCachedInstances.Values)
				(v as IDisposable)?.Dispose();
			weakCachedInstances.Clear();
			strongCachedInstances.Clear();
		}
	}
}
