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
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a SpecializedMember (a member on which type substitution has been performed).
	/// </summary>
	public abstract class SpecializedMember : IMember
	{
		readonly IType declaringType;
		readonly IMember memberDefinition;
		IType returnType;
		
		protected SpecializedMember(IType declaringType, IMember memberDefinition)
		{
			if (declaringType == null)
				throw new ArgumentNullException("declaringType");
			if (memberDefinition == null)
				throw new ArgumentNullException("memberDefinition");
			
			this.declaringType = declaringType;
			this.memberDefinition = memberDefinition;
		}
		
		protected virtual void Initialize(TypeVisitor substitution)
		{
			this.returnType = Substitute(memberDefinition.ReturnType, substitution);
		}
		
		public virtual IMemberReference ToMemberReference()
		{
			return new SpecializingMemberReference(declaringType.ToTypeReference(), memberDefinition.ToMemberReference());
		}
		
		internal static TypeVisitor GetSubstitution(IType declaringType)
		{
			ParameterizedType pt = declaringType as ParameterizedType;
			if (pt != null)
				return pt.GetSubstitution();
			else
				return null;
		}
		
		internal static IType Substitute(IType type, TypeVisitor substitution)
		{
			if (substitution == null)
				return type;
			else
				return type.AcceptVisitor(substitution);
		}
		
		internal IMethod WrapAccessor(IMethod accessorDefinition)
		{
			if (accessorDefinition == null)
				return null;
			else
				return new SpecializedMethod(declaringType, accessorDefinition);
		}
		
		public IType DeclaringType {
			get { return declaringType; }
		}
		
		public IMember MemberDefinition {
			get { return memberDefinition.MemberDefinition; }
		}
		
		public IUnresolvedMember UnresolvedMember {
			get { return memberDefinition.UnresolvedMember; }
		}
		
		public IType ReturnType {
			get { return returnType; }
			protected set { returnType = value; }
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
		
		IList<IMember> interfaceImplementations;
		
		public IList<IMember> InterfaceImplementations {
			get {
				return LazyInitializer.EnsureInitialized(ref interfaceImplementations, FindInterfaceImplementations);
			}
		}
		
		IList<IMember> FindInterfaceImplementations()
		{
			var definitionImplementations = memberDefinition.InterfaceImplementations;
			IMember[] result = new IMember[definitionImplementations.Count];
			for (int i = 0; i < result.Length; i++) {
				result[i] = Specialize(definitionImplementations[i]);
			}
			return result;
		}
		
		/// <summary>
		/// Specialize another member using the same type arguments as this member.
		/// </summary>
		protected virtual IMember Specialize(IMember otherMember)
		{
			return SpecializingMemberReference.CreateSpecializedMember(declaringType, memberDefinition, null);
		}
		
		public bool IsExplicitInterfaceImplementation {
			get { return memberDefinition.IsExplicitInterfaceImplementation; }
		}
		
		public string Documentation {
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
			return this.declaringType.Equals(other.declaringType) && this.memberDefinition.Equals(other.memberDefinition);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return 1000000007 * declaringType.GetHashCode() + 1000000009 * memberDefinition.GetHashCode();
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			b.Append(declaringType.ToString());
			b.Append('.');
			b.Append(this.Name);
			b.Append(':');
			b.Append(returnType.ToString());
			b.Append(']');
			return b.ToString();
		}
	}
	
	public abstract class SpecializedParameterizedMember : SpecializedMember, IParameterizedMember
	{
		IList<IParameter> parameters;
		
		protected SpecializedParameterizedMember(IType declaringType, IParameterizedMember memberDefinition)
			: base(declaringType, memberDefinition)
		{
		}
		
		protected override void Initialize(TypeVisitor substitution)
		{
			base.Initialize(substitution);
			
			var paramDefs = ((IParameterizedMember)this.MemberDefinition).Parameters;
			if (paramDefs.Count == 0) {
				this.parameters = EmptyList<IParameter>.Instance;
			} else {
				var parameters = new IParameter[paramDefs.Count];
				for (int i = 0; i < parameters.Length; i++) {
					IType newType = Substitute(paramDefs[i].Type, substitution);
					if (newType != paramDefs[i].Type) {
						parameters[i] = new SpecializedParameter(paramDefs[i], newType);
					} else {
						parameters[i] = paramDefs[i];
					}
				}
				this.parameters = Array.AsReadOnly(parameters);
			}
		}
		
		public IList<IParameter> Parameters {
			get { return parameters; }
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
			for (int i = 0; i < parameters.Count; i++) {
				if (i > 0) b.Append(", ");
				b.Append(parameters[i].ToString());
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
