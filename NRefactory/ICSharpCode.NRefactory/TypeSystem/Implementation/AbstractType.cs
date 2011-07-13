// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation for IType interface.
	/// </summary>
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
		
		public abstract bool? IsReferenceType(ITypeResolveContext context);
		
		public virtual int TypeParameterCount {
			get { return 0; }
		}
		
		public virtual IType DeclaringType {
			get { return null; }
		}
		
		public virtual ITypeDefinition GetDefinition()
		{
			return null;
		}
		
		IType ITypeReference.Resolve(ITypeResolveContext context)
		{
			return this;
		}
		
		public virtual IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			return EmptyList<IType>.Instance;
		}
		
		public virtual IEnumerable<IType> GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter = null)
		{
			return EmptyList<IType>.Instance;
		}
		
		public virtual IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public virtual IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public virtual IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null)
		{
			return EmptyList<IProperty>.Instance;
		}
		
		public virtual IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null)
		{
			return EmptyList<IField>.Instance;
		}
		
		public virtual IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null)
		{
			return EmptyList<IEvent>.Instance;
		}
		
		public virtual IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null)
		{
			return GetMethods(context, filter).SafeCast<IMethod, IMember>()
				.Concat(GetProperties(context, filter).SafeCast<IProperty, IMember>())
				.Concat(GetFields(context, filter).SafeCast<IField, IMember>())
				.Concat(GetEvents(context, filter).SafeCast<IEvent, IMember>());
		}
		
		public override bool Equals(object obj)
		{
			return Equals(obj as IType);
		}
		
		public abstract override int GetHashCode();
		public abstract bool Equals(IType other);
		
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
