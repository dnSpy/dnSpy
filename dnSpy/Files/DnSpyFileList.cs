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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.Files {
	public sealed class DnSpyFileList {
		readonly object lockObj;
		readonly ObservableCollection<DnSpyFile> files;

		public IDnSpyFileListOptions DnSpyFileListOptions {
			get { return options; }
		}
		readonly IDnSpyFileListOptions options;

		public AssemblyResolver AssemblyResolver {
			get { return assemblyResolver; }
		}
		readonly AssemblyResolver assemblyResolver;

		internal bool IsDirty { get; set; }

		//TODO: Remove these no-lock methods and props
		internal int Count_NoLock {
			get { return files.Count; }
		}
		internal int IndexOf_NoLock(DnSpyFile file) {
			return files.IndexOf(file);
		}
		internal void RemoveAt_NoLock(int index) {
			files.RemoveAt(index);
		}
		internal void Insert_NoLock(int index, DnSpyFile file) {
			files.Insert(index, file);
		}
		internal object GetLockObj() {
			return lockObj;
		}

		//TODO: Remove
		public bool IsReArranging { get; internal set; }

		sealed class DisableAssemblyLoadHelper : IDisposable {
			readonly DnSpyFileList list;

			public DisableAssemblyLoadHelper(DnSpyFileList list) {
				this.list = list;
				Interlocked.Increment(ref list.counter_DisableAssemblyLoad);
			}

			public void Dispose() {
				Interlocked.Decrement(ref list.counter_DisableAssemblyLoad);
			}
		}

		internal bool AssemblyLoadEnabled {
			get { return counter_DisableAssemblyLoad == 0; }
		}
		int counter_DisableAssemblyLoad;

		public IDisposable DisableAssemblyLoad() {
			return new DisableAssemblyLoadHelper(this);
		}

		public string Name {
			get { return name; }
		}
		readonly string name;

		public bool UseGAC {
			get { return options.UseGAC; }
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged {
			add { files.CollectionChanged += value; }
			remove { files.CollectionChanged -= value; }
		}

		public DnSpyFileList(IDnSpyFileListOptions options, string name) {
			this.options = options;
			this.name = name;
			this.lockObj = new object();
			this.files = new ObservableCollection<DnSpyFile>();
			this.assemblyResolver = new AssemblyResolver(this);
		}

		public DnSpyFile[] GetDnSpyFiles() {
			lock (lockObj)
				return files.ToArray();
		}

		public ModuleDef[] GetAllModules() {
			lock (lockObj) {
				return files.SelectMany(a => {
					var asm = a.AssemblyDef;
					if (asm != null)
						return (IEnumerable<ModuleDef>)asm.Modules; // cast to make compiler happy
					var mod = a.ModuleDef;
					if (mod != null)
						return new ModuleDef[] { mod };
					return new ModuleDef[0];
				}).ToArray();
			}
		}

		public DnSpyFile Find(string filename) {
			return FindByKey(new FilenameKey(filename));
		}

		public DnSpyFile FindByKey(IDnSpyFilenameKey key) {
			lock (lockObj)
				return FindByKey_NoLock(key);
		}

		DnSpyFile FindByKey_NoLock(IDnSpyFilenameKey key) {
			if (key == null)
				return null;
			foreach (var file in files) {
				if (key.Equals(file.Key))
					return file;
			}
			return null;
		}

		public DnSpyFile Add(DnSpyFile file) {
			options.Dispatcher.VerifyAccess();
			lock (lockObj) {
				var key = file.Key;
				var existingFile = FindByKey_NoLock(key);
				if (existingFile != null)
					return existingFile;

				AddToList_NoLock(file, -1);
				return file;
			}
		}

		public DnSpyFile FindAssembly(IAssembly assembly) {
			lock (lockObj) {
				var comparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
				foreach (var file in files) {
					if (comparer.Equals(file.AssemblyDef, assembly))
						return file;
				}
				return null;
			}
		}

		internal DnSpyFile CreateDnSpyFile(ModuleDef module) {
			return DnSpyFile.Create(module, options.UseDebugSymbols, assemblyResolver);
		}

		internal DnSpyFile CreateDnSpyFile(string filename) {
			return CreateDnSpyFile(filename, options.UseMemoryMappedIO, options.UseDebugSymbols);
		}

		DnSpyFile CreateDnSpyFile(string filename, bool useMemoryMappedIO, bool loadSyms) {
			return DnSpyFile.CreateFromFile(filename, options.UseMemoryMappedIO, options.UseDebugSymbols, assemblyResolver);
		}

		public DnSpyFile OpenFile(string file, bool isAutoLoaded = false) {
			return GetOrCreate(file, true, isAutoLoaded, false);
		}

		public DnSpyFile OpenFileDelay(string file, bool isAutoLoaded) {
			return GetOrCreate(file, true, isAutoLoaded, true);
		}

		internal DnSpyFile GetOrCreate(string file, bool canAdd, bool isAutoLoaded, bool delay) {
			var key = new FilenameKey(file);

			lock (lockObj) {
				var existingFile = FindByKey_NoLock(key);
				if (existingFile != null)
					return existingFile;

				var newFile = CreateDnSpyFile(file, options.UseMemoryMappedIO, options.UseDebugSymbols);
				newFile.IsAutoLoaded = isAutoLoaded;
				return ForceAddFileToList_NoLock(newFile, canAdd, delay, -1, true);
			}
		}

		internal DnSpyFile AddFile(DnSpyFile newFile, bool canAdd, bool delay, bool canDispose = true) {
			lock (lockObj) {
				var file = FindByKey_NoLock(newFile.Key);
				if (file != null) {
					if (canDispose && newFile != file)
						newFile.Dispose();
					return file;
				}

				return ForceAddFileToList_NoLock(newFile, canAdd, delay, -1, canDispose);
			}
		}

		internal DnSpyFile ForceAddFileToList(DnSpyFile newFile, bool canAdd, bool delayLoad, int index, bool canDispose) {
			lock (lockObj)
				return ForceAddFileToList_NoLock(newFile, canAdd, delayLoad, index, canDispose);
		}

		internal DnSpyFile ForceAddFileToList_NoLock(DnSpyFile newFile, bool canAdd, bool delayLoad, int index, bool canDispose) {
			if (!canAdd)
				return newFile;

			delayLoad |= !options.Dispatcher.CheckAccess();

			// Sometimes the treeview will completely mess up if we immediately add the file.
			// Wait a little while for the treeview to finish its things before we add it.
			if (delayLoad)
				return DelayLoadFile_NoLock(newFile, index, canDispose);
			AddToList_NoLock(newFile, index);
			return newFile;
		}

		void AddToList_NoLock(DnSpyFile newFile, int index) {
			var key = newFile.Key;
			if ((uint)index < (uint)files.Count)
				files.Insert(index, newFile);
			else
				files.Add(newFile);
		}

		readonly Dictionary<IDnSpyFilenameKey, Tuple<DnSpyFile, int>> delayLoadedFiles = new Dictionary<IDnSpyFilenameKey, Tuple<DnSpyFile, int>>();
		DnSpyFile DelayLoadFile_NoLock(DnSpyFile newFile, int index, bool canDispose) {
			bool startThread;
			lock (delayLoadedFiles) {
				var key = newFile.Key;
				Tuple<DnSpyFile, int> info;
				if (delayLoadedFiles.TryGetValue(key, out info)) {
					if (canDispose && info.Item1 != newFile)
						newFile.Dispose();
					return info.Item1;
				}
				delayLoadedFiles.Add(key, Tuple.Create(newFile, index));
				startThread = delayLoadedFiles.Count == 1;
			}
			if (startThread)
				options.Dispatcher.BeginInvoke(DispatcherPrio.Loaded, () => DelayLoadFileMainThread());
			return newFile;
		}

		void DelayLoadFileMainThread() {
			options.Dispatcher.VerifyAccess();
			List<Tuple<DnSpyFile, int>> newFiles;
			lock (delayLoadedFiles) {
				newFiles = new List<Tuple<DnSpyFile, int>>(delayLoadedFiles.Values);
				delayLoadedFiles.Clear();
			}

			lock (lockObj) {
				foreach (var info in newFiles) {
					var newFile = info.Item1;
					var file = FindByKey(newFile.Key);
					if (file == null)
						AddToList_NoLock(info.Item1, info.Item2);
					else {
						// We can't call newFile.Dispose() since it could still be in use by some
						// decompiler thread.
					}
				}
			}
		}

		public void Remove(DnSpyFile file, bool canDispose) {
			options.Dispatcher.VerifyAccess();
			lock (lockObj) {
				files.Remove(file);
				if (canDispose)
					file.Dispose();
			}
			if (canDispose)
				CallGC();
		}

		void CallGC() {
			if (CallGC_called)
				return;
			CallGC_called = true;
			options.Dispatcher.BeginInvoke(DispatcherPrio.ContextIdle, () => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				CallGC_called = false;
			});
		}
		bool CallGC_called;
	}
}
