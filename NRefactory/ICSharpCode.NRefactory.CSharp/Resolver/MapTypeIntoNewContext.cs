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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Converts a type by replacing all type definitions with the equivalent definitions in the new context.
	/// </summary>
	sealed class MapTypeIntoNewContext : TypeVisitor
	{
		readonly ITypeResolveContext context;
		
		public MapTypeIntoNewContext(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public override IType VisitTypeDefinition(ITypeDefinition type)
		{
			if (type.DeclaringTypeDefinition != null) {
				ITypeDefinition decl = type.DeclaringTypeDefinition.AcceptVisitor(this) as ITypeDefinition;
				if (decl != null) {
					foreach (ITypeDefinition c in decl.NestedTypes) {
						if (c.Name == type.Name && c.TypeParameterCount == type.TypeParameterCount)
							return c;
					}
				}
				return type;
			} else {
				return context.GetTypeDefinition(type.Namespace, type.Name, type.TypeParameterCount, StringComparer.Ordinal) ?? type;
			}
		}
		
		public override IType VisitTypeParameter(ITypeParameter type)
		{
			// TODO: how to map type parameters?
			// It might have constraints, and those constraints might be mutually recursive.
			// Maybe reintroduce ITypeParameter.Owner?
			throw new NotImplementedException();
		}
	}
}
