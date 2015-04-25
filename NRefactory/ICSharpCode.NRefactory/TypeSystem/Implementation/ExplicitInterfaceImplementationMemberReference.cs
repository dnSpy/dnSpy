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
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// References a member that is an explicit interface implementation.
	/// </summary>
	/// <remarks>
	/// Resolving an ExplicitInterfaceImplementationMemberReference requires a context
	/// that provides enough information for resolving the declaring type reference
	/// and the interface member reference.
	/// Note that the interface member reference is resolved in '<c>context.WithCurrentTypeDefinition(declaringType.GetDefinition())</c>'
	/// - this is done to ensure that open generics in the interface member reference resolve to the type parameters of the
	/// declaring type.
	/// </remarks>
	[Serializable]
	public sealed class ExplicitInterfaceImplementationMemberReference : IMemberReference
	{
		ITypeReference typeReference;
		IMemberReference interfaceMemberReference;
		
		public ExplicitInterfaceImplementationMemberReference(ITypeReference typeReference, IMemberReference interfaceMemberReference)
		{
			if (typeReference == null)
				throw new ArgumentNullException("typeReference");
			if (interfaceMemberReference == null)
				throw new ArgumentNullException("interfaceMemberReference");
			this.typeReference = typeReference;
			this.interfaceMemberReference = interfaceMemberReference;
		}
		
		public ITypeReference DeclaringTypeReference {
			get { return typeReference; }
		}
		
		public IMember Resolve(ITypeResolveContext context)
		{
			IType declaringType = typeReference.Resolve(context);
			IMember interfaceMember = interfaceMemberReference.Resolve(context.WithCurrentTypeDefinition(declaringType.GetDefinition()));
			if (interfaceMember == null)
				return null;
			IEnumerable<IMember> members;
			if (interfaceMember.SymbolKind == SymbolKind.Accessor) {
				members = declaringType.GetAccessors(
					m => m.IsExplicitInterfaceImplementation,
					GetMemberOptions.IgnoreInheritedMembers);
			} else {
				members = declaringType.GetMembers(
					m => m.SymbolKind == interfaceMember.SymbolKind && m.IsExplicitInterfaceImplementation,
					GetMemberOptions.IgnoreInheritedMembers);
			}
			return members.FirstOrDefault(m => m.ImplementedInterfaceMembers.Count == 1 && interfaceMember.Equals(m.ImplementedInterfaceMembers[0]));
		}
		
		ISymbol ISymbolReference.Resolve(ITypeResolveContext context)
		{
			return Resolve(context);
		}
	}
}
