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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.DnSpy.Metadata;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents {
	[Export(typeof(IDsDocumentService))]
	sealed class DsDocumentService : IDsDocumentService {
		// PERF: Most of the time we only read from the assembly list so use a ReaderWriterLockSlim instead of a normal lock
		readonly ReaderWriterLockSlim rwLock;
		readonly List<DocumentInfo> documents;
		readonly object tempCacheLock;
		HashSet<IDsDocument> tempCache;
		readonly IDsDocumentProvider[] documentProviders;
		readonly AssemblyResolver assemblyResolver;

		// PERF: Must be a struct; class is 9% slower (decompile mscorlib+dnSpy = 83 files)
		readonly struct DocumentInfo {
			readonly List<AssemblyRef> alternativeAssemblyNames;
			public readonly IDsDocument Document;

			public DocumentInfo(IDsDocument document) {
				alternativeAssemblyNames = new List<AssemblyRef>();
				Document = document;
			}

			public bool IsAlternativeAssemblyName(IAssembly asm) {
				var list = alternativeAssemblyNames;
				int count = list.Count;
				for (int i = 0; i < count; i++) {
					if (AssemblyNameComparer.CompareAll.Equals(list[i], asm))
						return true;
				}
				return false;
			}

			public void AddAlternativeAssemblyName(IAssembly asm) {
				if (!IsAlternativeAssemblyName(asm))
					alternativeAssemblyNames.Add(asm.ToAssemblyRef());
			}
		}

		public IAssemblyResolver AssemblyResolver => assemblyResolver;

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
		public event EventHandler<NotifyDocumentCollectionChangedEventArgs>? CollectionChanged;
		public IDsDocumentServiceSettings Settings { get; }

		[ImportingConstructor]
		public DsDocumentService(IDsDocumentServiceSettings documentServiceSettings, [ImportMany] IDsDocumentProvider[] documentProviders, [ImportMany] Lazy<IRuntimeAssemblyResolver, IRuntimeAssemblyResolverMetadata>[] runtimeAsmResolvers) {
			rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			documents = new List<DocumentInfo>();
			tempCacheLock = new object();
			tempCache = new HashSet<IDsDocument>();
			assemblyResolver = new AssemblyResolver(this, runtimeAsmResolvers.OrderBy(a => a.Metadata.Order).ToArray());
			this.documentProviders = documentProviders.OrderBy(a => a.Order).ToArray();
			Settings = documentServiceSettings;
		}

		void CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs eventArgs, bool delayLoad = true) {
			if (delayLoad && !(dispatcher is null))
				dispatcher(() => CallCollectionChanged2(eventArgs));
			else
				CallCollectionChanged2(eventArgs);
		}

		void CallCollectionChanged2(NotifyDocumentCollectionChangedEventArgs eventArgs) {
			if (eventArgs.Type == NotifyDocumentCollectionType.Clear)
				assemblyResolver.OnAssembliesCleared();
			CollectionChanged?.Invoke(this, eventArgs);
		}

		public IDsDocument[] GetDocuments() {
			rwLock.EnterReadLock();
			try {
				return documents.Select(a => a.Document).ToArray();
			}
			finally {
				rwLock.ExitReadLock();
			}
		}

		public void Clear() {
			IDsDocument[] oldDocuments;
			rwLock.EnterWriteLock();
			try {
				oldDocuments = documents.Select(a => a.Document).ToArray();
				documents.Clear();
			}
			finally {
				rwLock.ExitWriteLock();
			}
			if (oldDocuments.Length != 0)
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateClear(oldDocuments, null));
		}

		static AssemblyNameComparerFlags ToAssemblyNameComparerFlags(FindAssemblyOptions options) {
			AssemblyNameComparerFlags flags = 0;
			if ((options & FindAssemblyOptions.Name) != 0)
				flags |= AssemblyNameComparerFlags.Name;
			if ((options & FindAssemblyOptions.Version) != 0)
				flags |= AssemblyNameComparerFlags.Version;
			if ((options & FindAssemblyOptions.PublicKeyToken) != 0)
				flags |= AssemblyNameComparerFlags.PublicKeyToken;
			if ((options & FindAssemblyOptions.Culture) != 0)
				flags |= AssemblyNameComparerFlags.Culture;
			if ((options & FindAssemblyOptions.ContentType) != 0)
				flags |= AssemblyNameComparerFlags.ContentType;
			return flags;
		}

		internal const FindAssemblyOptions DefaultOptions = FindAssemblyOptions.All & ~FindAssemblyOptions.Version;
		public IDsDocument? FindAssembly(IAssembly assembly) => FindAssembly(assembly, DefaultOptions);
		public IDsDocument? FindAssembly(IAssembly assembly, FindAssemblyOptions options) {
			var flags = ToAssemblyNameComparerFlags(options);
			var comparer = new AssemblyNameComparer(flags);
			rwLock.EnterReadLock();
			try {
				foreach (var info in documents) {
					if (comparer.Equals(info.Document.AssemblyDef, assembly))
						return info.Document;
				}
				foreach (var info in documents) {
					if (info.IsAlternativeAssemblyName(assembly))
						return info.Document;
				}
			}
			finally {
				rwLock.ExitReadLock();
			}
			lock (tempCacheLock) {
				foreach (var document in tempCache) {
					if (comparer.Equals(document.AssemblyDef, assembly))
						return document;
				}
			}
			return null;
		}

		public IDsDocument? Resolve(IAssembly asm, ModuleDef? sourceModule) {
			var document = FindAssembly(asm);
			if (!(document is null))
				return document;
			var asmDef = AssemblyResolver.Resolve(asm, sourceModule);
			if (!(asmDef is null))
				return FindAssembly(asm);
			return null;
		}

		public IDsDocument? Find(IDsDocumentNameKey key) => Find(key, checkTempCache: false);

		internal IDsDocument? Find(IDsDocumentNameKey key, bool checkTempCache) {
			IDsDocument doc;
			rwLock.EnterReadLock();
			try {
				doc = Find_NoLock(key).Document;
			}
			finally {
				rwLock.ExitReadLock();
			}
			if (!(doc is null))
				return doc;

			if (checkTempCache) {
				lock (tempCacheLock) {
					foreach (var document in tempCache) {
						if (key.Equals(document.Key))
							return document;
					}
				}
			}

			return null;
		}

		DocumentInfo Find_NoLock(IDsDocumentNameKey key) {
			Debug2.Assert(!(key is null));
			if (key is null)
				return default;
			foreach (var info in documents) {
				if (key.Equals(info.Document.Key))
					return info;
			}
			return default;
		}

		public IDsDocument GetOrAdd(IDsDocument document) {
			if (document is null)
				throw new ArgumentNullException(nameof(document));

			IDsDocument result;
			rwLock.EnterUpgradeableReadLock();
			try {
				var existing = Find_NoLock(document.Key).Document;
				if (!(existing is null))
					result = existing;
				else {
					rwLock.EnterWriteLock();
					try {
						documents.Add(new DocumentInfo(document));
						result = document;
					}
					finally {
						rwLock.ExitWriteLock();
					}
				}
			}
			finally {
				rwLock.ExitUpgradeableReadLock();
			}

			if (result == document)
				NotifyDocumentAdded(result, null);
			return result;
		}

		void NotifyDocumentAdded(IDsDocument document, object? data, bool delayLoad = true) {
			(document as IDsDocument2)?.OnAdded();
			CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateAdd(document, data), delayLoad);
		}

		public IDsDocument ForceAdd(IDsDocument document, bool delayLoad, object? data) {
			if (document is null)
				throw new ArgumentNullException(nameof(document));

			rwLock.EnterWriteLock();
			try {
				documents.Add(new DocumentInfo(document));
			}
			finally {
				rwLock.ExitWriteLock();
			}

			NotifyDocumentAdded(document, data, delayLoad);
			return document;
		}

		internal IDsDocument GetOrAddCanDispose(IDsDocument document, IAssembly origAssemblyRef, bool isAutoLoaded) {
			document.IsAutoLoaded = isAutoLoaded;
			IDsDocument result;
			DocumentInfo info;
			rwLock.EnterReadLock();
			try {
				info = Find_NoLock(document.Key);
				result = info.Document;
			}
			finally {
				rwLock.ExitReadLock();
			}
			if (result is null) {
				if (!AssemblyLoadEnabled)
					return AddTempCachedDocument(document);
				result = GetOrAdd(document);
			}
			if (!(info.Document is null) && !(origAssemblyRef is null) && document.AssemblyDef is AssemblyDef asm) {
				if (!AssemblyNameComparer.CompareAll.Equals(origAssemblyRef, asm)) {
					rwLock.EnterWriteLock();
					try {
						info.AddAlternativeAssemblyName(origAssemblyRef);
					}
					finally {
						rwLock.ExitWriteLock();
					}
				}
			}
			if (result != document)
				Dispose(document);
			return result;
		}

		IDsDocument AddTempCachedDocument(IDsDocument document) {
			// PERF: most of the time this method has been called with the same document.
			// If so, mmap'd IO has already been disabled and we don't need to do it again.
			bool addIt;
			lock (tempCacheLock)
				addIt = !AssemblyLoadEnabled && !tempCache.Contains(document);

			if (addIt) {
				// Disable mmap'd I/O before adding it to the temp cache to prevent another thread from
				// getting the same file while we're disabling mmap'd I/O. Could lead to crashes.
				DisableMMapdIO(document);
				lock (tempCacheLock) {
					if (!AssemblyLoadEnabled)
						tempCache.Add(document);
				}
			}

			return document;
		}

		void ClearTempCache() {
			lock (tempCacheLock) {
				if (tempCache.Count > 0)
					tempCache = new HashSet<IDsDocument>();
			}
		}

		// Should be called if we don't save it in the documents list. It will eventually be GC'd but it's
		// better to disable mmap'd I/O as soon as possible. The document must've been created by us.
		static IDsDocument DisableMMapdIO(IDsDocument document) {
			MemoryMappedIOHelper.DisableMemoryMappedIO(document);
			return document;
		}

		public IDsDocument? TryGetOrCreate(DsDocumentInfo info, bool isAutoLoaded) =>
			TryGetOrCreateInternal(info, isAutoLoaded, false);

		internal IDsDocument? TryGetOrCreateInternal(DsDocumentInfo info, bool isAutoLoaded, bool isResolve) {
			var key = TryCreateKey(info);
			if (key is null)
				return null;
			var existing = Find(key);
			if (!(existing is null))
				return existing;

			var newDocument = TryCreateDocument(info);
			if (newDocument is null)
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

		IDsDocumentNameKey? TryCreateKey(DsDocumentInfo info) {
			foreach (var provider in documentProviders) {
				try {
					var key = provider.CreateKey(this, info);
					if (!(key is null))
						return key;
				}
				catch (Exception ex) {
					Debug.WriteLine($"{nameof(IDsDocumentProvider)} ({provider.GetType()}) failed with an exception: {ex.Message}");
				}
			}

			return null;
		}

		internal IDsDocument? TryCreateDocument(DsDocumentInfo info) {
			foreach (var provider in documentProviders) {
				try {
					var document = provider.Create(this, info);
					if (!(document is null))
						return document;
				}
				catch (Exception ex) {
					Debug.WriteLine($"{nameof(IDsDocumentProvider)} ({provider.GetType()}) failed with an exception: {ex.Message}");
				}
			}

			return null;
		}

		public IDsDocument? TryCreateOnly(DsDocumentInfo info) => TryCreateDocument(info);

		public IDsDocument CreateDocument(DsDocumentInfo documentInfo, string filename, bool isModule) =>
			CreateDocumentCore(documentInfo, null, filename, true, isModule);

		public IDsDocument CreateDocument(DsDocumentInfo documentInfo, byte[] fileData, string? filename, bool isFileLayout, bool isModule) =>
			CreateDocumentCore(documentInfo, fileData, filename ?? string.Empty, isFileLayout, isModule);

		IDsDocument CreateDocumentCore(DsDocumentInfo documentInfo, byte[]? fileData, string filename, bool isFileLayout, bool isModule) {
			try {
				IPEImage peImage;

				if (!(fileData is null))
					peImage = new PEImage(fileData, filename, isFileLayout ? ImageLayout.File : ImageLayout.Memory, verify: true);
				else {
					Debug.Assert(isFileLayout);

					// Quick check to prevent exceptions from being thrown
					if (!File.Exists(filename))
						return new DsUnknownDocument(filename);

					if (Settings.UseMemoryMappedIO)
						peImage = new PEImage(filename);
					else
						peImage = new PEImage(File.ReadAllBytes(filename), filename);
				}

				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				// Mono doesn't check that the Size field is >= 0x48
				bool isDotNet = dotNetDir.VirtualAddress != 0 /*&& dotNetDir.Size >= 0x48*/;
				if (isDotNet) {
					try {
						var options = new ModuleCreationOptions(DsDotNetDocumentBase.CreateModuleContext(assemblyResolver));
						options.TryToLoadPdbFromDisk = false;
						if (isModule)
							return DsDotNetDocument.CreateModule(documentInfo, ModuleDefMD.Load(peImage, options), true);
						return DsDotNetDocument.CreateAssembly(documentInfo, ModuleDefMD.Load(peImage, options), true);
					}
					catch {
					}
				}

				return new DsPEDocument(peImage);
			}
			catch {
			}

			return new DsUnknownDocument(filename);
		}

		public void Remove(IDsDocumentNameKey key) {
			Debug2.Assert(!(key is null));
			if (key is null)
				return;

			IDsDocument? removedDocument;
			rwLock.EnterWriteLock();
			try {
				removedDocument = Remove_NoLock(key);
			}
			finally {
				rwLock.ExitWriteLock();
			}
			Debug2.Assert(!(removedDocument is null));

			if (!(removedDocument is null))
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateRemove(removedDocument, null));
		}

		IDsDocument? Remove_NoLock(IDsDocumentNameKey key) {
			if (key is null)
				return null;

			for (int i = 0; i < documents.Count; i++) {
				var info = documents[i];
				if (key.Equals(info.Document.Key)) {
					documents.RemoveAt(i);
					return info.Document;
				}
			}

			return null;
		}

		public void Remove(IEnumerable<IDsDocument> documents) {
			var removedDocuments = new List<IDsDocument>();
			rwLock.EnterWriteLock();
			try {
				var dict = new Dictionary<IDsDocument, int>();
				int i = 0;
				foreach (var n in this.documents)
					dict[n.Document] = i++;
				var list = new List<(IDsDocument document, int index)>(documents.Select(a => {
					bool b = dict.TryGetValue(a, out int j);
					return (a, b ? j : -1);
				}));
				list.Sort((a, b) => b.index.CompareTo(a.index));
				foreach (var t in list) {
					if (t.index < 0)
						continue;
					Debug.Assert((uint)t.index < (uint)this.documents.Count);
					Debug.Assert(this.documents[t.index].Document == t.document);
					this.documents.RemoveAt(t.index);
					removedDocuments.Add(t.document);
				}
			}
			finally {
				rwLock.ExitWriteLock();
			}

			if (removedDocuments.Count > 0)
				CallCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateRemove(removedDocuments.ToArray(), null));
		}

		public void SetDispatcher(Action<Action> action) {
			if (!(dispatcher is null))
				throw new InvalidOperationException("SetDispatcher() can only be called once");
			dispatcher = action ?? throw new ArgumentNullException(nameof(action));
		}
		Action<Action>? dispatcher;
	}
}
