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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	public class CSharpAssembly : IAssembly
	{
		readonly ICompilation compilation;
		readonly ITypeResolveContext context;
		readonly CSharpProjectContent projectContent;
		IList<IAttribute> assemblyAttributes;
		IList<IAttribute> moduleAttributes;
		
		internal CSharpAssembly(ICompilation compilation, CSharpProjectContent projectContent)
		{
			this.compilation = compilation;
			this.projectContent = projectContent;
			this.context = new SimpleTypeResolveContext(this);
		}
		
		public bool IsMainAssembly {
			get { return compilation.MainAssembly == this; }
		}
		
		public IUnresolvedAssembly UnresolvedAssembly {
			get { return projectContent; }
		}
		
		public string AssemblyName {
			get { return projectContent.AssemblyName; }
		}
		
		public IList<IAttribute> AssemblyAttributes {
			get {
				return GetAttributes(ref assemblyAttributes, true);
			}
		}
		
		public IList<IAttribute> ModuleAttributes {
			get {
				return GetAttributes(ref moduleAttributes, false);
			}
		}
		
		IList<IAttribute> GetAttributes(ref IList<IAttribute> field, bool assemblyAttributes)
		{
			IList<IAttribute> result = field;
			if (result != null) {
				LazyInit.ReadBarrier();
				return result;
			} else {
				result = new List<IAttribute>();
				foreach (var parsedFile in projectContent.Files.OfType<CSharpParsedFile>()) {
					var attributes = assemblyAttributes ? parsedFile.AssemblyAttributes : parsedFile.ModuleAttributes;
					var context = new CSharpTypeResolveContext(this, parsedFile.RootUsingScope.Resolve(compilation));
					foreach (var unresolvedAttr in attributes) {
						result.Add(unresolvedAttr.CreateResolvedAttribute(context));
					}
				}
				return LazyInit.GetOrSet(ref field, result);
			}
		}
		
		NS rootNamespace;
		
		public INamespace RootNamespace {
			get {
				NS root = this.rootNamespace;
				if (root != null) {
					LazyInit.ReadBarrier();
					return root;
				} else {
					root = new NS(this);
					Dictionary<string, NS> dict = new Dictionary<string, NS>();
					dict.Add(string.Empty, root);
					foreach (var pair in GetTypes()) {
						NS ns = GetOrAddNamespace(dict, pair.Key.Namespace);
						ns.types[pair.Key] = pair.Value;
					}
					return LazyInit.GetOrSet(ref this.rootNamespace, root);
				}
			}
		}
		
		static NS GetOrAddNamespace(Dictionary<string, NS> dict, string fullName)
		{
			NS ns;
			if (dict.TryGetValue(fullName, out ns))
				return ns;
			int pos = fullName.LastIndexOf('.');
			NS parent;
			string name;
			if (pos < 0) {
				parent = dict[string.Empty]; // root
				name = fullName;
			} else {
				parent = GetOrAddNamespace(dict, fullName.Substring(0, pos));
				name = fullName.Substring(pos + 1);
			}
			ns = new NS(parent, fullName, name);
			parent.childNamespaces.Add(ns);
			dict.Add(fullName, ns);
			return ns;
		}
		
		public ICompilation Compilation {
			get { return compilation; }
		}
		
		public bool InternalsVisibleTo(IAssembly assembly)
		{
			if (this == assembly)
				return true;
			foreach (string shortName in GetInternalsVisibleTo()) {
				if (assembly.AssemblyName == shortName)
					return true;
			}
			return false;
		}
		
		volatile string[] internalsVisibleTo;
		
		string[] GetInternalsVisibleTo()
		{
			var result = this.internalsVisibleTo;
			if (result != null) {
				return result;
			} else {
				internalsVisibleTo = (
					from attr in this.AssemblyAttributes
					where attr.AttributeType.Name == "InternalsVisibleToAttribute"
					&& attr.AttributeType.Namespace == "System.Runtime.CompilerServices"
					&& attr.PositionalArguments.Count == 1
					select GetShortName(attr.PositionalArguments.Single().ConstantValue as string)
				).ToArray();
				return internalsVisibleTo;
			}
		}
		
		static string GetShortName(string fullAssemblyName)
		{
			if (fullAssemblyName == null)
				return null;
			int pos = fullAssemblyName.IndexOf(',');
			if (pos < 0)
				return fullAssemblyName;
			else
				return fullAssemblyName.Substring(0, pos);
		}
		
		Dictionary<FullNameAndTypeParameterCount, DefaultResolvedTypeDefinition> typeDict;
		
		Dictionary<FullNameAndTypeParameterCount, DefaultResolvedTypeDefinition> GetTypes()
		{
			var dict = this.typeDict;
			if (dict != null) {
				LazyInit.ReadBarrier();
				return dict;
			} else {
				var comparer = FullNameAndTypeParameterCountComparer.Ordinal;
				dict = projectContent.TopLevelTypeDefinitions
					.GroupBy(t => new FullNameAndTypeParameterCount(t.Namespace, t.Name, t.TypeParameters.Count), comparer)
					.ToDictionary(g => g.Key, g => new DefaultResolvedTypeDefinition(context, g.ToArray()), comparer);
				return LazyInit.GetOrSet(ref this.typeDict, dict);
			}
		}
		
		public ITypeDefinition GetTypeDefinition(string ns, string name, int typeParameterCount)
		{
			var key = new FullNameAndTypeParameterCount(ns ?? string.Empty, name, typeParameterCount);
			DefaultResolvedTypeDefinition def;
			if (GetTypes().TryGetValue(key, out def))
				return def;
			else
				return null;
		}
		
		public IEnumerable<ITypeDefinition> TopLevelTypeDefinitions {
			get {
				return GetTypes().Values;
			}
		}
		
		public override string ToString()
		{
			return "[CSharpAssembly " + this.AssemblyName + "]";
		}
		
		sealed class NS : INamespace
		{
			readonly CSharpAssembly assembly;
			readonly NS parentNamespace;
			readonly string fullName;
			readonly string name;
			internal readonly List<NS> childNamespaces = new List<NS>();
			internal readonly Dictionary<FullNameAndTypeParameterCount, ITypeDefinition> types;
			
			public NS(CSharpAssembly assembly)
			{
				this.assembly = assembly;
				this.fullName = string.Empty;
				this.name = string.Empty;
				this.types = new Dictionary<FullNameAndTypeParameterCount, ITypeDefinition>(new FullNameAndTypeParameterCountComparer(assembly.compilation.NameComparer));
			}
			
			public NS(NS parentNamespace, string fullName, string name)
			{
				this.assembly = parentNamespace.assembly;
				this.parentNamespace = parentNamespace;
				this.fullName = fullName;
				this.name = name;
				this.types = new Dictionary<FullNameAndTypeParameterCount, ITypeDefinition>(parentNamespace.types.Comparer);
			}
			
			string INamespace.ExternAlias {
				get { return null; }
			}
			
			string INamespace.FullName {
				get { return fullName; }
			}
			
			string INamespace.Name {
				get { return name; }
			}
			
			INamespace INamespace.ParentNamespace {
				get { return parentNamespace; }
			}
			
			IEnumerable<INamespace> INamespace.ChildNamespaces {
				get { return childNamespaces; }
			}
			
			IEnumerable<ITypeDefinition> INamespace.Types {
				get { return types.Values; }
			}
			
			ICompilation IResolved.Compilation {
				get { return assembly.Compilation; }
			}
			
			INamespace INamespace.GetChildNamespace(string name)
			{
				var nameComparer = assembly.compilation.NameComparer;
				foreach (NS childNamespace in childNamespaces) {
					if (nameComparer.Equals(name, childNamespace.name))
						return childNamespace;
				}
				return null;
			}
			
			ITypeDefinition INamespace.GetTypeDefinition(string name, int typeParameterCount)
			{
				return assembly.GetTypeDefinition(this.fullName, name, typeParameterCount);
			}
		}
	}
}
