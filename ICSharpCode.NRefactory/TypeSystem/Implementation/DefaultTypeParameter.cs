// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="ITypeParameter"/>.
	/// </summary>
	public class DefaultTypeParameter : AbstractFreezable, ITypeParameter, ISupportsInterning
	{
		IEntity parent;
		
		string name;
		int index;
		IList<ITypeReference> constraints;
		IList<IAttribute> attributes;
		VarianceModifier variance;
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
		
		public DefaultTypeParameter(IMethod parentMethod, int index, string name)
		{
			if (parentMethod == null)
				throw new ArgumentNullException("parentMethod");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, "Value must not be negative");
			if (name == null)
				throw new ArgumentNullException("name");
			this.parent = parentMethod;
			this.index = index;
			this.name = name;
		}
		
		public DefaultTypeParameter(ITypeDefinition parentClass, int index, string name)
		{
			if (parentClass == null)
				throw new ArgumentNullException("parentClass");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, "Value must not be negative");
			if (name == null)
				throw new ArgumentNullException("name");
			this.parent = parentClass;
			this.index = index;
			this.name = name;
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
				if (parent is IMethod)
					return "``" + index.ToString();
				else
					return "`" + index.ToString();
			}
		}
		
		public bool? IsReferenceType {
			get {
				switch (flags.Data & (FlagReferenceTypeConstraint | FlagValueTypeConstraint)) {
					case FlagReferenceTypeConstraint:
						return true;
					case FlagValueTypeConstraint:
						return false;
					default:
						return null;
				}
			}
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
		
		public override int GetHashCode()
		{
			int hashCode = parent.GetHashCode();
			unchecked {
				hashCode += 1000000033 * index.GetHashCode();
			}
			return hashCode;
		}
		
		public override bool Equals(object obj)
		{
			return Equals(obj as IType);
		}
		
		public bool Equals(IType other)
		{
			DefaultTypeParameter p = other as DefaultTypeParameter;
			if (p == null)
				return false;
			return parent.Equals(p.parent)
				&& index == p.index;
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
		
		public IEntity Parent {
			get { return parent; }
		}
		
		public IMethod ParentMethod {
			get { return parent as IMethod; }
		}
		
		public ITypeDefinition ParentClass {
			get { return parent as ITypeDefinition; }
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
		
		public virtual IType BoundTo {
			get { return null; }
		}
		
		public virtual ITypeParameter UnboundTypeParameter {
			get { return null; }
		}
		
		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeParameter(this);
		}
		
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
		
		DefaultTypeDefinition GetDummyClassForTypeParameter()
		{
			DefaultTypeDefinition c = new DefaultTypeDefinition(ParentClass ?? ParentMethod.DeclaringTypeDefinition, this.Name);
			c.Region = new DomRegion(parent.Region.FileName, parent.Region.BeginLine, parent.Region.BeginColumn);
			if (HasValueTypeConstraint) {
				c.ClassType = ClassType.Struct;
			} else if (HasDefaultConstructorConstraint) {
				c.ClassType = ClassType.Class;
			} else {
				c.ClassType = ClassType.Interface;
			}
			return c;
		}
		
		public IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null)
		{
			if (HasDefaultConstructorConstraint || HasValueTypeConstraint) {
				DefaultMethod m = DefaultMethod.CreateDefaultConstructor(GetDummyClassForTypeParameter());
				if (filter(m))
					return new [] { m };
			}
			return EmptyList<IMethod>.Instance;
		}
		
		public IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null)
		{
			// TODO: get methods from constraints
			IType objectType = context.GetClass("System", "Object", 0, StringComparer.Ordinal);
			IEnumerable<IMethod> objectMethods;
			if (objectType != null)
				objectMethods = objectType.GetMethods(context, filter);
			else
				objectMethods = EmptyList<IMethod>.Instance;
			
			// don't return static methods (those cannot be called from type parameter)
			return objectMethods.Where(m => !m.IsStatic);
		}
		
		public IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null)
		{
			return EmptyList<IProperty>.Instance;
		}
		
		public IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null)
		{
			return EmptyList<IField>.Instance;
		}
		
		public IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null)
		{
			return EmptyList<IEvent>.Instance;
		}
		
		IEnumerable<IType> IType.GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter)
		{
			return EmptyList<IType>.Instance;
		}
		
		public IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			IType defaultBaseType = context.GetClass("System", HasValueTypeConstraint ? "ValueType" : "Object", 0, StringComparer.Ordinal);
			if (defaultBaseType != null)
				yield return defaultBaseType;
			
			foreach (ITypeReference constraint in this.Constraints) {
				yield return constraint.Resolve(context);
			}
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			constraints = provider.InternList(constraints);
			attributes = provider.InternList(attributes);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return GetHashCode();
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			return this == other;
		}
		
		public override string ToString()
		{
			return this.ReflectionName;
		}
	}
}
