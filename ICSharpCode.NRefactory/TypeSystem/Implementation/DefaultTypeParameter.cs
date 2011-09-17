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

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="ITypeParameter"/>.
	/// </summary>
	[Serializable]
	public sealed class DefaultTypeParameter : AbstractFreezable, ITypeParameter, ISupportsInterning
	{
		string name;
		int index;
		IList<ITypeReference> constraints;
		IList<IAttribute> attributes;
		
		DomRegion region;
		
		// Small fields: byte+byte+short
		VarianceModifier variance;
		EntityType ownerType;
		BitVector16 flags;
		
		const ushort FlagReferenceTypeConstraint      = 0x0001;
		const ushort FlagValueTypeConstraint          = 0x0002;
		const ushort FlagDefaultConstructorConstraint = 0x0004;
		
		protected override void FreezeInternal()
		{
			constraints = FreezeList(constraints);
			attributes = FreezeList(attributes);
			base.FreezeInternal();
		}
		
		public DefaultTypeParameter(EntityType ownerType, int index, string name)
		{
			if (!(ownerType == EntityType.TypeDefinition || ownerType == EntityType.Method))
				throw new ArgumentException("owner must be a type or a method", "ownerType");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, "Value must not be negative");
			if (name == null)
				throw new ArgumentNullException("name");
			this.ownerType = ownerType;
			this.index = index;
			this.name = name;
		}
		
		public TypeKind Kind {
			get { return TypeKind.TypeParameter; }
		}
		
		public string Name {
			get { return name; }
		}
		
		string INamedElement.FullName {
			get { return name; }
		}
		
		string INamedElement.Namespace {
			get { return string.Empty; }
		}
		
		public string ReflectionName {
			get {
				if (ownerType == EntityType.Method)
					return "``" + index.ToString();
				else
					return "`" + index.ToString();
			}
		}
		
		public bool? IsReferenceType(ITypeResolveContext context)
		{
			switch (flags.Data & (FlagReferenceTypeConstraint | FlagValueTypeConstraint)) {
				case FlagReferenceTypeConstraint:
					return true;
				case FlagValueTypeConstraint:
					return false;
			}
			// protect against cyclic dependencies between type parameters
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					foreach (ITypeReference constraintRef in this.Constraints) {
						IType constraint = constraintRef.Resolve(context);
						ITypeDefinition constraintDef = constraint.GetDefinition();
						// While interfaces are reference types, an interface constraint does not
						// force the type parameter to be a reference type; so we need to explicitly look for classes here.
						if (constraintDef != null && constraintDef.Kind == TypeKind.Class)
							return true;
						if (constraint is ITypeParameter) {
							bool? isReferenceType = constraint.IsReferenceType(context);
							if (isReferenceType.HasValue)
								return isReferenceType.Value;
						}
					}
				}
			}
			return null;
		}
		
		int IType.TypeParameterCount {
			get { return 0; }
		}
		
		IType IType.DeclaringType {
			get { return null; }
		}
		
		ITypeDefinition IType.GetDefinition()
		{
			return null;
		}
		
		IType ITypeReference.Resolve(ITypeResolveContext context)
		{
			return this;
		}
		
