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
using System.Diagnostics.Contracts;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation for IType interface.
	/// </summary>
	[Serializable]
	public abstract class AbstractType : IType
	{
		public virtual string FullName {
			get {
				string ns = this.Namespace;
				string name = this.Name;
				if (string.IsNullOrEmpty(ns)) {
					return name;
				} else {
					return ns + "." + name;
				}
			}
		}
		
		public abstract string Name { get; }
		
		public virtual string Namespace {
			get { return string.Empty; }
		}
		
		public virtual string ReflectionName {
			get { return this.FullName; }
		}
		
		public abstract bool? IsReferenceType  { get; }
		
		public abstract TypeKind Kind { get; }
		
		public virtual int TypeParameterCount {
			get { return 0; }
		}

		readonly static IList<IType> emptyTypeArguments = new IType[0];
		public virtual IList<IType> TypeArguments {
			get { return emptyTypeArguments; }
		}

		public virtual IType DeclaringType {
			get { return null; }
		}

		public virtual bool IsParameterized { 
			get { return false; }
		}

		public virtual ITypeDefinition GetDefinition()
		{
			return null;
		}
		
		public virtual IEnumerable<IType> DirectBaseTypes {
			get { return EmptyList<IType>.Instance; }
		}
		
		public abstract ITypeReference ToTypeReference();
		
		public virtual IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IType>.Instance;
		}
		
		public virtual IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IType>.Instance;
		}
		
		public virtual IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public virtual IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public virtual IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public virtual IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IProperty>.Instance;
		}
		
		public virtual IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IField>.Instance;
		}
		
		public virtual IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IEvent>.Instance;
		}
		
		public virtual IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			IEnumerable<IMember> members = GetMethods(filter, options);
			return members
				.Concat(GetProperties(filter, options))
				.Concat(GetFields(filter, options))
				.Concat(GetEvents(filter, options));
		}
		
		public virtual IEnumerable<IMethod> GetAccessors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public TypeParameterSubstitution GetSubstitution()
		{
			return TypeParameterSubstitution.Identity;
		}
		
		public TypeParameterSubstitution GetSubstitution(IList<IType> methodTypeArguments)
		{
			return TypeParameterSubstitution.Identity;
		}

		public override sealed bool Equals(object obj)
		{
			return Equals(obj as IType);
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public virtual bool Equals(IType other)
		{
			return this == other; // use reference equality by default
		}
		
		public override string ToString()
		{
			return this.ReflectionName;
		}
		
		public virtual IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitOtherType(this);
		}
		
		public virtual IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
	}
}
