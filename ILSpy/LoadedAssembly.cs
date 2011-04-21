// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents an assembly loaded into ILSpy.
	/// </summary>
	public sealed class LoadedAssembly
	{
		readonly Task<AssemblyDefinition> assemblyTask;
		readonly AssemblyList assemblyList;
		readonly string fileName;
		string shortName;
		
		public LoadedAssembly(AssemblyList assemblyList, string fileName)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			this.assemblyList = assemblyList;
			this.fileName = fileName;
			
			this.assemblyTask = Task.Factory.StartNew<AssemblyDefinition>(LoadAssembly); // requires that this.fileName is set
			this.shortName = Path.GetFileNameWithoutExtension(fileName);
		}
		
		public AssemblyDefinition AssemblyDefinition {
			get {
				try {
					return assemblyTask.Result;
				} catch (AggregateException) {
					return null;
				}
			}
		}
		
		public AssemblyList AssemblyList {
			get { return assemblyList; }
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public string ShortName {
			get { return shortName; }
		}
		
		public bool IsLoaded {
			get { return assemblyTask.IsCompleted; }
		}
		
		public bool HasLoadError {
			get { return assemblyTask.IsFaulted; }
		}
		
		AssemblyDefinition LoadAssembly()
		{
			// runs on background thread
			ReaderParameters p = new ReaderParameters();
			p.AssemblyResolver = new MyAssemblyResolver(this);
			try {
				if (DecompilerSettingsPanel.CurrentDecompilerSettings.UseDebugSymbols) {
					SetSymbolSettings(p);
				}
				return AssemblyDefinition.ReadAssembly(fileName, p);
			} finally {
				if (p.SymbolStream != null)
					p.SymbolStream.Dispose();
			}
		}
		
		private void SetSymbolSettings(ReaderParameters p)
		{
			// search for pdb in same directory as dll
			string pdbName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".pdb");
			if (File.Exists(pdbName)) {
				p.ReadSymbols = true;
				p.SymbolStream = File.OpenRead(pdbName);
			}
			
			// TODO: use symbol cache, get symbols from microsoft
		}
		
		[ThreadStatic]
		static int assemblyLoadDisableCount;
		
		public static IDisposable DisableAssemblyLoad()
		{
			assemblyLoadDisableCount++;
			return new DecrementAssemblyLoadDisableCount();
		}
		
		sealed class DecrementAssemblyLoadDisableCount : IDisposable
		{
			bool disposed;
			
			public void Dispose()
			{
				if (!disposed) {
					disposed = true;
					assemblyLoadDisableCount--;
				}
			}
		}
		
		sealed class MyAssemblyResolver : IAssemblyResolver
		{
			readonly LoadedAssembly parent;
			
			public MyAssemblyResolver(LoadedAssembly parent)
			{
				this.parent = parent;
			}
			
			public AssemblyDefinition Resolve(AssemblyNameReference name)
			{
				var node = parent.LookupReferencedAssembly(name.FullName);
				return node != null ? node.AssemblyDefinition : null;
			}
			
			public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
			{
				var node = parent.LookupReferencedAssembly(name.FullName);
				return node != null ? node.AssemblyDefinition : null;
			}
			
			public AssemblyDefinition Resolve(string fullName)
			{
				var node = parent.LookupReferencedAssembly(fullName);
				return node != null ? node.AssemblyDefinition : null;
			}
			
			public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
			{
				var node = parent.LookupReferencedAssembly(fullName);
				return node != null ? node.AssemblyDefinition : null;
			}
		}
		
		public LoadedAssembly LookupReferencedAssembly(string fullName)
		{
			foreach (LoadedAssembly asm in assemblyList.GetAssemblies()) {
				if (asm.AssemblyDefinition != null && fullName.Equals(asm.AssemblyDefinition.FullName, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			if (assemblyLoadDisableCount > 0)
				return null;
			
			if (!App.Current.Dispatcher.CheckAccess()) {
				// Call this method on the GUI thread.
				return (LoadedAssembly)App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Func<string, LoadedAssembly>(LookupReferencedAssembly), fullName);
			}
			
			var name = AssemblyNameReference.Parse(fullName);
			string file = GacInterop.FindAssemblyInNetGac(name);
			if (file == null) {
				string dir = Path.GetDirectoryName(this.fileName);
				if (File.Exists(Path.Combine(dir, name.Name + ".dll")))
					file = Path.Combine(dir, name.Name + ".dll");
				else if (File.Exists(Path.Combine(dir, name.Name + ".exe")))
					file = Path.Combine(dir, name.Name + ".exe");
			}
			if (file != null) {
				return assemblyList.OpenAssembly(file);
			} else {
				return null;
			}
		}
		
		public Task ContinueWhenLoaded(Action<Task<AssemblyDefinition>> onAssemblyLoaded, TaskScheduler taskScheduler)
		{
			return this.assemblyTask.ContinueWith(onAssemblyLoaded, taskScheduler);
		}
		
		public void WaitUntilLoaded()
		{
			assemblyTask.Wait();
		}
	}
}
