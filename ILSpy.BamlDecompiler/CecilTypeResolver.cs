// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Linq;
using ICSharpCode.ILSpy;
using Mono.Cecil;
using Ricciolo.StylesExplorer.MarkupReflection;

namespace ILSpy.BamlDecompiler
{
	/// <summary>
	/// Description of CecilTypeResolver.
	/// </summary>
	public class CecilTypeResolver : ITypeResolver
	{
		IAssemblyResolver resolver;
		AssemblyDefinition thisAssembly;
		
		public CecilTypeResolver(IAssemblyResolver resolver, AssemblyDefinition asm)
		{
			this.resolver = resolver;
			this.thisAssembly = asm;
		}
		
		public bool IsLocalAssembly(string name)
		{
			return MakeShort(name) == this.thisAssembly.Name.Name;
		}
		
		string MakeShort(string name)
		{
			int endOffset = name.IndexOf(',');
			if (endOffset == -1)
				return name;
			
			return name.Substring(0, endOffset);
		}
		
		public IType GetTypeByAssemblyQualifiedName(string name)
		{
			int comma = name.IndexOf(',');
			
			if (comma == -1)
				throw new ArgumentException("invalid name");
			
			string fullName = name.Substring(0, comma);
			string assemblyName = name.Substring(comma + 1).Trim();
			
			var type = thisAssembly.MainModule.GetType(fullName);
			
			if (type == null) {
				type = TryFindInExportedTypes(fullName, thisAssembly);
			}
			
			if (type == null) {
				var otherAssembly = resolver.Resolve(assemblyName);
				if (otherAssembly == null)
					throw new Exception("could not resolve '" + assemblyName + "'!");
				type = otherAssembly.MainModule.GetType(fullName.Replace('+', '/'));
				
				if (type == null) {
					type = TryFindInExportedTypes(fullName, otherAssembly);
				}
			}
			
			if (type == null)
				throw new Exception("could not resolve '" + name + "'!");
			
			return new CecilType(type);
		}

		TypeDefinition TryFindInExportedTypes(string fullName, AssemblyDefinition asm)
		{
			foreach (var exportedType in asm.MainModule.ExportedTypes) {
				if (exportedType.IsForwarder && exportedType.FullName == fullName) {
					return exportedType.Resolve();
				}
			}
			
			return null;
		}
		
		public IDependencyPropertyDescriptor GetDependencyPropertyDescriptor(string name, IType ownerType, IType targetType)
		{
			if (!(ownerType is CecilType))
				throw new ArgumentException();
			
			return new CecilDependencyPropertyDescriptor(name, ((CecilType)ownerType).type);
		}
		
		public string RuntimeVersion {
			get {
				return thisAssembly.MainModule.Runtime.ToString();
			}
		}
	}
}
