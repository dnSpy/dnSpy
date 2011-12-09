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
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public abstract class AbstractTypeParameter : ITypeParameter
	{
		readonly ICompilation compilation;
		readonly EntityType ownerType;
		readonly IEntity owner;
		readonly int index;
		readonly string name;
		readonly IList<IAttribute> attributes;
		readonly DomRegion region;
		readonly VarianceModifier variance;
		
		protected AbstractTypeParameter(IEntity owner, int index, string name, VarianceModifier variance, IList<IAttribute> attributes, DomRegion region)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
			this.owner = owner;
			this.compilation = owner.Compilation;
			this.ownerType = owner.EntityType;
			this.index = index;
			this.name = name ?? ((this.OwnerType == EntityType.Method ? "!!" : "!") + index.ToString());
			this.attributes = attributes ?? EmptyList<IAttribute>.Instance;
			this.region = region;
			this.variance = variance;
		}
		
		protected AbstractTypeParameter(ICompilation compilation, EntityType ownerType, int index, string name, VarianceModifier variance, IList<IAttribute> attributes, DomRegion region)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			this.compilation = compilation;
			this.ownerType = ownerType;
			this.index = index;
			this.name = name ?? ((this.OwnerType == EntityType.Method ? "!!" : "!") + index.ToString());
			this.attributes = attributes ?? EmptyList<IAttribute>.Instance;
			this.region = region;
			this.variance = variance;
		}
		
		public EntityType OwnerType {
			get { return ownerType; }
		}
		
		public IEntity Owner {
			get { return owner; }
		}
		
		public int Index {
			get { return index; }
		}
		
		public IList<IAttribute> Attributes {
			get { return attributes; }
		}
		
		public VarianceModifier Variance {
			get { return variance; }
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public ICompilation Compilation {
			get { return compilation; }
		}
		
		volatile IType effectiveBaseClass;
		
		public IType EffectiveBaseClass {
			get {
				if (effectiveBaseClass == null)
					effectiveBaseClass = CalculateEffectiveBaseClass();
				return effectiveBaseClass;
			}
		}
		
		IType CalculateEffectiveBaseClass()
		{
			// protect against cyclic type parameters
			using (var busyLock = BusyManager.Enter(this)) {
				if (!busyLock.Success)
					return SpecialType.UnknownType;
				
				if (HasValueTypeConstraint)
					return this.Compilation.FindType(KnownTypeCode.ValueType);
				
				List<IType> classTypeConstraints = new List<IType>();
				foreach (IType constraint in this.DirectBaseTypes) {
					if (constraint.Kind == TypeKind.Class) {
						classTypeConstraints.Add(constraint);
					} else if (constraint.Kind == TypeKind.TypeParameter) {
						IType baseClass = ((ITypeParameter)constraint).EffectiveBaseClass;
						if (baseClass.Kind == TypeKind.Class)
							classTypeConstraints.Add(baseClass);
					}
				}
				if (classTypeConstraints.Count == 0)
					return this.Compilation.FindType(KnownTypeCode.Object);
				// Find the derived-most type in the resulting set:
				IType result = classTypeConstraints[0];
				for (int i = 1; i < classTypeConstraints.Count; i++) {
					if (classTypeConstraints[i].GetDefinition().IsDerivedFrom(result.GetDefinition()))
						result = classTypeConstraints[i];
				}
				return result;
			}
		}
		
		public IList<IType> EffectiveInterfaceSet {
			get {
				throw new NotImplementedException();
			}
		}
		
		public abstract bool HasDefaultConstructorConstraint { get; }
		public abstract bool HasReferenceTypeConstraint { get; }
		public abstract bool HasValueTypeConstraint { get; }
		
		public TypeKind Kind {
			get { return TypeKind.TypeParameter; }
		}
		
		public bool? IsReferenceType {
			get {
				if (this.HasValueTypeConstraint)
					return false;
				if (this.HasReferenceTypeConstraint)
					return true;
				
				// A type parameter is known to be a reference type if it has the reference type constraint
				// or its effective base class is not object or System.ValueType.
				IType effectiveBaseClass = this.EffectiveBaseClass;
				if (effectiveBaseClass.Kind == TypeKind.Class || effectiveBaseClass.Kind == TypeKind.Delegate) {
					ITypeDefinition effectiveBaseClassDef = effectiveBaseClass.GetDefinition();
					if (effectiveBaseClassDef != null) {
						switch (effectiveBaseClassDef.KnownTypeCode) {
							case KnownTypeCode.Object:
							case KnownTypeCode.ValueType:
							case KnownTypeCode.Enum:
								return null;
						}
					}
					return true;
				} else if (effectiveBaseClass.Kind == TypeKind.Struct || effectiveBaseClass.Kind == TypeKind.Enum) {
					return false;
				}
				return null;
			}
		}
		
		IType IType.DeclaringType {
			get { return null; }
		}
		
		int IType.TypeParameterCount {
			get { return 0; }
		}
		
		public abstract IEnumerable<IType> DirectBaseTypes { get; }
		
		public string Name {
			get { return name; }
		}
		
		string INamedElement.Namespace {
			get { return string.Empty; }
		}
		
		string INamedElement.FullName {
			get { return name; }
		}
		
		public string ReflectionName {
			get {
				return (this.OwnerType == EntityType.Method ? "``" : "`") + index.ToString();
			}
		}
		
		ITypeDefinition IType.GetDefinition()
		{
			return null;
		}
		
		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeParameter(this);
		}
		
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
		
		public ITypeReference ToTypeReference()
		{
			return new TypeParameterReference(this.OwnerType, this.Index);
		}
		
		IEnumerable<IType> IType.GetNestedTypes(Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}
		
		IEnumerable<IType> IType.GetNestedTypes(IList<IType> typeArguments, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}
		
		static readonly IUnresolvedMethod dummyConstructor = CreateDummyConstructor();
		
		static IUnresolvedMethod CreateDummyConstructor()
		{
			var m = new DefaultUnresolvedMethod {
				EntityType = EntityType.Constructor,
				Name = ".ctor",
				Accessibility = Accessibility.Public,
				IsSynthetic = true,
				ReturnType = KnownTypeReference.Void
			};
			m.Freeze();
			return m;
		}
		
		public IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				if (this.HasDefaultConstructorConstraint || this.HasValueTypeConstraint) {
					if (filter == null || filter(dummyConstructor)) {
						var resolvedCtor = (IMethod)dummyConstructor.CreateResolved(compilation.TypeResolveContext);
						IMethod m = new SpecializedMethod(this, resolvedCtor, EmptyList<IType>.Instance);
						return new [] { m };
					}
				}
				return EmptyList<IMethod>.Instance;
			} else {
				return GetMembersHelper.GetConstructors(this, filter, options);
			}
		}
		
		public IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, typeArguments, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IProperty>.Instance;
			else
				return GetMembersHelper.GetProperties(this, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IField>.Instance;
			else
				return GetMembersHelper.GetFields(this, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IEvent>.Instance;
			else
				return GetMembersHelper.GetEvents(this, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMember>.Instance;
			else
				return GetMembersHelper.GetMembers(this, FilterNonStatic(filter), options);
		}
		
		static Predicate<T> FilterNonStatic<T>(Predicate<T> filter) where T : class, IUnresolvedMember
		{
			if (filter == null)
				return member => !member.IsStatic;
			else
				return member => !member.IsStatic && filter(member);
		}
		
		public sealed override bool Equals(object obj)
		{
			return Equals(obj as IType);
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public virtual bool Equals(IType other)
		{
			return this == other; // use reference equality for type parameters
		}
		
		public override string ToString()
		{
			return this.ReflectionName;
		}
	}
}
