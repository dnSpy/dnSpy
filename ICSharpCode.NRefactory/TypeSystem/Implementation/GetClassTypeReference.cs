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
using System.Linq.Expressions;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type Reference used when the fully qualified type name is known.
	/// </summary>
	[Serializable]
	public sealed class GetClassTypeReference : ITypeReference, ISupportsInterning
	{
		IAssemblyReference assembly;
		string nameSpace, name;
		int typeParameterCount;
		
		/// <summary>
		/// Creates a new GetClassTypeReference that searches a type in the specified assembly.
		/// </summary>
		public GetClassTypeReference(IAssemblyReference assembly, string nameSpace, string name, int typeParameterCount)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");
			if (nameSpace == null)
				throw new ArgumentNullException("nameSpace");
			if (name == null)
				throw new ArgumentNullException("name");
			this.assembly = assembly;
			this.nameSpace = nameSpace;
			this.name = name;
			this.typeParameterCount = typeParameterCount;
		}
		
		public IAssemblyReference Assembly { get { return assembly; } }
		public string Namespace { get { return nameSpace; } }
		public string Name { get { return name; } }
		public int TypeParameterCount { get { return typeParameterCount; } }
		
		public IType Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
			IAssembly asm = assembly.Resolve(context);
			IType type = null;
			if (asm != null) {
				type = asm.GetTypeDefinition(nameSpace, name, typeParameterCount);
			}
			return type ?? new UnknownType(nameSpace, name, typeParameterCount);
		}
		
		public override string ToString()
		{
			if (typeParameterCount == 0)
				return BuildQualifiedName(nameSpace, name);
			else
				return BuildQualifiedName(nameSpace, name) + "`" + typeParameterCount;
		}
		
		static string BuildQualifiedName (string name1, string name2)
		{
			if (string.IsNullOrEmpty (name1))
				return name2;
			if (string.IsNullOrEmpty (name2))
				return name1;
			return name1 + "." + name2;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			assembly = provider.Intern(assembly);
			nameSpace = provider.Intern(nameSpace);
			name = provider.Intern(name);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return 33 * assembly.GetHashCode() + 27 * nameSpace.GetHashCode() + name.GetHashCode() + typeParameterCount;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			GetClassTypeReference o = other as GetClassTypeReference;
			return o != null && assembly == o.assembly && name == o.name && nameSpace == o.nameSpace && typeParameterCount == o.typeParameterCount;
		}
	}
}
