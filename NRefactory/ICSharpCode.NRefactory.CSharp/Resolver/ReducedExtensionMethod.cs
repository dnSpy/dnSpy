//
// ReducedExtensionMethod.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// An invocated extension method hides the extension parameter in its parameter list.
	/// It's used to hide the internals of extension method invocation in certain situation to simulate the
	/// syntactic way of writing extension methods on semantic level.
	/// </summary>
	public class ReducedExtensionMethod : IMethod
	{
		readonly IMethod baseMethod;

		public ReducedExtensionMethod(IMethod baseMethod)
		{
			this.baseMethod = baseMethod;
		}

		public override bool Equals(object obj)
		{
			var other = obj as ReducedExtensionMethod;
			if (other == null)
				return false;
			return baseMethod.Equals(other.baseMethod);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return baseMethod.GetHashCode() + 1;
			}
		}

		public override string ToString()
		{
			return string.Format("[ReducedExtensionMethod: ReducedFrom={0}]", ReducedFrom);
		}

		#region IMember implementation

		[Serializable]
		public sealed class ReducedExtensionMethodMemberReference : IMemberReference
		{
			readonly IMethod baseMethod;

			public ReducedExtensionMethodMemberReference (IMethod baseMethod)
			{
				this.baseMethod = baseMethod;
			}

			public IMember Resolve(ITypeResolveContext context)
			{
				return new ReducedExtensionMethod ((IMethod)baseMethod.ToReference ().Resolve (context));
			}
			
			ISymbol ISymbolReference.Resolve(ITypeResolveContext context)
			{
				return Resolve(context);
			}

			public ITypeReference DeclaringTypeReference {
				get {
					return baseMethod.ToReference ().DeclaringTypeReference;
				}
			}
		}

		public IMemberReference ToMemberReference()
		{
			return new ReducedExtensionMethodMemberReference (baseMethod);
		}
		
		public IMemberReference ToReference()
		{
			return new ReducedExtensionMethodMemberReference (baseMethod);
		}
		
		ISymbolReference ISymbol.ToReference()
		{
			return ToReference();
		}

		public IMember MemberDefinition {
			get {
				return baseMethod.MemberDefinition;
			}
		}

		public IUnresolvedMember UnresolvedMember {
			get {
				return baseMethod.UnresolvedMember;
			}
		}

		public IType ReturnType {
			get {
				return baseMethod.ReturnType;
			}
		}

		public System.Collections.Generic.IList<IMember> ImplementedInterfaceMembers {
			get {
				return baseMethod.ImplementedInterfaceMembers;
			}
		}

		public bool IsExplicitInterfaceImplementation {
			get {
				return baseMethod.IsExplicitInterfaceImplementation;
			}
		}

		public bool IsVirtual {
			get {
				return baseMethod.IsVirtual;
			}
		}

		public bool IsOverride {
			get {
				return baseMethod.IsOverride;
			}
		}

		public bool IsOverridable {
			get {
				return baseMethod.IsOverridable;
			}
		}

		public TypeParameterSubstitution Substitution {
			get {
				return baseMethod.Substitution;
			}
		}

		public IMethod Specialize(TypeParameterSubstitution substitution)
		{
			return new ReducedExtensionMethod((IMethod)baseMethod.Specialize(substitution));
		}
		
		IMember IMember.Specialize(TypeParameterSubstitution substitution)
		{
			return Specialize(substitution);
		}
		
		public bool IsParameterized {
			get  { return baseMethod.IsParameterized; }
		}

		#endregion

		#region IMethod implementation

		public System.Collections.Generic.IList<IUnresolvedMethod> Parts {
			get {
				return baseMethod.Parts;
			}
		}

		public System.Collections.Generic.IList<IAttribute> ReturnTypeAttributes {
			get {
				return baseMethod.ReturnTypeAttributes;
			}
		}

		public System.Collections.Generic.IList<ITypeParameter> TypeParameters {
			get {
				return baseMethod.TypeParameters;
			}
		}

		public bool IsExtensionMethod {
			get {
				return true;
			}
		}

		public bool IsConstructor {
			get {
				return baseMethod.IsConstructor;
			}
		}

		public bool IsDestructor {
			get {
				return baseMethod.IsDestructor;
			}
		}

		public bool IsOperator {
			get {
				return baseMethod.IsOperator;
			}
		}

		public bool IsPartial {
			get {
				return baseMethod.IsPartial;
			}
		}

		public bool IsAsync {
			get {
				return baseMethod.IsAsync;
			}
		}

		public bool HasBody {
			get {
				return baseMethod.HasBody;
			}
		}

		public bool IsAccessor {
			get {
				return baseMethod.IsAccessor;
			}
		}

		public IMember AccessorOwner {
			get {
				return baseMethod.AccessorOwner;
			}
		}

		public IMethod ReducedFrom {
			get {
				return baseMethod;
			}
		}

		public IList<IType> TypeArguments {
			get {
				return baseMethod.TypeArguments;
			}
		}
		#endregion

		#region IParameterizedMember implementation
		List<IParameter> parameters;
		public System.Collections.Generic.IList<IParameter> Parameters {
			get {
				if (parameters == null)
					parameters = new List<IParameter> (baseMethod.Parameters.Skip (1));
				return parameters;
			}
		}

		#endregion

		#region IEntity implementation

		public SymbolKind SymbolKind {
			get {
				return baseMethod.SymbolKind;
			}
		}

		[Obsolete("Use the SymbolKind property instead.")]
		public EntityType EntityType {
			get {
				return baseMethod.EntityType;
			}
		}
		
		public DomRegion Region {
			get {
				return baseMethod.Region;
			}
		}

		public DomRegion BodyRegion {
			get {
				return baseMethod.BodyRegion;
			}
		}

		public ITypeDefinition DeclaringTypeDefinition {
			get {
				return baseMethod.DeclaringTypeDefinition;
			}
		}

		public IType DeclaringType {
			get {
				return baseMethod.DeclaringType;
			}
		}

		public IAssembly ParentAssembly {
			get {
				return baseMethod.ParentAssembly;
			}
		}

		public System.Collections.Generic.IList<IAttribute> Attributes {
			get {
				return baseMethod.Attributes;
			}
		}

		public ICSharpCode.NRefactory.Documentation.DocumentationComment Documentation {
			get {
				return baseMethod.Documentation;
			}
		}

		public bool IsStatic {
			get {
				return false;
			}
		}

		public bool IsAbstract {
			get {
				return baseMethod.IsAbstract;
			}
		}

		public bool IsSealed {
			get {
				return baseMethod.IsSealed;
			}
		}

		public bool IsShadowing {
			get {
				return baseMethod.IsShadowing;
			}
		}

		public bool IsSynthetic {
			get {
				return baseMethod.IsSynthetic;
			}
		}

		#endregion

		#region IHasAccessibility implementation

		public Accessibility Accessibility {
			get {
				return baseMethod.Accessibility;
			}
		}

		public bool IsPrivate {
			get {
				return baseMethod.IsPrivate;
			}
		}

		public bool IsPublic {
			get {
				return baseMethod.IsPublic;
			}
		}

		public bool IsProtected {
			get {
				return baseMethod.IsProtected;
			}
		}

		public bool IsInternal {
			get {
				return baseMethod.IsInternal;
			}
		}

		public bool IsProtectedOrInternal {
			get {
				return baseMethod.IsProtectedOrInternal;
			}
		}

		public bool IsProtectedAndInternal {
			get {
				return baseMethod.IsProtectedAndInternal;
			}
		}

		#endregion

		#region INamedElement implementation

		public string FullName {
			get {
				return baseMethod.FullName;
			}
		}

		public string Name {
			get {
				return baseMethod.Name;
			}
		}

		public string ReflectionName {
			get {
				return baseMethod.ReflectionName;
			}
		}

		public string Namespace {
			get {
				return baseMethod.Namespace;
			}
		}

		#endregion

		#region ICompilationProvider implementation

		public ICompilation Compilation {
			get {
				return baseMethod.Compilation;
			}
		}

		#endregion
	}
}

