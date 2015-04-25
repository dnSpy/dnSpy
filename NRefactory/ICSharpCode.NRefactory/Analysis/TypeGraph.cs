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
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Analysis
{
	/// <summary>
	/// A graph where type definitions are nodes; and edges are given by inheritance.
	/// </summary>
	public class TypeGraph
	{
		Dictionary<AssemblyQualifiedTypeName, TypeGraphNode> dict;
		
		/// <summary>
		/// Builds a graph of all type definitions in the specified set of assemblies.
		/// </summary>
		/// <param name="assemblies">The input assemblies. The assemblies may belong to multiple compilations.</param>
		/// <remarks>The resulting graph may be cyclic if there are cyclic type definitions.</remarks>
		public TypeGraph(IEnumerable<IAssembly> assemblies)
		{
			if (assemblies == null)
				throw new ArgumentNullException("assemblies");
			dict = new Dictionary<AssemblyQualifiedTypeName, TypeGraphNode>();
			foreach (IAssembly assembly in assemblies) {
				foreach (ITypeDefinition typeDef in assembly.GetAllTypeDefinitions()) {
					// Overwrite previous entry - duplicates can occur if there are multiple versions of the
					// same project loaded in the solution (e.g. separate .csprojs for separate target frameworks)
					dict[new AssemblyQualifiedTypeName(typeDef)] = new TypeGraphNode(typeDef);
				}
			}
			foreach (IAssembly assembly in assemblies) {
				foreach (ITypeDefinition typeDef in assembly.GetAllTypeDefinitions()) {
					TypeGraphNode typeNode = dict[new AssemblyQualifiedTypeName(typeDef)];
					foreach (IType baseType in typeDef.DirectBaseTypes) {
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null) {
							TypeGraphNode baseTypeNode;
							if (dict.TryGetValue(new AssemblyQualifiedTypeName(baseTypeDef), out baseTypeNode)) {
								typeNode.BaseTypes.Add(baseTypeNode);
								baseTypeNode.DerivedTypes.Add(typeNode);
							}
						}
					}
				}
			}
		}
		
		public TypeGraphNode GetNode(ITypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				return null;
			return GetNode(new AssemblyQualifiedTypeName(typeDefinition));
		}
		
		public TypeGraphNode GetNode(AssemblyQualifiedTypeName typeName)
		{
			TypeGraphNode node;
			if (dict.TryGetValue(typeName, out node))
				return node;
			else
				return null;
		}
	}
}
