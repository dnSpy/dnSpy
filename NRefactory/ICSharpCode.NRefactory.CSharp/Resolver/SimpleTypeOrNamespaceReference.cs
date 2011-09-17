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

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a simple C# name. (a single non-qualified identifier with an optional list of type arguments)
	/// </summary>
	[Serializable]
	public sealed class SimpleTypeOrNamespaceReference : ITypeOrNamespaceReference, ISupportsInterning
	{
		readonly ITypeDefinition parentTypeDefinition;
		readonly UsingScope parentUsingScope;
		string identifier;
		IList<ITypeReference> typeArguments;
		readonly SimpleNameLookupMode lookupMode;
		
		public SimpleTypeOrNamespaceReference(string identifier, IList<ITypeReference> typeArguments, ITypeDefinition parentTypeDefinition, UsingScope parentUsingScope, SimpleNameLookupMode lookupMode = SimpleNameLookupMode.Type)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
			this.parentTypeDefinition = parentTypeDefinition;
			this.parentUsingScope = parentUsingScope;
			this.lookupMode = lookupMode;
		}
		
		public string Identifier {
			get { return identifier; }
		}
		
		public IList<ITypeReference> TypeArguments {
			get { return new ReadOnlyCollection<ITypeReference>(typeArguments); }
		}
		
		/// <summary>
		/// Adds a suffix to the identifier.
		/// Does not modify the existing type reference, but returns a new one.
		/// </summary>
		public SimpleTypeOrNamespaceReference AddSuffix(string suffix)
		{
			return new SimpleTypeOrNamespaceReference(identifier + suffix, typeArguments, parentTypeDefinition, parentUsingScope, lookupMode);
		}
		
		public ResolveResult DoResolve(ITypeResolveContext context)
		{
			CacheManager cacheManager = context.CacheManager;
			if (cacheManager != null) {
				ResolveResult cachedResult = cacheManager.GetShared(this) as ResolveResult;
				if (cachedResult != null)
					return cachedResult;
			}
			
			CSharpResolver r = new CSharpResolver(context);
			r.CurrentTypeDefinition = parentTypeDefinition;
			r.CurrentUsingScope = parentUsingScope;
			IType[] typeArgs = new IType[typeArguments.Count];
			for (int i = 0; i < typeArgs.Length; i++) {
				typeArgs[i] = typeArguments[i].Resolve(context);
			}
			ResolveResult rr = r.LookupSimpleNameOrTypeName(identifier, typeArgs, lookupMode);
			if (cacheManager != null)
				cacheManager.SetShared(this, rr);
			return rr;
		}
		
		public NamespaceResolveResult ResolveNamespace(ITypeResolveContext context)
		{
			// TODO: use resolve context for original project, if possible
			return DoResolve(context) as NamespaceResolveResult;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			// TODO: use resolve context for original project, if possible; then map the result type into the new context
			TypeResolveResult rr = DoResolve(context) as TypeResolveResult;
			return rr != null ? rr.Type : SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (typeArguments.Count == 0)
				return identifier;
			else
				return identifier + "<" + string.Join(",", typeArguments) + ">";
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			identifier = provider.Intern(identifier);
			typeArguments = provider.InternList(typeArguments);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			int hashCode = 0;
			unchecked {
				if (parentTypeDefinition != null)
					hashCode += 1000000007 * parentTypeDefinition.GetHashCode();
				if (parentUsingScope != null)
					hashCode += 1000000009 * parentUsingScope.GetHashCode();
				
				hashCode += 1000000021 * identifier.GetHashCode();
				hashCode += 1000000033 * typeArguments.GetHashCode();
				hashCode += 1000000087 * (int)lookupMode;
			}
			return hashCode;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			SimpleTypeOrNamespaceReference o = other as SimpleTypeOrNamespaceReference;
			return o != null && this.parentTypeDefinition == o.parentTypeDefinition
				&& this.parentUsingScope == o.parentUsingScope && this.identifier == o.identifier
				&& this.typeArguments == o.typeArguments && this.lookupMode == o.lookupMode;
		}
	}
}
