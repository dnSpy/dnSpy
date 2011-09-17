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
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a SpecializedMember (a member on which type substitution has been performed).
	/// </summary>
	public abstract class SpecializedMember : IMember
	{
		readonly IType declaringType;
		readonly IMember memberDefinition;
		readonly ITypeReference returnType;
		
		protected SpecializedMember(IType declaringType, IMember memberDefinition)
			: this(declaringType, memberDefinition, GetSubstitution(declaringType), null)
		{
		}
		
		internal SpecializedMember(IType declaringType, IMember memberDefinition, TypeVisitor substitution, ITypeResolveContext context)
		{
			if (declaringType == null)
				throw new ArgumentNullException("declaringType");
			if (memberDefinition == null)
				throw new ArgumentNullException("memberDefinition");
			
			this.declaringType = declaringType;
			this.memberDefinition = memberDefinition;
			this.returnType = Substitute(memberDefinition.ReturnType, substitution, context);
		}
		
		internal static TypeVisitor GetSubstitution(IType declaringType)
		{
			ParameterizedType pt = declaringType as ParameterizedType;
			if (pt != null)
				return pt.GetSubstitution();
			else
				return null;
		}
		
		internal static ITypeReference Substitute(ITypeReference type, TypeVisitor substitution, ITypeResolveContext context)
		{
			if (substitution == null)
				return type;
			if (context != null)
				return type.Resolve(context).AcceptVisitor(substitution);
			else
				return SubstitutionTypeReference.Create(type, substitution);
		}
		
		public IType DeclaringType {
			get { return declaringType; }
		}
		
		public IMember MemberDefinition {
			get { return memberDefinition; }
		}
		
		public ITypeReference ReturnType {
			get { return returnType; }
		}
		
		public IList<IExplicitInterfaceImplementation> InterfaceImplementations {
			get { return memberDefinition.InterfaceImplementations; }
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
		
		public IProjectContent ProjectContent {
			get { return memberDefinition.ProjectContent; }
		}
		
		public IParsedFile ParsedFile {
			get { return memberDefinition.ParsedFile; }
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
		
		public bool IsFrozen {
			get { return memberDefinition.IsFrozen; }
		}
		
		void IFreezable.Freeze()
		{
			if (!memberDefinition.IsFrozen)
				throw new NotSupportedException();
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
		readonly IList<IParameter> parameters;
		
		protected SpecializedParameterizedMember(IType declaringType, IParameterizedMember memberDefinition)
			: this(declaringType, memberDefinition, GetSubstitution(declaringType), null)
		{
		}
		
		internal SpecializedParameterizedMember(IType declaringType, IParameterizedMember memberDefinition, TypeVisitor substitution, ITypeResolveContext context)
			: base(declaringType, memberDefinition, substitution, context)
		{
			var paramDefs = memberDefinition.Parameters;
			if (paramDefs.Count == 0) {
				this.parameters = EmptyList<IParameter>.Instance;
			} else {
				var parameters = new IParameter[paramDefs.Count];
				for (int i = 0; i < parameters.Length; i++) {
					ITypeReference newType = Substitute(paramDefs[i].Type, substitution, context);
					if (newType != paramDefs[i].Type) {
						parameters[i] = new DefaultParameter(paramDefs[i]) { Type = newType };
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
	}
}
