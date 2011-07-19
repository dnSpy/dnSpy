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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Linq;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;

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
		
		internal readonly ConcurrentDictionary<string, LoadedAssembly> assemblyLookupCache = new ConcurrentDictionary<string, LoadedAssembly>();
		
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
				OpenAssembly((string)asm);
			}
			this.dirty = false; // OpenAssembly() sets dirty, so reset it afterwards
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
		/// Saves this assembly list to XML.
		/// </summary>
		internal XElement SaveAsXml()
		{
			return new XElement(
				"List",
				new XAttribute("name", this.ListName),
				assemblies.Select(asm => new XElement("Assembly", asm.FileName))
			);
		}
		
		/// <summary>
		/// Gets the name of this list.
		/// </summary>
		public string ListName {
			get { return listName; }
		}
		
		void Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			assemblyLookupCache.Clear();
			// Whenever the assembly list is modified, mark it as dirty
			// and enqueue a task that saves it once the UI has finished modifying the assembly list.
			if (!dirty) {
				dirty = true;
				App.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Background,
					new Action(
						delegate {
							dirty = false;
							AssemblyListManager.SaveList(this);
							assemblyLookupCache.Clear();
						})
				);
			}
		}
		
		/// <summary>
		/// Opens an assembly from disk.
		/// Returns the existing assembly node if it is already loaded.
		/// </summary>
		public LoadedAssembly OpenAssembly(string file)
		{
			App.Current.Dispatcher.VerifyAccess();
			
			file = Path.GetFullPath(file);
			
			foreach (LoadedAssembly asm in this.assemblies) {
				if (file.Equals(asm.FileName, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			
			var newAsm = new LoadedAssembly(this, file);
			lock (assemblies) {
				this.assemblies.Add(newAsm);
			}
			return newAsm;
		}
		
		public void Unload(LoadedAssembly assembly)
		{
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
			App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(
				delegate {
					gcRequested = false;
					GC.Collect();
				}));
		}
		
		public void Sort(IComparer<LoadedAssembly> comparer)
		{
			Sort(0, int.MaxValue, comparer);
		}
		
		public void Sort(int index, int count, IComparer<LoadedAssembly> comparer)
		{
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
