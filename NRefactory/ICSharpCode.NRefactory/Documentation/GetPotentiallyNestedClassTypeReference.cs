// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.Documentation
{
	/// <summary>
	/// A type reference of the form 'Some.Namespace.TopLevelType.NestedType`n'.
	/// We do not know the boundary between namespace name and top level type, so we have to try
	/// all possibilities.
	/// The type parameter count only applies to the innermost type, all outer types must be non-generic.
	/// </summary>
	[Serializable]
	class GetPotentiallyNestedClassTypeReference : ITypeReference
	{
		readonly string typeName;
		readonly int typeParameterCount;
		
		public GetPotentiallyNestedClassTypeReference(string typeName, int typeParameterCount)
		{
			this.typeName = typeName;
			this.typeParameterCount = typeParameterCount;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			string[] parts = typeName.Split('.');
			var assemblies = new [] { context.CurrentAssembly }.Concat(context.Compilation.Assemblies);
			for (int i = parts.Length - 1; i >= 0; i--) {
				string ns = string.Join(".", parts, 0, i);
				string name = parts[i];
				int topLevelTPC = (i == parts.Length - 1 ? typeParameterCount : 0);
				foreach (var asm in assemblies) {
					if (asm == null)
						continue;
					ITypeDefinition typeDef = asm.GetTypeDefinition(new TopLevelTypeName(ns, name, topLevelTPC));
					for (int j = i + 1; j < parts.Length && typeDef != null; j++) {
						int tpc = (j == parts.Length - 1 ? typeParameterCount : 0);
						typeDef = typeDef.NestedTypes.FirstOrDefault(n => n.Name == parts[j] && n.TypeParameterCount == tpc);
					}
					if (typeDef != null)
						return typeDef;
				}
			}
			int idx = typeName.LastIndexOf('.');
			if (idx < 0)
				return new UnknownType("", typeName, typeParameterCount);
			// give back a guessed namespace/type name
			return  new UnknownType(typeName.Substring(0, idx), typeName.Substring(idx + 1), typeParameterCount);
		}
	}
}
