// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Simple compilation implementation.
	/// </summary>
	public class SimpleCompilation : ICompilation
	{
		readonly ISolutionSnapshot solutionSnapshot;
		readonly ITypeResolveContext context;
		readonly CacheManager cacheManager = new CacheManager();
		readonly KnownTypeCache knownTypeCache;
		readonly IAssembly mainAssembly;
		readonly IList<IAssembly> assemblies;
		readonly IList<IAssembly> referencedAssemblies;
		INamespace rootNamespace;
		
		public SimpleCompilation(IUnresolvedAssembly mainAssembly, params IAssemblyReference[] assemblyReferences)
			: this(new DefaultSolutionSnapshot(), mainAssembly, (IEnumerable<IAssemblyReference>)assemblyReferences)
		{
		}
		
		public SimpleCompilation(IUnresolvedAssembly mainAssembly, IEnumerable<IAssemblyReference> assemblyReferences)
			: this(new DefaultSolutionSnapshot(), mainAssembly, assemblyReferences)
		{
		}
		
		public SimpleCompilation(ISolutionSnapshot solutionSnapshot, IUnresolvedAssembly mainAssembly, params IAssemblyReference[] assemblyReferences)
			: this(solutionSnapshot, mainAssembly, (IEnumerable<IAssemblyReference>)assemblyReferences)
		{
		}
		
		public SimpleCompilation(ISolutionSnapshot solutionSnapshot, IUnresolvedAssembly mainAssembly, IEnumerable<IAssemblyReference> assemblyReferences)
		{
			if (solutionSnapshot == null)
				throw new ArgumentNullException("solutionSnapshot");
			if (mainAssembly == null)
				throw new ArgumentNullException("mainAssembly");
			if (assemblyReferences == null)
				throw new ArgumentNullException("assemblyReferences");
			this.solutionSnapshot = solutionSnapshot;
			this.context = new SimpleTypeResolveContext(this);
			this.mainAssembly = mainAssembly.Resolve(context);
			List<IAssembly> assemblies = new List<IAssembly>();
			assemblies.Add(this.mainAssembly);
			List<IAssembly> referencedAssemblies = new List<IAssembly>();
			foreach (var asmRef in assemblyReferences) {
				IAssembly asm;
				try {
					asm = asmRef.Resolve(context);
				} catch (InvalidOperationException) {
					throw new InvalidOperationException("Tried to initialize compilation with an invalid assembly reference. (Forgot to load the assembly reference ? - see CecilLoader)");
				}
				if (asm != null && !assemblies.Contains(asm))
					assemblies.Add(asm);
				if (asm != null && !referencedAssemblies.Contains(asm))
					referencedAssemblies.Add(asm);
			}
			this.assemblies = assemblies.AsReadOnly();
			this.referencedAssemblies = referencedAssemblies.AsReadOnly();
			this.knownTypeCache = new KnownTypeCache(this);
		}
		
		public IAssembly MainAssembly {
			get {
				if (mainAssembly == null)
					throw new InvalidOperationException("Compilation isn't initialized yet");
				return mainAssembly;
			}
		}
		
		public IList<IAssembly> Assemblies {
			get {
				if (assemblies == null)
					throw new InvalidOperationException("Compilation isn't initialized yet");
				return assemblies;
			}
		}
		
		public IList<IAssembly> ReferencedAssemblies {
			get {
				if (referencedAssemblies == null)
					throw new InvalidOperationException("Compilation isn't initialized yet");
				return referencedAssemblies;
			}
		}
		
		public ITypeResolveContext TypeResolveContext {
			get { return context; }
		}
		
		public INamespace RootNamespace {
			get {
				INamespace ns = LazyInit.VolatileRead(ref this.rootNamespace);
				if (ns != null) {
					return ns;
				} else {
					if (referencedAssemblies == null)
						throw new InvalidOperationException("Compilation isn't initialized yet");
					return LazyInit.GetOrSet(ref this.rootNamespace, CreateRootNamespace());
				}
			}
		}
		
		protected virtual INamespace CreateRootNamespace()
		{
			// SimpleCompilation does not support extern aliases; but derived classes might.
			// CreateRootNamespace() is virtual so that derived classes can change the global namespace.
			INamespace[] namespaces = new INamespace[referencedAssemblies.Count + 1];
			namespaces[0] = mainAssembly.RootNamespace;
			for (int i = 0; i < referencedAssemblies.Count; i++) {
				namespaces[i + 1] = referencedAssemblies[i].RootNamespace;
			}
			return new MergedNamespace(this, namespaces);
		}
		
		public CacheManager CacheManager {
			get { return cacheManager; }
		}
		
		public virtual INamespace GetNamespaceForExternAlias(string alias)
		{
			if (string.IsNullOrEmpty(alias))
				return this.RootNamespace;
			// SimpleCompilation does not support extern aliases; but derived classes might.
			return null;
		}
		
		public IType FindType(KnownTypeCode typeCode)
		{
			return knownTypeCache.FindType(typeCode);
		}
		
		public StringComparer NameComparer {
			get { return StringComparer.Ordinal; }
		}
		
		public ISolutionSnapshot SolutionSnapshot {
			get { return solutionSnapshot; }
		}
		
		public override string ToString()
		{
			return "[SimpleCompilation " + mainAssembly.AssemblyName + "]";
		}
	}
}
