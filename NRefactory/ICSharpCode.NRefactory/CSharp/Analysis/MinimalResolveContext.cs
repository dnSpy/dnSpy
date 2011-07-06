// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Resolve context represents the minimal mscorlib required for evaluating constants.
	/// </summary>
	sealed class MinimalResolveContext : AbstractAnnotatable, IProjectContent, ISynchronizedTypeResolveContext
	{
		static readonly Lazy<MinimalResolveContext> instance = new Lazy<MinimalResolveContext>(() => new MinimalResolveContext());
		
		public static MinimalResolveContext Instance {
			get { return instance.Value; }
		}
		
		readonly ReadOnlyCollection<string> namespaces = Array.AsReadOnly(new string[] { "System" });
		readonly IAttribute[] assemblyAttributes = new IAttribute[0];
		readonly ITypeDefinition systemObject, systemValueType;
		readonly ReadOnlyCollection<ITypeDefinition> types;
		
		private MinimalResolveContext()
		{
			List<ITypeDefinition> types = new List<ITypeDefinition>();
			types.Add(systemObject = new DefaultTypeDefinition(this, "System", "Object"));
			types.Add(systemValueType = new DefaultTypeDefinition(this, "System", "ValueType") { BaseTypes = { systemObject } });
			types.Add(CreateStruct("System", "Boolean"));
			types.Add(CreateStruct("System", "SByte"));
			types.Add(CreateStruct("System", "Byte"));
			types.Add(CreateStruct("System", "Int16"));
			types.Add(CreateStruct("System", "UInt16"));
			types.Add(CreateStruct("System", "Int32"));
			types.Add(CreateStruct("System", "UInt32"));
			types.Add(CreateStruct("System", "Int64"));
			types.Add(CreateStruct("System", "UInt64"));
			types.Add(CreateStruct("System", "Single"));
			types.Add(CreateStruct("System", "Double"));
			types.Add(CreateStruct("System", "Decimal"));
			types.Add(new DefaultTypeDefinition(this, "System", "String") { BaseTypes = { systemObject } });
			foreach (ITypeDefinition type in types)
				type.Freeze();
			this.types = types.AsReadOnly();
		}
		
		ITypeDefinition CreateStruct(string nameSpace, string name)
		{
			return new DefaultTypeDefinition(this, nameSpace, name) {
				ClassType = ClassType.Struct,
				BaseTypes = { systemValueType }
			};
		}
		
		public ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			foreach (ITypeDefinition type in types) {
				if (nameComparer.Equals(type.Name, name) && nameComparer.Equals(type.Namespace, nameSpace) && type.TypeParameterCount == typeParameterCount)
					return type;
			}
			return null;
		}
		
		public IEnumerable<ITypeDefinition> GetTypes()
		{
			return types;
		}
		
		public IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
		{
			return types.Where(t => nameComparer.Equals(t.Namespace, nameSpace));
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
		
		public IList<IAttribute> AssemblyAttributes {
			get { return assemblyAttributes; }
		}
		
		ICSharpCode.NRefactory.Utils.CacheManager ITypeResolveContext.CacheManager {
			get {
				// We don't support caching
				return null;
			}
		}
		
		ISynchronizedTypeResolveContext ITypeResolveContext.Synchronize()
		{
			// This class is immutable
			return this;
		}
		
		void IDisposable.Dispose()
		{
			// exit from Synchronize() block
		}
		
		IParsedFile IProjectContent.GetFile(string fileName)
		{
			return null;
		}
		
		IEnumerable<IParsedFile> IProjectContent.Files {
			get {
				return EmptyList<IParsedFile>.Instance;
			}
		}
	}
}
