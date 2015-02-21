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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.ILSpy.Options;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents an assembly loaded into ILSpy.
	/// </summary>
	public sealed class LoadedAssembly
	{
		readonly Task<ModuleDef> assemblyTask;
		readonly AssemblyList assemblyList;
		readonly string fileName;
		readonly string shortName;

		static string[] gacPaths;
		static string[] GacPaths {
			get {
				return gacPaths ?? (gacPaths = new string[] {
					Fusion.GetGacPath(false),
					Fusion.GetGacPath(true)
				}); 
			}
		}

		/// <summary>
		/// true if this assembly is located in the GAC
		/// </summary>
		public bool IsGAC {
			get {
				foreach (var p in GacPaths) {
					if (IsSubPath(p, fileName))
						return true;
				}
				return false;
			}
		}

		static bool IsSubPath(string path, string fileName)
		{
			fileName = Path.GetFullPath(Path.GetDirectoryName(fileName));
			var root = Path.GetPathRoot(fileName);
			while (fileName != root) {
				if (path == fileName)
					return true;
				fileName = Path.GetDirectoryName(fileName);
			}
			return false;
		}

		/// <summary>
		/// Constructor to create modules present in multi-module assemblies
		/// </summary>
		/// <param name="assemblyList"></param>
		/// <param name="module"></param>
		public LoadedAssembly(AssemblyList assemblyList, ModuleDef module)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			if (module == null)
				throw new ArgumentNullException("module");
			if (string.IsNullOrEmpty(module.Location))
				throw new ArgumentException("module has no filename");
			this.assemblyList = assemblyList;
			this.fileName = module.Location;
			Debug.Assert(module.Assembly != null && module.Assembly.ManifestModule != module, "Use other constructor for net modules or if it's the main module in an assembly");

			this.assemblyTask = Task.Factory.StartNew<ModuleDef>(() => LoadModule(module));
			this.shortName = Path.GetFileNameWithoutExtension(fileName);
		}
		
		public LoadedAssembly(AssemblyList assemblyList, string fileName, Stream stream = null)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			this.assemblyList = assemblyList;
			this.fileName = fileName;
			
			this.assemblyTask = Task.Factory.StartNew<ModuleDef>(LoadAssembly, stream); // requires that this.fileName is set
			this.shortName = Path.GetFileNameWithoutExtension(fileName);
		}
		
		/// <summary>
		/// Gets the Cecil ModuleDefinition.
		/// Can be null when there was a load error.
		/// </summary>
		public ModuleDef ModuleDefinition {
			get {
				try {
					return assemblyTask.Result;
				} catch (AggregateException) {
					return null;
				}
			}
		}
		
		/// <summary>
		/// Gets the Cecil AssemblyDefinition.
		/// Is null when there was a load error; or when opening a netmodule.
		/// </summary>
		public AssemblyDef AssemblyDefinition {
			get {
				var module = this.ModuleDefinition;
				return module != null ? module.Assembly : null;
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

		public string Text {
			get {
				if (AssemblyDefinition != null) {
					return String.Format("{0} ({1})", ShortName, AssemblyDefinition.Name.Version);
				} else {
					return ShortName;
				}
			}
		}
		
		public bool IsLoaded {
			get { return assemblyTask.IsCompleted; }
		}
		
		public bool HasLoadError {
			get { return assemblyTask.IsFaulted; }
		}

		ModuleDef LoadModule(ModuleDef module)
		{
			// runs on background thread
			module.Context = CreateModuleContext();
			return InitializeModule(module);
		}
		
		ModuleDef LoadAssembly(object state)
		{
			var stream = state as Stream;
			ModuleDefinition module;

			// runs on background thread
			ModuleDef module;
			if (stream != null)
			{
				// Read the module from a precrafted stream
				module = ModuleDefMD.Load(stream, CreateModuleContext());
			}
			else
			{
				// Read the module from disk (by default)
				module = ModuleDefMD.Load(fileName, CreateModuleContext());
			}

			return InitializeModule(module);
		}

		ModuleContext CreateModuleContext()
		{
			ModuleContext moduleCtx = new ModuleContext();
			moduleCtx.AssemblyResolver = new MyAssemblyResolver(this);
			moduleCtx.Resolver = new Resolver(moduleCtx.AssemblyResolver);
			return moduleCtx;
		}

		ModuleDef InitializeModule(ModuleDef module)
		{
			module.EnableTypeDefFindCache = true;
			if (DecompilerSettingsPanel.CurrentDecompilerSettings.UseDebugSymbols) {
				try {
					LoadSymbols(module as ModuleDefMD);
				} catch (IOException) {
				} catch (UnauthorizedAccessException) {
				} catch (InvalidOperationException) {
					// ignore any errors during symbol loading
				}
			}
			return module;
		}
		
		private void LoadSymbols(ModuleDefMD module)
		{
			if (module == null)
				return;

			// search for pdb in same directory as dll
			string pdbName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".pdb");
			if (File.Exists(pdbName)) {
				module.LoadPdb(pdbName);
				return;
			}
			
			// TODO: use symbol cache, get symbols from microsoft
		}
		
		static int assemblyLoadDisableCount;
		
		public static IDisposable DisableAssemblyLoad()
		{
			Interlocked.Increment(ref assemblyLoadDisableCount);
			return new DecrementAssemblyLoadDisableCount();
		}
		
		sealed class DecrementAssemblyLoadDisableCount : IDisposable
		{
			bool disposed;
			
			public void Dispose()
			{
				if (!disposed) {
					disposed = true;
					Interlocked.Decrement(ref assemblyLoadDisableCount);
					// clear the lookup cache since we might have stored the lookups failed due to DisableAssemblyLoad()
					if (MainWindow.Instance != null)
						MainWindow.Instance.CurrentAssemblyList.ClearCache();
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

			public bool AddToCache(AssemblyDef asm)
			{
				return false;
			}

			public bool Remove(AssemblyDef asm)
			{
				return false;
			}

			public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule)
			{
				var node = parent.LookupReferencedAssembly(assembly, sourceModule);
				return node != null ? node.AssemblyDefinition : null;
			}

			public void Clear() {
			}
		}
		
		public IAssemblyResolver GetAssemblyResolver()
		{
			return new MyAssemblyResolver(this);
		}
		
		public LoadedAssembly LookupReferencedAssembly(IAssembly name)
		{
			return LookupReferencedAssembly(name, null);
		}

		public LoadedAssembly LookupReferencedAssembly(IAssembly name, ModuleDef sourceModule)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (name.IsContentTypeWindowsRuntime) {
				return assemblyList.winRTMetadataLookupCache.GetOrAdd(name.Name, LookupWinRTMetadata);
			} else {
				return LookupReferencedAssembly(name.FullName, sourceModule);
			}
		}
		
		public LoadedAssembly LookupReferencedAssembly(string fullName, ModuleDef sourceModule)
		{
			var asm = assemblyList.assemblyLookupCache.GetOrAdd(fullName, n => LookupReferencedAssemblyInternal(n, sourceModule));
			if (asm != null && asm.AssemblyDefinition != null && !asm.AssemblyDefinition.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
				assemblyList.assemblyLookupCache.TryAdd(asm.AssemblyDefinition.FullName, asm);
			return asm;
		}
		
		LoadedAssembly LookupReferencedAssemblyInternal(string fullName, ModuleDef sourceModule)
		{
			foreach (LoadedAssembly asm in assemblyList.GetAssemblies()) {
				if (asm.AssemblyDefinition != null && fullName.Equals(asm.AssemblyDefinition.FullName, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			
			if (App.Current != null && !App.Current.Dispatcher.CheckAccess()) {
				// Call this method on the GUI thread.
				return (LoadedAssembly)App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Func<string, LoadedAssembly>(n => LookupReferencedAssembly(n, sourceModule)), fullName);
			}
			
			var name = new AssemblyNameInfo(fullName);
			var loadedAsm = LookupFromSearchPaths(name, sourceModule, true);
			if (loadedAsm != null)
				return assemblyList.AddAssembly(loadedAsm, assemblyLoadDisableCount == 0);

			if (assemblyList.UseGAC) {
				var file = GacInterop.FindAssemblyInNetGac(name);
				if (file != null)
					return assemblyList.OpenAssembly(file, assemblyLoadDisableCount == 0);
			}

			loadedAsm = LookupFromSearchPaths(name, sourceModule, false);
			if (loadedAsm != null)
				return assemblyList.AddAssembly(loadedAsm, assemblyLoadDisableCount == 0);

			return null;
		}

		LoadedAssembly LookupFromSearchPaths(IAssembly asmName, ModuleDef sourceModule, bool exactCheck) {
			LoadedAssembly asm;
			if (sourceModule != null && !string.IsNullOrEmpty(sourceModule.Location)) {
				asm = TryLoadFromDir(asmName, exactCheck, Path.GetDirectoryName(sourceModule.Location));
				if (asm != null)
					return asm;
			}
			lock (assemblyList.assemblySearchPaths) {
				foreach (var path in assemblyList.assemblySearchPaths) {
					asm = TryLoadFromDir(asmName, exactCheck, path);
					if (asm != null)
						return asm;
				}
			}
			return TryLoadFromDir(asmName, exactCheck, Path.GetDirectoryName(this.fileName));
		}

		LoadedAssembly TryLoadFromDir(IAssembly asmName, bool exactCheck, string dirPath) {
			string baseName;
			try {
				baseName = Path.Combine(dirPath, asmName.Name);
			} catch (ArgumentException) { // eg. invalid chars in asmName.Name
				return null;
			}
			return TryLoadFromDir2(asmName, exactCheck, baseName + ".dll") ??
				   TryLoadFromDir2(asmName, exactCheck, baseName + ".exe");
		}

		LoadedAssembly TryLoadFromDir2(IAssembly asmName, bool exactCheck, string fileName) {
			if (!File.Exists(fileName))
				return null;

			var loadedAsm = new LoadedAssembly(assemblyList, fileName);
			ModuleDef mod = null;
			bool error = true;
			try {
				mod = loadedAsm.ModuleDefinition;
				if (mod == null)
					return null;
				var asm = mod.Assembly;
				if (asm == null)
					return null;
				bool b = exactCheck ?
					AssemblyNameComparer.CompareAll.Equals(asmName, asm) :
					AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(asmName, asm);
				if (!b)
					return null;

				error = false;
				return loadedAsm;
			}
			finally {
				if (error) {
					if (mod != null)
						mod.Dispose();
				}
			}
		}
		
		LoadedAssembly LookupWinRTMetadata(string name)
		{
			foreach (LoadedAssembly asm in assemblyList.GetAssemblies()) {
				if (asm.AssemblyDefinition != null && name.Equals(asm.AssemblyDefinition.Name, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			if (App.Current != null && !App.Current.Dispatcher.CheckAccess()) {
				// Call this method on the GUI thread.
				return (LoadedAssembly)App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Func<string, LoadedAssembly>(LookupWinRTMetadata), name);
			}
			
			string file;
			try {
				file = Path.Combine(Environment.SystemDirectory, "WinMetadata", name + ".winmd");
			} catch (ArgumentException) {
				return null;
			}
			if (File.Exists(file)) {
				return assemblyList.OpenAssembly(file, assemblyLoadDisableCount == 0);
			} else {
				return null;
			}
		}
		
		public Task ContinueWhenLoaded(Action<Task<ModuleDef>> onAssemblyLoaded, TaskScheduler taskScheduler)
		{
			return this.assemblyTask.ContinueWith(onAssemblyLoaded, taskScheduler);
		}
		
		/// <summary>
		/// Wait until the assembly is loaded.
		/// Throws an AggregateException when loading the assembly fails.
		/// </summary>
		public void WaitUntilLoaded()
		{
			assemblyTask.Wait();
		}
	}
}
