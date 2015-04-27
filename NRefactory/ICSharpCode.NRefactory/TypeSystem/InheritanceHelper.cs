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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Provides helper methods for inheritance.
	/// </summary>
	public static class InheritanceHelper
	{
		// TODO: maybe these should be extension methods?
		// or even part of the interface itself? (would allow for easy caching)
		
		#region GetBaseMember
		/// <summary>
		/// Gets the base member that has the same signature.
		/// </summary>
		public static IMember GetBaseMember(IMember member)
		{
			return GetBaseMembers(member, false).FirstOrDefault();
		}

		/// <summary>
		/// Gets all base members that have the same signature.
		/// </summary>
		/// <returns>
		/// List of base members with the same signature. The member from the derived-most base class is returned first.
		/// </returns>
		public static IEnumerable<IMember> GetBaseMembers(IMember member, bool includeImplementedInterfaces)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			if (member.IsExplicitInterfaceImplementation && member.ImplementedInterfaceMembers.Count == 1) {
				// C#-style explicit interface implementation
				member = member.ImplementedInterfaceMembers[0];
				yield return member;
			}
			
			// Remove generic specialization
			var substitution = member.Substitution;
			member = member.MemberDefinition;
			
			if (member.DeclaringTypeDefinition == null) {
				// For global methods, return empty list. (prevent SharpDevelop UDC crash 4524)
				yield break;
			}
			
			IEnumerable<IType> allBaseTypes;
			if (includeImplementedInterfaces) {
				allBaseTypes = member.DeclaringTypeDefinition.GetAllBaseTypes();
			} else {
				allBaseTypes = member.DeclaringTypeDefinition.GetNonInterfaceBaseTypes();
			}
			foreach (IType baseType in allBaseTypes.Reverse()) {
				if (baseType == member.DeclaringTypeDefinition)
					continue;

				IEnumerable<IMember> baseMembers;
				if (member.SymbolKind == SymbolKind.Accessor) {
					baseMembers = baseType.GetAccessors(m => m.Name == member.Name && !m.IsExplicitInterfaceImplementation, GetMemberOptions.IgnoreInheritedMembers);
				} else {
					baseMembers = baseType.GetMembers(m => m.Name == member.Name && !m.IsExplicitInterfaceImplementation, GetMemberOptions.IgnoreInheritedMembers);
				}
				foreach (IMember baseMember in baseMembers) {
					if (SignatureComparer.Ordinal.Equals(member, baseMember)) {
						yield return baseMember.Specialize(substitution);
					}
				}
			}
		}
		#endregion
		
		#region GetDerivedMember
		/// <summary>
		/// Finds the member declared in 'derivedType' that has the same signature (could override) 'baseMember'.
		/// </summary>
		public static IMember GetDerivedMember(IMember baseMember, ITypeDefinition derivedType)
		{
			if (baseMember == null)
				throw new ArgumentNullException("baseMember");
			if (derivedType == null)
				throw new ArgumentNullException("derivedType");
			
			if (baseMember.Compilation != derivedType.Compilation)
				throw new ArgumentException("baseMember and derivedType must be from the same compilation");
			
			baseMember = baseMember.MemberDefinition;
			bool includeInterfaces = baseMember.DeclaringTypeDefinition.Kind == TypeKind.Interface;
			IMethod method = baseMember as IMethod;
			if (method != null) {
				foreach (IMethod derivedMethod in derivedType.Methods) {
					if (derivedMethod.Name == method.Name && derivedMethod.Parameters.Count == method.Parameters.Count) {
						if (derivedMethod.TypeParameters.Count == method.TypeParameters.Count) {
							// The method could override the base method:
							if (GetBaseMembers(derivedMethod, includeInterfaces).Any(m => m.MemberDefinition == baseMember))
								return derivedMethod;
						}
					}
				}
			}
			IProperty property = baseMember as IProperty;
			if (property != null) {
				foreach (IProperty derivedProperty in derivedType.Properties) {
					if (derivedProperty.Name == property.Name && derivedProperty.Parameters.Count == property.Parameters.Count) {
						// The property could override the base property:
						if (GetBaseMembers(derivedProperty, includeInterfaces).Any(m => m.MemberDefinition == baseMember))
							return derivedProperty;
					}
				}
			}
			if (baseMember is IEvent) {
				foreach (IEvent derivedEvent in derivedType.Events) {
					if (derivedEvent.Name == baseMember.Name)
						return derivedEvent;
				}
			}
			if (baseMember is IField) {
				foreach (IField derivedField in derivedType.Fields) {
					if (derivedField.Name == baseMember.Name)
						return derivedField;
				}
			}
			return null;
		}
		#endregion
	}
}
