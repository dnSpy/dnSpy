// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	/// <summary>
	/// Interface representing a DotNet type
	/// </summary>
	public interface IType
	{
		IType BaseType { get; }
		string AssemblyQualifiedName { get; }
		bool IsSubclassOf(IType type);
		bool Equals(IType type);
	}
	
	public class UnresolvableType : IType
	{
		string assemblyQualifiedName;
		
		public UnresolvableType(string assemblyQualifiedName)
		{
			this.assemblyQualifiedName = assemblyQualifiedName;
		}
		
		public IType BaseType {
			get {
				return null;
			}
		}
		
		public string AssemblyQualifiedName {
			get {
				return assemblyQualifiedName;
			}
		}
		
		public bool IsSubclassOf(IType type)
		{
			return Equals(type);
		}
		
		public bool Equals(IType type)
		{
			return type is UnresolvableType && type.AssemblyQualifiedName == AssemblyQualifiedName;
		}
	}
}
