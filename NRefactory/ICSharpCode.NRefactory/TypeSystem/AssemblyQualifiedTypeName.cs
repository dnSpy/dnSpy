// Copyright (c) 2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.TypeSystem
{
	public struct AssemblyQualifiedTypeName : IEquatable<AssemblyQualifiedTypeName>
	{
		public readonly string AssemblyName;
		public readonly FullTypeName TypeName;
		
		public AssemblyQualifiedTypeName(FullTypeName typeName, string assemblyName)
		{
			this.AssemblyName = assemblyName;
			this.TypeName = typeName;
		}
		
		public AssemblyQualifiedTypeName(ITypeDefinition typeDefinition)
		{
			this.AssemblyName = typeDefinition.ParentAssembly.AssemblyName;
			this.TypeName = typeDefinition.FullTypeName;
		}
		
		public override string ToString()
		{
			if (string.IsNullOrEmpty(AssemblyName))
				return TypeName.ToString();
			else
				return TypeName.ToString() + ", " + AssemblyName;
		}
		
		public override bool Equals(object obj)
		{
			return (obj is AssemblyQualifiedTypeName) && Equals((AssemblyQualifiedTypeName)obj);
		}
		
		public bool Equals(AssemblyQualifiedTypeName other)
		{
			return this.AssemblyName == other.AssemblyName && this.TypeName == other.TypeName;
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (AssemblyName != null)
					hashCode += 1000000007 * AssemblyName.GetHashCode();
				hashCode += TypeName.GetHashCode();
			}
			return hashCode;
		}
		
		public static bool operator ==(AssemblyQualifiedTypeName lhs, AssemblyQualifiedTypeName rhs)
		{
			return lhs.Equals(rhs);
		}
		
		public static bool operator !=(AssemblyQualifiedTypeName lhs, AssemblyQualifiedTypeName rhs)
		{
			return !lhs.Equals(rhs);
		}
	}
}
