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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a SpecializedMember (a member on which type substitution has been performed).
	/// </summary>
	public abstract class SpecializedMember : IMember
	{
		readonly IMember memberDefinition;
		TypeParameterSubstitution substitution;
		
		IType declaringType;
		IType returnType;
		
		protected SpecializedMember(IMember memberDefinition)
		{
			if (memberDefinition == null)
				throw new ArgumentNullException("memberDefinition");
			
			SpecializedMember sm = memberDefinition as SpecializedMember;
			if (sm != null) {
				this.memberDefinition = sm.memberDefinition;
				this.substitution = sm.substitution;
			} else {
				this.memberDefinition = memberDefinition;
				this.substitution = TypeParameterSubstitution.Identity;
			}
		}
		
		/// <summary>
		/// Performs a substitution. This method may only be called by constructors in derived classes.
		/// </summary>
		protected void AddSubstitution(TypeParameterSubstitution newSubstitution)
		{
			Debug.Assert(declaringType == null);
			Debug.Assert(returnType == null);
			this.substitution = TypeParameterSubstitution.Compose(newSubstitution, this.substitution);
		}
		
		public static SpecializedMember Create(IMember memberDefinition, TypeParameterSubstitution substitution)
		{
			if (memberDefinition == null) {
				return null;
			} else if (memberDefinition is IMethod) {
				return new SpecializedMethod((IMethod)memberDefinition, substitution);
			} else if (memberDefinition is IProperty) {
				return new SpecializedProperty((IProperty)memberDefinition, substitution);
			} else if (memberDefinition is IField) {
				return new SpecializedField((IField)memberDefinition, substitution);
			} else if (memberDefinition is IEvent) {
				return new SpecializedEvent((IEvent)memberDefinition, substitution);
			} else {
				throw new NotSupportedException("Unknown IMember: " + memberDefinition);
			}
		}
		
		public IMemberReference ToMemberReference()
		{
			return new SpecializingMemberReference(
				memberDefinition.ToMemberReference(),
				ToTypeReference(substitution.ClassTypeArguments),
				ToTypeReference(substitution.MethodTypeArguments));
		}
		
		static IList<ITypeReference> ToTypeReference(IList<IType> typeArguments)
		{
			if (typeArguments == null)
				return null;
			else
				return typeArguments.Select(t => t.ToTypeReference()).ToArray();
		}
		
		internal IMethod WrapAccessor(ref IMethod cachingField, IMethod accessorDefinition)
		{
			if (accessorDefinition == null)
				return null;
			var result = LazyInit.VolatileRead(ref cachingField);
			if (result != null)
				return result;
			else
				return LazyInit.GetOrSet(ref cachingField, new SpecializedMethod(accessorDefinition, substitution));
		}
		
		/// <summary>
		/// Gets the substitution belonging to this specialized member.
		/// </summary>
		public TypeParameterSubstitution Substitution {
			get { return substitution; }
		}
		
		public IType DeclaringType {
			get {
				var result = LazyInit.VolatileRead(ref this.declaringType);
				if (result != null)
					return result;
				IType definitionDeclaringType = memberDefinition.DeclaringType;
				ITypeDefinition definitionDeclaringTypeDef = definitionDeclaringType as ITypeDefinition;
				if (definitionDeclaringTypeDef != null && definitionDeclaringType.TypeParameterCount > 0) {
					if (substitution.ClassTypeArguments != null && substitution.ClassTypeArguments.Count == definitionDeclaringType.TypeParameterCount) {
						result = new ParameterizedType(definitionDeclaringTypeDef, substitution.ClassTypeArguments);
					} else {
						result = new ParameterizedType(definitionDeclaringTypeDef, definitionDeclaringTypeDef.TypeParameters).AcceptVisitor(substitution);
					}
				} else {
					result = definitionDeclaringType.AcceptVisitor(substitution);
				}
				return LazyInit.GetOrSet(ref this.declaringType, result);
			}
			internal set {
				// This setter is used as an optimization when the code constructing
				// the SpecializedMember already knows the declaring type.
				Debug.Assert(this.declaringType == null);
				Debug.Assert(value != null);
				// As this setter is used only during construction before the member is published
				// to other threads, we don't need a volatile write.
				this.declaringType = value;
			}
		}
		
		public IMember MemberDefinition {
			get { return memberDefinition.MemberDefinition; }
		}
		
		public IUnresolvedMember UnresolvedMember {
			get { return memberDefinition.UnresolvedMember; }
		}
		
		public IType ReturnType {
			get {
				var result = LazyInit.VolatileRead(ref this.returnType);
				if (result != null)
					return result;
				else
					return LazyInit.GetOrSet(ref this.returnType, memberDefinition.ReturnType.AcceptVisitor(substitution));
			}
			protected set {
				// This setter is used for LiftedUserDefinedOperator, a special case of specialized member
				// (not a normal type parameter substitution).
				
				// As this setter is used only during construction before the member is published
				// to other threads, we don't need a volatile write.
				this.returnType = value;
			}
		}
		
		public bool IsVirtual {
			get { return memberDefinition.IsVirtual; }
		}
		
		public bool IsOverride {
			get { return memberDefinition.IsOverride; }
		}
		
		public bool IsOverridable {
			get { return memberDefinition.IsOverridable; }
		}
		
		public EntityType EntityType {
			get { return memberDefinition.EntityType; }
		}
		
		public DomRegion Region {
			get { return memberDefinition.Region; }
		}
		
		public DomRegion BodyRegion {
			get { return memberDefinition.BodyRegion; }
		}
		
		public ITypeDefinition DeclaringTypeDefinition {
			get { return memberDefinition.DeclaringTypeDefinition; }
		}
		
		public IList<IAttribute> Attributes {
			get { return memberDefinition.Attributes; }
		}
		
		IList<IMember> implementedInterfaceMembers;
		
		public IList<IMember> ImplementedInterfaceMembers {
			get {
				return LazyInitializer.EnsureInitialized(ref implementedInterfaceMembers, FindImplementedInterfaceMembers);
			}
		}
		
		IList<IMember> FindImplementedInterfaceMembers()
		{
			var definitionImplementations = memberDefinition.ImplementedInterfaceMembers;
			IMember[] result = new IMember[definitionImplementations.Count];
			for (int i = 0; i < result.Length; i++) {
				result[i] = SpecializedMember.Create(definitionImplementations[i], substitution);
			}
			return result;
		}
		
		public bool IsExplicitInterfaceImplementation {
			get { return memberDefinition.IsExplicitInterfaceImplementation; }
		}
		
		public DocumentationComment Documentation {
			get { return memberDefinition.Documentation; }
		}
		
		public Accessibility Accessibility {
			get { return memberDefinition.Accessibility; }
		}
		
		public bool IsStatic {
			get { return memberDefinition.IsStatic; }
		}
		
		public bool IsAbstract {
			get { return memberDefinition.IsAbstract; }
		}
		
		public bool IsSealed {
			get { return memberDefinition.IsSealed; }
		}
		
		public bool IsShadowing {
			get { return memberDefinition.IsShadowing; }
		}
		
		public bool IsSynthetic {
			get { return memberDefinition.IsSynthetic; }
		}
		
		public bool IsPrivate {
			get { return memberDefinition.IsPrivate; }
		}
		
		public bool IsPublic {
			get { return memberDefinition.IsPublic; }
		}
		
		public bool IsProtected {
			get { return memberDefinition.IsProtected; }
		}
		
		public bool IsInternal {
			get { return memberDefinition.IsInternal; }
		}
		
		public bool IsProtectedOrInternal {
			get { return memberDefinition.IsProtectedOrInternal; }
		}
		
		public bool IsProtectedAndInternal {
			get { return memberDefinition.IsProtectedAndInternal; }
		}
		
		public string FullName {
			get { return memberDefinition.FullName; }
		}
		
		public string Name {
			get { return memberDefinition.Name; }
		}
		
		public string Namespace {
			get { return memberDefinition.Namespace; }
		}
		
		public string ReflectionName {
			get { return memberDefinition.ReflectionName; }
		}
		
		public ICompilation Compilation {
			get { return memberDefinition.Compilation; }
		}
		
		public IAssembly ParentAssembly {
			get { return memberDefinition.ParentAssembly; }
		}
		
		public override bool Equals(object obj)
		{
			SpecializedMember other = obj as SpecializedMember;
			if (other == null)
				return false;
			return this.memberDefinition.Equals(other.memberDefinition) && this.substitution.Equals(other.substitution);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return 1000000007 * memberDefinition.GetHashCode() + 1000000009 * substitution.GetHashCode();
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			b.Append(this.DeclaringType.ToString());
			b.Append('.');
			b.Append(this.Name);
			b.Append(':');
			b.Append(this.ReturnType.ToString());
			b.Append(']');
			return b.ToString();
		}
	}
	
	public abstract class SpecializedParameterizedMember : SpecializedMember, IParameterizedMember
	{
		IList<IParameter> parameters;
		
		protected SpecializedParameterizedMember(IParameterizedMember memberDefinition)
			: base(memberDefinition)
		{
		}
		
		public IList<IParameter> Parameters {
			get {
				var result = LazyInit.VolatileRead(ref this.parameters);
				if (result != null)
					return result;
				else
					return LazyInit.GetOrSet(ref this.parameters, CreateParameters(this.Substitution));
			}
			protected set {
				// This setter is used for LiftedUserDefinedOperator, a special case of specialized member
				// (not a normal type parameter substitution).
				
				// As this setter is used only during construction before the member is published
				// to other threads, we don't need a volatile write.
				this.parameters = value;
			}
		}
		
		protected IList<IParameter> CreateParameters(TypeVisitor substitution)
		{
			var paramDefs = ((IParameterizedMember)this.MemberDefinition).Parameters;
			if (paramDefs.Count == 0) {
				return EmptyList<IParameter>.Instance;
			} else {
				var parameters = new IParameter[paramDefs.Count];
				for (int i = 0; i < parameters.Length; i++) {
					IType newType = paramDefs[i].Type.AcceptVisitor(substitution);
					if (newType != paramDefs[i].Type) {
						parameters[i] = new SpecializedParameter(paramDefs[i], newType);
					} else {
						parameters[i] = paramDefs[i];
					}
				}
				return Array.AsReadOnly(parameters);
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			b.Append(this.DeclaringType.ToString());
			b.Append('.');
			b.Append(this.Name);
			b.Append('(');
			for (int i = 0; i < this.Parameters.Count; i++) {
				if (i > 0) b.Append(", ");
				b.Append(this.Parameters[i].ToString());
			}
			b.Append("):");
			b.Append(this.ReturnType.ToString());
			b.Append(']');
			return b.ToString();
		}
		
		sealed class SpecializedParameter : IParameter
		{
			readonly IParameter originalParameter;
			readonly IType newType;
			
			public SpecializedParameter(IParameter originalParameter, IType newType)
			{
				if (originalParameter is SpecializedParameter)
					this.originalParameter = ((SpecializedParameter)originalParameter).originalParameter;
				else
					this.originalParameter = originalParameter;
				this.newType = newType;
			}
			
			public IList<IAttribute> Attributes {
				get { return originalParameter.Attributes; }
			}
			
			public bool IsRef {
				get { return originalParameter.IsRef; }
			}
			
			public bool IsOut {
				get { return originalParameter.IsOut; }
			}
			
			public bool IsParams {
				get { return originalParameter.IsParams; }
			}
			
			public bool IsOptional {
				get { return originalParameter.IsOptional; }
			}
			
			public string Name {
				get { return originalParameter.Name; }
			}
			
			public DomRegion Region {
				get { return originalParameter.Region; }
			}
			
			public IType Type {
				get { return newType; }
			}
			
			public bool IsConst {
				get { return originalParameter.IsConst; }
			}
			
			public object ConstantValue {
				get { return originalParameter.ConstantValue; }
			}
			
			public override string ToString()
			{
				return DefaultParameter.ToString(this);
			}
		}
	}
}