//		public override int GetHashCode()
//		{
//			unchecked {
//				return (int)ownerType * 178256151 + index;
//			}
//		}
//
//		public override bool Equals(object obj)
//		{
//			return Equals(obj as IType);
//		}
		
		public bool Equals(IType other)
		{
			// Use reference equality for type parameters. While we could consider any types with same
			// ownerType + index as equal for the type system, doing so makes it difficult to cache calculation
			// results based on types - e.g. the cache in the Conversions class.
			return this == other;
			// We can still consider type parameters of different methods/classes to be equal to each other,
			// if they have been interned. But then also all constraints are equal, so caching conversions
			// is valid in that case.
		}
		
		public EntityType OwnerType {
			get {
				return ownerType;
			}
		}
		
		public int Index {
			get { return index; }
		}
		
		public IList<IAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IAttribute>();
				return attributes;
			}
		}
		
		public IList<ITypeReference> Constraints {
			get {
				if (constraints == null)
					constraints = new List<ITypeReference>();
				return constraints;
			}
		}
		
		public bool HasDefaultConstructorConstraint {
			get { return flags[FlagDefaultConstructorConstraint]; }
			set {
				CheckBeforeMutation();
				flags[FlagDefaultConstructorConstraint] = value;
			}
		}
		
		public bool HasReferenceTypeConstraint {
			get { return flags[FlagReferenceTypeConstraint]; }
			set {
				CheckBeforeMutation();
				flags[FlagReferenceTypeConstraint] = value;
			}
		}
		
		public bool HasValueTypeConstraint {
			get { return flags[FlagValueTypeConstraint]; }
			set {
				CheckBeforeMutation();
				flags[FlagValueTypeConstraint] = value;
			}
		}
		
		public VarianceModifier Variance {
			get { return variance; }
			set {
				CheckBeforeMutation();
				variance = value;
			}
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeParameter(this);
		}
		
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
		
		static readonly SimpleProjectContent dummyProjectContent = new SimpleProjectContent();
		
		DefaultTypeDefinition GetDummyClassForTypeParameter()
		{
			DefaultTypeDefinition c = new DefaultTypeDefinition(dummyProjectContent, string.Empty, this.Name);
			c.Region = this.Region;
			if (HasValueTypeConstraint) {
				c.Kind = TypeKind.Struct;
			} else if (HasDefaultConstructorConstraint) {
				c.Kind = TypeKind.Class;
			} else {
				c.Kind = TypeKind.Interface;
			}
			return c;
		}
		
		public IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				if (HasDefaultConstructorConstraint || HasValueTypeConstraint) {
					DefaultMethod m = DefaultMethod.CreateDefaultConstructor(GetDummyClassForTypeParameter());
					if (filter(m))
						return new [] { m };
				}
				return EmptyList<IMethod>.Instance;
			} else {
				return GetMembersHelper.GetConstructors(this, context, filter, options);
			}
		}
		
		public IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, typeArguments, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IProperty>.Instance;
			else
				return GetMembersHelper.GetProperties(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IField>.Instance;
			else
				return GetMembersHelper.GetFields(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IEvent>.Instance;
			else
				return GetMembersHelper.GetEvents(this, context, FilterNonStatic(filter), options);
		}
		
		public IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMember>.Instance;
			else
				return GetMembersHelper.GetMembers(this, context, FilterNonStatic(filter), options);
		}
		
		static Predicate<T> FilterNonStatic<T>(Predicate<T> filter) where T : class, IMember
		{
			if (filter == null)
				return member => !member.IsStatic;
			else
				return member => !member.IsStatic && filter(member);
		}
		
		IEnumerable<IType> IType.GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}
		
		IEnumerable<IType> IType.GetNestedTypes(IList<IType> typeArguments, ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}
		
		public IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			bool hasNonInterfaceConstraint = false;
			foreach (ITypeReference constraint in this.Constraints) {
				IType c = constraint.Resolve(context);
				yield return c;
				if (c.Kind != TypeKind.Interface)
					hasNonInterfaceConstraint = true;
			}
			// Do not add the 'System.Object' constraint if there is another constraint with a base class.
			if (HasValueTypeConstraint || !hasNonInterfaceConstraint) {
				IType defaultBaseType = context.GetTypeDefinition("System", HasValueTypeConstraint ? "ValueType" : "Object", 0, StringComparer.Ordinal);
				if (defaultBaseType != null)
					yield return defaultBaseType;
			}
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			// protect against cyclic constraints
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					constraints = provider.InternList(constraints);
					attributes = provider.InternList(attributes);
				}
			}
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				int hashCode = GetHashCode();
				if (name != null)
					hashCode += name.GetHashCode();
				if (attributes != null)
					hashCode += attributes.GetHashCode();
				if (constraints != null)
					hashCode += constraints.GetHashCode();
				hashCode += 771 * flags.Data + 900103 * (int)variance;
				return hashCode;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultTypeParameter o = other as DefaultTypeParameter;
			return o != null
				&& this.attributes == o.attributes
				&& this.constraints == o.constraints
				&& this.name == o.name
				&& this.flags == o.flags
				&& this.ownerType == o.ownerType
				&& this.index == o.index
				&& this.variance == o.variance;
		}
		
		public override string ToString()
		{
			return this.ReflectionName;
		}
	}
}
