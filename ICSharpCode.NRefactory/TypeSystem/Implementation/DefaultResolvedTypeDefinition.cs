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
using System.Collections.ObjectModel;
using System.Linq;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="ITypeDefinition"/>.
	/// </summary>
	public class DefaultResolvedTypeDefinition : ITypeDefinition
	{
		readonly ITypeResolveContext parentContext;
		readonly IUnresolvedTypeDefinition[] parts;
		Accessibility accessibility = Accessibility.Internal;
		List<IUnresolvedMember> unresolvedMembers = new List<IUnresolvedMember>();
		ProjectedListWithContextPerElement<ITypeResolveContext, IUnresolvedMember, IMember> resolvedMembers;
		bool isAbstract, isSealed, isShadowing;
		bool isSynthetic = true; // true if all parts are synthetic
		
		public DefaultResolvedTypeDefinition(ITypeResolveContext parentContext, params IUnresolvedTypeDefinition[] parts)
		{
			if (parentContext == null || parentContext.CurrentAssembly == null)
				throw new ArgumentException("Parent context does not specify any assembly", "parentContext");
			if (parts == null || parts.Length == 0)
				throw new ArgumentException("No parts were specified", "parts");
			this.parentContext = parentContext;
			this.parts = parts;
			ITypeResolveContext contextForTypeParameters = parts[0].CreateResolveContext(parentContext);
			contextForTypeParameters = contextForTypeParameters.WithCurrentTypeDefinition(this);
			if (parentContext.CurrentTypeDefinition == null || parentContext.CurrentTypeDefinition.TypeParameterCount == 0) {
				this.TypeParameters = parts[0].TypeParameters.CreateResolvedTypeParameters(contextForTypeParameters);
			} else {
				// This is a nested class inside a generic class; copy type parameters from outer class if we can:
				var outerClass = parentContext.CurrentTypeDefinition;
				ITypeParameter[] typeParameters = new ITypeParameter[parts[0].TypeParameters.Count];
				for (int i = 0; i < typeParameters.Length; i++) {
					var unresolvedTP = parts[0].TypeParameters[i];
					if (i < outerClass.TypeParameterCount && outerClass.TypeParameters[i].Name == unresolvedTP.Name)
						typeParameters[i] = outerClass.TypeParameters[i];
					else
						typeParameters[i] = unresolvedTP.CreateResolvedTypeParameter(contextForTypeParameters);
				}
				this.TypeParameters = Array.AsReadOnly(typeParameters);
			}
			List<IUnresolvedAttribute> unresolvedAttributes = new List<IUnresolvedAttribute>();
			List<ITypeResolveContext> contextPerAttribute = new List<ITypeResolveContext>();
			List<ITypeResolveContext> contextPerMember = new List<ITypeResolveContext>();
			bool addDefaultConstructorIfRequired = false;
			foreach (IUnresolvedTypeDefinition part in parts) {
				ITypeResolveContext parentContextForPart = part.CreateResolveContext(parentContext);
				ITypeResolveContext contextForPart = parentContextForPart.WithCurrentTypeDefinition(this);
				foreach (var attr in part.Attributes) {
					unresolvedAttributes.Add(attr);
					contextPerAttribute.Add(parentContextForPart);
				}
				foreach (var member in part.Members) {
					unresolvedMembers.Add(member);
					contextPerMember.Add(contextForPart);
				}
				
				isAbstract  |= part.IsAbstract;
				isSealed    |= part.IsSealed;
				isShadowing |= part.IsShadowing;
				isSynthetic &= part.IsSynthetic; // true if all parts are synthetic
				
				DefaultUnresolvedTypeDefinition dutd = part as DefaultUnresolvedTypeDefinition;
				if (dutd != null) {
					addDefaultConstructorIfRequired |= dutd.AddDefaultConstructorIfRequired;
				}
				
				// internal is the default, so use another part's accessibility until we find a non-internal accessibility
				if (accessibility == Accessibility.Internal)
					accessibility = part.Accessibility;
			}
			if (addDefaultConstructorIfRequired) {
				TypeKind kind = this.Kind;
				if (kind == TypeKind.Class && !this.IsStatic && !unresolvedMembers.Any(m => m.EntityType == EntityType.Constructor && !m.IsStatic)
				    || kind == TypeKind.Enum || kind == TypeKind.Struct)
				{
					contextPerMember.Add(parts[0].CreateResolveContext(parentContext).WithCurrentTypeDefinition(this));
					unresolvedMembers.Add(DefaultUnresolvedMethod.CreateDefaultConstructor(parts[0]));
				}
			}
			this.Attributes = new ProjectedListWithContextPerElement<ITypeResolveContext, IUnresolvedAttribute, IAttribute>(contextPerAttribute, unresolvedAttributes, (c, a) => a.CreateResolvedAttribute(c));
			this.resolvedMembers = new ProjectedListWithContextPerElement<ITypeResolveContext, IUnresolvedMember, IMember>(contextPerMember, unresolvedMembers, (c, m) => m.CreateResolved(c));
		}
		
		public IList<ITypeParameter> TypeParameters { get; private set; }
		public IList<IAttribute> Attributes { get; private set; }
		
		public IList<IUnresolvedTypeDefinition> Parts {
			get { return parts; }
		}
		
		public EntityType EntityType {
			get { return parts[0].EntityType; }
		}
		
		public virtual TypeKind Kind {
			get { return parts[0].Kind; }
		}
		
		IList<ITypeDefinition> nestedTypes;
		
		public IList<ITypeDefinition> NestedTypes {
			get {
				IList<ITypeDefinition> result = this.nestedTypes;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					result = (
						from part in parts
						from nestedTypeRef in part.NestedTypes
						group nestedTypeRef by nestedTypeRef.Name into g
						select new DefaultResolvedTypeDefinition(new SimpleTypeResolveContext(this), g.ToArray())
					).ToList<ITypeDefinition>().AsReadOnly();
					return LazyInit.GetOrSet(ref this.nestedTypes, result);
				}
			}
		}
		
		#region Members
		public IList<IMember> Members {
			get { return resolvedMembers; }
		}
		
		public IEnumerable<IField> Fields {
			get {
				for (int i = 0; i < unresolvedMembers.Count; i++) {
					if (unresolvedMembers[i].EntityType == EntityType.Field)
						yield return (IField)resolvedMembers[i];
				}
			}
		}
		
		public IEnumerable<IMethod> Methods {
			get {
				for (int i = 0; i < unresolvedMembers.Count; i++) {
					if (unresolvedMembers[i] is IUnresolvedMethod)
						yield return (IMethod)resolvedMembers[i];
				}
			}
		}
		
		public IEnumerable<IProperty> Properties {
			get {
				for (int i = 0; i < unresolvedMembers.Count; i++) {
					switch (unresolvedMembers[i].EntityType) {
						case EntityType.Property:
						case EntityType.Indexer:
							yield return (IProperty)resolvedMembers[i];
							break;
					}
				}
			}
		}
		
		public IEnumerable<IEvent> Events {
			get {
				for (int i = 0; i < unresolvedMembers.Count; i++) {
					if (unresolvedMembers[i].EntityType == EntityType.Event)
						yield return (IEvent)resolvedMembers[i];
				}
			}
		}
		#endregion
		
		volatile KnownTypeCode knownTypeCode = (KnownTypeCode)(-1);
		
		public KnownTypeCode KnownTypeCode {
			get {
				KnownTypeCode result = this.knownTypeCode;
				if (result == (KnownTypeCode)(-1)) {
					result = KnownTypeCode.None;
					for (int i = 0; i < KnownTypeReference.KnownTypeCodeCount; i++) {
						KnownTypeReference r = KnownTypeReference.Get((KnownTypeCode)i);
						if (r != null && r.Resolve(parentContext) == this) {
							result = (KnownTypeCode)i;
							break;
						}
					}
					this.knownTypeCode = result;
				}
				return result;
			}
		}
		
		volatile IType enumUnderlyingType;
		
		public IType EnumUnderlyingType {
			get {
				IType result = this.enumUnderlyingType;
				if (result == null) {
					if (this.Kind == TypeKind.Enum) {
						result = CalculateEnumUnderlyingType();
					} else {
						result = SpecialType.UnknownType;
					}
					this.enumUnderlyingType = result;
				}
				return result;
			}
		}
		
		IType CalculateEnumUnderlyingType()
		{
			foreach (var part in parts) {
				var context = part.CreateResolveContext(parentContext).WithCurrentTypeDefinition(this);
				foreach (var baseTypeRef in part.BaseTypes) {
					IType type = baseTypeRef.Resolve(context);
					if (type.Kind != TypeKind.Unknown)
						return type;
				}
			}
			return this.Compilation.FindType(KnownTypeCode.Int32);
		}
		
		volatile byte hasExtensionMethods; // 0 = unknown, 1 = true, 2 = false
		
		public bool HasExtensionMethods {
			get {
				byte val = this.hasExtensionMethods;
				if (val == 0) {
					if (CalculateHasExtensionMethods())
						val = 1;
					else
						val = 2;
					this.hasExtensionMethods = val;
				}
				return val == 1;
			}
		}
		
		bool CalculateHasExtensionMethods()
		{
			bool noExtensionMethods = true;
			foreach (var part in parts) {
				// Return true if any part has extension methods
				if (part.HasExtensionMethods == true)
					return true;
				if (part.HasExtensionMethods == null)
					noExtensionMethods = false;
			}
			// Return false if all parts are known to have no extension methods
			if (noExtensionMethods)
				return false;
			// If unsure, look at the resolved methods.
			return Methods.Any(m => m.IsExtensionMethod);
		}
		
		public bool? IsReferenceType {
			get {
				switch (this.Kind) {
					case TypeKind.Class:
					case TypeKind.Interface:
					case TypeKind.Module:
					case TypeKind.Delegate:
						return true;
					case TypeKind.Struct:
					case TypeKind.Enum:
					case TypeKind.Void:
						return false;
					default:
						throw new InvalidOperationException("Invalid value for TypeKind");
				}
			}
		}
		
		public int TypeParameterCount {
			get { return this.TypeParameters.Count; }
		}
		
		#region DirectBaseTypes
		IList<IType> directBaseTypes;
		
		public IEnumerable<IType> DirectBaseTypes {
			get {
				IList<IType> result = this.directBaseTypes;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					result = CalculateDirectBaseTypes();
					return LazyInit.GetOrSet(ref this.directBaseTypes, result);
				}
			}
		}
		
		IList<IType> CalculateDirectBaseTypes()
		{
			List<IType> result = new List<IType>();
			bool hasNonInterface = false;
			if (this.Kind != TypeKind.Enum) {
				foreach (var part in parts) {
					var context = part.CreateResolveContext(parentContext).WithCurrentTypeDefinition(this);
					foreach (var baseTypeRef in part.BaseTypes) {
						IType baseType = baseTypeRef.Resolve(context);
						if (!(baseType.Kind == TypeKind.Unknown || result.Contains(baseType))) {
							result.Add(baseType);
							if (baseType.Kind != TypeKind.Interface)
								hasNonInterface = true;
						}
					}
				}
			}
			if (!hasNonInterface && !(this.Name == "Object" && this.Namespace == "System" && this.TypeParameterCount == 0)) {
				KnownTypeCode primitiveBaseType;
				switch (this.Kind) {
					case TypeKind.Enum:
						primitiveBaseType = KnownTypeCode.Enum;
						break;
					case TypeKind.Struct:
					case TypeKind.Void:
						primitiveBaseType = KnownTypeCode.ValueType;
						break;
					case TypeKind.Delegate:
						primitiveBaseType = KnownTypeCode.Delegate;
						break;
					default:
						primitiveBaseType = KnownTypeCode.Object;
						break;
				}
				IType t = parentContext.Compilation.FindType(primitiveBaseType);
				if (t.Kind != TypeKind.Unknown)
					result.Add(t);
			}
			return result;
		}
		#endregion
		
		public string FullName {
			get { return parts[0].FullName; }
		}
		
		public string Name {
			get { return parts[0].Name; }
		}
		
		public string ReflectionName {
			get { return parts[0].ReflectionName; }
		}
		
		public string Namespace {
			get { return parts[0].Namespace; }
		}
		
		public DomRegion Region {
			get { return parts[0].Region; }
		}
		
		public DomRegion BodyRegion {
			get { return parts[0].BodyRegion; }
		}
		
		public ITypeDefinition DeclaringTypeDefinition {
			get { return parentContext.CurrentTypeDefinition; }
		}
		
		public IType DeclaringType {
			get { return parentContext.CurrentTypeDefinition; }
		}
		
		public IAssembly ParentAssembly {
			get { return parentContext.CurrentAssembly; }
		}
		
		public virtual string Documentation {
			get {
				IDocumentationProvider provider = AbstractResolvedEntity.FindDocumentation(parentContext);
				if (provider != null)
					return provider.GetDocumentation(this);
				else
					return null;
			}
		}
		
		public ICompilation Compilation {
			get { return parentContext.Compilation; }
		}
		
		#region Modifiers
		public bool IsStatic    { get { return isAbstract && isSealed; } }
		public bool IsAbstract  { get { return isAbstract; } }
		public bool IsSealed    { get { return isSealed; } }
		public bool IsShadowing { get { return isShadowing; } }
		public bool IsSynthetic { get { return isSynthetic; } }
		
		public Accessibility Accessibility {
			get { return accessibility; }
		}
		
		bool IHasAccessibility.IsPrivate {
			get { return accessibility == Accessibility.Private; }
		}
		
		bool IHasAccessibility.IsPublic {
			get { return accessibility == Accessibility.Public; }
		}
		
		bool IHasAccessibility.IsProtected {
			get { return accessibility == Accessibility.Protected; }
		}
		
		bool IHasAccessibility.IsInternal {
			get { return accessibility == Accessibility.Internal; }
		}
		
		bool IHasAccessibility.IsProtectedOrInternal {
			get { return accessibility == Accessibility.ProtectedOrInternal; }
		}
		
		bool IHasAccessibility.IsProtectedAndInternal {
			get { return accessibility == Accessibility.ProtectedAndInternal; }
		}
		#endregion
		
		ITypeDefinition IType.GetDefinition()
		{
			return this;
		}
		
		IType IType.AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeDefinition(this);
		}
		
		IType IType.VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
		
		public ITypeReference ToTypeReference()
		{
			ITypeDefinition declTypeDef = this.DeclaringTypeDefinition;
			if (declTypeDef != null) {
				return new NestedTypeReference(declTypeDef.ToTypeReference(), this.Name, this.TypeParameterCount - declTypeDef.TypeParameterCount);
			} else {
				IAssembly asm = this.ParentAssembly;
				IAssemblyReference asmRef;
				if (asm != null)
					asmRef = new DefaultAssemblyReference(asm.AssemblyName);
				else
					asmRef = null;
				return new GetClassTypeReference(asmRef, this.Namespace, this.Name, this.TypeParameterCount);
			}
		}
		
		public IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			const GetMemberOptions opt = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;
			if ((options & opt) == opt) {
				if (filter == null)
					return this.NestedTypes;
				else
					return GetNestedTypesImpl(filter);
			} else {
				return GetMembersHelper.GetNestedTypes(this, filter, options);
			}
		}
		
		IEnumerable<IType> GetNestedTypesImpl(Predicate<ITypeDefinition> filter)
		{
			foreach (var nestedType in this.NestedTypes) {
				if (filter(nestedType))
					yield return nestedType;
			}
		}
		
		public IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return GetMembersHelper.GetNestedTypes(this, typeArguments, filter, options);
		}
		
		#region GetMembers()
		IEnumerable<TResolved> GetFilteredMembers<TUnresolved, TResolved>(Predicate<TUnresolved> filter) where TUnresolved : class, IUnresolvedMember where TResolved : class, IMember
		{
			for (int i = 0; i < unresolvedMembers.Count; i++) {
				TUnresolved unresolved = unresolvedMembers[i] as TUnresolved;
				if (unresolved != null && (filter == null || filter(unresolved))) {
					yield return (TResolved)resolvedMembers[i];
				}
			}
		}
		
		public virtual IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFilteredMembers<IUnresolvedMethod, IMethod>(Utils.ExtensionMethods.And(m => !m.IsConstructor, filter));
			} else {
				return GetMembersHelper.GetMethods(this, filter, options);
			}
		}
		
		public virtual IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return GetMembersHelper.GetMethods(this, typeArguments, filter, options);
		}
		
		public virtual IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFilteredMembers<IUnresolvedMethod, IMethod>(Utils.ExtensionMethods.And(m => m.IsConstructor && !m.IsStatic, filter));
			} else {
				return GetMembersHelper.GetConstructors(this, filter, options);
			}
		}
		
		public virtual IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFilteredMembers<IUnresolvedProperty, IProperty>(filter);
			} else {
				return GetMembersHelper.GetProperties(this, filter, options);
			}
		}
		
		public virtual IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFilteredMembers<IUnresolvedField, IField>(filter);
			} else {
				return GetMembersHelper.GetFields(this, filter, options);
			}
		}
		
		public virtual IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFilteredMembers<IUnresolvedEvent, IEvent>(filter);
			} else {
				return GetMembersHelper.GetEvents(this, filter, options);
			}
		}
		
		public virtual IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFilteredMembers<IUnresolvedMember, IMember>(filter);
			} else {
				return GetMembersHelper.GetMembers(this, filter, options);
			}
		}
		#endregion
		
		public bool Equals(IType other)
		{
			return this == other;
		}
		
		public override string ToString()
		{
			return this.ReflectionName;
		}
	}
}
