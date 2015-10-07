// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Linq;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// A list of assemblies.
	/// </summary>
	public sealed class AssemblyList
	{
		readonly string listName;
		readonly bool canSave;

		public bool CanSave {
			get { return canSave; }
		}
		
		/// <summary>Dirty flag, used to mark modifications so that the list is saved later</summary>
		bool dirty;
		
		internal readonly ConcurrentDictionary<string, LoadedAssembly> assemblyLookupCache = new ConcurrentDictionary<string, LoadedAssembly>(StringComparer.OrdinalIgnoreCase);
		internal readonly ConcurrentDictionary<string, LoadedAssembly> winRTMetadataLookupCache = new ConcurrentDictionary<string, LoadedAssembly>(StringComparer.OrdinalIgnoreCase);
		internal List<string> assemblySearchPaths = new List<string>();

		/// <summary>
		/// true if we're rearranging the list, eg. moving assemblies or sorting the list
		/// </summary>
		public bool IsReArranging { get; internal set; }

		public bool UseGAC {
			get { return useGAC; }
			set { useGAC = value; }
		}
		bool useGAC = true;
		
		/// <summary>
		/// The assemblies in this list.
		/// Needs locking for multi-threaded access!
		/// Write accesses are allowed on the GUI thread only (but still need locking!)
		/// </summary>
		/// <remarks>
		/// Technically read accesses need locking when done on non-GUI threads... but whenever possible, use the
		/// thread-safe <see cref="GetAssemblies()"/> method.
		/// </remarks>
		readonly ObservableCollection<LoadedAssembly> assemblies = new ObservableCollection<LoadedAssembly>();

		internal int Count_NoLock {
			get { return assemblies.Count; }
		}

		internal int IndexOf_NoLock(LoadedAssembly asm)
		{
			return assemblies.IndexOf(asm);
		}

		internal void RemoveAt_NoLock(int index)
		{
			assemblies.RemoveAt(index);
		}

		internal void Insert_NoLock(int index, LoadedAssembly asm)
		{
			assemblies.Insert(index, asm);
		}

		internal object GetLockObj()
		{
			return assemblies;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged {
			add {
				assemblies.CollectionChanged += value;
			}
			remove {
				assemblies.CollectionChanged -= value;
			}
		}
		
		public AssemblyList(string listName, bool canSave = true)
		{
			this.listName = listName;
			this.canSave = canSave;
			assemblies.CollectionChanged += Assemblies_CollectionChanged;
		}
		
		/// <summary>
		/// Loads an assembly list from XML.
		/// </summary>
		public AssemblyList(XElement listElement)
			: this(SessionSettings.Unescape((string)listElement.Attribute("name")))
		{
			foreach (var asm in listElement.Elements("Assembly")) {
				try {
					OpenAssembly(SessionSettings.Unescape((string)asm));
				}
				catch (ArgumentException) { // invalid filename
				}
			}
			this.dirty = false; // OpenAssembly() sets dirty, so reset it afterwards
		}
		
		/// <summary>
		/// Adds an assembly search path
		/// </summary>
		/// <param name="path"></param>
		public void AddSearchPath(string path) {
			if (!string.IsNullOrWhiteSpace(path)) {
				lock (assemblySearchPaths)
					assemblySearchPaths.Add(path);
			}
		}

		/// <summary>
		/// Gets the loaded assemblies. This method is thread-safe.
		/// </summary>
		public LoadedAssembly[] GetAssemblies()
		{
			lock (assemblies) {
				return assemblies.ToArray();
			}
		}
		
		/// <summary>
		/// Gets the loaded modules and includes all modules in multifile assemblies. This method is thread-safe.
		/// </summary>
		public ModuleDef[] GetAllModules()
		{
			lock (assemblies) {
				return assemblies.SelectMany(a => {
					var asm = a.AssemblyDefinition;
					if (asm != null)
						return (IEnumerable<ModuleDef>)asm.Modules; // cast to make compiler happy
					var mod = a.ModuleDefinition;
					if (mod != null)
						return new ModuleDef[] { mod };
					return new ModuleDef[0];
				}).ToArray();
			}
		}

		/// <summary>
		/// Saves this assembly list to XML.
		/// </summary>
		internal XElement SaveAsXml()
		{
			lock (assemblies) {
				return new XElement(
					"List",
					new XAttribute("name", SessionSettings.Escape(this.ListName)),
					assemblies.Where(asm => !asm.IsAutoLoaded && !string.IsNullOrWhiteSpace(asm.FileName)).Select(asm => new XElement("Assembly", SessionSettings.Escape(asm.FileName)))
				);
			}
		}
		
		/// <summary>
		/// Gets the name of this list.
		/// </summary>
		public string ListName {
			get { return listName; }
		}
		
		void Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			bool callGc = ClearCache();
			// Whenever the assembly list is modified, mark it as dirty
			// and enqueue a task that saves it once the UI has finished modifying the assembly list.
			if (!dirty) {
				dirty = true;
				if (App.Current == null) {
					dirty = false;
					AssemblyListManager.SaveList(this);
					callGc |= ClearCache();
					if (callGc)
						CallGc();
				}
				else {
					App.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Background,
					new Action(
						delegate {
							dirty = false;
							AssemblyListManager.SaveList(this);
							ClearCache();
							callGc |= ClearCache();
							if (callGc)
								CallGc();
						})
					);
				}
			}
			else if (callGc)
				CallGc();
		}

		void CallGc()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		internal void RefreshSave()
		{
			if (!dirty) {
				dirty = true;
				App.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Background,
					new Action(
						delegate {
							dirty = false;
							AssemblyListManager.SaveList(this);
						})
				);
			}
		}
		
		internal bool ClearCache()
		{
			bool callGc = assemblyLookupCache.Count > 0 || winRTMetadataLookupCache.Count > 0;
			assemblyLookupCache.Clear();
			winRTMetadataLookupCache.Clear();
			return callGc;
		}

		internal LoadedAssembly FindAssemblyByAssemblyName(string asmName)
		{
			lock (assemblies)
				return FindAssemblyByAssemblyName_NoLock(asmName);
		}

		LoadedAssembly FindAssemblyByAssemblyName_NoLock(string asmName)
		{
			foreach (LoadedAssembly asm in this.assemblies) {
				if (asm.AssemblyDefinition != null && asm.AssemblyDefinition.FullName.Equals(asmName, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			return null;
		}

		internal LoadedAssembly FindAssemblyByAssemblySimplName(string asmSimpleName)
		{
			lock (assemblies)
				return FindAssemblyByAssemblySimplName_NoLock(asmSimpleName);
		}

		LoadedAssembly FindAssemblyByAssemblySimplName_NoLock(string asmSimpleName)
		{
			foreach (LoadedAssembly asm in this.assemblies) {
				if (asm.AssemblyDefinition != null && asmSimpleName.Equals(asm.AssemblyDefinition.Name, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			return null;
		}

		internal LoadedAssembly FindAssemblyByFileName(string fileName)
		{
			lock (assemblies)
				return FindAssemblyByFileName_NoLock(fileName);
		}

		LoadedAssembly FindAssemblyByFileName_NoLock(string fileName)
		{
			foreach (LoadedAssembly asm in this.assemblies) {
				if (string.IsNullOrWhiteSpace(asm.FileName))
					continue;
				if (asm.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			return null;
		}
		
		/// <summary>
		/// Opens an assembly from disk.
		/// Returns the existing assembly node if it is already loaded.
		/// </summary>
		public LoadedAssembly OpenAssembly(string file, bool isAutoLoaded=false)
		{
			return OpenAssemblyInternal(file, true, isAutoLoaded, false);
		}

		public LoadedAssembly OpenAssemblyDelay(string file, bool isAutoLoaded)
		{
			return OpenAssemblyInternal(file, true, isAutoLoaded, true);
		}

		internal LoadedAssembly OpenAssemblyInternal(string file, bool canAdd, bool isAutoLoaded, bool delay)
		{
			file = Path.GetFullPath(file);

			lock (assemblies) {
				var asm = FindAssemblyByFileName_NoLock(file);
				if (asm != null)
					return asm;

				var newAsm = new LoadedAssembly(this, file);
				newAsm.IsAutoLoaded = isAutoLoaded;
				return ForceAddAssemblyToList(newAsm, canAdd, delay, -1, true);
			}
		}

		internal LoadedAssembly AddAssembly(LoadedAssembly newAsm, bool canAdd, bool delay, bool canDispose = true)
		{
			lock (assemblies) {
				var asm = FindAssemblyByFileName_NoLock(newAsm.FileName);
				if (asm != null) {
					if (canDispose)
						newAsm.TheLoadedFile.Dispose();
					return asm;
				}

				return ForceAddAssemblyToList(newAsm, canAdd, delay, -1, canDispose);
			}
		}

		internal LoadedAssembly ForceAddAssemblyToList(LoadedAssembly newAsm, bool canAdd, bool delay, int index, bool canDispose)
		{
			if (!canAdd)
				return newAsm;

			// Happens when we start dnSpy (try with many tabs) and try to exit while it's still
			// decompiling and the decompiler resolves a reference.
			if (App.Current == null && MainWindow.Instance != null)
				return newAsm;

			if (App.Current == null)
				delay = false;
			else if (!App.Current.CheckAccess())
				delay = true;

			// Sometimes the treeview will completely mess up if we immediately add the asm.
			// Wait a little while for the treeview to finish its things before we add it.
			if (delay)
				return DelayLoadAssembly(newAsm, index, canDispose);
			AddToList(newAsm, index);
			return newAsm;
		}

		void AddToList(LoadedAssembly newAsm, int index)
		{
			if (index >= 0 && index < assemblies.Count)
				assemblies.Insert(index, newAsm);
			else
				assemblies.Add(newAsm);
		}

		Dictionary<string, Tuple<LoadedAssembly, int>> delayLoadedAsms = new Dictionary<string, Tuple<LoadedAssembly, int>>(StringComparer.OrdinalIgnoreCase);
		LoadedAssembly DelayLoadAssembly(LoadedAssembly newAsm, int index, bool canDispose)
		{
			System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(newAsm.FileName));
			bool startThread;
			lock (delayLoadedAsms) {
				Tuple<LoadedAssembly, int> info;
				if (delayLoadedAsms.TryGetValue(newAsm.FileName, out info)) {
					if (canDispose)
						newAsm.TheLoadedFile.Dispose();
					return info.Item1;
				}
				delayLoadedAsms.Add(newAsm.FileName, Tuple.Create(newAsm, index));
				startThread = delayLoadedAsms.Count == 1;
			}
			if (startThread)
				App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => DelayLoadAssemblyMainThread()));
			return newAsm;
		}

		void DelayLoadAssemblyMainThread()
		{
			App.Current.Dispatcher.VerifyAccess();
			List<Tuple<LoadedAssembly, int>> newAsms;
			lock (delayLoadedAsms) {
				newAsms = new List<Tuple<LoadedAssembly, int>>(delayLoadedAsms.Values);
				delayLoadedAsms.Clear();
			}

			lock (assemblies) {
				foreach (var info in newAsms) {
					var newAsm = info.Item1;
					var asm = FindAssemblyByFileName_NoLock(newAsm.FileName);
					if (asm == null)
						AddToList(info.Item1, info.Item2);
					else {
						// We can't call newAsm.ModuleDefinition.Dispose() since it could still
						// be in use by some decompiler thread.
					}
				}
			}
		}
		
		public void Unload(LoadedAssembly assembly, bool canDispose)
		{
			if (App.Current != null)
				App.Current.Dispatcher.VerifyAccess();
			lock (assemblies) {
				assemblies.Remove(assembly);
				if (canDispose)
					assembly.Dispose();
			}
			if (canDispose)
				RequestGC();
		}
		
		static bool gcRequested;
		
		void RequestGC()
		{
			if (gcRequested) return;
			gcRequested = true;
			if (App.Current == null) {
				gcRequested = false;
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
			else {
				App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(
				delegate {
					gcRequested = false;
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}));
			}
		}
		
		public void Sort(IComparer<LoadedAssembly> comparer)
		{
			Sort(0, int.MaxValue, comparer);
		}
		
		public void Sort(int index, int count, IComparer<LoadedAssembly> comparer)
		{
			if (App.Current != null)
				App.Current.Dispatcher.VerifyAccess();
			lock (assemblies) {
				List<LoadedAssembly> list = new List<LoadedAssembly>(assemblies);
				list.Sort(index, Math.Min(count, list.Count - index), comparer);
				assemblies.Clear();
				assemblies.AddRange(list);
			}
		}
	}
}
