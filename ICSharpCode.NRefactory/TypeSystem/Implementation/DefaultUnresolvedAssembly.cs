// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation for <see cref="IUnresolvedAssembly"/>.
	/// </summary>
	[Serializable]
	public class DefaultUnresolvedAssembly : AbstractFreezable, IUnresolvedAssembly
	{
		string assemblyName;
		IList<IUnresolvedAttribute> assemblyAttributes;
		IList<IUnresolvedAttribute> moduleAttributes;
		Dictionary<FullNameAndTypeParameterCount, IUnresolvedTypeDefinition> typeDefinitions = new Dictionary<FullNameAndTypeParameterCount, IUnresolvedTypeDefinition>(FullNameAndTypeParameterCountComparer.Ordinal);
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			assemblyAttributes = FreezableHelper.FreezeListAndElements(assemblyAttributes);
			moduleAttributes = FreezableHelper.FreezeListAndElements(moduleAttributes);
			foreach (var type in typeDefinitions.Values) {
				FreezableHelper.Freeze(type);
			}
		}
		
		public DefaultUnresolvedAssembly(string assemblyName)
		{
			if (assemblyName == null)
				throw new ArgumentNullException("assemblyName");
			this.assemblyName = assemblyName;
			this.assemblyAttributes = new List<IUnresolvedAttribute>();
			this.moduleAttributes = new List<IUnresolvedAttribute>();
		}
		
		public string AssemblyName {
			get { return assemblyName; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				FreezableHelper.ThrowIfFrozen(this);
				assemblyName = value;
			}
		}
		
		public IList<IUnresolvedAttribute> AssemblyAttributes {
			get { return assemblyAttributes; }
		}
		
		IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.AssemblyAttributes {
			get { return assemblyAttributes; }
		}
		
		public IList<IUnresolvedAttribute> ModuleAttributes {
			get { return moduleAttributes; }
		}
		
		IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.ModuleAttributes {
			get { return moduleAttributes; }
		}
		
		public IEnumerable<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get { return typeDefinitions.Values; }
		}
		
		/// <summary>
		/// Adds a new top-level type definition to this assembly.
		/// </summary>
		/// <remarks>DefaultUnresolvedAssembly does not support partial classes.
		/// Adding more than one part of a type will cause an ArgumentException.</remarks>
		public void AddTypeDefinition(IUnresolvedTypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			if (typeDefinition.DeclaringTypeDefinition != null)
				throw new ArgumentException("Cannot add nested types.");
			FreezableHelper.ThrowIfFrozen(this);
			var key = new FullNameAndTypeParameterCount(typeDefinition.Namespace, typeDefinition.Name, typeDefinition.TypeParameters.Count);
			typeDefinitions.Add(key, typeDefinition);
		}
		
		public IUnresolvedTypeDefinition GetTypeDefinition(string ns, string name, int typeParameterCount)
		{
			var key = new FullNameAndTypeParameterCount(ns ?? string.Empty, name, typeParameterCount);
			IUnresolvedTypeDefinition td;
			if (typeDefinitions.TryGetValue(key, out td))
				return td;
			else
				return null;
		}
		
		public IAssembly Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			Freeze();
			var cache = context.Compilation.CacheManager;
			IAssembly asm = (IAssembly)cache.GetShared(this);
			if (asm != null) {
				return asm;
			} else {
				asm = new DefaultResolvedAssembly(context.Compilation, this);
				return (IAssembly)cache.GetOrAddShared(this, asm);
			}
		}
		
		public override string ToString()
		{
			return "[" + GetType().Name + " " + assemblyName + "]";
		}
		
		//[NonSerialized]
		//List<Dictionary<FullNameAndTypeParameterCount, IUnresolvedTypeDefinition>> cachedTypeDictionariesPerNameComparer;
		
		Dictionary<FullNameAndTypeParameterCount, IUnresolvedTypeDefinition> GetTypeDictionary(StringComparer nameComparer)
		{
			Debug.Assert(IsFrozen);
			if (nameComparer == StringComparer.Ordinal)
				return typeDefinitions;
			else
				throw new NotImplementedException();
		}
		
		#region UnresolvedNamespace
		sealed class UnresolvedNamespace
		{
			internal readonly string FullName;
			internal readonly string Name;
			internal readonly List<UnresolvedNamespace> Children = new List<UnresolvedNamespace>();
			
			public UnresolvedNamespace(string fullName, string name)
			{
				this.FullName = fullName;
				this.Name = name;
			}
		}
		
		[NonSerialized]
		List<KeyValuePair<StringComparer, UnresolvedNamespace>> unresolvedNamespacesPerNameComparer;
		
		UnresolvedNamespace GetUnresolvedRootNamespace(StringComparer nameComparer)
		{
			Debug.Assert(IsFrozen);
			LazyInitializer.EnsureInitialized(ref unresolvedNamespacesPerNameComparer);
			lock (unresolvedNamespacesPerNameComparer) {
				foreach (var pair in unresolvedNamespacesPerNameComparer) {
					if (pair.Key == nameComparer)
						return pair.Value;
				}
				var root = new UnresolvedNamespace(string.Empty, string.Empty);
				var dict = new Dictionary<string, UnresolvedNamespace>(nameComparer);
				dict.Add(root.FullName, root);
				foreach (var typeName in typeDefinitions.Keys) {
					GetOrAddNamespace(dict, typeName.Namespace);
				}
				unresolvedNamespacesPerNameComparer.Add(new KeyValuePair<StringComparer, UnresolvedNamespace>(nameComparer, root));
				return root;
			}
		}
		
		static UnresolvedNamespace GetOrAddNamespace(Dictionary<string, UnresolvedNamespace> dict, string fullName)
		{
			UnresolvedNamespace ns;
			if (dict.TryGetValue(fullName, out ns))
				return ns;
			int pos = fullName.LastIndexOf('.');
			UnresolvedNamespace parent;
			string name;
			if (pos < 0) {
				parent = dict[string.Empty]; // root
				name = fullName;
			} else {
				parent = GetOrAddNamespace(dict, fullName.Substring(0, pos));
				name = fullName.Substring(pos + 1);
			}
			ns = new UnresolvedNamespace(fullName, name);
			parent.Children.Add(ns);
			dict.Add(fullName, ns);
			return ns;
		}
		#endregion
		
		sealed class DefaultResolvedAssembly : IAssembly
		{
			readonly DefaultUnresolvedAssembly unresolved;
			readonly ICompilation compilation;
			readonly ITypeResolveContext context;
			readonly Dictionary<FullNameAndTypeParameterCount, IUnresolvedTypeDefinition> unresolvedTypeDict;
			readonly ConcurrentDictionary<IUnresolvedTypeDefinition, ITypeDefinition> typeDict = new ConcurrentDictionary<IUnresolvedTypeDefinition, ITypeDefinition>();
			readonly INamespace rootNamespace;
			
			public DefaultResolvedAssembly(ICompilation compilation, DefaultUnresolvedAssembly unresolved)
			{
				this.compilation = compilation;
				this.unresolved = unresolved;
				this.unresolvedTypeDict = unresolved.GetTypeDictionary(compilation.NameComparer);
				this.rootNamespace = new NS(this, unresolved.GetUnresolvedRootNamespace(compilation.NameComparer), null);
				this.context = new SimpleTypeResolveContext(this);
				this.AssemblyAttributes = unresolved.AssemblyAttributes.ToList().CreateResolvedAttributes(context);
				this.ModuleAttributes = unresolved.ModuleAttributes.ToList().CreateResolvedAttributes(context);
			}
			
			public IUnresolvedAssembly UnresolvedAssembly {
				get { return unresolved; }
			}
			
			public bool IsMainAssembly {
				get { return this.Compilation.MainAssembly == this; }
			}
			
			public string AssemblyName {
				get { return unresolved.AssemblyName; }
			}
			
			public IList<IAttribute> AssemblyAttributes { get; private set; }
			public IList<IAttribute> ModuleAttributes { get; private set; }
			
			public INamespace RootNamespace {
				get { return rootNamespace; }
			}
			
			public ICompilation Compilation {
				get { return compilation; }
			}
			
			public bool InternalsVisibleTo(IAssembly assembly)
			{
				return assembly == this;
			}
			
			public ITypeDefinition GetTypeDefinition(string ns, string name, int typeParameterCount)
			{
				return GetTypeDefinition(unresolved.GetTypeDefinition(ns, name, typeParameterCount));
			}
			
			public ITypeDefinition GetTypeDefinition(IUnresolvedTypeDefinition unresolved)
			{
				if (unresolved == null)
					return null;
				return typeDict.GetOrAdd(unresolved, t => CreateTypeDefinition(t));
			}
			
			ITypeDefinition CreateTypeDefinition(IUnresolvedTypeDefinition unresolved)
			{
				if (unresolved.DeclaringTypeDefinition != null) {
					ITypeDefinition declaringType = GetTypeDefinition(unresolved.DeclaringTypeDefinition);
					return new DefaultResolvedTypeDefinition(context.WithCurrentTypeDefinition(declaringType), unresolved);
				} else if (unresolved.Name == "Void" && unresolved.Namespace == "System" && unresolved.TypeParameters.Count == 0) {
					return new VoidTypeDefinition(context, unresolved);
				} else {
					return new DefaultResolvedTypeDefinition(context, unresolved);
				}
			}
			
			public IEnumerable<ITypeDefinition> TopLevelTypeDefinitions {
				get {
					return unresolved.TopLevelTypeDefinitions.Select(t => GetTypeDefinition(t));
				}
			}
			
			sealed class NS : INamespace
			{
				readonly DefaultResolvedAssembly assembly;
				readonly UnresolvedNamespace ns;
				readonly INamespace parentNamespace;
				readonly IList<NS> childNamespaces;
				IEnumerable<ITypeDefinition> types;
				
				public NS(DefaultResolvedAssembly assembly, UnresolvedNamespace ns, INamespace parentNamespace)
				{
					this.assembly = assembly;
					this.ns = ns;
					this.parentNamespace = parentNamespace;
					this.childNamespaces = new ProjectedList<NS, UnresolvedNamespace, NS>(
						this, ns.Children, (self, c) => new NS(self.assembly, c, self));
				}
				
				string INamespace.ExternAlias {
					get { return null; }
				}
				
				string INamespace.FullName {
					get { return ns.FullName; }
				}
				
				string INamespace.Name {
					get { return ns.Name; }
				}
				
				INamespace INamespace.ParentNamespace {
					get { return parentNamespace; }
				}
				
				IEnumerable<INamespace> INamespace.ChildNamespaces {
					get { return childNamespaces; }
				}
				
				INamespace INamespace.GetChildNamespace(string name)
				{
					var nameComparer = assembly.compilation.NameComparer;
					for (int i = 0; i < childNamespaces.Count; i++) {
						if (nameComparer.Equals(name, ns.Children[i].Name))
							return childNamespaces[i];
					}
					return null;
				}
				
				ICompilation IResolved.Compilation {
					get { return assembly.compilation; }
				}
				
				IEnumerable<ITypeDefinition> INamespace.Types {
					get {
						var result = this.types;
						if (result != null) {
							LazyInit.ReadBarrier();
							return result;
						} else {
							var hashSet = new HashSet<ITypeDefinition>();
							foreach (IUnresolvedTypeDefinition typeDef in assembly.UnresolvedAssembly.TopLevelTypeDefinitions) {
								if (typeDef.Namespace == ns.FullName)
									hashSet.Add(assembly.GetTypeDefinition(typeDef));
							}
							return LazyInit.GetOrSet(ref this.types, hashSet.ToArray());
						}
					}
				}
				
				ITypeDefinition INamespace.GetTypeDefinition(string name, int typeParameterCount)
				{
					var key = new FullNameAndTypeParameterCount(ns.FullName, name, typeParameterCount);
					IUnresolvedTypeDefinition unresolvedTypeDef;
					if (assembly.unresolvedTypeDict.TryGetValue(key, out unresolvedTypeDef))
						return assembly.GetTypeDefinition(unresolvedTypeDef);
					else
						return null;
				}
			}
		}
	}
}
