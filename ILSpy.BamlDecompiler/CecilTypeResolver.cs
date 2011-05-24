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
		LoadedAssembly assembly;
		
		public CecilTypeResolver(LoadedAssembly assembly)
		{
			this.assembly = assembly;
		}
		
		public IType GetTypeByAssemblyQualifiedName(string name)
		{
			int comma = name.IndexOf(',');
			
			if (comma == -1)
				throw new ArgumentException("invalid name");
			
			string fullName = name.Substring(0, comma);
			string assemblyName = name.Substring(comma + 1).Trim();
			
			var type = assembly.AssemblyDefinition.MainModule.GetType(fullName);
			if (type == null) {
				var otherAssembly = assembly.LookupReferencedAssembly(assemblyName);
				if (otherAssembly == null)
					throw new Exception("could not resolve '" + assemblyName + "'!");
				type = otherAssembly.AssemblyDefinition.MainModule.GetType(fullName);
			}
			
			return new CecilType(type);
		}
		
		public IDependencyPropertyDescriptor GetDependencyPropertyDescriptor(string name, IType ownerType, IType targetType)
		{
			if (!(ownerType is CecilType))
				throw new ArgumentException();
			
			return new CecilDependencyPropertyDescriptor(name, ((CecilType)ownerType).type);
		}
	}
}
