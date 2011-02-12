// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Type parameter of a generic class/method.
	/// </summary>
	public class DefaultTypeParameter : AbstractFreezable, ITypeParameter
	{
		public static readonly IList<ITypeParameter> EmptyTypeParameterList = EmptyList<ITypeParameter>.Instance;
		
		readonly string name;
		readonly IMethod method;
		readonly IClass targetClass;
		readonly int index;
		IList<IReturnType> constraints = new List<IReturnType>();
		
		protected override void FreezeInternal()
		{
			constraints = FreezeList(constraints);
			base.FreezeInternal();
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public int Index {
			get {
				return index;
			}
		}
		
		public IMethod Method {
			get {
				return method;
			}
		}
		
		public IClass Class {
			get {
				return targetClass;
			}
		}
		
		public IList<IReturnType> Constraints {
			get {
				return constraints;
			}
		}
		
		public IList<IAttribute> Attributes {
			get {
				return DefaultAttribute.EmptyAttributeList;
			}
		}
		
		bool hasConstructableConstraint = false;
		bool hasReferenceTypeConstraint = false;
		bool hasValueTypeConstraint = false;
		
		/// <summary>
		/// Gets/Sets if the type parameter has the 'new()' constraint.
		/// </summary>
		public bool HasConstructableConstraint {
			get { return hasConstructableConstraint; }
			set {
				CheckBeforeMutation();
				hasConstructableConstraint = value;
			}
		}
		
		/// <summary>
		/// Gets/Sets if the type parameter has the 'class' constraint.
		/// </summary>
		public bool HasReferenceTypeConstraint {
			get { return hasReferenceTypeConstraint; }
			set {
				CheckBeforeMutation();
				hasReferenceTypeConstraint = value;
			}
		}
		
		/// <summary>
		/// Gets/Sets if the type parameter has the 'struct' constraint.
		/// </summary>
		public bool HasValueTypeConstraint {
			get { return hasValueTypeConstraint; }
			set {
				CheckBeforeMutation();
				hasValueTypeConstraint = value;
			}
		}
		
		public DefaultTypeParameter(IMethod method, string name, int index)
		{
			this.method = method;
			this.targetClass = method.DeclaringType;
			this.name = name;
			this.index = index;
		}
		
		public DefaultTypeParameter(IMethod method, Type type)
		{
			this.method = method;
			this.targetClass = method.DeclaringType;
			this.name = type.Name;
			this.index = type.GenericParameterPosition;
		}
		
		public DefaultTypeParameter(IClass targetClass, string name, int index)
		{
			this.targetClass = targetClass;
			this.name = name;
			this.index = index;
		}
		
		public DefaultTypeParameter(IClass targetClass, Type type)
		{
			this.targetClass = targetClass;
			this.name = type.Name;
			this.index = type.GenericParameterPosition;
		}
		
		public override bool Equals(object obj)
		{
			DefaultTypeParameter tp = obj as DefaultTypeParameter;
			if (tp == null) return false;
			if (tp.index != index) return false;
			if (tp.name != name) return false;
			if (tp.hasConstructableConstraint != hasConstructableConstraint) return false;
			if (tp.hasReferenceTypeConstraint != hasReferenceTypeConstraint) return false;
			if (tp.hasValueTypeConstraint != hasValueTypeConstraint) return false;
			if (tp.method != method) {
				if (tp.method == null || method == null) return false;
				if (tp.method.FullyQualifiedName != method.FullyQualifiedName) return false;
			} else {
				if (tp.targetClass.FullyQualifiedName != targetClass.FullyQualifiedName) return false;
			}
			return true;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public override string ToString()
		{
			return String.Format("[{0}: {1}]", GetType().Name, name);
		}
		
		public static DefaultClass GetDummyClassForTypeParameter(ITypeParameter p)
		{
			DefaultClass c = new DefaultClass(p.Class.CompilationUnit, p.Name);
			if (p.Method != null) {
				c.Region = new DomRegion(p.Method.Region.BeginLine, p.Method.Region.BeginColumn);
			} else {
				c.Region = new DomRegion(p.Class.Region.BeginLine, p.Class.Region.BeginColumn);
			}
			c.Modifiers = ModifierEnum.Public;
			if (p.HasValueTypeConstraint) {
				c.ClassType = ClassType.Struct;
			} else if (p.HasConstructableConstraint) {
				c.ClassType = ClassType.Class;
			} else {
				c.ClassType = ClassType.Interface;
			}
			return c;
		}
		
		/// <summary>
		/// Gets the type that was used to bind this type parameter.
		/// This property returns null for generic methods/classes, it
		/// is non-null only for constructed versions of generic methods.
		/// </summary>
		public virtual IReturnType BoundTo {
			get { return null; }
		}
		
		/// <summary>
		/// If this type parameter was bound, returns the unbound version of it.
		/// </summary>
		public virtual ITypeParameter UnboundTypeParameter {
			get { return this; }
		}
	}
}
