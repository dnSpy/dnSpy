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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Resolve context represents the minimal mscorlib required for evaluating constants.
	/// </summary>
	public sealed class MinimalResolveContext : AbstractAnnotatable, IProjectContent, ISynchronizedTypeResolveContext
	{
		static readonly Lazy<MinimalResolveContext> instance = new Lazy<MinimalResolveContext>(() => new MinimalResolveContext());
		
		public static MinimalResolveContext Instance {
			get { return instance.Value; }
		}
		
		readonly ReadOnlyCollection<string> namespaces = Array.AsReadOnly(new string[] { "System" });
		readonly ITypeDefinition systemObject, systemValueType;
		readonly ReadOnlyCollection<ITypeDefinition> types;
		
		private MinimalResolveContext()
		{
			List<ITypeDefinition> types = new List<ITypeDefinition>();
			
			systemObject = new DefaultTypeDefinition(this, "System", "Object") {
				Accessibility = Accessibility.Public
			};
			systemValueType = new DefaultTypeDefinition(this, "System", "ValueType") {
				Accessibility = Accessibility.Public,
				BaseTypes = { systemObject }
			};
			// TypeCode.Empty = void
			types.Add(new VoidTypeDefinition(this));
			// types are added in the order they are defined in the TypeCode enum
			types.Add(systemObject);
			types.Add(CreateClass("System", "DBNull"));
			types.Add(CreateStruct("System", "Boolean"));
			types.Add(CreateStruct("System", "Char"));
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
			types.Add(CreateStruct("System", "DateTime"));
			types.Add(systemValueType); // misuse unused enum value (TypeCode)17 for System.ValueType
			types.Add(CreateClass("System", "String"));
			foreach (ITypeDefinition type in types)
				type.Freeze();
			this.types = types.AsReadOnly();
		}
		
		ITypeDefinition CreateClass(string nameSpace, string name)
		{
			return new DefaultTypeDefinition(this, nameSpace, name) {
				Kind = TypeKind.Class,
				Accessibility = Accessibility.Public,
				BaseTypes = { systemObject }
			};
		}
		
		ITypeDefinition CreateStruct(string nameSpace, string name)
		{
			return new DefaultTypeDefinition(this, nameSpace, name) {
				Kind = TypeKind.Struct,
				Accessibility = Accessibility.Public,
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
		
		public ITypeDefinition GetKnownTypeDefinition(TypeCode typeCode)
		{
			return types[(int)typeCode];
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
		
		IList<IAttribute> IProjectContent.AssemblyAttributes {
			get { return EmptyList<IAttribute>.Instance; }
		}
		
		IList<IAttribute> IProjectContent.ModuleAttributes {
			get { return EmptyList<IAttribute>.Instance; }
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
		
		void IProjectContent.UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile)
		{
			throw new NotSupportedException();
		}
		
		public string AssemblyName {
			get { return "MinimalResolveContext"; }
		}
	}
}
