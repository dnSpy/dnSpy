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
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// References a member that is an explicit interface implementation.
	/// </summary>
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
		
		public IMember Resolve(ITypeResolveContext context)
		{
			IMember interfaceMember = interfaceMemberReference.Resolve(context);
			if (interfaceMember == null)
				return null;
			IType type = typeReference.Resolve(context);
			var members = type.GetMembers(
				m => m.EntityType == interfaceMember.EntityType && m.IsExplicitInterfaceImplementation,
				GetMemberOptions.IgnoreInheritedMembers);
			return members.FirstOrDefault(m => m.InterfaceImplementations.Count == 1 && interfaceMember.Equals(m.InterfaceImplementations[0]));
		}
	}
}
