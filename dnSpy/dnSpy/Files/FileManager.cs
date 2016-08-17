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
using dnSpy.Contracts.Files;

namespace dnSpy.Files {
	[Export(typeof(IFileManager))]
	sealed class FileManager : IFileManager {
		readonly object lockObj;
		readonly List<IDnSpyFile> files;
		readonly List<IDnSpyFile> tempCache;
		readonly IDnSpyFileProvider[] dnSpyFileProviders;

		public IAssemblyResolver AssemblyResolver { get; }

		sealed class DisableAssemblyLoadHelper : IDisposable {
			readonly FileManager fileManager;

			public DisableAssemblyLoadHelper(FileManager fileManager) {
				this.fileManager = fileManager;
				Interlocked.Increment(ref fileManager.counter_DisableAssemblyLoad);
			}

			public void Dispose() {
				int value = Interlocked.Decrement(ref fileManager.counter_DisableAssemblyLoad);
				if (value == 0)
					fileManager.ClearTempCache();
			}
		}

		bool AssemblyLoadEnabled => counter_DisableAssemblyLoad == 0;
		int counter_DisableAssemblyLoad;

		public IDisposable DisableAssemblyLoad() => new DisableAssemblyLoadHelper(this);
		public event EventHandler<NotifyFileCollectionChangedEventArgs> CollectionChanged;
		public IFileManagerSettings Settings { get; }

		[ImportingConstructor]
		public FileManager(IFileManagerSettings fileManagerSettings, [ImportMany] IDnSpyFileProvider[] dnSpyFileProviders) {
			this.lockObj = new object();
			this.files = new List<IDnSpyFile>();
			this.tempCache = new List<IDnSpyFile>();
			this.AssemblyResolver = new AssemblyResolver(this);
			this.dnSpyFileProviders = dnSpyFileProviders.OrderBy(a => a.Order).ToArray();
			this.Settings = fileManagerSettings;
		}

		void CallCollectionChanged(NotifyFileCollectionChangedEventArgs eventArgs, bool delayLoad = true) {
			if (delayLoad && dispatcher != null)
				dispatcher(() => CallCollectionChanged2(eventArgs));
			else
				CallCollectionChanged2(eventArgs);
		}

		void CallCollectionChanged2(NotifyFileCollectionChangedEventArgs eventArgs) =>
			CollectionChanged?.Invoke(this, eventArgs);

		public IDnSpyFile[] GetFiles() {
			lock (lockObj)
				return files.ToArray();
		}

		public void Clear() {
			IDnSpyFile[] oldFiles;
			lock (lockObj) {
				oldFiles = files.ToArray();
				files.Clear();
			}
			if (oldFiles.Length != 0)
				CallCollectionChanged(NotifyFileCollectionChangedEventArgs.CreateClear(oldFiles, null));
		}

		public IDnSpyFile FindAssembly(IAssembly assembly) {
			var comparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			lock (lockObj) {
				foreach (var file in files) {
					if (comparer.Equals(file.AssemblyDef, assembly))
						return file;
				}
			}
			lock (tempCache) {
				foreach (var file in tempCache) {
					if (comparer.Equals(file.AssemblyDef, assembly))
						return file;
				}
			}
			return null;
		}

		public IDnSpyFile Resolve(IAssembly asm, ModuleDef sourceModule) {
			var file = FindAssembly(asm);
			if (file != null)
				return file;
			var asmDef = this.AssemblyResolver.Resolve(asm, sourceModule);
			if (asmDef != null)
				return FindAssembly(asm);
			return null;
		}

		public IDnSpyFile Find(IDnSpyFilenameKey key) {
			lock (lockObj)
				return Find_NoLock(key);
		}

		IDnSpyFile Find_NoLock(IDnSpyFilenameKey key) {
			Debug.Assert(key != null);
			if (key == null)
				return null;
			foreach (var file in files) {
				if (key.Equals(file.Key))
					return file;
			}
			return null;
		}

		public IDnSpyFile GetOrAdd(IDnSpyFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			IDnSpyFile result;
			lock (lockObj)
				result = GetOrAdd_NoLock(file);
			if (result == file)
				CallCollectionChanged(NotifyFileCollectionChangedEventArgs.CreateAdd(result, null));
			return result;
		}

		public IDnSpyFile ForceAdd(IDnSpyFile file, bool delayLoad, object data) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			lock (lockObj)
				files.Add(file);

			CallCollectionChanged(NotifyFileCollectionChangedEventArgs.CreateAdd(file, data), delayLoad);
			return file;
		}

		internal IDnSpyFile GetOrAddCanDispose(IDnSpyFile file) {
			file.IsAutoLoaded = true;
			var result = Find(file.Key);
			if (result == null) {
				if (!AssemblyLoadEnabled)
					return AddTempCachedFile(file);
				result = GetOrAdd(file);
			}
			if (result != file)
				Dispose(file);
			return result;
		}

