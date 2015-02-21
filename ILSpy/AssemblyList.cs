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
		
		/// <summary>Dirty flag, used to mark modifications so that the list is saved later</summary>
		bool dirty;
		
		internal readonly ConcurrentDictionary<string, LoadedAssembly> assemblyLookupCache = new ConcurrentDictionary<string, LoadedAssembly>(StringComparer.OrdinalIgnoreCase);
		internal readonly ConcurrentDictionary<string, LoadedAssembly> winRTMetadataLookupCache = new ConcurrentDictionary<string, LoadedAssembly>(StringComparer.OrdinalIgnoreCase);
		internal List<string> assemblySearchPaths = new List<string>();

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
		internal readonly ObservableCollection<LoadedAssembly> assemblies = new ObservableCollection<LoadedAssembly>();
		
		public AssemblyList(string listName)
		{
			this.listName = listName;
			assemblies.CollectionChanged += Assemblies_CollectionChanged;
		}
		
		/// <summary>
		/// Loads an assembly list from XML.
		/// </summary>
		public AssemblyList(XElement listElement)
			: this((string)listElement.Attribute("name"))
		{
			foreach (var asm in listElement.Elements("Assembly")) {
				try {
					OpenAssembly((string)asm);
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
					new XAttribute("name", this.ListName),
					assemblies.Select(asm => new XElement("Assembly", asm.FileName))
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
			ClearCache();
			// Whenever the assembly list is modified, mark it as dirty
			// and enqueue a task that saves it once the UI has finished modifying the assembly list.
			if (!dirty) {
				dirty = true;
				if (App.Current == null) {
					dirty = false;
					AssemblyListManager.SaveList(this);
					ClearCache();
				}
				else {
					App.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Background,
					new Action(
						delegate {
							dirty = false;
							AssemblyListManager.SaveList(this);
							ClearCache();
						})
					);
				}
			}
		}
		
		internal void ClearCache()
		{
			assemblyLookupCache.Clear();
			winRTMetadataLookupCache.Clear();
		}
		
		/// <summary>
		/// Opens an assembly from disk.
		/// Returns the existing assembly node if it is already loaded.
		/// </summary>
		public LoadedAssembly OpenAssembly(string file)
		{
			return OpenAssembly(file, true);
		}

		internal LoadedAssembly OpenAssembly(string file, bool canAdd) {
			if (App.Current != null)
				App.Current.Dispatcher.VerifyAccess();

			file = Path.GetFullPath(file);

			lock (assemblies) {
				foreach (LoadedAssembly asm in this.assemblies) {
					if (file.Equals(asm.FileName, StringComparison.OrdinalIgnoreCase))
						return asm;
				}

				var newAsm = new LoadedAssembly(this, file);
				if (canAdd)
					this.assemblies.Add(newAsm);
				return newAsm;
			}
		}

		internal LoadedAssembly AddAssembly(LoadedAssembly newAsm, bool canAdd)
		{
			if (App.Current != null)
				App.Current.Dispatcher.VerifyAccess();

			lock (assemblies) {
				foreach (LoadedAssembly asm in this.assemblies) {
					if (newAsm.FileName.Equals(asm.FileName, StringComparison.OrdinalIgnoreCase)) {
						var mod = newAsm.ModuleDefinition;
						if (mod != null)
							mod.Dispose();
						return asm;
					}
				}

				if (canAdd)
					this.assemblies.Add(newAsm);
				return newAsm;
			}
		}

		/// <summary>
		/// Replace the assembly object model from a crafted stream, without disk I/O
		/// Returns null if it is not already loaded.
		/// </summary>
		public LoadedAssembly HotReplaceAssembly(string file, Stream stream)
		{
			App.Current.Dispatcher.VerifyAccess();
			file = Path.GetFullPath(file);

			lock (this.assemblies) {
				var target = this.assemblies.FirstOrDefault(asm => file.Equals(asm.FileName, StringComparison.OrdinalIgnoreCase));
				if (target == null)
					return null;

				var index = this.assemblies.IndexOf(target);
				var newAsm = new LoadedAssembly(this, file, stream);
				this.assemblies.Remove(target);
				this.assemblies.Insert(index, newAsm);
				return newAsm;
			}
		}
		
		public void Unload(LoadedAssembly assembly)
		{
			if (App.Current != null)
				App.Current.Dispatcher.VerifyAccess();
			lock (assemblies) {
				assemblies.Remove(assembly);
			}
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
			}
			else {
				App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(
				delegate {
					gcRequested = false;
					GC.Collect();
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
