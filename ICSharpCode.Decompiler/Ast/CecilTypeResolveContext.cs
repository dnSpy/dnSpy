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
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	/// <summary>
	/// ITypeResolveContext implementation that lazily loads types from Cecil.
	/// </summary>
	public class CecilTypeResolveContext : AbstractAnnotatable, ISynchronizedTypeResolveContext, IProjectContent
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
		
		public string AssemblyName {
			get { return module.Assembly.Name.Name; }
		}
		
		public IList<IAttribute> ModuleAttributes { get; private set; }
		
		public IList<IAttribute> AssemblyAttributes { get; private set; }
		
		public ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
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
		
		public ITypeDefinition GetKnownTypeDefinition(TypeCode typeCode)
		{
			return GetTypeDefinition("System", ReflectionHelper.GetShortNameByTypeCode(typeCode), 0, StringComparer.Ordinal);
		}
		
		public IEnumerable<ITypeDefinition> GetTypes()
		{
			foreach (TypeDefinition cecilType in module.Types) {
				yield return GetClass(cecilType);
			}
		}
		
		public IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
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
		
		IEnumerable<IParsedFile> IProjectContent.Files {
			get { return new IParsedFile[0]; }
		}
		
		void IProjectContent.UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile)
		{
			throw new NotSupportedException();
		}
		
		IParsedFile IProjectContent.GetFile(string fileName)
		{
			return null;
		}
	}
}