		IDnSpyFile AddTempCachedFile(IDnSpyFile file) {
			// Disable mmap'd I/O before adding it to the temp cache to prevent another thread from
			// getting the same file while we're disabling mmap'd I/O. Could lead to crashes.
			DisableMMapdIO(file);

			lock (tempCache) {
				if (!AssemblyLoadEnabled)
					tempCache.Add(file);
			}
			return file;
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

		// Should be called if we don't save it in the files list. It will eventually be GC'd but it's
		// better to disable mmap'd I/O as soon as possible. The file must've been created by us.
		static IDnSpyFile DisableMMapdIO(IDnSpyFile file) {
			MemoryMappedIOHelper.DisableMemoryMappedIO(file);
			return file;
		}

		IDnSpyFile GetOrAdd_NoLock(IDnSpyFile file) {
			var existing = Find_NoLock(file.Key);
			if (existing != null)
				return existing;

			files.Add(file);
			return file;
		}

		public IDnSpyFile TryGetOrCreate(DnSpyFileInfo info, bool isAutoLoaded) =>
			TryGetOrCreateInternal(info, isAutoLoaded, false);

		internal IDnSpyFile TryGetOrCreateInternal(DnSpyFileInfo info, bool isAutoLoaded, bool isResolve) {
			var key = TryCreateKey(info);
			if (key == null)
				return null;
			var existing = Find(key);
			if (existing != null)
				return existing;

			var newFile = TryCreateDnSpyFile(info);
			if (newFile == null)
				return null;
			newFile.IsAutoLoaded = isAutoLoaded;
			if (isResolve && !AssemblyLoadEnabled)
				return AddTempCachedFile(newFile);

			var result = GetOrAdd(newFile);
			if (result != newFile)
				Dispose(newFile);

			return result;
		}

		static void Dispose(IDnSpyFile file) => (file as IDisposable)?.Dispose();

		IDnSpyFilenameKey TryCreateKey(DnSpyFileInfo info) {
			foreach (var provider in dnSpyFileProviders) {
				try {
					var key = provider.CreateKey(this, info);
					if (key != null)
						return key;
				}
				catch (Exception ex) {
					Debug.WriteLine($"{nameof(IDnSpyFileProvider)} ({provider.GetType()}) failed with an exception: {ex.Message}");
				}
			}

			return null;
		}

		internal IDnSpyFile TryCreateDnSpyFile(DnSpyFileInfo info) {
			foreach (var provider in dnSpyFileProviders) {
				try {
					var file = provider.Create(this, info);
					if (file != null)
						return file;
				}
				catch (Exception ex) {
					Debug.WriteLine($"{nameof(IDnSpyFileProvider)} ({provider.GetType()}) failed with an exception: {ex.Message}");
				}
			}

			return null;
		}

		public IDnSpyFile TryCreateOnly(DnSpyFileInfo info) => TryCreateDnSpyFile(info);
		internal static IDnSpyFile CreateDnSpyFileFromFile(DnSpyFileInfo fileInfo, string filename, bool useMemoryMappedIO, bool loadPDBFiles, IAssemblyResolver asmResolver) =>
			DnSpyFile.CreateDnSpyFileFromFile(fileInfo, filename, useMemoryMappedIO, loadPDBFiles, asmResolver, false);

		public void Remove(IDnSpyFilenameKey key) {
			Debug.Assert(key != null);
			if (key == null)
				return;

			IDnSpyFile removedFile;
			lock (lockObj)
				removedFile = Remove_NoLock(key);
			Debug.Assert(removedFile != null);

			if (removedFile != null)
				CallCollectionChanged(NotifyFileCollectionChangedEventArgs.CreateRemove(removedFile, null));
		}

		IDnSpyFile Remove_NoLock(IDnSpyFilenameKey key) {
			if (key == null)
				return null;

			for (int i = 0; i < files.Count; i++) {
				if (key.Equals(files[i].Key)) {
					files.RemoveAt(i);
					return files[i];
				}
			}

			return null;
		}

		public void Remove(IEnumerable<IDnSpyFile> files) {
			var removedFiles = new List<IDnSpyFile>();
			lock (lockObj) {
				var dict = new Dictionary<IDnSpyFile, int>();
				int i = 0;
				foreach (var n in this.files)
					dict[n] = i++;
				var list = new List<Tuple<IDnSpyFile, int>>(files.Select(a => {
					int j;
					bool b = dict.TryGetValue(a, out j);
					Debug.Assert(b);
					return Tuple.Create(a, b ? j : -1);
				}));
				list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
				foreach (var t in list) {
					if (t.Item2 < 0)
						continue;
					Debug.Assert((uint)t.Item2 < (uint)this.files.Count);
					Debug.Assert(this.files[t.Item2] == t.Item1);
					this.files.RemoveAt(t.Item2);
					removedFiles.Add(t.Item1);
				}
			}

			if (removedFiles.Count > 0)
				CallCollectionChanged(NotifyFileCollectionChangedEventArgs.CreateRemove(removedFiles.ToArray(), null));
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
