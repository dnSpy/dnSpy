// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	/// <summary>
	/// ITypeResolveContext implementation that lazily loads types from Cecil.
	/// </summary>
	public class CecilTypeResolveContext : ISynchronizedTypeResolveContext, IProjectContent
	{
		readonly ModuleDefinition module;
		readonly string[] namespaces;
		readonly CecilLoader loader;
		Dictionary<TypeDefinition, WeakReference> dict = new Dictionary<TypeDefinition, WeakReference>();
		int countUntilNextCleanup = 4;
		
		public CecilTypeResolveContext(ModuleDefinition module)
		{
			this.loader = new CecilLoader();
			this.loader.IncludeInternalMembers = true;
			this.module = module;
			this.namespaces = module.Types.Select(t => t.Namespace).Distinct().ToArray();
			
			List<IAttribute> assemblyAttributes = new List<IAttribute>();
			foreach (var attr in module.Assembly.CustomAttributes) {
				assemblyAttributes.Add(loader.ReadAttribute(attr));
			}
			this.AssemblyAttributes = assemblyAttributes.AsReadOnly();
		}
		
		ITypeDefinition GetClass(TypeDefinition cecilType)
		{
			lock (dict) {
				WeakReference wr;
				ITypeDefinition type;
				if (dict.TryGetValue(cecilType, out wr)) {
					type = (ITypeDefinition)wr.Target;
				} else {
					wr = null;
					type = null;
				}
				if (type == null) {
					type = loader.LoadType(cecilType, this);
				}
				if (wr == null) {
					if (--countUntilNextCleanup <= 0)
						CleanupDict();
					wr = new WeakReference(type);
					dict.Add(cecilType, wr);
				} else {
					wr.Target = type;
				}
				return type;
			}
		}
		
		void CleanupDict()
		{
			List<TypeDefinition> deletedKeys = new List<TypeDefinition>();
			foreach (var pair in dict) {
				if (!pair.Value.IsAlive) {
					deletedKeys.Add(pair.Key);
				}
			}
			foreach (var key in deletedKeys) {
				dict.Remove(key);
			}
			countUntilNextCleanup = dict.Count + 4;
		}
		
		public IList<IAttribute> AssemblyAttributes { get; private set; }
		
		public ITypeDefinition GetClass(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			if (typeParameterCount > 0)
				name = name + "`" + typeParameterCount.ToString();
			if (nameComparer == StringComparer.Ordinal) {
				TypeDefinition cecilType = module.GetType(nameSpace, name);
				if (cecilType != null)
					return GetClass(cecilType);
				else
					return null;
			}
			foreach (TypeDefinition cecilType in module.Types) {
				if (nameComparer.Equals(name, cecilType.Name)
				    && nameComparer.Equals(nameSpace, cecilType.Namespace)
				    && cecilType.GenericParameters.Count == typeParameterCount)
				{
					return GetClass(cecilType);
				}
			}
			return null;
		}
		
		public IEnumerable<ITypeDefinition> GetClasses()
		{
			foreach (TypeDefinition cecilType in module.Types) {
				yield return GetClass(cecilType);
			}
		}
		
		public IEnumerable<ITypeDefinition> GetClasses(string nameSpace, StringComparer nameComparer)
		{
			foreach (TypeDefinition cecilType in module.Types) {
				if (nameComparer.Equals(nameSpace, cecilType.Namespace))
					yield return GetClass(cecilType);
			}
		}
		
		public IEnumerable<string> GetNamespaces()
		{
			return namespaces;
		}
		
		public string GetNamespace(string nameSpace, StringComparer nameComparer)
		{
			foreach (string ns in namespaces) {
				if (nameComparer.Equals(ns, nameSpace))
					return ns;
			}
			return null;
		}
		
		ICSharpCode.NRefactory.Utils.CacheManager ITypeResolveContext.CacheManager {
			get {
				// We don't support caching
				return null;
			}
		}
		
		ISynchronizedTypeResolveContext ITypeResolveContext.Synchronize()
		{
			// This class is logically immutable
			return this;
		}
		
		void IDisposable.Dispose()
		{
			// exit from Synchronize() block
		}
	}
}
