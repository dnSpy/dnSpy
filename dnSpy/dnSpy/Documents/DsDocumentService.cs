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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents {
	[Export(typeof(IDsDocumentService))]
	sealed class DsDocumentService : IDsDocumentService {
		readonly object lockObj;
		readonly List<IDsDocument> documents;
		readonly List<IDsDocument> tempCache;
		readonly IDsDocumentProvider[] documentProviders;

		public IAssemblyResolver AssemblyResolver { get; }

		sealed class DisableAssemblyLoadHelper : IDisposable {
			readonly DsDocumentService documentService;

			public DisableAssemblyLoadHelper(DsDocumentService documentService) {
				this.documentService = documentService;
				Interlocked.Increment(ref documentService.counter_DisableAssemblyLoad);
			}

			public void Dispose() {
				int value = Interlocked.Decrement(ref documentService.counter_DisableAssemblyLoad);
				if (value == 0)
					documentService.ClearTempCache();
			}
		}

		bool AssemblyLoadEnabled => counter_DisableAssemblyLoad == 0;
		int counter_DisableAssemblyLoad;

		public IDisposable DisableAssemblyLoad() => new DisableAssemblyLoadHelper(this);
		public event EventHandler<NotifyDocumentCollectionChangedEventArgs> CollectionChanged;
		public IDsDocumentServiceSettings Settings { get; }

		[ImportingConstructor]
		public DsDocumentService(IDsDocumentServiceSettings documentServiceSettings, [ImportMany] IDsDocumentProvider[] documentProviders) {
			this.lockObj = new object();
			this.documents = new List<IDsDocument>();
			this.tempCache = new List<IDsDocument>();
			this.AssemblyResolver = new AssemblyResolver(this);
			this.documentProviders = documentProviders.OrderBy(a => a.Order).ToArray();
			this.Settings = documentServiceSettings;
		}

		void CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs eventArgs, bool delayLoad = true) {
			if (delayLoad && dispatcher != null)
				dispatcher(() => CallCollectionChanged2(eventArgs));
			else
				CallCollectionChanged2(eventArgs);
		}

		void CallCollectionChanged2(NotifyDocumentCollectionChangedEventArgs eventArgs) =>
			CollectionChanged?.Invoke(this, eventArgs);

		public IDsDocument[] GetDocuments() {
			lock (lockObj)
				return documents.ToArray();
		}

		public void Clear() {
			IDsDocument[] oldDocuments;
			lock (lockObj) {
				oldDocuments = documents.ToArray();
				documents.Clear();
			}
			if (oldDocuments.Length != 0)
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateClear(oldDocuments, null));
		}

		public IDsDocument FindAssembly(IAssembly assembly) {
			var comparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			lock (lockObj) {
				foreach (var document in documents) {
					if (comparer.Equals(document.AssemblyDef, assembly))
						return document;
				}
			}
			lock (tempCache) {
				foreach (var document in tempCache) {
					if (comparer.Equals(document.AssemblyDef, assembly))
						return document;
				}
			}
			return null;
		}

		public IDsDocument Resolve(IAssembly asm, ModuleDef sourceModule) {
			var document = FindAssembly(asm);
			if (document != null)
				return document;
			var asmDef = this.AssemblyResolver.Resolve(asm, sourceModule);
			if (asmDef != null)
				return FindAssembly(asm);
			return null;
		}

		public IDsDocument Find(IDsDocumentNameKey key) {
			lock (lockObj)
				return Find_NoLock(key);
		}

		IDsDocument Find_NoLock(IDsDocumentNameKey key) {
			Debug.Assert(key != null);
			if (key == null)
				return null;
			foreach (var document in documents) {
				if (key.Equals(document.Key))
					return document;
			}
			return null;
		}

		public IDsDocument GetOrAdd(IDsDocument document) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			IDsDocument result;
			lock (lockObj)
				result = GetOrAdd_NoLock(document);
			if (result == document)
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateAdd(result, null));
			return result;
		}

		public IDsDocument ForceAdd(IDsDocument document, bool delayLoad, object data) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			lock (lockObj)
				documents.Add(document);

			CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateAdd(document, data), delayLoad);
			return document;
		}

		internal IDsDocument GetOrAddCanDispose(IDsDocument document) {
			document.IsAutoLoaded = true;
			var result = Find(document.Key);
			if (result == null) {
				if (!AssemblyLoadEnabled)
					return AddTempCachedDocument(document);
				result = GetOrAdd(document);
			}
			if (result != document)
				Dispose(document);
			return result;
		}

		IDsDocument AddTempCachedDocument(IDsDocument document) {
			// Disable mmap'd I/O before adding it to the temp cache to prevent another thread from
			// getting the same file while we're disabling mmap'd I/O. Could lead to crashes.
			DisableMMapdIO(document);

			lock (tempCache) {
				if (!AssemblyLoadEnabled)
					tempCache.Add(document);
			}
			return document;
		}

		void ClearTempCache() {
			bool collect;
			lock (tempCache) {
				collect = tempCache.Count > 0;
				tempCache.Clear();
			}
			if (collect) {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		// Should be called if we don't save it in the documents list. It will eventually be GC'd but it's
		// better to disable mmap'd I/O as soon as possible. The document must've been created by us.
		static IDsDocument DisableMMapdIO(IDsDocument document) {
			MemoryMappedIOHelper.DisableMemoryMappedIO(document);
			return document;
		}

		IDsDocument GetOrAdd_NoLock(IDsDocument document) {
			var existing = Find_NoLock(document.Key);
			if (existing != null)
				return existing;

			documents.Add(document);
			return document;
		}

		public IDsDocument TryGetOrCreate(DsDocumentInfo info, bool isAutoLoaded) =>
			TryGetOrCreateInternal(info, isAutoLoaded, false);

		internal IDsDocument TryGetOrCreateInternal(DsDocumentInfo info, bool isAutoLoaded, bool isResolve) {
			var key = TryCreateKey(info);
			if (key == null)
				return null;
			var existing = Find(key);
			if (existing != null)
				return existing;

			var newDocument = TryCreateDocument(info);
			if (newDocument == null)
				return null;
			newDocument.IsAutoLoaded = isAutoLoaded;
			if (isResolve && !AssemblyLoadEnabled)
				return AddTempCachedDocument(newDocument);

			var result = GetOrAdd(newDocument);
			if (result != newDocument)
				Dispose(newDocument);

			return result;
		}

		static void Dispose(IDsDocument document) => (document as IDisposable)?.Dispose();

		IDsDocumentNameKey TryCreateKey(DsDocumentInfo info) {
			foreach (var provider in documentProviders) {
				try {
					var key = provider.CreateKey(this, info);
					if (key != null)
						return key;
				}
				catch (Exception ex) {
					Debug.WriteLine($"{nameof(IDsDocumentProvider)} ({provider.GetType()}) failed with an exception: {ex.Message}");
				}
			}

			return null;
		}

		internal IDsDocument TryCreateDocument(DsDocumentInfo info) {
			foreach (var provider in documentProviders) {
				try {
					var document = provider.Create(this, info);
					if (document != null)
						return document;
				}
				catch (Exception ex) {
					Debug.WriteLine($"{nameof(IDsDocumentProvider)} ({provider.GetType()}) failed with an exception: {ex.Message}");
				}
			}

			return null;
		}

		public IDsDocument TryCreateOnly(DsDocumentInfo info) => TryCreateDocument(info);
		internal static IDsDocument CreateDocumentFromFile(DsDocumentInfo documentInfo, string filename, bool useMemoryMappedIO, bool loadPDBFiles, IAssemblyResolver asmResolver) =>
			DsDocument.CreateDocumentFromFile(documentInfo, filename, useMemoryMappedIO, loadPDBFiles, asmResolver, false);

		public void Remove(IDsDocumentNameKey key) {
			Debug.Assert(key != null);
			if (key == null)
				return;

			IDsDocument removedDocument;
			lock (lockObj)
				removedDocument = Remove_NoLock(key);
			Debug.Assert(removedDocument != null);

			if (removedDocument != null)
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateRemove(removedDocument, null));
		}

		IDsDocument Remove_NoLock(IDsDocumentNameKey key) {
			if (key == null)
				return null;

			for (int i = 0; i < documents.Count; i++) {
				if (key.Equals(documents[i].Key)) {
					documents.RemoveAt(i);
					return documents[i];
				}
			}

			return null;
		}

		public void Remove(IEnumerable<IDsDocument> documents) {
			var removedDocuments = new List<IDsDocument>();
			lock (lockObj) {
				var dict = new Dictionary<IDsDocument, int>();
				int i = 0;
				foreach (var n in this.documents)
					dict[n] = i++;
				var list = new List<Tuple<IDsDocument, int>>(documents.Select(a => {
					int j;
					bool b = dict.TryGetValue(a, out j);
					Debug.Assert(b);
					return Tuple.Create(a, b ? j : -1);
				}));
				list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
				foreach (var t in list) {
					if (t.Item2 < 0)
						continue;
					Debug.Assert((uint)t.Item2 < (uint)this.documents.Count);
					Debug.Assert(this.documents[t.Item2] == t.Item1);
					this.documents.RemoveAt(t.Item2);
					removedDocuments.Add(t.Item1);
				}
			}

			if (removedDocuments.Count > 0)
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateRemove(removedDocuments.ToArray(), null));
		}

		public void SetDispatcher(Action<Action> action) {
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (dispatcher != null)
				throw new InvalidOperationException("SetDispatcher() can only be called once");
			dispatcher = action;
		}
		Action<Action> dispatcher;
	}
}
