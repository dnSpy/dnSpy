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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	/// <summary>
	/// Represents a simple C# name. (a single non-qualified identifier with an optional list of type arguments)
	/// </summary>
	[Serializable]
	public sealed class SimpleTypeOrNamespaceReference : TypeOrNamespaceReference, ISupportsInterning
	{
		readonly string identifier;
		readonly IList<ITypeReference> typeArguments;
		readonly NameLookupMode lookupMode;
		
		public SimpleTypeOrNamespaceReference(string identifier, IList<ITypeReference> typeArguments, NameLookupMode lookupMode = NameLookupMode.Type)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
			this.lookupMode = lookupMode;
		}
		
		public string Identifier {
			get { return identifier; }
		}
		
		public IList<ITypeReference> TypeArguments {
			get { return typeArguments; }
		}
		
		public NameLookupMode LookupMode {
			get { return lookupMode; }
		}
		
		/// <summary>
		/// Adds a suffix to the identifier.
		/// Does not modify the existing type reference, but returns a new one.
		/// </summary>
		public SimpleTypeOrNamespaceReference AddSuffix(string suffix)
		{
			return new SimpleTypeOrNamespaceReference(identifier + suffix, typeArguments, lookupMode);
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			var typeArgs = typeArguments.Resolve(resolver.CurrentTypeResolveContext);
			return resolver.LookupSimpleNameOrTypeName(identifier, typeArgs, lookupMode);
		}
		
		public override IType ResolveType(CSharpResolver resolver)
		{
			TypeResolveResult trr = Resolve(resolver) as TypeResolveResult;
			return trr != null ? trr.Type : new UnknownType(null, identifier, typeArguments.Count);
		}
		
		public override string ToString()
		{
			if (typeArguments.Count == 0)
				return identifier;
			else
				return identifier + "<" + string.Join(",", typeArguments) + ">";
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			int hashCode = 0;
			unchecked {
				hashCode += 1000000021 * identifier.GetHashCode();
				hashCode += 1000000033 * typeArguments.GetHashCode();
				hashCode += 1000000087 * (int)lookupMode;
			}
			return hashCode;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			SimpleTypeOrNamespaceReference o = other as SimpleTypeOrNamespaceReference;
			return o != null && this.identifier == o.identifier
				&& this.typeArguments == o.typeArguments && this.lookupMode == o.lookupMode;
		}
	}
}
